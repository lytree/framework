# 发布链路三个高风险项处置说明

日期：2026-11-07
范围：仅发布链路（空包、Push 失败语义、PackageId 一致性、SDK 版本耦合）。
未触碰：DynamicApi 分析器、`src/Shared` 死代码、README 文档。

## 一、`Framework.Logging` 空包

### 核实结论

| 项 | 事实 |
|----|------|
| 源码 | `src/Framework.Logging/` 下 **0 个 `.cs` 文件**，仅有 `Framework.Logging.csproj`、`README.md`、`NLog.config` |
| csproj | 目标框架 `net10.0`；`<Version>2.0.0-alpha.1</Version>`；`<PackageId>YLFramework.Logging</PackageId>`；仅 3 个 NLog `PackageReference`；`GenerateDocumentationFile=true` |
| 打包产物 | 只生成空程序集 `lib/net10.0/Framework.Logging.dll`（无任何类型），nupkg 实际内容为 README + NLog 依赖声明 |
| 解决方案 | `Framework.slnx` 第 14 行包含该项目 |
| 代码引用 | 无任何项目 / 测试引用它；仅解决方案、根 `Readme.md`、`AGENTS.md`、Charts 的 README 提及 |
| nuget.org | **已发布且有多版本**：`0.0.1-alpha.1`、`0.0.1`、`1.0.2-alpha.7`、`1.0.3-alpha.1~6`、`2.0.0-alpha.1`、`3.0.0-alpha.1` |
| README 声称的 API | `AddFrameworkNLog()` 在源码中不存在（`AddNLogWeb()` 来自 NLog 包） |

### 关键发现：打包范围不是由 slnx 决定的

`build/Build.cs` 的 `ResolveProjects()` 使用文件系统 glob `src/**/*.csproj`，不读取 `Framework.slnx`。
因此**只把项目从 slnx 摘除并不能阻止它被 `-t Pack` 打包**；必须同时处理 csproj 或文件本身。

### 处置选项（历史备选，最终采用 A）

| 选项 | 做法 | 风险 | 说明 |
|------|------|------|------|
| A. 停止发布（推荐） | 在 `Framework.Logging.csproj` 增加 `<IsPackable>false</IsPackable>`，项目仍留在 slnx | 最低：一行、可逆；nuget 上既有版本不受影响，只是不再产生新版本 | 不影响 `dotnet build Framework.slnx`；`ResolveProjects` 仍会列出它，但 `dotnet pack` 对 `IsPackable=false` 的项目成功且不产出 nupkg |
| B. 从解决方案摘除 | slnx 移除 + 同时设置 `IsPackable=false`（或删除/迁走目录） | 中：仅改 slnx 无效（见上）；删除目录会丢失 NLog.config 参考价值 | 需要两步，且对本地 `dotnet build Framework.slnx` 的覆盖面有变化 |
| C. 明确弃用包 | 新增一个 `[Obsolete]` 标记类型 + 发一个最终弃用版本 + README 更新 | 高：需要写代码与再次发布；README 本轮明确不碰；且 `Obsolete` 只能加在类型上，会改变「空包」现状 | 更适合需要向用户显式提示迁移的场景 |

**建议**：选 A（`IsPackable=false`）。依据：包已在 nuget 上有多版本，删除/摘除都改变发布范围与仓库结构；A 不破坏既有消费者，也不需要新增代码或改 README，风险最小且完全可逆。

### 处置（已落地：方案 A）

用户已拍板采用 **A**，已在 `src/Framework.Logging/Framework.Logging.csproj` 的 `PropertyGroup` 中增加：

```xml
<IsPackable>false</IsPackable>
```

未采用 B（不摘除工程、不改 `Framework.slnx`），未采用 C（不新增 `[Obsolete]` 类型、不发最终弃用版本）。

落地后的实际影响：

