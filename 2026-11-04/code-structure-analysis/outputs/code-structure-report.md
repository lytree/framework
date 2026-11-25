# YLFramework 单仓代码结构分析报告

- 分析对象：`f:\Code\Dotnet\framework`
- 分析日期：2026-11-04
- 分析方式：只读静态分析。所有结论均基于实际读取的 `.cs` / `.csproj` / `Build.cs` / `public.yml` / 已产出的 `.nupkg` 内容，以及 PowerShell 统计（排除 `bin/`、`obj/`）。
- 统计口径：文件数 = `.cs` 文件数；行数 = 物理行（`Measure-Object -Line`）；用例数 = `[Test]` / `[Test(` 特性出现次数（TUnit 数据驱动用例的实际执行数可能更多）。

---

## 1. 解决方案与目录布局

### 1.1 整体结构树

```
f:\Code\Dotnet\framework
├── Framework.slnx                     # 11 个 src 工程 + 5 个 test 工程 + 6 个文档/配置文件
├── Directory.Packages.props           # CPM：29 条 PackageVersion
├── global.json                        # SDK 10.0.400 + MTP runner
├── NuGet.config                       # <clear/> + nuget.org
├── AGENTS.md / Readme.md              # 仓库说明（与代码存在漂移，见 §7.2 L1）
├── build/
│   ├── Build.cs                       # Cake Frosting 任务链
│   └── Directory.Packages.props       # 仅关闭 build/ 目录的 CPM（避免 NU1008）
├── src/
│   ├── Directory.Build.props          # 仅 src 继承：Deterministic/Symbols/NoWarn
│   ├── Shared/                        # 73 个 .cs 共享源码，无 csproj、无 namespace
│   ├── Framework.AspNetCore/          # 17 files / 944 lines
│   ├── Framework.AspNetCore.Middleware/ # 64 files / 3870 lines
│   ├── Framework.Charts/              # 3 files / 201 lines（PackageId=YLFramework.Plot）
│   ├── Framework.DynamicApi/          # 13 files / 731 lines
│   ├── Framework.DynamicApi.Generator/ # 7 files / 987 lines（netstandard2.1，不可打包）
│   ├── Framework.Logging/             # 0 个 .cs（仅 csproj + NLog.config + README）
│   ├── Framework.Modbus/              # 39 files / 5274 lines
│   ├── Framework.Mvvm/                # 3 files / 76 lines
│   ├── Framework.Repository/          # 33 files / 1164 lines
│   ├── Framework.SlideCaptcha/        # 34 files / 1664 lines
│   └── Framework.ZLogging/            # 7 files / 2422 lines
├── test/                              # 5 个 TUnit/MTP 测试工程
│   ├── Framework.Tests/               # 21 cases（直接编译 Shared 源码副本）
│   ├── Framework.Modbus.Tests/        # 82 cases
│   ├── Framework.ZLogging.Tests/      # 63 cases
│   ├── Framework.DynamicApi.Tests/    # 19 cases
│   └── Framework.DynamicApi.Generator.Tests/ # 23 cases
└── nugets/                            # 20 个历史产物（均为 2.0.0-alpha.1）
```

### 1.2 解决方案内容

