using System.Reflection;
using System.Text.RegularExpressions;
using TUnit.Core;

namespace Framework.Logging.Tests.Templates;

/// <summary>
/// 守护「消息模板」与「字段常量」的一致性。
///
/// <para>
/// 这是本包最容易随时间腐化的地方：有人加了个模板，占位符随手写成 <c>{UserId}</c> 之外的名字，
/// 编译不报错、日志看起来也正常，但日志平台里就是查不到。这些测试把约定钉死在 CI 里。
/// </para>
/// </summary>
public class LogTemplateTests
{
	/// <summary>匹配 <c>{Name}</c> / <c>{@Name}</c> / <c>{$Name}</c> / <c>{Name:format}</c> / <c>{Name,5}</c>。</summary>
	static readonly Regex PlaceholderPattern = new(
		@"\{[@$]?(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:[,:][^{}]*)?\}",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	[Test]
	public async Task Templates_AreDiscovered()
	{
		// 反射失配（比如常量被改成 static readonly）时立刻失败，避免后面的测试空转通过。
		await Assert.That(AllTemplates().Count).IsGreaterThan(20);
	}

	[Test]
	public async Task EveryPlaceholder_IsDeclaredInLogProperties()
	{
		var declared = DeclaredProperties();
		var violations = new List<string>();

		foreach (var (name, template) in AllTemplates())
		{
			foreach (Match match in PlaceholderPattern.Matches(template))
			{
				var placeholder = match.Groups["name"].Value;
				if (!declared.Contains(placeholder))
				{
					violations.Add($"{name}: 占位符 {{{placeholder}}} 未在 LogProperties 中声明 —— {template}");
				}
			}
		}

		await Assert.That(violations).IsEmpty();
	}

	[Test]
	public async Task EveryPlaceholder_HasCaseExactMatch()
	{
		// Serilog 的属性名区分大小写：{UserId} 与 {UserID} 是两个不同的字段。
		var declared = DeclaredProperties();
		var lowerCased = declared.ToDictionary(name => name.ToUpperInvariant(), name => name, StringComparer.Ordinal);
		var violations = new List<string>();

		foreach (var (name, template) in AllTemplates())
		{
			foreach (Match match in PlaceholderPattern.Matches(template))
			{
				var placeholder = match.Groups["name"].Value;
				if (declared.Contains(placeholder)) continue;

				if (lowerCased.TryGetValue(placeholder.ToUpperInvariant(), out var expected))
				{
					violations.Add($"{name}: 占位符 {{{placeholder}}} 大小写与 {nameof(LogProperties)}.{expected} 不一致");
				}
			}
		}

		await Assert.That(violations).IsEmpty();
	}

	[Test]
	public async Task Templates_UseNamedPlaceholdersOnly()
	{
		// {0} 在 Serilog 里会被当成名为 "0" 的属性，等于丢失语义。
		var violations = AllTemplates()
			.Where(item => Regex.IsMatch(item.Template, @"\{[@$]?\d"))
			.Select(item => item.Name)
			.ToArray();

		await Assert.That(violations).IsEmpty();
	}

	[Test]
	public async Task Templates_AreAllNonEmpty()
	{
		var empty = AllTemplates().Where(item => string.IsNullOrWhiteSpace(item.Template)).Select(item => item.Name).ToArray();
		await Assert.That(empty).IsEmpty();
	}

	[Test]
	public async Task Templates_DoNotUseStringInterpolation()
	{
		// 模板必须是字面量，插值会立即求值，结构化字段全部丢失。
		// 常量字段天然不可能是插值，这里额外确认它们确实是编译期常量。
		var nonLiteral = typeof(LogTemplates)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(field => field.FieldType == typeof(string) && !field.IsLiteral)
			.Select(field => field.Name)
			.ToArray();

		await Assert.That(nonLiteral).IsEmpty();
	}

	[Test]
	[Arguments(nameof(LogTemplates.RequestCompleted), 4)]
	[Arguments(nameof(LogTemplates.DatabaseCommandCompleted), 3)]
	[Arguments(nameof(LogTemplates.OutboundCallRetrying), 3)]
	[Arguments(nameof(LogTemplates.ValidationFailed), 2)]
	public async Task KeyTemplates_HaveExpectedPlaceholderCount(string fieldName, int expected)
	{
		var template = (string)typeof(LogTemplates).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!
			.GetRawConstantValue()!;

		await Assert.That(PlaceholderPattern.Matches(template).Count).IsEqualTo(expected);
	}

	#region 反射辅助

	static IReadOnlyList<(string Name, string Template)> AllTemplates()
		=> typeof(LogTemplates)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(field => field.IsLiteral && field.FieldType == typeof(string))
			.Select(field => (field.Name, (string)field.GetRawConstantValue()!))
			.ToArray();

	static HashSet<string> DeclaredProperties()
		=> typeof(LogProperties)
			.GetFields(BindingFlags.Public | BindingFlags.Static)
			.Where(field => field.IsLiteral && field.FieldType == typeof(string))
			.Select(field => (string)field.GetRawConstantValue()!)
			.ToHashSet(StringComparer.Ordinal);

	#endregion
}
