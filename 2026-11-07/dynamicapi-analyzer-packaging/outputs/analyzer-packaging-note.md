# DynamicApi 源生成器分析器分发核实与修复说明

日期：2026-11-07
范围：仅 `Framework.DynamicApi` 的源生成器打包方式；未触碰其他包、未改 `Framework.slnx`、未提交、未推送 nuget。

## 一、核实结论

### 1. 原有打包链路

| 环节 | 事实 |
|------|------|
| 分析器引用 | `Framework.DynamicApi.csproj` 的 `ProjectReference` 已使用 `ReferenceOutputAssembly="false"` + `OutputItemType="Analyzer"`，开发期编译能加载生成器 |
| 打包方式（原实现） | 另有一条 `<None Include="..\Framework.DynamicApi.Generator\bin\$(Configuration)\netstandard2.1\Framework.DynamicApi.Generator.dll" Pack="true" PackagePath="analyzers/dotnet/cs" />`，**跨项目硬编码 bin 路径** |
| `build/Build.cs` | 不参与分析器拷贝：`PackTask` 只是对 `src/**/*.csproj` 逐个执行 `dotnet pack`（`NoBuild=true`、`NoRestore=true`、可选 `-property:PackageVersion=...`）；`PushTask` 只推送 `nugets/*.nupkg` |
| `Directory.Packages.props` | `Microsoft.CodeAnalysis.CSharp` 固定 `4.14.0`；生成器 csproj 以 `PrivateAssets="all"` 引用，版本受 CPM 统一约束 |
| 生成器自身打包 | `Framework.DynamicApi.Generator.csproj` 已设 `<IsPackable>false</IsPackable>`，保持不被单独打包 |
| 构建附加行为 | `src/Directory.Build.targets` 设 `GeneratePackageOnBuild=true`，所以 `dotnet build` 也会在各自 `bin/<Config>/` 下产出 nupkg（既有行为，本轮不改） |

### 2. 关于「现有 nupkg 里没有 analyzers/dotnet/cs」的精确结论

实测下载 nuget.org 上的两个版本并解包：

| 版本 | `analyzers/dotnet/cs/` | 其它条目 |
|------|------------------------|----------|
| `2.0.0-alpha.1` | **无** | 仅 `lib/net10.0/Framework.DynamicApi.{dll,pdb,xml}` |
| `3.0.0-alpha.1` | **有**：`Framework.DynamicApi.Generator.dll` | 同左 + `buildTransitive/YLFramework.DynamicApi.props` |

结合 git 历史：生成器是提交 `7c8cdc7`（2026-10-24）才加入的，该提交同时把 `Version` 提到 `3.0.0-alpha.1` 并加上了上述 `None` 打包项；`2.0.0-alpha.1` 发布时项目里根本还没有生成器。

因此：

- **结构报告中「装包后生成器不生效」对应的应是更早的 `2.0.0-alpha.1`（生成器尚未存在）**；当前 `3.0.0-alpha.1` 实际是带 analyzer 的。
- 但原实现仍然脆弱，这才是需要修的真实问题：
  1. 硬编码 `bin\$(Configuration)\netstandard2.1\`，路径正确与否取决于「外层是否恰好先用同一 Configuration 构建过生成器」；
  2. 生成器输出布局一旦变化（多目标框架、`AppendTargetFrameworkToOutputPath`、RID、`--output`），路径立即失配；
  3. 文件不存在时不会静默丢包，而是直接 `error NU5019 / NU5026` 让发布失败（本轮已实测复现 NU5019）。
- **确切根因一句话**：包内容由「跨项目硬编码 bin 路径」决定，发布成败取决于构建顺序与输出布局的偶然一致，而不是由 MSBuild 解析的生成器实际产物决定。

## 二、修复方式（标准源生成器打包）

只改 `src/Framework.DynamicApi/Framework.DynamicApi.csproj`：

```xml
<ItemGroup>
	<!-- 编译期以 Analyzer 引入，不参与引用程序集 -->
	<ProjectReference Include="..\Framework.DynamicApi.Generator\Framework.DynamicApi.Generator.csproj"
					  ReferenceOutputAssembly="false"
					  OutputItemType="Analyzer" />
	<None Include="buildTransitive\YLFramework.DynamicApi.props"
		  Pack="true" PackagePath="buildTransitive" Visible="false" />
</ItemGroup>

<PropertyGroup>
	<TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);_PackDynamicApiGenerator</TargetsForTfmSpecificContentInPackage>
</PropertyGroup>

<Target Name="_PackDynamicApiGenerator">
	<MSBuild Projects="$(MSBuildThisFileDirectory)..\Framework.DynamicApi.Generator\Framework.DynamicApi.Generator.csproj"
			 Targets="GetTargetPath"
			 Properties="Configuration=$(Configuration)"
			 RemoveProperties="TargetFramework;RuntimeIdentifier;TargetPlatformIdentifier">
		<Output TaskParameter="TargetOutputs" ItemName="_DynamicApiGeneratorTargetPath" />
	</MSBuild>
	<ItemGroup>
		<TfmSpecificPackageFile Include="@(_DynamicApiGeneratorTargetPath)"
							   PackagePath="analyzers/dotnet/cs" />
	</ItemGroup>
