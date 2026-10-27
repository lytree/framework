# AGENTS.md

## Build System
- Build orchestrated by Cake Frosting: `dotnet run build/Build.cs`
- Default task chain: Clean → Restore → Compile → Pack (Push on tag push)
- Output packages to `./nugets/`
- Versioning: 每个包在自身 csproj 声明 `<Version>`，不依赖 GitVersion / Git tag 推算（`GitVersion.yml` 已移除）
- .NET SDK pinned via `global.json` to `10.0.400` (`rollForward=latestFeature`)

## Developer Commands
```bash
# Build all (Release)
dotnet build Framework.slnx

# Build and pack all packages
dotnet run build/Build.cs

# Run all tests (TUnit, targets net10.0)
dotnet test

# Run tests for a single project (MTP runner via global.json)
dotnet test test/Framework.ZLogging.Tests/Framework.ZLogging.Tests.csproj

# Run tests by directly executing the test exe (MTP)
./test/Framework.ZLogging.Tests/bin/Release/net10.0/Framework.ZLogging.Tests.exe

# Pack specific project
dotnet pack src/Framework.Repository/Framework.Repository.csproj
```

## Package Versioning
- **依赖版本集中（CPM）**：根目录 `Directory.Packages.props` 单一文件声明所有 NuGet 依赖版本；csproj 内 `<PackageReference Include="X" />` 不写版本。
- 升级工作流：修改 `Directory.Packages.props` 中对应行即可，所有项目即时生效；个别项目若需临时覆盖，可在其 csproj 内写 `<PackageReference Include="X" Version="x.y.z" />`。
- **包版本不统一管理**：每个包自己的 `<Version>` 写在各自 csproj 里（`src/Directory.Build.props` 不再声明全仓库 `<Version>`），版本互相独立演进。
- `build/Build.cs` 的 `Pack` 默认使用各 csproj 的 `<Version>`；仅当显式传 `-Version x.y.z` 时才统一覆盖。
- 项目根目录不再保留任何 git 子模块；旧的 `packages` 子模块已移除。

## Architecture
- **Monorepo**: 10 src packages under `src/` + 1 非打包源生成器项目（`src/Framework.DynamicApi.Generator`）+ `src/Shared/` 共享源码目录, 5 test projects under `test/`
- **Shared source**: 通用工具代码放在 `src/Shared/`，不再产出独立 `YLFramework` 程序集。需要它的包在 csproj 里用 `<Compile Include="..\Shared\...\*.cs" Link="Shared\...\%(Filename)%(Extension)" />` 直接引入源码编译，包之间**不互相 ProjectReference 核心库**。
- **Namespace convention**: 各包命名空间保持不变（`Framework.Repository.*`、`Framework.AspNetCore.*`、`Framework.DynamicApi.*`、`Framework.SlideCaptcha.*`、`Framework.ZLogging.*`、`Framework.Charts.*`、`Framework.Mvvm.*`）；**唯一例外是 `src/Shared`——文件不声明 namespace，类型落在全局命名空间，且顶层类型一律 `internal`**（多包内联同一份源码，`public` 会导致下游 `CS0433`），扩展类按文件区分名字（`Extensions` / `ListExtensions` / `StreamExtensions` …）以免合并冲突
- **Solution file**: `Framework.slnx` (not .sln)
- **Package versioning**: see "Package Versioning" section above
- **Target frameworks**: `net10.0` only (defined inline in each csproj)
- **Package prefix**: `YLFramework.*` (e.g., `YLFramework.AspNetCore`, `YLFramework.Repository`)

## Key Packages
| Package | Notes |
|---------|-------|
| `src/Shared` | 共享源码（BCL 扩展、集合、缓冲、IO、加密、压缩、JSON、拼音、代理），被各包文件级引入 |
| `Framework.AspNetCore` | ASP.NET Core integration |
| `Framework.Mvvm` | MVVM with CommunityToolkit.Mvvm |
| `Framework.Repository` | linq2db-based repository + Mapster |
| `Framework.Charts` | Chart generation |
| `Framework.Modbus` | Modbus 主站（TCP / UDP / RTU / ASCII）+ 全功能码，设计参照 jamod |
| `Framework.DynamicApi` | 编译期源生成 Minimal API 端点（零反射 / AOT），生成器随包以 `analyzers/dotnet/cs` 分发 |
| `Framework.ZLogging` / `Framework.Logging` | Infrastructure |

## CI / Release
- **单包发布（推荐）**：标签 `YLFramework.Repository/v2.0.0-alpha.1` → CI 只打包并推送该包
- **全量发布**：标签 `V2.0.0-alpha.1` → 打包并推送 `src` 下所有包
- 单包发布时标签版本必须与 csproj 的 `<Version>` 一致，否则 `Pack` 直接抛错
- 每个包按自身 csproj 的 `<Version>` 打包；构建不依赖 GitVersion，CI 里也不再安装 GitVersion.Tool
- 本地等价命令：`dotnet run build/Build.cs -t Pack --Package=YLFramework.Repository`

