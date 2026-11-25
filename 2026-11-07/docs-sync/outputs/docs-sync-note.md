# 文档同步到代码真实状态（README.md / AGENTS.md）

本轮只改文档，未改动任何源码、csproj、构建脚本。核实依据：`Framework.slnx`、各 `src/*/*.csproj`、`build/Build.cs`、`.github/workflows/public.yml`、`global.json`、`Directory.Packages.props`、`test/` 目录。

## 一、根 README.md

### 1. 包数量与能力描述

- 原文（第 3 行）：`提供 9 个互相独立、可单独引用的 NuGet 包，覆盖……日志的常见基础设施能力。`
- 改后：`src/` 下共 12 个工程，其中 10 个可独立引用的 NuGet 包；另有 2 个不打包工程（`Framework.Logging`、`Framework.DynamicApi.Generator`）。能力列表补充「Modbus 主站/从站」。
- 依据：`Framework.slnx` 的 `/src/` 节点共 12 个 `Project`；`src/Framework.Logging/Framework.Logging.csproj`、`src/Framework.DynamicApi.Generator/Framework.DynamicApi.Generator.csproj` 均声明 `<IsPackable>false</IsPackable>`，其余 10 个工程未关闭打包。

### 2. 包清单

- 原文：单表 9 行，含 `YLFramework.Charts` 与 `YLFramework.Logging`，缺 Modbus 两项。
- 改后：可打包清单 10 行（新增 `YLFramework.Modbus`、`YLFramework.Modbus.Abstractions`；`Framework.Charts` 行的包名改为 `YLFramework.Plot` 并注明工程名；Middleware 行补充 Modbus TCP / RTU-over-TCP 从站）；另设「不参与打包的工程」小表列出 `Framework.Logging`（已停止发布）与 `Framework.DynamicApi.Generator`（随 DynamicApi 以 `analyzers/dotnet/cs` 分发）。
- 依据：`src/Framework.Modbus/Framework.Modbus.csproj`（`PackageId=YLFramework.Modbus`、`PackageReference System.IO.Ports`）；`src/Framework.Modbus.Abstractions/Framework.Modbus.Abstractions.csproj`（`PackageId=YLFramework.Modbus.Abstractions`，无外部 NuGet 依赖）；`src/Framework.Charts/Framework.Charts.csproj`（`PackageId=YLFramework.Plot`、`PackageReference ScottPlot`）；`src/Framework.AspNetCore.Middleware/Framework.AspNetCore.Middleware.csproj`（引用 `YLFramework.Modbus.Abstractions`）；现有 Middleware 从站代码位于 `src/Framework.AspNetCore.Middleware/Modbus/`。

### 3. 仓库结构树

- 原文：`src/ # 10 个生产包 + Shared 共享源码`；`test/ # 3 个测试工程`；`Directory.Build.props # 全仓库默认开关`。
- 改后：`src/ # 12 个工程（10 个可打包 + 2 个不打包）+ Shared`；`test/ # 5 个测试工程（TUnit + MTP）`；目录级配置改为 `src/Directory.Build.props` 与 `src/Directory.Build.targets`，并注明仓库根目录没有 `Directory.Build.props`。
- 依据：`Framework.slnx` 的 `/test/` 节点共 5 个 `Project`；根目录实际文件清单（存在 `Directory.Packages.props`、`global.json`、`NuGet.config`，无 `Directory.Build.props`）；`src/Directory.Build.props`、`src/Directory.Build.targets` 实际存在。`build/Directory.Packages.props` 的说明经核实无误，保留。

### 4. 构建/打包命令说明

- 原文：`# 打包全部包（输出到 ./nugets）`
- 改后：`# 全量编译 + 打包（输出到 ./nugets；IsPackable=false 的工程不产出 nupkg）`
- 依据：`build/Build.cs` 的 `ResolveProjects()` 在未指定 `-Package` 时返回 `src/**/*.csproj`（含 2 个不可打包工程），`PackTask` 逐个 `dotnet pack`，`IsPackable=false` 的工程不产生 nupkg。

### 5. 发版标签