</Target>
```

关键点：

- 删除了 `None Include="..\..\bin\$(Configuration)\netstandard2.1\..."` 这条硬编码路径；
- 通过 `MSBuild` 任务的 `GetTargetPath` 让**生成器工程自己**给出实际输出 dll 路径，Configuration / 目标框架 / 输出布局变化时自动跟随；
- 用 `TargetsForTfmSpecificContentInPackage` + `TfmSpecificPackageFile` 挂钩，这是 SDK 官方支持的「按 TFM 追加自定义包内容」入口，`PackagePath="analyzers/dotnet/cs"` 直接落到分析器目录；
- `RemoveProperties` 必须清掉外层 TFM 等全局属性：首版实现因嵌套 `MSBuild` 继承了外层的 `TargetFramework=net10.0`，生成器被按 `net10.0` 求值，解析到不存在的 `bin/Debug/net10.0` 而报错；清掉后正确解析为 `netstandard2.1`；
- 只取 `TargetPath`（生成器 dll），不带 `pdb` / `deps.json`；
- Roslyn 依赖不落包：`Microsoft.CodeAnalysis.CSharp` 在生成器里是 `PrivateAssets="all"`，且 `TfmSpecificPackageFile` 仅包含生成器自身 dll，因此 `Microsoft.CodeAnalysis*.dll` 不会被打入 nupkg。

## 三、nupkg 内容验证

验证命令（先删除生成器的 `bin/Release`，证明不再依赖硬编码的既有产物）：

```powershell
Remove-Item .\src\Framework.DynamicApi.Generator\bin\Release -Recurse -Force
dotnet build src\Framework.DynamicApi\Framework.DynamicApi.csproj -c Release --no-incremental
dotnet pack  src\Framework.DynamicApi\Framework.DynamicApi.csproj -c Release --no-build -o $env:TEMP\dapi-pack-fixed
```

结果（`dapi-pack-fixed/YLFramework.DynamicApi.3.0.0-alpha.1.nupkg` 条目）：

| 条目 | 大小 |
|------|------|
| `analyzers/dotnet/cs/Framework.DynamicApi.Generator.dll` | 55296 |
| `buildTransitive/YLFramework.DynamicApi.props` | 328 |
| `lib/net10.0/Framework.DynamicApi.dll` | 22528 |
| `lib/net10.0/Framework.DynamicApi.pdb` | 29748 |
| `lib/net10.0/Framework.DynamicApi.xml` | 29228 |
| `YLFramework.DynamicApi.nuspec` / `[Content_Types].xml` / `_rels/.rels` / `nuget.psmdcp` | — |

核对项：

- analyzers 目录**已就位**，dll 大小 55296 与 `src/Framework.DynamicApi.Generator/bin/Release/netstandard2.1/Framework.DynamicApi.Generator.dll` 完全一致；
- 全包内 **没有** `Microsoft.CodeAnalysis*` 相关 dll；`analyzers/dotnet/cs/` 下也只有生成器 dll（无 pdb/deps.json）；
- nuspec：`<id>YLFramework.DynamicApi</id>`、`<version>3.0.0-alpha.1</version>`，`<dependencies>` 为空分组，未泄漏 Roslyn 依赖；
- Debug 配置的 `dotnet build`（`GeneratePackageOnBuild=true`）产出的 `bin/Debug/...nupkg` 条目与上表一致，说明 Debug/Release 两种配置都能正确出包。

## 四、兼容性

| 项 | 结论 |
|----|------|
| `PackageId` | 保持 `YLFramework.DynamicApi` |
| `Version` | 保持 `3.0.0-alpha.1` |
| 公开 API | 无任何 C# 代码改动，API 不变 |
| 生成器工程 | 仍 `IsPackable=false`，不被单独打包 |
| 包内文件集合 | 与修复前完全一致（条目名与数量相同），仅生成路径的来源改为 MSBuild 解析，下游无感知 |
| `build/Build.cs` | 无需改动；`PackTask` 的 `dotnet pack --no-build` 流程已实测通过 |
| `Framework.slnx` | 无需改动；生成器仍作为解决方案项目参与编译 |
| CI | 未改 `.github/workflows/public.yml`；Linux 上不再依赖 Windows 风格的硬编码 `bin\...` 路径，跨平台更稳 |

## 五、验证汇总

| 项 | 结果 |
|----|------|
| `dotnet build Framework.slnx` | **0 error**（仅既有 NU1510 等警告） |
| `dotnet pack`（清空生成器 Release 输出后、`--no-build`） | 成功，退出码 0；analyzers 条目就位 |
| `test/Framework.DynamicApi.Generator.Tests` | 23 通过 / 0 失败 |
| `test/Framework.DynamicApi.Tests` | 29 通过 / 0 失败 |
| 未做 | 未提交 git、未推送 nuget、未改动其他包 |

## 六、剩余风险

1. **编译宿主限制（既有）**：生成器目标框架为 `netstandard2.1`，只能被 .NET Core 系编译器宿主加载（`dotnet build` / VS Code / Rider）；Visual Studio 2022 与 `msbuild.exe` 的 .NET Framework 编译器无法加载。生成器 csproj 已通过 `NoWarn RS1041` 明确接受该约束，本轮未改。
2. **包内不含生成器 pdb / deps.json**：与修复前一致；Roslyn 分析器只需 dll，且 deps 由编译器宿主提供，无需随包分发。
3. **`pack --no-build` 与构建配置不一致时会明确失败**（NU5019/NU5026），这是预期行为；`build/Build.cs` 的 Pack 始终跟随同一 Configuration 的 Compile，不会触发。
4. **`GeneratePackageOnBuild=true`（既有）**：每次 `dotnet build` 都会在源工程 `bin/` 下产出 nupkg，与 `nugets/` 发布目录互不干扰；属既有约定，本轮未调整。
5. **未做终端消费方安装验证**：本轮以 nupkg 内容核对为主；如需要，可在临时本地源上新建消费工程引用该包，确认 `obj/**/generated` 下出现生成代码。
