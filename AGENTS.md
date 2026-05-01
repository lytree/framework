# AGENTS.md

## Build System
- Build orchestrated by Cake Frosting: `dotnet run build/Build.cs`
- Default task chain: Clean → Restore → Compile → Pack (Push on tag push)
- Output packages to `./nugets/`
- Version via GitVersion (semantic versioning, tag prefix `[vV]`)

## Developer Commands
```bash
# Build all (Release)
dotnet build Framework.slnx

# Build and pack all packages
dotnet run build/Build.cs

# Run all tests (TUnit, targets net8.0)
dotnet test

# Run tests for a single project
dotnet test test/Framework.Tests/Framework.Tests.csproj

# Pack specific project
dotnet pack src/Framework/Framework.csproj
```

## Architecture
- **Monorepo**: 10 src packages under `src/`, 1 test project under `test/`
- **Solution file**: `Framework.slnx` (not .sln)
- **Central package management**: `packages/` submodule imports `Directory.Packages.*.props`
- **Target frameworks**: `$(NetCore)` = `net8.0;net10.0` (defined in `packages/Directory.Packages.All.props`)
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

## Code Style
- Nullable reference types enabled
- Many nullable warnings suppressed in `src/Directory.Build.props` (CS8600-8625, etc.)
- XML doc generation enabled (`GenerateDocumentationFile`)