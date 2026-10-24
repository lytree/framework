using TUnit.Core;

namespace Framework.DynamicApi.Generator.Tests;

public class DiagnosticTests
{
	[Test]
	public async Task ComplexParameterOnGet_ReportsYLDAPI003AndSkipsEndpoint()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;

			namespace Sample;

			[DynamicApi]
			public class ReportService : IDynamicApi
			{
				public Task<string> QueryAsync(ReportFilter filter) => Task.FromResult(string.Empty);
			}

			public class ReportFilter { public int Page { get; set; } }
			""");

		await Assert.That(result.Diagnostics.Any(d => d.Id == "YLDAPI003")).IsTrue();
		await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
	}

	[Test]
	public async Task MvcRoutedType_ReportsYLDAPI001AndSkips()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;
			using Microsoft.AspNetCore.Mvc;

			namespace Sample;

			[DynamicApi]
			[Route("custom")]
			public class RoutedService : IDynamicApi
			{
				public Task<int> GetAsync() => Task.FromResult(1);
			}
			""");

		await Assert.That(result.Diagnostics.Any(d => d.Id == "YLDAPI001")).IsTrue();
		await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
	}

	[Test]
	public async Task UnknownVerbFallsBackToPost()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;

			namespace Sample;

			[DynamicApi]
			public class SyncService : IDynamicApi
			{
				public Task SyncAsync() => Task.CompletedTask;
			}
			""");

		var generated = GeneratorTestHelper.GetGeneratedText(result, "SyncService");

		await Assert.That(generated).Contains("app.MapPost(");
	}

	[Test]
	public async Task NonFormatResultDisablesWrapping()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;

			namespace Sample;

			[DynamicApi]
			public class PingService : IDynamicApi
			{
				[NonFormatResult]
				public Task<int> GetPingAsync() => Task.FromResult(1);
			}
			""");

		var generated = GeneratorTestHelper.GetGeneratedText(result, "PingService");

		await Assert.That(generated).Contains("ProducesResponseTypeMetadata(200, typeof(int)");
		await Assert.That(generated).DoesNotContain("WriteWrappedAsync");
		await Assert.That(generated).Contains("DynamicApiResponseWriter.WriteAsync(__ctx, __result)");
	}

	[Test]
	public async Task ExplicitBindingAttributeIsPreserved()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;
			using Microsoft.AspNetCore.Mvc;

			namespace Sample;

			[DynamicApi]
			public class RouteService : IDynamicApi
			{
				public Task<string> GetAsync([FromRoute] long id) => Task.FromResult(string.Empty);
			}
			""");

		var generated = GeneratorTestHelper.GetGeneratedText(result, "RouteService");

		await Assert.That(generated).Contains("__ctx.Request.RouteValues[\"id\"]");
		await Assert.That(generated).Contains("long.TryParse(");
	}

	[Test]
	public async Task UnsupportedParameterKindReportsYLDAPI006AndSkipsEndpoint()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;
			using Microsoft.AspNetCore.Http;

			namespace Sample;

			[DynamicApi]
			public class UploadService : IDynamicApi
			{
				public Task<string> UploadAsync(IFormFile file) => Task.FromResult(file.FileName);
			}
			""");

		await Assert.That(result.Diagnostics.Any(d => d.Id == "YLDAPI006")).IsTrue();
		await Assert.That(result.GeneratedSources.Length).IsEqualTo(0);
	}

	[Test]
	public async Task IResultReturnExecutesDirectly()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;
			using Microsoft.AspNetCore.Http;

			namespace Sample;

			[DynamicApi]
			public class ResultService : IDynamicApi
			{
				public Task<IResult> GetStatusAsync() => Task.FromResult(Results.Ok("ok"));
			}
			""");

		var generated = GeneratorTestHelper.GetGeneratedText(result, "ResultService");

		await Assert.That(generated).Contains("await __result.ExecuteAsync(__ctx)");
		await Assert.That(generated).DoesNotContain("ProducesResponseTypeMetadata");
	}

	[Test]
	public async Task VoidTaskReturnWritesEmptyResponse()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;

			namespace Sample;

			[DynamicApi]
			public class JobService : IDynamicApi
			{
				public Task DeleteJobAsync(long id) => Task.CompletedTask;
			}
			""");

		var generated = GeneratorTestHelper.GetGeneratedText(result, "JobService");

		await Assert.That(generated).Contains("await __service.DeleteJobAsync(id);");
		await Assert.That(generated).Contains("DynamicApiResponseWriter.WriteEmpty(__ctx)");
	}

	[Test]
	public async Task DuplicateControllerAndActionAcrossNamespaces_ReportsYLDAPI004()
	{
		var result = GeneratorTestHelper.Run("""
			using System.Threading.Tasks;
			using Framework.DynamicApi;
			using Framework.DynamicApi.Attributes;

			namespace A
			{
				[DynamicApi]
				public class UserService : IDynamicApi
				{
					public Task<int> GetAsync() => Task.FromResult(1);
				}
			}

			namespace B
			{
				[DynamicApi]
				public class UserService : IDynamicApi
				{
					public Task<int> GetAsync(long id) => Task.FromResult(1);
				}
			}
			""");

		await Assert.That(result.Diagnostics.Any(d => d.Id == "YLDAPI004")).IsTrue();
	}
}
