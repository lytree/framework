using Framework.DynamicApi.Enums;
using TUnit.Core;

namespace Framework.DynamicApi.Tests;

public class NamingConventionTests
{
	[Test]
	[Arguments("UserService", NamingConventionEnum.KebabCase, "user-service")]
	[Arguments("UserService", NamingConventionEnum.SnakeCase, "user_service")]
	[Arguments("UserService", NamingConventionEnum.ExtensionCase, "user.service")]
	[Arguments("UserService", NamingConventionEnum.PascalCase, "UserService")]
	[Arguments("UserService", NamingConventionEnum.CamelCase, "userService")]
	[Arguments("UserService", NamingConventionEnum.Custom, "UserService")]
	public async Task GetFormatName_FollowsNamingConvention(string value, NamingConventionEnum convention, string expected)
	{
		await Assert.That(DynamicApiRouteResolver.GetFormatName(value, convention)).IsEqualTo(expected);
	}

	[Test]
	[Arguments("GetUserListAsync", "get-user-list-async")]
	[Arguments("IDCard", "id-card")]
	[Arguments("", "")]
	public async Task GetSeparateWords_SplitsOnCaseBoundaries(string value, string expected)
	{
		await Assert.That(DynamicApiRouteResolver.GetSeparateWords(value)).IsEqualTo(expected);
	}

	[Test]
	public async Task FormatControllerName_RemovesPostfixAndFormats()
	{
		var name = DynamicApiRouteResolver.FormatControllerName(
			"UserService",
			new List<string> { "AppService", "Service" },
			NamingConventionEnum.KebabCase);

		await Assert.That(name).IsEqualTo("user");
	}

	[Test]
	public async Task FormatControllerName_RemovesFirstMatchingPostfix()
	{
		var name = DynamicApiRouteResolver.FormatControllerName(
			"UserAppService",
			new List<string> { "AppService", "Service" },
			NamingConventionEnum.KebabCase);

		await Assert.That(name).IsEqualTo("user");
	}

	[Test]
	public async Task FormatControllerName_CustomUsesFormatterOnStrippedName()
	{
		var name = DynamicApiRouteResolver.FormatControllerName(
			"UserService",
			new List<string> { "Service" },
			NamingConventionEnum.Custom,
			stripped => $"svc-{stripped}");

		await Assert.That(name).IsEqualTo("svc-User");
	}

	[Test]
	public async Task FormatControllerName_CustomWithoutFormatterKeepsStrippedName()
	{
		var name = DynamicApiRouteResolver.FormatControllerName(
			"UserService",
			new List<string> { "Service" },
			NamingConventionEnum.Custom);

		await Assert.That(name).IsEqualTo("User");
	}

	[Test]
	public async Task FormatActionName_RemovesPostfixAndFormats()
	{
		var name = DynamicApiRouteResolver.FormatActionName(
			"GetUserAsync",
			new List<string> { "Async" },
			NamingConventionEnum.KebabCase);

		await Assert.That(name).IsEqualTo("get-user");
	}

	[Test]
	public async Task FormatActionName_CustomStripsVerbPrefixWhenFormatterReturnsNull()
	{
		var name = DynamicApiRouteResolver.FormatActionName(
			"GetUserAsync",
			new List<string> { "Async" },
			NamingConventionEnum.Custom,
			httpVerbs: DynamicApiRouteResolver.DefaultHttpVerbs);

		await Assert.That(name).IsEqualTo("User");
	}

	[Test]
	public async Task FormatActionName_CustomReturnsEmptyWhenOnlyVerbRemains()
	{
		var name = DynamicApiRouteResolver.FormatActionName(
			"GetAsync",
			new List<string> { "Async" },
			NamingConventionEnum.Custom,
			httpVerbs: DynamicApiRouteResolver.DefaultHttpVerbs);

		await Assert.That(name).IsEqualTo(string.Empty);
	}

	[Test]
	public async Task FormatActionName_CustomFormatterReceivesOriginalName()
	{
		var name = DynamicApiRouteResolver.FormatActionName(
			"GetUserAsync",
			new List<string> { "Async" },
			NamingConventionEnum.Custom,
			_ => "detail");

		await Assert.That(name).IsEqualTo("detail");
	}

	[Test]
	public async Task FormatActionName_EmptyInputIsReturnedAsIs()
	{
		await Assert.That(DynamicApiRouteResolver.FormatActionName(string.Empty)).IsEqualTo(string.Empty);
	}
}

