using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>TOP 子句解析测试。</summary>
public sealed class TopClauseTests
{
    private static SqlSelectQuery SelectQuery(string sql) =>
        (SqlSelectQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task Top_WithCountAndStar_IsParsed()
    {
        var query = SelectQuery("select top 10 * from t");

        await Assert.That(query.Top).IsNotNull();
        await Assert.That(((SqlLiteral)query.Top!.Count!).Value).IsEqualTo("10");
        await Assert.That(query.Top.Percent).IsFalse();
        await Assert.That(query.Top.WithTies).IsFalse();

        // 验证 TOP 10 后面的 * 不会被当作乘法运算符吃掉。
        await Assert.That(query.Items.Length).IsEqualTo(1);
        await Assert.That(((SqlColumnRef)query.Items[0].Expression).IsStar).IsTrue();
    }

    [Test]
    public async Task Top_ParenthesizedPercentWithTies_IsParsed()
    {
        var query = SelectQuery("select top (5) percent with ties id from t");

        var top = query.Top!;
        await Assert.That(((SqlLiteral)top.Count!).Value).IsEqualTo("5");
        await Assert.That(top.Percent).IsTrue();
        await Assert.That(top.WithTies).IsTrue();
        await Assert.That(query.Items[0].Expression).IsTypeOf<SqlIdentifier>();
    }

    [Test]
    public async Task Top_AfterDistinct_IsParsed()
    {
        var query = SelectQuery("select distinct top 10 id from t");

        await Assert.That(query.Quantifier).IsEqualTo(SelectQuantifier.Distinct);
        await Assert.That(query.Top).IsNotNull();
    }

    [Test]
    public async Task Top_ParameterCount_IsParsed()
    {
        var query = SelectQuery("select top (@n) id from t");

        var parameter = (SqlParameterRef)query.Top!.Count!;
        await Assert.That(parameter.Prefix).IsEqualTo("@");
        await Assert.That(parameter.Name).IsEqualTo("n");
    }

    [Test]
    public async Task Top_PercentWithoutTies_IsParsed()
    {
        var query = SelectQuery("select top 10 percent id from t");

        await Assert.That(query.Top!.Percent).IsTrue();
        await Assert.That(query.Top.WithTies).IsFalse();
    }

    [Test]
    public async Task Top_WithoutCount_ReportsError()
    {
        var success = Sql.TryParse("select top from t", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