| 关注点 | 结论 |
|--------|------|
| 全量 Pack | `ResolveProjects()` 仍枚举到 12 个 csproj（含 `Framework.Logging`），但 `dotnet pack` 对 `IsPackable=false` 项目直接跳过、退出码 0；产物目录 `nugets/` 只产生 10 个 `*.nupkg`（另有 2 个既有 `IsPackable=false` 的 `Framework.DynamicApi.Generator` 同样不产包） |
| 其他包 | 不受影响，10 个可打包项目全部正常产出 nupkg |
| nuget 历史版本 | 不受影响；本轮没有任何删除/下架动作，只是不再产生新版本 |
| 是否会被误推 | 不会。`Push` 只扫描 `nugets/*.nupkg` 实际文件，且 `Push` 依赖 `Pack` → `Compile` → `Restore` → `Clean`，每次先清空 `nugets/`，不存在历史 Logging nupkg 残留被误推的路径 |
| 仍会「提及」它的位置 | ① `build/Build.cs` 的 `ResolveProjects()`（仅枚举，不会产出包）；② 根 `Readme.md` 的包清单第 19 行（纯文档，不参与打包/推送）。本轮按约定不碰 README，该行后续可单独更新 |

结论：**不需要修改 `build/Build.cs` 或 CI 全量发布逻辑**，`IsPackable=false` 已足以阻断该包的产出与推送。

## 二、`Push` 缺 API Key 时静默成功

### 核实结论

`build/Build.cs` 原逻辑在 `NuGetApiKey` 为空时执行 `context.Log.Error(...)` 后直接 `return`，任务链仍以退出码 0 结束，CI 会误判成功。

其他跳过点核对：

| 位置 | 行为 | 结论 |
|------|------|------|
| `SkipDuplicate = true` | 包已存在时跳过上传 | **预期跳过**，保持幂等重推，不算配置错误 |
| `packages.Count == 0` | 抛 `CakeException` | 已经是显式失败 |
| 无标签时跳过推送 | `Build.cs` 内无此逻辑；由 CI 的 tag 触发条件决定 | 属于**预期跳过**（工作流不触发），不是静默成功 |

### 处置（已落地）

- 缺少 `NUGET_API_KEY` 时改为抛 `CakeException`，消息说明：CI 中该值由 `NuGet/login`(OIDC) 换取，本地只打包请执行 `-t Pack`。
- `Pack` 任务与 `Default` 任务链不依赖 `Push`，因此本地 `dotnet run build/Build.cs`（默认打包）与 `-t Pack` 不受影响。
- CI 正常路径由 `NuGet/login` 注入临时 Key，行为不变。

### 验证方式与结果（未向 nuget.org 推送）

验证原则：不触碰真实远端。具体做法：

1. 先在当前 PowerShell 会话显式 `Remove-Item Env:NUGET_API_KEY`，并打印 `KEY_SET=False` 确认环境变量确实不存在。
2. **Push 失败路径**：执行
   `dotnet run build/Build.cs -t Push --Package=YLFramework.Logging --NugetSource=http://127.0.0.1:1/nonexistent`
   - `--NugetSource` 指向本机不可达端口，即使走到推送步骤也不会命中 nuget.org；
   - `--Package=YLFramework.Logging` 让 Compile/Pack 只处理该工程，缩短链路；
   - 任务链为 Clean → Restore → Compile → Pack → Push，前四个 `Succeeded`，`Push` 为 `Failed`。
3. **结果**：
   - 进程退出码 `-1`（非 0），CI 会判定失败；
   - 输出末尾可读错误：
     `Error: 缺少环境变量 NUGET_API_KEY，无法推送到 http://127.0.0.1:1/nonexistent。CI 中该值由 NuGet/login(OIDC) 换取；本地只打包请执行 -t Pack，不要执行 -t Push。`
   - 抛错发生在 `PushTask.Run` 的第一段 Key 检查，**早于** nupkg 扫描与 `DotNetNuGetPush` 调用，因此没有也不可能发起任何网络推送。

## 三、Charts 的 PackageId 不一致

### 核实结论

| 项 | 事实 |
|----|------|
| 工程 / 目录 | `Framework.Charts` |
| PackageId | `YLFramework.Plot` |
| CI 触发标签 | `'YLFramework.*/v*'`（另有全量 `'[Vv]*'`） |
| CI 单包标识 | 标签 `YLFramework.Plot/vX.Y.Z` → `--Package=YLFramework.Plot`；`Framework.Charts/...` **不会触发工作流** |
| 打包匹配 | `ResolveProjects()` 同时按「项目名」或「csproj 的 PackageId」匹配，所以 `--Package=Framework.Charts` 与 `--Package=YLFramework.Plot` 都能打包 |
| 推送过滤 | 原实现按用户传入的 `context.Package + "."` 过滤 nupkg 文件名；nupkg 实际是 `YLFramework.Plot.2.0.0-alpha.1.nupkg`，故 `--Package=Framework.Charts` 匹配为 0 并抛错 |

