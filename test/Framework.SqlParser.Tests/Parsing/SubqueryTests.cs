using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>子查询与量化比较解析测试。</summary>
public sealed class SubqueryTests
{
    private static SqlSelectQuery SelectQuery(string sql) =>
        (SqlSelectQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task ScalarSubquery_InProjection_IsParsed()
    {
        var query = SelectQuery("select (select max(x) from t) as m from u");

        var subquery = (SqlSubqueryExpression)query.Items[0].Expression;
        await Assert.That(subquery.Query).IsTypeOf<SqlSelectQuery>();
        await Assert.That(query.Items[0].Alias!.Value).IsEqualTo("m");
    }

    [Test]
    public async Task CorrelatedExistsSubquery_IsParsed()
    {
        var query = SelectQuery(
            "select * from t where exists (select 1 from u where u.id = t.id)");

        var exists = (SqlExistsExpression)query.Where!;
        var inner = (SqlSelectQuery)exists.Query;
        await Assert.That(inner.Where).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task QuantifiedComparisonAny_IsParsed()
    {
        var query = SelectQuery("select * from t where x = any (select y from u)");

        var comparison = (SqlBinaryExpression)query.Where!;
        await Assert.That(comparison.Operator).IsEqualTo(SqlBinaryOperator.Equal);

        var quantified = (SqlQuantifiedSubqueryExpression)comparison.Right;
        await Assert.That(quantified.Quantifier).IsEqualTo(SqlQuantifier.Any);
        await Assert.That(quantified.Query).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task QuantifiedComparisonSomeAndAll_AreParsed()
    {
        var some = SelectQuery("select * from t where x = some (select y from u)");
        var someComparison = (SqlBinaryExpression)some.Where!;
        await Assert.That(((SqlQuantifiedSubqueryExpression)someComparison.Right).Quantifier)
            .IsEqualTo(SqlQuantifier.Some);

        var all = SelectQuery("select * from t where x > all (select y from u)");
        var allComparison = (SqlBinaryExpression)all.Where!;
        await Assert.That(allComparison.Operator).IsEqualTo(SqlBinaryOperator.GreaterThan);
        await Assert.That(((SqlQuantifiedSubqueryExpression)allComparison.Right).Quantifier)
            .IsEqualTo(SqlQuantifier.All);
    }

    [Test]
    public async Task LateralDerivedTable_IsFlagged()
    {
        var query = SelectQuery("select * from t, lateral (select * from u where u.id = t.id) x");

        await Assert.That(query.From.Length).IsEqualTo(2);
        var derived = (SqlDerivedTable)query.From[1];
        await Assert.That(derived.Lateral).IsTrue();
        await Assert.That(derived.Alias!.Value).IsEqualTo("x");
    }

    [Test]
    public async Task JoinLateral_IsParsed()
    {
        var query = SelectQuery("select * from t join lateral (select * from u) x on true");

        var join = (SqlJoinedTable)query.From[0];
        await Assert.That(join.JoinType).IsEqualTo(SqlJoinType.Inner);

        var derived = (SqlDerivedTable)join.Right;
        await Assert.That(derived.Lateral).IsTrue();
    }

    [Test]
    public async Task QuantifiedComparisonRequiresSubquery()
    {
        var success = Sql.TryParse("select * from t where x = any (1, 2)", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Severity == SqlDiagnosticSeverity.Error)).IsTrue();
    }
}
