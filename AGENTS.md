# AGENTS.md

## Build System
- Build orchestrated by Cake Frosting: `dotnet run build/Build.cs`
- Default task chain: Clean → Restore → Compile → Pack (Push on tag push)
- Output packages to `./nugets/`
- Version via GitVersion (semantic versioning, tag prefix `[vV]`)
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
dotnet pack src/Framework/Framework.csproj
```

## Package Versioning（按项目独立管理）
- **CPM 已禁用**，每个项目自带 `<ProjectName>.Packages.props`，内联 `Version="..."`。
- `packages/` 子模块仍存在但**不再**用于包版本管理（保留以备兼容）。
- 示例：`src/Framework/Framework.Packages.props`、每个 csproj 通过 `<Import Project="..." />` 引入。
- 升级版本：直接编辑对应 `.Packages.props` 文件即可，无需触碰子模块。

## Architecture
- **Monorepo**: 10 src packages under `src/`, 1 test project under `test/`
- **Solution file**: `Framework.slnx` (not .sln)
- **Package versioning**: see "Package Versioning" section above
- **Target frameworks**: `net10.0` only (defined inline in each csproj)
- **Package prefix**: `YLFramework.*` (e.g., `YLFramework.AspNetCore`, `YLFramework.Repository`)

## Key Packages
| Package | Notes |
|---------|-------|
| `Framework` | Core utilities (imaging, compression, hashing, logging via ZLogger) |
| `Framework.AspNetCore` | ASP.NET Core integration |
| `Framework.Mvvm` | MVVM with CommunityToolkit.Mvvm |
| `Framework.Repository` | FreeSql-based repository + Mapster |
| `Framework.Charts` | Chart generation |
| `Framework.ZLogging` / `Framework.Logging` | Infrastructure |

## CI / Release
- Push tag matching `V*.*.*` triggers `build/Build.cs -t Push` which packs and pushes to NuGet
- GitVersion determines version from git tags/branch

## Test Projects
- `test/Framework.ZLogging.Tests/` — ZLogging 单元测试（TUnit，30+ 用例）
- `test/Framework.Tests/` — Framework 核心工具测试（TUnit，PooledMemoryStream 等）

## Testing Platform (MTP)
- TUnit tests run via Microsoft.Testing.Platform (MTP). `global.json` opts in via `"test": { "runner": "Microsoft.Testing.Platform" }`.
- On .NET 10 SDK and later, VSTest-based `dotnet test` is no longer supported for MTP projects; the global.json opt-in is required.
- 测试工程已开启 `TestingPlatformDotnetTestSupport=true`。

## Test Project Structure (`test/Framework.ZLogging.Tests/`)
按特性拆分（每个目录对应一个职责域）：

| 目录 | 覆盖范围 |
|------|---------|
| `Fixtures/` | 共享测试基础设施（`TempDirectory` 等自动清理） |
| `Options/` | `ZLoggerSpectreConsoleOptions` 默认值 / 自定义 |
| `Extensions/` | `AddZLoggerSpectreConsole` / `AddZLoggerSpectreConsoleAndFile` |
| `Lifecycle/` | `ZLoggerSpectreConsoleLoggerProvider` 生命周期（Dispose / DisposeAsync / 幂等） |
| `FileIO/` | 文件输出（追加 / 覆盖 / 编码 / 目录自动创建 / 空路径禁用） |
| `ExceptionRendering/` | 异常渲染（嵌套异常 / 自定义格式化器 / Markup 转义） |
| `Concurrency/` | 并发与销毁竞争 |

TUnit 1.65.0 用法约定：
- 断言全部 `await Assert.That(...).IsXxx(...)`。
- 共享 disposable 资源（`TempDirectory`）通过 `[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]` 注入。
- 跨测试需要隔离时使用 `[NotInParallel]`。
- 文件 I/O 测试需显式 `await provider.DisposeAsync()` 后再读取，避免异步写循环未完成。

## Code Style
- Nullable reference types enabled
- Many nullable warnings suppressed in `src/Directory.Build.props` (CS8600-8625, etc.)
- XML doc generation enabled (`GenerateDocumentationFile`)



