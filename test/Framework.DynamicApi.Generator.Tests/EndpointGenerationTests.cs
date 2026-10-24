using TUnit.Core;

namespace Framework.DynamicApi.Generator.Tests;

public class EndpointGenerationTests
{
	private const string SampleSource = """
		using System.Threading.Tasks;
		using Framework.DynamicApi;
		using Framework.DynamicApi.Attributes;

		namespace Sample;

		[DynamicApi]
		public class UserService : IDynamicApi
		{
			public Task<UserDto> GetUserAsync(long id) => Task.FromResult(new UserDto());

			public Task<long> CreateUserAsync(CreateUserRequest request) => Task.FromResult(1L);

			public Task DeleteUserAsync(long id) => Task.CompletedTask;

			[NonDynamicMethod]
			public Task HiddenAsync() => Task.CompletedTask;
		}

		public class UserDto { }

		public class CreateUserRequest { }
		""";

	[Test]
	public async Task GeneratesOneSourcePerServicePlusAssemblyEntry()
	{
		var result = GeneratorTestHelper.Run(SampleSource);

		await Assert.That(result.GeneratedSources.Any(s => s.HintName == "DynamicApi_Sample_UserService.cs")).IsTrue();
		await Assert.That(result.GeneratedSources.Any(s => s.HintName == "DynamicApiEndpoints_Sample.cs")).IsTrue();
	}

	[Test]
	public async Task MapsVerbsFromMethodNamePrefix()
	{
		var generated = GeneratorTestHelper.GetGeneratedText(GeneratorTestHelper.Run(SampleSource), "UserService");

		await Assert.That(generated).Contains("app.MapGet(route0,");
		await Assert.That(generated).Contains("app.MapPost(route1,");
		await Assert.That(generated).Contains("app.MapDelete(route2,");
	}

	[Test]
	public async Task BindsSimpleParametersFromQueryAndComplexOnesFromBody()
	{
		var generated = GeneratorTestHelper.GetGeneratedText(GeneratorTestHelper.Run(SampleSource), "UserService");

		await Assert.That(generated).Contains("long.TryParse(__ctx.Request.Query[\"id\"].ToString(), out var id)");
		await Assert.That(generated).Contains("DynamicApiBodyReader.ReadAsync<global::Sample.CreateUserRequest>(__ctx)");
	}

	[Test]
	public async Task InjectsServiceAndWrapsResultWithUnifiedResponse()
	{
		var generated = GeneratorTestHelper.GetGeneratedText(GeneratorTestHelper.Run(SampleSource), "UserService");

		await Assert.That(generated).Contains("GetRequiredService<global::Sample.UserService>()");
		await Assert.That(generated).Contains("DynamicApiResponseWriter.WriteWrappedAsync(__ctx, __result)");
		await Assert.That(generated).Contains("ProducesResponseTypeMetadata(200, typeof(global::Framework.DynamicApi.Response.ResponseResul<global::Sample.UserDto>)");
	}

	[Test]
	public async Task EmitsRequestDelegateInsteadOfBindingDelegate()
	{
		var generated = GeneratorTestHelper.GetGeneratedText(GeneratorTestHelper.Run(SampleSource), "UserService");

		await Assert.That(generated).Contains("(global::Microsoft.AspNetCore.Http.RequestDelegate)(async (global::Microsoft.AspNetCore.Http.HttpContext __ctx) =>");
	}

	[Test]
	public async Task SkipsMethodsMarkedNonDynamic()
	{
		var generated = GeneratorTestHelper.GetGeneratedText(GeneratorTestHelper.Run(SampleSource), "UserService");

		await Assert.That(generated).DoesNotContain("HiddenAsync");
	}

	[Test]
	public async Task EmitsAssemblyEntryPointInAssemblySpecificNamespace()
	{
		var generated = GeneratorTestHelper.GetGeneratedText(GeneratorTestHelper.Run(SampleSource), "DynamicApiEndpoints_Sample");

		await Assert.That(generated).Contains("namespace Framework.DynamicApi.Generated.Sample");
		await Assert.That(generated).Contains("public static class DynamicApiEndpoints");
		await Assert.That(generated).Contains("DynamicApi_Sample_UserService.Map(app, options)");
	}

	[Test]
	public async Task SkipsTypesNotImplementingMarkerInterface()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi.Attributes;

			namespace Sample;

			[DynamicApi]
			public class NotAService
			{
				public Task<int> GetAsync() => Task.FromResult(1);
			}
			""");

		await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
	}

	[Test]
	public async Task SkipsTypesMarkedNonDynamicApi()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;

			namespace Sample;

			[DynamicApi]
			[NonDynamicApi]
			public class ExcludedService : IDynamicApi
			{
				public Task<int> GetAsync() => Task.FromResult(1);
			}
			""");

		await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
	}
}
