using TUnit.Core;

namespace Framework.DynamicApi.Tests;

public class RouteBuildingTests
{
	[Test]
	[Arguments("api", "", "user", "get-user", "api/user/get-user")]
	[Arguments("api", "admin", "user", "list", "api/admin/user/list")]
	[Arguments("", "", "user", "get", "/user/get")]
	[Arguments("api", "", "user", "", "api/user/")]
	public async Task BuildRoute_JoinsSegmentsAndNormalizesDoubleSlash(
		string prefix, string area, string controller, string action, string expected)
	{
		await Assert.That(DynamicApiRouteResolver.BuildRoute(prefix, area, controller, action)).IsEqualTo(expected);
	}
}