### 处置（已落地，保持包身份不变）

- 在 `BuildContext` 新增 `ResolvePackageId()`：解析目标项目后，优先取 csproj 的 `<PackageId>`，未声明时退回项目名。
- `Push` 改为用 `ResolvePackageId()` 的前缀过滤 nupkg，而不是用 `--Package` 字符串。
- `PackageId` 保持 `YLFramework.Plot`，nuget 上既有包身份与消费者不受影响。

影响面：`--Package=Framework.Charts` 现在能正确推送 `YLFramework.Plot.*.nupkg`；`--Package=YLFramework.Plot` 行为不变；全量发布不受影响。

**待拍板**：若希望 CI 支持 `Framework.Charts/vX.Y.Z` 形式的标签，需要在 `public.yml` 的 `push.tags` 增加 `'Framework.*/v*'` 触发模式（会同时覆盖其他项目名标签）。本轮未扩大触发范围。

## 四、SDK 版本耦合（`global.json` 10.0.400 vs CI 10.0.x）

### 核实结论

- `global.json`：`sdk.version = 10.0.400`，`rollForward = latestFeature`，`allowPrerelease = false`，并含 `test.runner = Microsoft.Testing.Platform`。
- CI 原使用 `actions/setup-dotnet@v4` 的 `dotnet-version: "10.0.x"`，安装 .NET 10 最新 SDK。
- `latestFeature` 允许向上滚动到更高的 10.0.x feature band，因此 CI 安装 10.0.4xx/10.0.5xx 都能解析；只有安装低于 `10.0.400` 的 feature band 才会失败，而 `10.0.x` 总是取最新版，故**当前不会造成硬失败**。
- 真实风险是**版本漂移**（本地钉 10.0.400、CI 可能用更高版本）与 **allowPrerelease=false**（若 CI 某天装到 10.0.x 预览版会失败），不是 CI/本地不一致的确定性失败。

### 处置（已落地，最小改动）

- CI 的 setup-dotnet 改为 `global-json-file: global.json`，直接读取仓库的 SDK 版本，保证 CI 与本地使用同一份 SDK 决策来源。
- 未改 `rollForward`：`latestFeature` 保留，避免把版本钉死导致本地/CI 缺少某个 feature band 时无法构建。

### 对 MTP 的影响

无。`test.runner = Microsoft.Testing.Platform` 仍由同一份 `global.json` 读取；`dotnet test` 的 MTP opt-in 逻辑不受 setup-dotnet 的版本来源影响。

## 五、验证

| 项 | 结果 |
|----|------|
| `dotnet build Framework.slnx` | **0 error**；仅既有警告（NU1510、Nullable、XML 注释等），改动 `IsPackable` 后重新验证仍为 0 error |
| `dotnet run build/Build.cs -t Pack`（无 `NUGET_API_KEY`） | **成功**，退出码 0；`nugets/` 产出 10 个 `*.nupkg`，**无 `YLFramework.Logging.*.nupkg`**；日志显示「待打包项目 12 个」并包含 `Framework.Logging`，但该工程被 SDK 跳过、不产包 |
| `dotnet run build/Build.cs -t Push`（无 `NUGET_API_KEY`，源码指向 `127.0.0.1:1`） | **明确失败**，退出码 `-1`（非 0），错误信息可读；`Push` 状态 `Failed`，前序 `Pack` 正常 |
| 单元测试 | 本轮只改 `Framework.Logging.csproj`，无被测代码变更，未运行测试 |
| git / 真实推送 | 未提交、未向 nuget.org 推送；`-t Push` 只到 Key 校验点即抛错 |

## 六、待用户拍板清单

1. ~~`Framework.Logging` 处置~~：已拍板采用 A（`IsPackable=false`）并落地，见第一节。
2. Charts 标签：是否需要在 CI 增加 `'Framework.*/v*'` 触发模式，以接受 `Framework.Charts/vX.Y.Z` 形式的标签（当前规范标签仍为 `YLFramework.Plot/vX.Y.Z`）。
3. 文档同步（可选）：根 `Readme.md` 包清单仍列有 `YLFramework.Logging`，与「停止发新版本」的现状不一致；按本轮约定未改 README，是否需要单独更新由用户决定。
