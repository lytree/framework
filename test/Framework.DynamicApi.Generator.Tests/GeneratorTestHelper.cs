using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Framework.DynamicApi.Generator.Tests;

/// <summary>
/// 直接以 Roslyn GeneratorDriver 运行生成器，用于断言生成代码与诊断。
/// </summary>
internal static class GeneratorTestHelper
{
	private static readonly MetadataReference[] References = LoadReferences();

	public static GeneratorRunResult Run(
		string source,
		string assemblyName = "Sample",
		IReadOnlyDictionary<string, string>? globalOptions = null,
		params string[] extraSources)
	{
		var trees = new List<SyntaxTree>
		{
			CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))
		};

		trees.AddRange(extraSources.Select(extra => CSharpSyntaxTree.ParseText(extra, new CSharpParseOptions(LanguageVersion.Latest))));

		var compilation = CSharpCompilation.Create(
			assemblyName,
			trees,
			References,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

		var driver = CSharpGeneratorDriver.Create(
			new[] { new DynamicApiGenerator().AsSourceGenerator() },
			additionalTexts: null,
			parseOptions: null,
			optionsProvider: new TestAnalyzerConfigOptionsProvider(globalOptions));

		return driver.RunGenerators(compilation).GetRunResult().Results.Single();
	}

	public static string GetGeneratedText(GeneratorRunResult result, string hintNameFragment)
	{
		var source = result.GeneratedSources.SingleOrDefault(s => s.HintName.Contains(hintNameFragment));

		if (source.SourceText is null)
			throw new InvalidOperationException($"未找到包含 '{hintNameFragment}' 的生成源，实际为：{string.Join(", ", result.GeneratedSources.Select(s => s.HintName))}");

		return source.SourceText.ToString();
	}

	private static MetadataReference[] LoadReferences()
	{
		var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

		var paths = new List<string>(trustedPlatformAssemblies.Split(Path.PathSeparator).Where(path => path.Length > 0));
		paths.Add(typeof(Framework.DynamicApi.IDynamicApi).Assembly.Location);

		return paths
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Select(path => MetadataReference.CreateFromFile(path))
			.ToArray();
	}

	private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
	{
		private readonly AnalyzerConfigOptions _global;

		public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string>? options)
		{
			_global = new TestAnalyzerConfigOptions(options ?? new Dictionary<string, string>());
		}

		public override AnalyzerConfigOptions GlobalOptions => _global;

		public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;

		public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
	}

	private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
	{
		public static readonly TestAnalyzerConfigOptions Empty = new(new Dictionary<string, string>());

		private readonly IReadOnlyDictionary<string, string> _options;

		public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> options) => _options = options;

		public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _options.TryGetValue(key, out value);
	}
}