[Framework.slnx](file:///f:/Code/Dotnet/framework/Framework.slnx) 包含：

| 分类 | 数量 | 内容 |
|---|---|---|
| `/src/` 工程 | 11 | 10 个可打包包 + 1 个 `Framework.DynamicApi.Generator`（`IsPackable=false`） |
| `/test/` 工程 | 5 | Framework.Tests、Modbus.Tests、ZLogging.Tests、DynamicApi.Tests、DynamicApi.Generator.Tests |
| `/document/` 文件 | 6 | `.gitattributes`、`.gitignore`、`Directory.Packages.props`、`src/Directory.Build.props`、`NuGet.config`、`Readme.md` |
| `/src/Shared/` 文件 | 2 | 仅 `README.md`、`MODULES.md`（共享源码没有被 solution 收录为工程） |

根目录没有 `Directory.Build.props` / `Directory.Build.targets`。全仓库构建属性只存在于 [src/Directory.Build.props](file:///f:/Code/Dotnet/framework/src/Directory.Build.props)，因此 `test/` 工程不继承该文件的 `NoWarn`、`Deterministic`、`IncludeSymbols` 等设置。

`build/Build.cs` 使用文件式 C#（`#:package Cake.Frosting@*`）；为避免根目录 CPM 与脚本自带包版本冲突，[build/Directory.Packages.props](file:///f:/Code/Dotnet/framework/build/Directory.Packages.props) 只做一件事：把 `ManagePackageVersionsCentrally` 设为 `false`。

---

## 2. 各 src 包职责与规模

| 目录 | PackageId / Version | 文件/行数 | public 类型数 | 定位与主要 API 面 | 外部依赖 |
|---|---|---:|---:|---|---|
| `Framework.AspNetCore` | `YLFramework.AspNetCore` / 2.0.0-alpha.1 | 17 / 944 | 16 | Web API 基础层：`ApiResponse<T>`/`ApiResult`/`ApiExceptionMiddleware`；`IBaseRouter` + `AddIoC`/`AddRouters` 模块化路由；`AddSocketConnectionFactory`；`HttpContext`/`HttpRequest` 扩展；`IKestrelMiddleware`/`ApplicationBuilder` 连接管道 | `Microsoft.AspNetCore.App` FrameworkReference、BouncyCastle.Cryptography |
| `Framework.AspNetCore.Middleware` | `YLFramework.AspNetCore.Middleware` / 2.0.0-alpha.1 | 64 / 3870 | 47 | Kestrel 连接中间件集合：Echo、FlowAnalyze（流量统计）、FlowXor、HttpProxy（基于 YARP）、**Modbus 从站**（27 files / 2305 lines）、Telnet/TelnetProxy、TlsDetection。入口为 8 个同名 `ListenOptionsExtensions` 分部类（均在 `Microsoft.AspNetCore.Hosting` 命名空间） | BouncyCastle、Yarp.ReverseProxy；ProjectReference → AspNetCore |
| `Framework.ZLogging` | `YLFramework.ZLogging` / 2.0.0-alpha.1 | 7 / 2422 | 16 | 自研 `ILoggerProvider`：Spectre.Console 彩色控制台 + 异步文件写入（按体积滚动、重连、重试）；入口 `AddSpectreConsole` / `AddSpectreFile` / `AddSpectreConsoleAndFile` / `AddSpectre`；`SpectreLogOptions`、`SpectreObjectPrinter` | Microsoft.Extensions.Logging、Spectre.Console、Spectre.Console.Ansi |
| `Framework.Logging` | `YLFramework.Logging` / 2.0.0-alpha.1 | **0 / 0** | **0** | 仅 csproj + `NLog.config` + README，**没有任何 C# 源码**；已产出的 nupkg 中只有空程序集 `lib/net10.0/Framework.Logging.dll` | NLog、NLog.Extensions.Logging、NLog.Web.AspNetCore |
| `Framework.Mvvm` | `YLFramework.Mvvm` / 2.0.0-alpha.1 | 3 / 76 | 4 | 窗口尺寸/加载状态消息：`HeightChange`、`WidthChange`、`WindowsLoading`、静态可变 `WindowsVariable` | CommunityToolkit.Mvvm |
| `Framework.Modbus` | `YLFramework.Modbus` / 2.0.0-alpha.1 | 39 / 5274 | 111 | Modbus 主站（jamod 风格）：TCP/UDP/RTU/ASCII 传输、完整功能码消息、`IModbusMaster` 门面、`ModbusMasterFactory`、`Locator`/`ReadReference`/`WriteReference` | System.IO.Ports |
| `Framework.Charts` | **`YLFramework.Plot`** / 2.0.0-alpha.1 | 3 / 201 | 2 | ScottPlot 静态工厂 `Plots`（`Plots.cs` + `Plots.Default.cs` 两个分部）+ `FixedNumericManual` 刻度生成器；导出 PNG/SVG/流 | ScottPlot（间接使用 SkiaSharp） |
| `Framework.Repository` | `YLFramework.Repository` / 2.0.0-alpha.1 | 33 / 1164 | 35 | linq2db 仓储基类 `RepositoryBase<TEntity,TKey>`/`IRepositoryBase`、实体基类与软删除/租户/版本接口、雪花 ID/事务/属性、Mapster 映射、`FileHelper` | linq2db、Mapster、System.Text.Json、Yitter.IdGenerator、BouncyCastle |
| `Framework.DynamicApi` | `YLFramework.DynamicApi` / **3.0.0-alpha.1** | 13 / 731 | 12 | 编译期动态 API 的运行时契约：`[DynamicApi]` 等特性、`DynamicApiOptions`、`DynamicApiRouteResolver`、`FormatResultFilter`、`DynamicApiBodyReader/ResponseWriter`、`ResponseResul<T>` | ASP.NET Core FrameworkReference；Analyzer ProjectReference → Generator |
| `Framework.DynamicApi.Generator` | 无（`IsPackable=false`） | 7 / 987 | 2 | 增量源生成器 `IIncrementalGenerator`：扫描 `[DynamicApi]` 类、生成端点注册代码与诊断、聚合多模块入口（`DynamicApiModules` MSBuild 属性） | Microsoft.CodeAnalysis.CSharp（PrivateAssets=all） |
| `Framework.SlideCaptcha` | `YLFramework.SlideCaptcha` / 2.0.0-alpha.1 | 34 / 1664 | 36 | 滑块验证码：`CaptchaBuilder`、`ICaptcha`/`DefaultCaptcha`、图片生成（SkiaSharp）、资源管理（内嵌/文件/Options）、存储、轨迹校验（Basic/Simple） | 7 个 Microsoft.Extensions.*、SkiaSharp、SkiaSharp.NativeAssets.Linux.NoDependencies |

合计：**220 个 .cs 文件 / 17,333 行**（不含 `src/Shared` 的 73 文件 / 12,281 行）。

规模特征明显不均：`Modbus`（5,274 行）、`Middleware`（3,870 行）、`ZLogging`（2,422 行）三个包占生产代码约 2/3；`Mvvm`（76 行）与 `Logging`（0 行）更接近"占位包"。

---

## 3. `src/Shared` 共享源码内联机制的实际使用情况

`src/Shared` 不是工程，没有 csproj，也不产出程序集。各包在 csproj 内用 `<Compile Include>` 直接引入源码编译。实测结果：

- Shared 共 **73 个 `.cs` 文件 / 12,281 行**。
- 被至少一个项目引用（去重）：**38 个（52%）**。
- 从未被引用：**35 个（48%）**，主要集中在 `Proxy/`（15）、`Helpers/`（15）、`Mime/`、`Net/`、`System.Xml/`、`Utils/`、`UnsupportedException.cs`。

### 3.1 各包引入明细

| 包 | 引入的 Shared 内容 |
|---|---|
| `Framework.AspNetCore` | `Helpers/Helper.Json.cs`；`System.Text.Json.Serialization/DateTimeJsonConverter.cs`、`DateTimeOffsetJsonConverter.cs` |
| `Framework.AspNetCore.Middleware` | `System.Buffers/*`（8 个）；`System.IO/*`（4 个）；`System.IO.Pipelines/DelegatingDuplexPipe.cs`；`System.Threading/TaskExtensions.cs`、`TaskToApm.cs`（共 15 个） |
| `Framework.Modbus` | `System.Buffers/PooledByteBuf.cs`（1 个） |
| `Framework.Repository` | 5 个 `Helpers/*`（Entity、Interface、Json、Encoding、DateTime）；`System/*`（4 个）；`System.Reflection/*`（2 个）；`System.Collections.Generic/*`（9 个）；`System.Convert/*`（1 个）；`System.Text.Json.Serialization/*`（2 个）（共 23 个） |
| `test/Framework.Tests` | `System.Buffers/ArrayOwner.cs`、`IArrayOwner.cs`、`System.ArrayPool.cs`、`PooledByteBuf.cs`；`System.IO/PooledMemoryStream.cs`（共 5 个） |

### 3.2 同一份源码被多个包编译（重复）

| Shared 文件 | 编译进 |
|---|---|
| `src/Shared/Helpers/Helper.Json.cs` | `Framework.AspNetCore`、`Framework.Repository` |
| `src/Shared/System.Buffers/PooledByteBuf.cs` | `Framework.AspNetCore.Middleware`、`Framework.Modbus` |
| `src/Shared/System.Text.Json.Serialization/DateTimeJsonConverter.cs` | `Framework.AspNetCore`、`Framework.Repository` |
| `src/Shared/System.Text.Json.Serialization/DateTimeOffsetJsonConverter.cs` | `Framework.AspNetCore`、`Framework.Repository` |

即 4 个文件存在跨包二进制重复；`Framework.Repository` 与 `Framework.AspNetCore` 同时引用时会各自带一份 JSON 转换器实现。

### 3.3 CS0433 与命名冲突评估

结论：**当前没有可证实的 CS0433 风险**，依据如下：

1. 所有 Shared 源文件均不声明 `namespace`（`Grep "^namespace"` 无匹配），类型落在全局命名空间。
2. 顶层类型全部为 `internal`。`Helper.DateTime.cs` 中出现的 `public class DateTimeRange`、`public enum DateTimeRangeType/RangeMode/DateRangeType` 是 `internal static partial class Helper` 内部的**嵌套**类型（[Helper.DateTime.cs#L422-L433](file:///f:/Code/Dotnet/framework/src/Shared/Helpers/Helper.DateTime.cs#L422-L433)），外部不可见，不违反"顶层 internal"规则。
3. 扩展类按文件区分名字：`Extensions`、`ListExtensions`、`CollectionExtensions`、`EnumerableExtensions`、`StreamExtensions`、`ReflectionExtensions`、`BufferWriterExtensions` 等，避免了同程序集内 `CS0101`。
4. `Framework.AspNetCore.Middleware` 中 8 个同名 `ListenOptionsExtensions.cs` 均为 `partial class` 且共享 `Microsoft.AspNetCore.Hosting` 命名空间，属于合法分部合并，不会产生类型冲突。

残留风险不是 CS0433 本身，而是：

- 未来有人把某个 Shared 类型改为 `public` 以满足公开签名需求时，会立即在"同时引用两个包"的下游触发 CS0433；[src/Shared/README.md#L20-L22](file:///f:/Code/Dotnet/framework/src/Shared/README.md#L20-L22) 只给了文字约定，没有编译期或 CI 校验。
- 同一文件被多个包编译后，修复只改一处、但各包需重新发版才会同步；版本独立演进会放大漂移窗口。
- `Helper` 是 18 个分部文件组成的整体，任意包只引入其中一部分就可能缺成员；`MODULES.md` 要求"所有分部一起复制"，但 csproj 实际是逐文件挑选，缺少自动依赖校验。

---

## 4. 包间依赖与耦合

### 4.1 ProjectReference

全仓库仅有 **2 条** src 包间 ProjectReference：

| 来源 | 目标 | 用途 |
|---|---|---|
| `Framework.AspNetCore.Middleware` | `Framework.AspNetCore` | 复用 `ServiceCollectionExtensions` / `ApiResponse` 等基础类型 |
| `Framework.DynamicApi` | `Framework.DynamicApi.Generator` | `ReferenceOutputAssembly=false` + `OutputItemType=Analyzer`，仅开发期作为分析器 |

没有核心工具库被 ProjectReference 复用，符合"Shared 源码内联、不建中心程序集"的设计。`Framework.Modbus` 与 `Framework.AspNetCore.Middleware` 内的 Modbus 从站**没有任何引用关系**，属于两套并行实现。

### 4.2 Directory.Packages.props（CPM）实际情况

[Directory.Packages.props](file:///f:/Code/Dotnet/framework/Directory.Packages.props) 声明 **29 条** `PackageVersion`。与所有 csproj 的 `PackageReference` 交叉比对：

- 实际被引用：**26 个**。
- 声明但全仓库未使用：**3 个** —— `SharpCompress` 0.50.4、`System.IO.Pipelines` 10.0.13、`ZLogger` 2.5.10。
- 被引用但未声明：**0 个**（因此当前不存在 `NU1010` 风险）。
- csproj 内显式 `Version=` 覆盖：**0 处**（全部走 CPM；`Microsoft.CodeAnalysis.CSharp` 只带 `PrivateAssets="all"`）。

值得注意的依赖事实：

1. **BouncyCastle 是"声明了但代码未用"的依赖**：`Framework.AspNetCore`、`Framework.AspNetCore.Middleware`、`Framework.Repository` 三个 csproj 都引用它，但三个包的 `.cs` 中没有任何 `BouncyCastle` 引用；唯一使用它的 Shared 文件 `Helper.SM4Encrypt.cs` **从未被任何包引入**（见 §3）。这是纯粹的死依赖与包体积负担。
2. **Middleware 重复声明了 BouncyCastle**：它已经 ProjectReference `Framework.AspNetCore`，NuGet 依赖会传递；自身再写一条属于冗余。
3. **ZLogger 声明未用**：`Framework.ZLogging` 实际只依赖 `Microsoft.Extensions.Logging` + Spectre.Console，README 也明确写"不依赖 ZLogger"；但 CPM 仍保留 `ZLogger 2.5.10`。
4. **Charts 直接 `using SkiaSharp` 却没有直接 `PackageReference`**：`Plots.cs` 第 8 行使用 `SkiaSharp`，依赖 ScottPlot 的传递依赖；CPM 的 `CentralPackageTransitivePinningEnabled=true` 会把传递版本钉到 `4.153.1`，ScottPlot 与 SkiaSharp 版本兼容性无人为约束。
5. **Framework.Logging 引用了 3 个 NLog 包但没有任何代码**，属于最明显的无效依赖组合。

---

## 5. 测试分布

| 测试工程 | 被测对象 | 文件/行数 | `[Test]` 数 | 覆盖方式 |
|---|---|---:|---:|---|
| `Framework.Modbus.Tests` | `Framework.Modbus` | 9 / 1339 | 82 | ProjectReference；PDU 编解码、校验和、串口帧、Locator、主站集成（FakeConnection/LoopbackSlave） |
| `Framework.ZLogging.Tests` | `Framework.ZLogging` | 10 / 1031 | 63 | ProjectReference；Options、扩展方法、文件 I/O、自定义格式、异常渲染、并发、生命周期 |
| `Framework.DynamicApi.Tests` | `Framework.DynamicApi` | 4 / 203 | 19 | ProjectReference；路由解析、命名约定、结果包装、Filter |
| `Framework.DynamicApi.Generator.Tests` | Generator + DynamicApi | 4 / 480 | 23 | ProjectReference；`GeneratorDriver` 断言生成代码、诊断、模块聚合 |
| `Framework.Tests` | Shared 源码副本 | 2 / 247 | 21 | **无 ProjectReference**；直接 `<Compile Include>` 5 个 Shared 文件，只测 `PooledByteBuf` 与 `PooledMemoryStream` |
| **合计** | | **29 / 3300** | **226** | |

### 覆盖缺口

10 个可打包包中，**只有 3 个有直接对应测试**（Modbus、ZLogging、DynamicApi；Generator 属不可打包的构建期工具，由独立测试工程覆盖）。零测试的包：

- `Framework.AspNetCore`（含 `ApiExceptionMiddleware`、`ApiResponse`、`RouterExtensions` 反射扫描）
- `Framework.AspNetCore.Middleware`（含 HttpProxy、TLS 检测、Telnet、**Modbus 从站**，安全/协议敏感面最大）
- `Framework.Repository`（仓储基类、软删除、递归删除、实体反射）
- `Framework.SlideCaptcha`（验证码生成与轨迹校验）
- `Framework.Charts`（仅 201 行，但导出逻辑完全未测）
- `Framework.Mvvm`（消息类型，风险低）
- `Framework.Logging`（无代码，无法测试）

`Framework.Tests` 的性质需要单独说明：它编译的是 Shared 源码的**副本**（[Framework.Tests.csproj](file:///f:/Code/Dotnet/framework/test/Framework.Tests/Framework.Tests.csproj#L16-L24)），而不是引用某个已发布包；因此测试通过只证明该副本逻辑正确，不能证明任何包内联后的集成行为。38 个被引用的 Shared 文件中，仅 5 个被编译进测试工程、2 个有实际用例。

---

## 6. 构建与发布

### 6.1 Cake 任务链

[build/Build.cs](file:///f:/Code/Dotnet/framework/build/Build.cs) 定义：

| 任务 | 依赖 | 行为 |
|---|---|---|
| `Clean` | — | `DotNetClean ./Framework.slnx`（Release）+ 清空 `./nugets` |
| `Restore` | Clean | `DotNetRestore ./Framework.slnx` |
| `Compile` | Restore | 全量时构建 slnx；指定 `-Package` 时只构建匹配的那个 csproj（依赖链由 ProjectReference 带上） |
| `Pack` | Compile | 遍历 `ResolveProjects()`，`NoBuild=true`、`NoRestore=true`；默认用各 csproj `<Version>`；`-Version` 时用 `PackageVersion` 覆盖 |
| `Push` | Pack | 缺 `NUGET_API_KEY` 时记录错误并 `return`；否则推送 `nugets/*.nupkg`；指定包时按 `context.Package + "."` 前缀过滤；`SkipDuplicate=true` |
| `Default` | Pack | 只打包，不推送 |

版本一致性保护：当 `-Package` 与 `-Version` 同时出现时，`Pack` 会用正则读取目标 csproj 的 `<Version>`，与标签版本不一致即抛 `CakeException`（[Build.cs#L186-L195](file:///f:/Code/Dotnet/framework/build/Build.cs#L186-L195)）。

### 6.2 版本声明与 CI 标签

- 10 个可打包包全部在自身 csproj 声明 `<Version>`；其中 9 个为 `2.0.0-alpha.1`，`Framework.DynamicApi` 为 `3.0.0-alpha.1`。
- [.github/workflows/public.yml](file:///f:/Code/Dotnet/framework/.github/workflows/public.yml) 的对应关系：

| 标签 | CI 解析 | Build.cs 调用 | 版本来源 |
|---|---|---|---|
| `V2.0.0-alpha.1`（`[Vv]*`） | `package=""`，`version=""` | `dotnet run build/Build.cs -t Push` | 各 csproj `<Version>` |
| `YLFramework.Repository/v2.0.0-alpha.1`（`YLFramework.*/v*`） | `package=YLFramework.Repository`，`version=2.0.0-alpha.1` | `... -t Push --Package=... --Version=...` | 标签版本，且必须与 csproj 一致 |

CI 其余关键点：`environment: release`、`permissions.id-token: write`、`NuGet/login@v1 user: lytree` 与 nuget.org Trusted Publishing 策略一致；`setup-dotnet` 安装 `10.0.x`；作业最后上传 `nugets` 为 artifact。

### 6.3 构建/发布结构上容易出错的地方

| 编号 | 问题 | 证据 |
|---|---|---|
| B1 | **缺少 API Key 时 `Push` 仍然成功退出（exit 0），发布变成静默空操作**。CI 中 OIDC 换 Key 失败时流水线会"绿色通过"但没有发布。 | [Build.cs#L223-L228](file:///f:/Code/Dotnet/framework/build/Build.cs#L223-L228) |
| B2 | **SDK 版本耦合**：`global.json` 钉死 `10.0.400` + `rollForward=latestFeature`，CI 只装 `10.0.x`。runner 上最新 10.0.x 若低于 10.0.400，restore 直接失败。 | [global.json#L2-L6](file:///f:/Code/Dotnet/framework/global.json#L2-L6)、[public.yml#L30](file:///f:/Code/Dotnet/framework/.github/workflows/public.yml#L30) |
| B3 | **单包 Push 过滤依赖"传入名 = PackageId 前缀"**。`ResolveProjects()` 同时接受项目名与 PackageId，但 `PushTask` 只用传入字符串过滤 nupkg。例如 `--Package=Framework.Charts` 能解析到项目，却找不到 `YLFramework.Plot.*.nupkg`，直接抛"没有找到待推送的 nupkg"。 | [Build.cs#L74-L79](file:///f:/Code/Dotnet/framework/build/Build.cs#L74-L79)、[Build.cs#L234-L244](file:///f:/Code/Dotnet/framework/build/Build.cs#L234-L244) |
| B4 | **全量发布没有过滤不可打包项目**：`ResolveProjects()` 返回 `src/**/*.csproj`，包含 `IsPackable=false` 的 Generator，也包含空包 Logging；`Pack` 会依次调用 `DotNetPack`，缺少"跳过不可打包/空包"的校验。 | [Build.cs#L67](file:///f:/Code/Dotnet/framework/build/Build.cs#L67)、[Framework.DynamicApi.Generator.csproj#L6](file:///f:/Code/Dotnet/framework/src/Framework.DynamicApi.Generator/Framework.DynamicApi.Generator.csproj#L6) |
| B5 | **标签通配过宽**：`[Vv]*` 匹配任何以 V/v 开头的标签（如 `Version-test`），可能触发误发布。 | [public.yml#L5-L7](file:///f:/Code/Dotnet/framework/.github/workflows/public.yml#L5-L7) |
| B6 | **DynamicApi 分析器打包依赖跨项目硬编码 bin 路径**：`..\Framework.DynamicApi.Generator\bin\$(Configuration)\netstandard2.1\Framework.DynamicApi.Generator.dll`。若单独 `dotnet pack` 目标包而生成器未先以同一 Configuration 构建，文件不存在时 pack 可能静默缺少 `analyzers/dotnet/cs`（消费者拿到的包不会生成任何端点）。现有 `nugets/YLFramework.DynamicApi.2.0.0-alpha.1.nupkg` 内确实只有 `lib/net10.0`，没有 `analyzers/` 与 `buildTransitive/`。 | [Framework.DynamicApi.csproj#L16-L25](file:///f:/Code/Dotnet/framework/src/Framework.DynamicApi/Framework.DynamicApi.csproj#L16-L25)、`nugets/YLFramework.DynamicApi.2.0.0-alpha.1.nupkg` 条目列表 |
| B7 | **本地 artifact 与源码版本漂移**：`nugets/` 中 20 个包版本均为 `2.0.0-alpha.1`，而当前 `Framework.DynamicApi` csproj 已是 `3.0.0-alpha.1`；这些产物不能代表当前源码。 | [Framework.DynamicApi.csproj#L9](file:///f:/Code/Dotnet/framework/src/Framework.DynamicApi/Framework.DynamicApi.csproj#L9)、`nugets/` 文件名 |

B6 的说明：现有 nupkg 的 commit 早于生成器功能合入（nuspec `commit=a864a46`，csproj 最后修改为 2026-10-24），因此它不能证明"当前配置一定打不出分析器"；但它证明了"缺分析器时 pack 仍能成功产出包"这一失败模式，而当前配置没有任何构建后校验（例如断言 nupkg 含 `analyzers/dotnet/cs/*.dll`）。

---

## 7. 结论与结构性风险

### 7.1 仓库结构特点（设计取舍）

1. **"源码内联"换取零程序集耦合**：`src/Shared` 以文件级 `<Compile Include>` 复制进各包，避免了核心库版本地狱，也让每个包真正自包含；代价是 73 个源文件中 35 个（48%）从未被用到，4 个文件被 2 个以上包各编译一份，修复需要多包齐发。
2. **包粒度严重不均，且部分包按"历史入口"而非领域拆分**：`Modbus`（5,274 行/111 个公开类型）与 `Middleware`（3,870 行/7 类协议）是巨型包；`Mvvm`（76 行）、`Logging`（0 行）是事实上的占位包。`Middleware` 内的 Modbus 从站（27 文件/2,305 行）与独立 `Modbus` 主站包构成两套并行协议实现。
3. **依赖边界基本干净，但存在"声明了却没用到"的依赖**：包间只有 2 条 ProjectReference（一条是分析器）；CPM 统一了 26 个实际依赖；同时 BouncyCastle（3 个包）与 ZLogger、SharpCompress、System.IO.Pipelines 属于死依赖。
4. **版本与发布强依赖"标签字符串 ↔ csproj 正则"约定**：每包独立版本是优点，但 `Build.cs` 用正则读 `<Version>`/`<PackageId>`，`Push` 又用字符串前缀过滤 nupkg，项目名与 PackageId 不一致（`Framework.Charts` → `YLFramework.Plot`）时会出现"能打不能推"的分裂行为。
5. **测试覆盖呈"深而窄"**：Modbus、ZLogging、DynamicApi 有较完整的单元/集成测试（226 个 `[Test]`），但 10 个可打包包中 7 个零测试；Shared 只测了 2 个功能点，且测试的是源码副本而非包内副本。
6. **文档领先于代码**：README/AGENTS 描述了已不存在的 `AddFrameworkNLog()`、根目录 `Directory.Build.props`、"9 个包/3 个测试工程"，并给 `Framework.Logging` 赋予了实际不存在的功能。

### 7.2 风险与改进建议

#### 高优先级

| 风险 | 证据 | 建议 |
|---|---|---|
| **H1 `Framework.Logging` 是空包却被纳入发布**：没有一行 C#，README 宣称的 `AddFrameworkNLog()` 不存在；已产出包内只有空 `Framework.Logging.dll`。消费者会拿到一个"能安装、无任何扩展方法"的包。 | [Framework.Logging.csproj](file:///f:/Code/Dotnet/framework/src/Framework.Logging/Framework.Logging.csproj)、[README.md#L14-L25](file:///f:/Code/Dotnet/framework/src/Framework.Logging/README.md#L14-L25)、`nugets/YLFramework.Logging.2.0.0-alpha.1.nupkg` | 要么补齐 `ILoggingBuilder` 扩展实现并打包 `NLog.config`，要么从 `src/` 移除/设为 `IsPackable=false`，同时修正 README。 |
| **H2 安全/协议敏感包零测试**：`Middleware` 的 HttpProxy、TlsDetection、Telnet、Modbus 从站，以及 `SlideCaptcha` 的轨迹校验均无测试；`Repository` 的软删除/递归删除也无测试。 | `test/` 仅 5 个工程；[Framework.AspNetCore.Middleware.csproj](file:///f:/Code/Dotnet/framework/src/Framework.AspNetCore.Middleware/Framework.AspNetCore.Middleware.csproj)、[Framework.SlideCaptcha.csproj](file:///f:/Code/Dotnet/framework/src/Framework.SlideCaptcha/Framework.SlideCaptcha.csproj) 无对应测试工程 | 至少为 Middleware 的帧解析/协议分支、SlideCaptcha 的 Validator、Repository 的删除语义补最小回归测试；优先覆盖纯逻辑部分（无需真实网络/串口）。 |
| **H3 发布可能静默不执行**：`PushTask` 在没有 API Key 时只记错误并 `return`，Cake 进程退出码为 0。 | [Build.cs#L223-L228](file:///f:/Code/Dotnet/framework/build/Build.cs#L223-L228) | 改为抛出异常；或在 CI 增加"至少推送了 N 个包"的校验。 |
| **H4 SDK 版本钉死与 CI 安装范围不匹配**：`10.0.400` + `latestFeature` 只能向上滚动同一 minor 的 feature band；CI 装 `10.0.x` 不保证 ≥400。 | [global.json#L3-L4](file:///f:/Code/Dotnet/framework/global.json#L3-L4)、[public.yml#L30](file:///f:/Code/Dotnet/framework/.github/workflows/public.yml#L30) | CI 改为 `dotnet-version: 10.0.400`（或读取 global.json），避免 restore 因 SDK 低于要求失败。 |

#### 中优先级

| 风险 | 证据 | 建议 |
|---|---|---|
| **M1 项目名 / PackageId 不一致导致"能打包不能推送"**：`Framework.Charts` 的 PackageId 是 `YLFramework.Plot`；`Push` 用 `context.Package + "."` 过滤 nupkg。 | [Framework.Charts.csproj#L7-L9](file:///f:/Code/Dotnet/framework/src/Framework.Charts/Framework.Charts.csproj#L7-L9)、[Build.cs#L74-L79](file:///f:/Code/Dotnet/framework/build/Build.cs#L74-L79)、[Build.cs#L237](file:///f:/Code/Dotnet/framework/build/Build.cs#L237) | `Push` 改为按 `ResolveProjects()` 解析出的 PackageId 过滤；或统一项目名与 PackageId；README 的"包清单"同步为 `YLFramework.Plot`。 |
| **M2 分析器打包无校验**：DynamicApi 依赖跨项目 bin 路径；现有 nupkg 无 `analyzers/` 条目。 | [Framework.DynamicApi.csproj#L16-L25](file:///f:/Code/Dotnet/framework/src/Framework.DynamicApi/Framework.DynamicApi.csproj#L16-L25) | 改为用 MSBuild target 在 pack 时复制生成器输出，并在 `Pack` 后断言 nupkg 含 `analyzers/dotnet/cs/*.dll` 与 `buildTransitive/*.props`。 |
| **M3 Shared 维护成本与漂移**：48% 文件未被使用；4 个文件多包重复；`Helper` 分部类需整体复制。 | §3 统计；[src/Shared/README.md#L14-L22](file:///f:/Code/Dotnet/framework/src/Shared/README.md#L14-L22)、[src/Shared/MODULES.md#L35-L63](file:///f:/Code/Dotnet/framework/src/Shared/MODULES.md#L35-L63) | 增加"Shared 文件被引用矩阵"生成脚本与 CI 检查；清理未使用的 Proxy/Compression/Crypto 模块或明确标注为"待用模板"；对重复文件建立单一归属说明。 |
| **M4 Middleware 包职责过宽**：一个包同时承载 7 类协议，其中 Modbus 从站 27 文件/2,305 行，与 `Framework.Modbus` 主站无共享契约。 | `src/Framework.AspNetCore.Middleware/Modbus/` 下 27 文件 / 2,305 行（见 [ModbusConnectionHandler.cs](file:///f:/Code/Dotnet/framework/src/Framework.AspNetCore.Middleware/Modbus/ModbusConnectionHandler.cs) 等）、[Framework.Modbus](file:///f:/Code/Dotnet/framework/src/Framework.Modbus/Framework.Modbus.csproj) | 按协议拆分为 `Middleware.Telnet` / `Middleware.Proxy` / `Middleware.Modbus` 等子包，或至少把 Modbus 从站独立；共享的 PDU/CRC 定义应抽到一处。 |
| **M5 死依赖增加包体积与攻击面**：BouncyCastle 在 3 个包中声明但代码未用；ZLogger/SharpCompress/System.IO.Pipelines 声明未用。 | [Directory.Packages.props#L28-L63](file:///f:/Code/Dotnet/framework/Directory.Packages.props#L28-L63)；三份 csproj 的 `PackageReference`；`Grep BouncyCastle` 仅命中 csproj | 移除未使用声明；Middleware 的 BouncyCastle 属传递依赖，无需重复声明。 |
| **M6 Charts 中文字体与 README 不符**：README 称 `simhei.ttf/simkai.ttf/simsun.ttc` "打包为资源随包发布"，但 csproj 无字体 `Resource/None` 条目，已产出 nupkg 也无字体；代码只用 `SKFontManager` 查找**系统已安装字体**。 | [Charts README#L12](file:///f:/Code/Dotnet/framework/src/Framework.Charts/README.md#L12)、[Plots.cs#L16-L37](file:///f:/Code/Dotnet/framework/src/Framework.Charts/Plots.cs#L16-L37)、`nugets/YLFramework.Plot.2.0.0-alpha.1.nupkg` 条目 | 二选一：真正把字体打进包并在运行时注册，或删除 README 该行并明确"依赖宿主系统字体"（容器环境需预装字体）。 |
| **M7 `[Vv]*` 标签通配过宽** | [public.yml#L5-L7](file:///f:/Code/Dotnet/framework/.github/workflows/public.yml#L5-L7) | 收紧为 `v[0-9]*` / `V[0-9]*`，或改为 `workflow_dispatch` + 显式参数。 |

#### 低优先级

| 风险 | 证据 | 建议 |
|---|---|---|
| **L1 文档与代码不一致（多处）**：`Readme.md` 称"9 个包"但表格列 9 个且漏掉 Modbus，实际 10 个可打包包；称"3 个测试工程"，实际 5 个；称根目录有 `Directory.Build.props`，实际只有 `src/Directory.Build.props`；`AGENTS.md` 称 ZLogging 75 用例，实测 63 个 `[Test]`。 | [Readme.md#L3](file:///f:/Code/Dotnet/framework/Readme.md#L3)、[Readme.md#L27](file:///f:/Code/Dotnet/framework/Readme.md#L27)、[Readme.md#L28-L33](file:///f:/Code/Dotnet/framework/Readme.md#L28-L33)、[AGENTS.md](file:///f:/Code/Dotnet/framework/AGENTS.md) | 以代码为准重写包清单/测试清单；把 README 链接与 csproj 的 `PackageId` 做一致性检查。 |
| **L2 `Mvvm` 使用全局可变静态状态** | [WindowsVariable.cs#L9-L18](file:///f:/Code/Dotnet/framework/src/Framework.Mvvm/Constant/WindowsVariable.cs#L9-L18) | 若保留该包，改为实例/消息传递；否则与 `HeightChange`/`WidthChange` 消息机制合并。 |
| **L3 `nugets/` 留存过期产物** | `nugets/*` 均为 2.0.0-alpha.1；`Framework.DynamicApi` 已 3.0.0-alpha.1；该目录未被 git 跟踪，也未写入 `.gitignore` | 将 `nugets/` 加入 `.gitignore`，或在文档中声明为本地输出目录。 |
| **L4 `Framework.Tests` 测的是 Shared 副本** | [Framework.Tests.csproj#L16-L24](file:///f:/Code/Dotnet/framework/test/Framework.Tests/Framework.Tests.csproj#L16-L24) | 至少增加一个引用真实包（`Framework.Repository` / `Framework.AspNetCore`）的测试工程，验证内联后的实际程序集行为。 |
| **L5 solution 未收录构建脚本** | [Framework.slnx](file:///f:/Code/Dotnet/framework/Framework.slnx) 无 `build/Build.cs`、`global.json`、`AGENTS.md` | 把 `build/Build.cs` 作为 `File` 节点加入 `/document/`，便于 IDE 内可见。 |

---

## 8. 附：关键数字汇总

| 指标 | 数值 |
|---|---|
| src 工程数 | 11（10 可打包 + 1 分析器） |
| test 工程数 | 5 |
| src 生产代码（不含 Shared） | 220 文件 / 17,333 行 |
| Shared 源码 | 73 文件 / 12,281 行；被引用 38（52%），未引用 35（48%） |
| public 类型（src，按包去重求和） | 281 个 |
| CPM 声明 / 实际使用 | 29 / 26 |
| 包间 ProjectReference | 2 |
| 测试用例 `[Test]` | 226（29 文件 / 3,300 行） |
| 有直接测试的可打包包 | 3 / 10（Modbus、ZLogging、DynamicApi） |

