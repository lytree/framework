using Framework.DynamicApi.Response;
using TUnit.Core;

namespace Framework.DynamicApi.Tests;

public class ResponseResulTests
{
	[Test]
	public async Task Constructor_SetsPayloadAndDefaults()
	{
		var response = new ResponseResul<int>(42);

		await Assert.That(response.Data).IsEqualTo(42);
		await Assert.That(response.Success).IsTrue();
		await Assert.That(response.Code).IsNull();
		await Assert.That(response.Msg).IsNull();
	}

	[Test]
	public async Task Constructor_AcceptsFailureMetadata()
	{
		var response = new ResponseResul<string>("x", success: false, code: "E01", msg: "failed");

		await Assert.That(response.Success).IsFalse();
		await Assert.That(response.Code).IsEqualTo("E01");
		await Assert.That(response.Msg).IsEqualTo("failed");
	}
}
