#:package Cake.Frosting@*


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.NuGet.Push;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}
// 定义构建中使用的常量
public static class BuildParameters
{
    public const string SolutionPath = "./Framework.slnx"; // 确保路径正确
    public const string Configuration = "Release";
    public const string PackageOutputDirectory = "./nugets";
    public const string ProjectPath = "./src/";
    public const string NuGetApiKey = "NUGET_API_KEY";
    public const string Version = "Version";
    public const string Package = "Package";
    public const string NugetSource = "https://api.nuget.org/v3/index.json";
}
public class BuildContext : FrostingContext
{
    public bool HasVersion { get; set; }
    /// <summary>按包发布时指定的 PackageId（或项目名）；为空表示全量。</summary>
    public string Package { get; set; }
    public bool HasPackage => !string.IsNullOrWhiteSpace(Package);
    public string NuGetApiKey { get; set; }
    public string NugetSource { get; set; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        HasVersion = context.Arguments.HasArgument(BuildParameters.Version);
        Package = context.Arguments.HasArgument(BuildParameters.Package) ? context.Arguments.GetArgument(BuildParameters.Package) : null;
        NuGetApiKey = context.Environment.GetEnvironmentVariable(BuildParameters.NuGetApiKey);
        NugetSource = context.Arguments.HasArgument(nameof(BuildParameters.NugetSource)) ? context.Arguments.GetArgument(nameof(BuildParameters.NugetSource)) : BuildParameters.NugetSource;
    }

    /// <summary>
    /// 解析本次要处理的项目集合：
    /// 未指定 <c>-Package</c> 时返回 src 下全部 csproj；指定时按 PackageId 或项目名精确匹配一个。
    /// </summary>
    public IReadOnlyList<FilePath> ResolveProjects()
    {
        var all = this.GetFiles(BuildParameters.ProjectPath + "**/*.csproj");

        if (!HasPackage)
        {
            return all.ToList();
        }

        var matched = all.Where(MatchesPackage).ToList();

        if (matched.Count == 0)
        {
            throw new CakeException($"没有找到 PackageId 或项目名为 '{Package}' 的 csproj（搜索范围：{BuildParameters.ProjectPath}**/*.csproj）");
        }

        if (matched.Count > 1)
        {
            throw new CakeException($"'{Package}' 匹配到多个 csproj，无法确定发布目标：{string.Join(", ", matched.Select(p => p.FullPath))}");
        }

        return matched;
    }