- 原文：仅示例 `YLFramework.Repository/v2.0.0-alpha.1`，未说明前缀取自 `PackageId`。
- 改后：新增说明——单包标签前缀是 csproj 的 `PackageId` 而非工程名，图表包对应 `YLFramework.Plot/v2.0.0-alpha.1`；全量发布措辞改为「`src` 下所有可打包工程一起发」。
- 依据：`src/Framework.Charts/Framework.Charts.csproj` 的 `<PackageId>YLFramework.Plot</PackageId>`；`build/Build.cs` 的 `PackTask` 按 `<Version>` 校验、`PushTask` 用 `ResolvePackageId()` 过滤 nupkg；`.github/workflows/public.yml` 的 `push.tags` 为 `'YLFramework.*/v*'` 与 `'[Vv]*'`。

### 6. 命名空间约定

- 原文：命名空间枚举缺 Modbus。
- 改后：在枚举中补 `Framework.Modbus.*`、`Framework.Modbus.Abstractions.*`。
- 依据：`src/Framework.Modbus.Abstractions/**` 声明 `namespace Framework.Modbus.Abstractions`；`src/Framework.Modbus/**` 声明 `Framework.Modbus.*` 子命名空间（如 `Framework.Modbus.Util`、`Framework.Modbus.ProcessImage`）。

### 7. 测试章节

- 原文：`测试项目使用 TUnit + Microsoft.Testing.Platform`（未给工程数量）。
- 改后：明确 `test/` 下 5 个测试工程，并逐一列出领域，全部使用 TUnit + MTP。
- 依据：`Framework.slnx` 的 `/test/` 5 个项目；各测试 csproj 均含 `<PackageReference Include="TUnit" />` 与 `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`；`global.json` 的 `test.runner=Microsoft.Testing.Platform`。

## 二、AGENTS.md

1. **Build System / 包数量**
   - 原文：`Monorepo: 10 src packages under src/ + 1 非打包源生成器项目 + Shared, 5 test projects`
   - 改后：`src/` 下 12 个工程 —— 10 个可打包 + 2 个不打包（`Framework.Logging`（`IsPackable=false`）、`Framework.DynamicApi.Generator`）+ Shared，5 个测试工程。
   - 依据：`Framework.slnx`；两个不打包工程的 csproj。

2. **Developer Commands**
   - 原文：`# Build all (Release)`
   - 改后：`# Build all（dotnet build 默认 Debug；Release 由 build/Build.cs 使用）`
   - 依据：`build/Build.cs` 的 `BuildParameters.Configuration = "Release"`；README/AGENTS 里的裸 `dotnet build` 未传 `-c Release`。

3. **命名空间约定**
   - 原文：枚举缺 Modbus。
   - 改后：补 `Framework.Modbus.*`、`Framework.Modbus.Abstractions.*`。
   - 依据：同上。

4. **Package prefix**
   - 原文：`YLFramework.*` 前缀示例。
   - 改后：补充「工程名与包名可以不同：`Framework.Charts` 的 `PackageId` 是 `YLFramework.Plot`」。
   - 依据：`src/Framework.Charts/Framework.Charts.csproj`。

5. **Key Packages 表**
   - 原文：`Framework.Modbus` 仅写主站；`Framework.ZLogging / Framework.Logging | Infrastructure`；缺 `Framework.Modbus.Abstractions` 与 Middleware 条目。
   - 改后：
     - `Framework.Modbus`：主站（TCP / UDP / RTU / ASCII）+ 全功能码，协议契约来自 `Framework.Modbus.Abstractions`；
     - 新增 `Framework.Modbus.Abstractions`：功能码 / 异常码 / CRC·LRC / PDU / 帧编解码，零外部 NuGet 依赖，主站与 Middleware 从站共用；
     - 新增 `Framework.AspNetCore.Middleware`：Kestrel 连接中间件集合，Modbus TCP / RTU-over-TCP 从站位于其 `Modbus/` 子模块，扩展功能码通过可选宿主接口（异常状态 / 诊断 / 通信事件 / 标识 / 文件记录 / FIFO）接入；
     - `Framework.Charts` 注明 `PackageId` 为 `YLFramework.Plot`；
     - `Framework.Logging` 单独列出并注明 `IsPackable=false`、无 C# 源码、已停止发布。
   - 依据：`src/Framework.AspNetCore.Middleware/Modbus/Hosting/ModbusSlaveHosts.cs` 中 6 个可选宿主接口；`src/Framework.AspNetCore.Middleware/Modbus/ModbusSlave.Extended.cs`；`src/Framework.Logging/Framework.Logging.csproj` 注释与 `IsPackable`；`src/Framework.Modbus.Abstractions/README.md`。

