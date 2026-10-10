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

## Package Versioning（集中式 CPM）
- **全仓库统一版本控制**：根目录 `Directory.Packages.props` 单一文件声明所有 NuGet 依赖版本。
- 启用 Central Package Management (`ManagePackageVersionsCentrally=true`)：csproj 内 `<PackageReference Include="X" />` 不写版本，统一在 `Directory.Packages.props` 中声明。
- 升级工作流：修改 `Directory.Packages.props` 中对应行即可，所有项目即时生效；个别项目若需临时覆盖，可在其 csproj 内写 `<PackageReference Include="X" Version="x.y.z" />`。
- 项目根目录不再保留任何 git 子模块；旧的 `packages` 子模块已移除。

## Architecture
- **Monorepo**: 9 src packages under `src/`, 3 test projects under `test/`
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
| `Framework.Repository` | linq2db-based repository + Mapster |
| `Framework.Charts` | Chart generation |
| `Framework.Modbus` | Modbus 主站（TCP / UDP / RTU / ASCII）+ 全功能码，设计参照 jamod |
| `Framework.ZLogging` / `Framework.Logging` | Infrastructure |

## CI / Release
- Push tag matching `V*.*.*` triggers `build/Build.cs -t Push` which packs and pushes to NuGet
- GitVersion determines version from git tags/branch

## Test Projects
- `test/Framework.ZLogging.Tests/` — ZLogging 单元测试（TUnit，30+ 用例）
- `test/Framework.Modbus.Tests/` — Modbus 测试（TUnit，93 用例：校验向量 / PDU 编解码 / RTU·ASCII 组帧 / TCP 回环集成）
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

TUnit 1.72.10 用法约定：
- 断言全部 `await Assert.That(...).IsXxx(...)`。
- 共享 disposable 资源（`TempDirectory`）通过 `[ClassDataSource<TempDirectory>(Shared = SharedType.PerClass)]` 注入。
- 跨测试需要隔离时使用 `[NotInParallel]`。
- 文件 I/O 测试需显式 `await provider.DisposeAsync()` 后再读取，避免异步写循环未完成。

## Code Style
- Nullable reference types enabled
- Many nullable warnings suppressed in `src/Directory.Build.props` (CS8600-8625, etc.)
- XML doc generation enabled (`GenerateDocumentationFile`)