    private bool MatchesPackage(FilePath projectPath)
    {
        var projectName = projectPath.GetFilenameWithoutExtension().ToString();
        if (string.Equals(projectName, Package, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(ReadPackageId(projectPath), Package, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>读取 csproj 中声明的 &lt;PackageId&gt;，未显式声明时返回 null。</summary>
    public static string ReadPackageId(FilePath projectPath)
    {
        var text = File.ReadAllText(projectPath.FullPath);
        var match = Regex.Match(text, @"<PackageId>\s*([^<]+?)\s*</PackageId>");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>读取 csproj 中声明的 &lt;Version&gt;，未声明时返回 null。</summary>
    public static string ReadDeclaredVersion(FilePath projectPath)
    {
        var text = File.ReadAllText(projectPath.FullPath);
        var match = Regex.Match(text, @"<Version>\s*([^<]+?)\s*</Version>");
        return match.Success ? match.Groups[1].Value : null;
    }
}
[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // 使用 DotNetCoreClean 清理 Solution
        context.DotNetClean(BuildParameters.SolutionPath, new DotNetCleanSettings
        {
            Configuration = BuildParameters.Configuration
        });
        // 可选：清理 NuGet 包输出目录
        context.CleanDirectory(BuildParameters.PackageOutputDirectory);
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRestore(BuildParameters.SolutionPath);
    }
}

[TaskName("Compile")]
[IsDependentOn(typeof(RestoreTask))]
public class CompileTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        var settings = new DotNetBuildSettings
        {
            Configuration = BuildParameters.Configuration,
            NoRestore = true // 已经在 Restore Task 中完成
        };

        // 单包发布时只编译目标项目，其依赖链由 ProjectReference 自动带上
        if (context.HasPackage)
        {
            var project = context.ResolveProjects()[0];
            context.Information($"只编译目标项目：{project.GetFilenameWithoutExtension()}");
            context.DotNetBuild(project.FullPath, settings);
            return;
        }

        context.DotNetBuild(BuildParameters.SolutionPath, settings);
    }
}


[TaskName("Pack")]
[IsDependentOn(typeof(CompileTask))]
public class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // 各包版本完全由自身 csproj 的 <Version> 声明，不依赖 GitVersion / Git tag 推导。
        var projectFiles = context.ResolveProjects();

        context.Information($"待打包项目 {projectFiles.Count} 个：{string.Join(", ", projectFiles.Select(p => p.GetFilenameWithoutExtension()))}");

        if (context.HasVersion)
        {
            context.Information($"使用显式指定版本：{context.Arguments.GetArgument(BuildParameters.Version)}");
        }

        // 不吞异常：任一包打包失败都应让发布流程失败，避免产出残缺的包集合
        foreach (var projectPath in projectFiles)
        {
            // 单包 + 显式版本：tag 上写的版本必须与 csproj 一致，避免 tag 与源码版本漂移
            if (context.HasPackage && context.HasVersion)
            {
                var requested = context.Arguments.GetArgument(BuildParameters.Version);
                var declared = BuildContext.ReadDeclaredVersion(projectPath);
                if (!string.IsNullOrEmpty(declared) && !string.Equals(declared, requested, StringComparison.OrdinalIgnoreCase))
                {
                    throw new CakeException($"{projectPath.GetFilenameWithoutExtension()} 的 <Version> 是 '{declared}'，但标签指定了 '{requested}'；请先把 csproj 的版本改成 {requested} 再发版。");
                }
            }

            context.DotNetPack(projectPath.FullPath, new DotNetPackSettings
            {
                Configuration = BuildParameters.Configuration,
                OutputDirectory = BuildParameters.PackageOutputDirectory,
                NoBuild = true, // 已经在 Compile Task 中完成
                NoRestore = true,
                // 默认使用各 csproj 的 <Version>；传入 -Version 时覆盖
                ArgumentCustomization = args => context.HasVersion
                    ? args.Append($"-property:PackageVersion={context.Arguments.GetArgument(BuildParameters.Version)}")
                    : args
            });
        }
    }
}

[TaskName("Push")]
[IsDependentOn(typeof(PackTask))]
public class PushTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // 1. 定义 NuGet 源 URL
        // 使用 NuGet.org 作为示例

        var nugetSource = context.NugetSource;
        // 2. 检查 API Key
        if (string.IsNullOrWhiteSpace(context.NuGetApiKey))
        {
            context.Log.Error("NuGet API Key is missing. Skipping package publish.");
            // 如果缺少 Key，可以选择直接退出任务
            return;
        }

        // 3. 查找所有 .nupkg 文件
        var packages = context.GetFiles(BuildParameters.PackageOutputDirectory + "/*.nupkg").ToList();

        // 单包发布时只推送目标包，避免误推同目录下的其它产物
        if (context.HasPackage)
        {
            packages = packages
                .Where(p => p.GetFilenameWithoutExtension().ToString().StartsWith(context.Package + ".", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (packages.Count == 0)
        {
            throw new CakeException($"在 {BuildParameters.PackageOutputDirectory} 下没有找到待推送的 nupkg（Package='{context.Package}'）。");
        }

        context.Information($"Found {packages.Count} NuGet package(s) to publish.");

        // 4. 循环并推送每个包
        foreach (var package in packages)
        {
            context.Information($"Publishing package: {package.GetFilename()} Source {context.NugetSource}");

            context.DotNetNuGetPush(package.FullPath, new DotNetNuGetPushSettings
            {
                Source = nugetSource,
                ApiKey = context.NuGetApiKey,
                // 确保 Cake 使用了你定义的 API Key
                SkipDuplicate = true // 如果包已存在，跳过上传
            });
        }

        context.Information("NuGet packages published successfully.");
    }
}



// ---------------------- 5. Default/Main Task ----------------------

[TaskName("Default")]
[IsDependentOn(typeof(PackTask))] // 确保所有任务都完成
public sealed class DefaultTask : FrostingTask<BuildContext>
{
}
