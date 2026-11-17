using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>VALUES 查询解析测试。</summary>
public sealed class ValuesQueryTests
{
    private static SqlValuesQuery ParseValues(string sql) =>
        (SqlValuesQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task MultipleRows_AreParsed()
    {
        var values = ParseValues("values (1, 'a'), (2, 'b')");

        await Assert.That(values.Rows.Length).IsEqualTo(2);
        await Assert.That(values.Rows[0].Values.Length).IsEqualTo(2);
        await Assert.That(((SqlLiteral)values.Rows[0].Values[0]).Value).IsEqualTo("1");
        await Assert.That(((SqlLiteral)values.Rows[1].Values[1]).Value).IsEqualTo("b");
    }

    [Test]
    public async Task SingleValueRow_IsParsed()
    {
        var values = ParseValues("values (1)");

        await Assert.That(values.Rows.Length).IsEqualTo(1);
        await Assert.That(values.Rows[0].Values.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ValuesWithExpressions_AreParsed()
    {
        var values = ParseValues("values (1 + 1)");

        await Assert.That(values.Rows[0].Values[0]).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task ValuesInDerivedTable_ParsesAliasAndColumns()
    {
        var statement = (SqlSelectStatement)Sql.Parse(
            "select * from (values (1, 2), (3, 4)) as v(a, b)");

        var query = (SqlSelectQuery)statement.Query;
        var derived = (SqlDerivedTable)query.From[0];

        await Assert.That(derived.Query).IsTypeOf<SqlValuesQuery>();
        await Assert.That(derived.Alias!.Value).IsEqualTo("v");
        await Assert.That(derived.ColumnAliases.Length).IsEqualTo(2);
    }

    [Test]
    public async Task EmptyRow_ReportsSyntaxError()
    {
        var success = Sql.TryParse("values ()", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task ValuesWithoutParentheses_ReportsSyntaxError()
    {
        var success = Sql.TryParse("values 1, 2", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
