using System.Collections.Generic;
using Framework.DynamicApi.Filters;
using Framework.DynamicApi.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using TUnit.Core;

namespace Framework.DynamicApi.Tests;

public class FormatResultFilterTests
{
	private sealed class TestInvocationContext : EndpointFilterInvocationContext
	{
		public override HttpContext HttpContext { get; } = new DefaultHttpContext();

		public override IList<object?> Arguments { get; } = Array.Empty<object?>();

		public override T GetArgument<T>(int index) => (T)Arguments[index]!;
	}

	private static ValueTask<object?> Result(object? value) => ValueTask.FromResult(value);

	[Test]
	public async Task WrapsPayloadIntoResponseResul()
	{
		var filter = new FormatResultFilter<int>();

		var result = await filter.InvokeAsync(new TestInvocationContext(), _ => Result(42));

		await Assert.That(result).IsTypeOf<Ok<ResponseResul<int>>>();
		var ok = (Ok<ResponseResul<int>>)result!;
		await Assert.That(ok.Value!.Data).IsEqualTo(42);
		await Assert.That(ok.Value!.Success).IsTrue();
	}

	[Test]
	public async Task WrapsNullPayload()
	{
		var filter = new FormatResultFilter<string>();

		var result = await filter.InvokeAsync(new TestInvocationContext(), _ => Result(null));

		await Assert.That(result).IsTypeOf<Ok<ResponseResul<string>>>();
		await Assert.That(((Ok<ResponseResul<string>>)result!).Value!.Data).IsNull();
	}

	[Test]
	public async Task PassesThroughIResult()
	{
		var filter = new FormatResultFilter<int>();
		var notFound = Results.NotFound();

		var result = await filter.InvokeAsync(new TestInvocationContext(), _ => Result(notFound));

		await Assert.That(result).IsSameReferenceAs(notFound);
	}

	[Test]
	public async Task PassesThroughAlreadyWrappedPayload()
	{
		var filter = new FormatResultFilter<int>();
		var wrapped = new ResponseResul<int>(7);

		var result = await filter.InvokeAsync(new TestInvocationContext(), _ => Result(wrapped));

		await Assert.That(result).IsSameReferenceAs(wrapped);
	}

	[Test]
	public async Task PassesThroughUnrelatedPayloadType()
	{
		var filter = new FormatResultFilter<int>();
		var text = "not-an-int";

		var result = await filter.InvokeAsync(new TestInvocationContext(), _ => Result(text));

		await Assert.That(result).IsSameReferenceAs(text);
	}
}