6. **CI / Release**
   - 原文：`全量发布……src 下所有包`；无 Charts 标签说明。
   - 改后：改为「所有可打包工程」，并新增「单包标签前缀是 `PackageId`，`Framework.Charts` 对应 `YLFramework.Plot/v2.0.0-alpha.1`」。
   - 依据：`build/Build.cs` 的 `PackTask` / `PushTask`；`.github/workflows/public.yml` 的标签规则。

7. **Test Projects**
   - 原文：`ZLogging 单元测试（TUnit，75 用例）`；Modbus / DynamicApi 两条未标注框架。
   - 改后：去掉固定「75 用例」（实际 `[Test]` 方法数与运行期数据驱动展开后的用例数不一致，不宜写死），统一标注 TUnit。
   - 依据：`test/` 下 26 个测试文件共 278 处 `[Test]`；其中 `test/Framework.ZLogging.Tests/` 为 69 处，且多个测试使用数据源参数化。

8. **TUnit 版本**
   - 原文：`TUnit 1.72.10 用法约定`
   - 改后：`TUnit 1.76.0 用法约定`
   - 依据：`Directory.Packages.props` 中 `<PackageVersion Include="TUnit" Version="1.76.0" />`。

## 三、额外发现并修正（不在清单内）

1. **`src/Framework.Charts/README.md` 字体「随包发布」不成立**
   - 原文：`simhei.ttf / simkai.ttf / simsun.ttc | 字体资源 | ……（打包为资源随包发布）`；设计要点写「内置中文字体确保中文标签不出现方框」。
   - 改后：明确字体文件**不随 NuGet 包分发**；说明运行期通过 `SKFontManager.Default.GetFontFamilies()` 从**系统已安装字体**中按 SimSun / SimKai / DejaVu Sans / Liberation Sans / Noto Sans / FreeSans 顺序挑选，未命中时回退首个可用字体。
   - 依据：`src/Framework.Charts/Framework.Charts.csproj` 只把 `README.md` 作为 `Resource` 打包，没有任何 `.ttf/.ttc` 的 `Pack` 项；`src/Framework.Charts/Plots.cs#L27-L37` 的 `ResolveSafeFont()` 使用系统字体列表，未加载仓库内字体文件。

2. **`src/Framework.Charts/README.md` 标题与包名不一致 / 输出格式夸大**
   - 原文：标题 `# YLFramework.Charts`；简介称可导出「PNG / SVG / 流」。
   - 改后：标题改为 `# YLFramework.Plot` 并注明工程名仍为 `Framework.Charts`；简介改为「渲染为 PNG 字节数组」；版本说明补充标签前缀 `YLFramework.Plot/v*`；「适用场景」里的 `Framework.Logging` 改为已发布的 `Framework.ZLogging`。
   - 依据：`Framework.Charts.csproj` 的 `PackageId`；`Plots.cs` 全部出口使用 `plt.GetImageBytes(width, height, ImageFormat.Png)`，无 SVG 出口。

3. **`src/Framework.AspNetCore.Middleware/README.md` 中间件清单漏掉 Modbus 从站**
   - 原文：中间件表仅列 Echo / FlowAnalyze / FlowXor / HttpProxy / TunnelProxy / Telnet / TelnetProxy / TlsDetection。
   - 改后：新增 `Modbus` 行（`UseModbusTcp()` / `UseModbusRtuOverTcp()`，链接到 `Modbus/README.md`）。
   - 依据：`src/Framework.AspNetCore.Middleware/Modbus/ListenOptionsExtensions.cs` 暴露两个入口；`src/Framework.AspNetCore.Middleware/Modbus/` 目录实际存在。

## 四、核实后未改动的项

- `README.md` 中 `build/Build.cs（+ build/Directory.Packages.props 关闭该目录 CPM）`：`build/Directory.Packages.props` 确实存在并设 `ManagePackageVersionsCentrally=false`。
- `.NET SDK pinned 10.0.400 (rollForward=latestFeature)`：与 `global.json` 一致。
- NuGet Trusted Publishing（OIDC）整节：与 `.github/workflows/public.yml`（`environment: release`、`id-token: write`、`NuGet/login@v1`、`user: lytree`）一致。
- `Test Project Structure (test/Framework.ZLogging.Tests/)` 的目录表：与现有目录结构一致。
- CPM 与「包版本各自独立」描述：与 `Directory.Packages.props`、各 csproj 的 `<Version>` 一致。
