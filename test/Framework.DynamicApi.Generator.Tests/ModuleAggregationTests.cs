using TUnit.Core;

namespace Framework.DynamicApi.Generator.Tests;

public class ModuleAggregationTests
{
	private const string WebServiceSource = """
		using System.Threading.Tasks;
		using Framework.DynamicApi;
		using Framework.DynamicApi.Attributes;

		namespace WebApp;

		[DynamicApi]
		public class OrderService : IDynamicApi
		{
			public Task<int> GetOrderAsync(long id) => Task.FromResult(1);
		}
		""";

	private const string FakeLibraryModule = """
		namespace Framework.DynamicApi.Generated.MyLib
		{
			public static class DynamicApiEndpoints
			{
				public static void MapDynamicApis() { }
			}
		}
		""";

	private static Dictionary<string, string> Modules(string value) => new()
	{
		["build_property.DynamicApiModules"] = value
	};

	[Test]
	public async Task DeclaredModuleIsAggregatedIntoAssemblyEntry()
	{
		var result = GeneratorTestHelper.Run(
			WebServiceSource,
			"WebApp",
			Modules("MyLib"),
			FakeLibraryModule);

		var generated = GeneratorTestHelper.GetGeneratedText(result, "DynamicApiEndpoints_WebApp");

		await Assert.That(generated)
			.Contains("global::Framework.DynamicApi.Generated.MyLib.DynamicApiEndpoints.MapDynamicApis(app, options);");
		await Assert.That(result.Diagnostics.Any(d => d.Id == "YLDAPI005")).IsFalse();
	}

	[Test]
	public async Task MissingModuleEntryReportsYLDAPI005AndIsSkipped()
	{
		var result = GeneratorTestHelper.Run(WebServiceSource, "WebApp", Modules("MissingLib"));

		var generated = GeneratorTestHelper.GetGeneratedText(result, "DynamicApiEndpoints_WebApp");

		await Assert.That(result.Diagnostics.Any(d => d.Id == "YLDAPI005")).IsTrue();
		await Assert.That(generated).DoesNotContain("Generated.MissingLib");
	}

	[Test]
	public async Task ModuleNamespaceIsSanitizedFromDottedAssemblyName()
	{
		var result = GeneratorTestHelper.Run(
			WebServiceSource,
			"WebApp",
			Modules("MyLib.Core"),
			"""
			namespace Framework.DynamicApi.Generated.MyLib_Core
			{
				public static class DynamicApiEndpoints
				{
					public static void MapDynamicApis() { }
				}
			}
			""");

		var generated = GeneratorTestHelper.GetGeneratedText(result, "DynamicApiEndpoints_WebApp");

		await Assert.That(generated)
			.Contains("global::Framework.DynamicApi.Generated.MyLib_Core.DynamicApiEndpoints.MapDynamicApis(app, options);");
	}
}