### NuGet Trusted Publishing（OIDC，无长期 API Key）
- nuget.org 上已建策略 `framework`：Publisher `GitHubActions` / Repository Owner `lytree` / Repository `framework` / Workflow `public.yml` / Environment `release` / Glob `YLFramework.*`，Scope 为 `Push new packages and package versions`
- 因此 `.github/workflows/public.yml` 必须保持三件事一致：作业级 `environment: release`、`permissions: id-token: write`、`uses: NuGet/login@v1`（`user: lytree`）
- 推送用的 `NUGET_API_KEY` 由 `NuGet/login` 用 OIDC token 换取的**临时 Key**（有效期 1 小时），`build/Build.cs` 仍按同名环境变量读取，无需改动；仓库不再需要 `NUGET_API_KEY` secret
- 改策略名 / 仓库名 / 工作流文件名 / 环境名任一项时，nuget.org 策略与 YAML 必须同步改，否则换 Key 会因 claim 不匹配失败
- 新建策略有过期风险：NuGet 需要一次成功发布才能拿到 GitHub repo/owner ID 并「永久激活」，7 天内不发布策略会自动失效（可点 `Activate for 7 days` 重置窗口）

## Test Projects
- `test/Framework.ZLogging.Tests/` — ZLogging 单元测试（TUnit，75 用例）
- `test/Framework.Tests/` — Framework 核心工具测试（TUnit，PooledMemoryStream、汉字拼音表等）
- `test/Framework.Modbus.Tests/` — Modbus 协议单元测试
- `test/Framework.DynamicApi.Tests/` — 路由解析 / 响应包装 / 生成端点的运行时测试
- `test/Framework.DynamicApi.Generator.Tests/` — 源生成器测试（`GeneratorDriver` 断言生成代码与诊断）

## Testing Platform (MTP)
- TUnit tests run via Microsoft.Testing.Platform (MTP). `global.json` opts in via `"test": { "runner": "Microsoft.Testing.Platform" }`.
- On .NET 10 SDK and later, VSTest-based `dotnet test` is no longer supported for MTP projects; the global.json opt-in is required.
- 测试工程已开启 `TestingPlatformDotnetTestSupport=true`。

## Test Project Structure (`test/Framework.ZLogging.Tests/`)
按特性拆分（每个目录对应一个职责域）：

| 目录 | 覆盖范围 |
|------|---------|
| `Fixtures/` | 共享测试基础设施（`TempDirectory` 自动清理、`LogTestHelper` 写入-释放-读取） |
| `Options/` | `SpectreLogOptions` 默认值 / 校验 / 路径解析 |
| `Extensions/` | `AddSpectreConsole` / `AddSpectreFile` / `AddSpectreConsoleAndFile` |
| `Lifecycle/` | `SpectreConsoleLoggerProvider` 生命周期（Dispose / DisposeAsync / 幂等 / 并发创建） |
| `FileIO/` | 文件输出（追加 / 覆盖 / 编码 / 无 BOM / 目录自动创建 / 按体积滚动 / 断连重连 / 定时刷盘） |
| `CustomFormats/` | 自定义指令 `{Value:format}`（内置格式 / 自定义 / 覆盖 / 异常容错） |
| `ExceptionRendering/` | 异常渲染（嵌套异常 / 类型名 / 堆栈 / AggregateException） |
| `Concurrency/` | 并发写入 / 销毁竞争 / 队列溢出 / 作用域捕获 |

TUnit 1.72.10 用法约定：
- 断言全部 `await Assert.That(...).IsXxx(...)`。
- 共享 disposable 资源（`TempDirectory`）通过 `[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]` 注入。
- 跨测试需要隔离时使用 `[NotInParallel]`。
- 文件 I/O 测试统一走 `LogTestHelper.CaptureAsync`：它保证「写入 → 释放 Provider → 读取」的顺序。
  直接 `await using` 后读取会撞上长连接句柄独占（`File.ReadAllText` 用独占语义，与写入侧冲突）；
  需要在 Provider 存活期间读取时用共享读 `FileShare.ReadWrite | FileShare.Delete`。
- 集合实参传日志要显式装箱 `(object)array`，否则走 `params object[]` 展开变成多个占位符。

## Code Style
- Nullable reference types enabled
- Many nullable warnings suppressed in `src/Directory.Build.props` (CS8600-8625, etc.)
- XML doc generation enabled (`GenerateDocumentationFile`)
