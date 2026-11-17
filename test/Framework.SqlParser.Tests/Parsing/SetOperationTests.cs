using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>集合运算解析测试。</summary>
public sealed class SetOperationTests
{
    private static SqlQueryExpression ParseQuery(string sql, SqlDialect? dialect = null) =>
        ((SqlSelectStatement)(dialect is null ? Sql.Parse(sql) : Sql.Parse(sql, dialect))).Query;

    [Test]
    public async Task Union_BuildsBinarySetOperation()
    {
        var set = (SqlSetOperationQuery)ParseQuery("select 1 union select 2");

        await Assert.That(set.Operator).IsEqualTo(SqlSetOperator.Union);
        await Assert.That(set.Quantifier).IsEqualTo(SqlSetQuantifier.Default);
        await Assert.That(set.Left).IsTypeOf<SqlSelectQuery>();
        await Assert.That(set.Right).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task UnionAll_CapturesQuantifier()
    {
        var set = (SqlSetOperationQuery)ParseQuery("select 1 union all select 2");

        await Assert.That(set.Quantifier).IsEqualTo(SqlSetQuantifier.All);
    }

    [Test]
    public async Task UnionDistinct_CapturesQuantifier()
    {
        var set = (SqlSetOperationQuery)ParseQuery("select 1 union distinct select 2");

        await Assert.That(set.Quantifier).IsEqualTo(SqlSetQuantifier.Distinct);
    }

    [Test]
    public async Task ChainedUnion_IsLeftAssociative()
    {
        var outer = (SqlSetOperationQuery)ParseQuery("select 1 union all select 2 union select 3");

        await Assert.That(outer.Operator).IsEqualTo(SqlSetOperator.Union);
        await Assert.That(outer.Quantifier).IsEqualTo(SqlSetQuantifier.Default);

        var inner = (SqlSetOperationQuery)outer.Left;
        await Assert.That(inner.Quantifier).IsEqualTo(SqlSetQuantifier.All);
    }

    [Test]
    public async Task Intersect_BindsTighterThanUnion()
    {
        var union = (SqlSetOperationQuery)ParseQuery("select 1 union select 2 intersect select 3");

        await Assert.That(union.Operator).IsEqualTo(SqlSetOperator.Union);

        var intersect = (SqlSetOperationQuery)union.Right;
        await Assert.That(intersect.Operator).IsEqualTo(SqlSetOperator.Intersect);
    }

    [Test]
    public async Task ExceptAndMinus_MapToExcept()
    {
        var except = (SqlSetOperationQuery)ParseQuery("select 1 except select 2");
        await Assert.That(except.Operator).IsEqualTo(SqlSetOperator.Except);

        var minus = (SqlSetOperationQuery)ParseQuery("select 1 minus select 2");
        await Assert.That(minus.Operator).IsEqualTo(SqlSetOperator.Except);
    }

    [Test]
    public async Task ParenthesizedTerms_ArePreserved()
    {
        var set = (SqlSetOperationQuery)ParseQuery("(select 1) union (select 2)");

        await Assert.That(set.Left).IsTypeOf<SqlParenthesizedQuery>();
        await Assert.That(set.Right).IsTypeOf<SqlParenthesizedQuery>();
    }

    [Test]
    public async Task OrderByAndLimit_ApplyToWholeSet()
    {
        var set = (SqlSetOperationQuery)ParseQuery("select 1 union select 2 order by 1 limit 3");

        await Assert.That(set.OrderBy).IsNotNull();
        await Assert.That(set.OrderBy!.Items.Length).IsEqualTo(1);
        await Assert.That(((SqlLiteral)set.Limit!.Count).Value).IsEqualTo("3");
    }

    [Test]
    public async Task SetOperation_WorksInDerivedTable()
    {
        var query = (SqlSelectQuery)ParseQuery("select * from (select 1 union select 2) x");

        var derived = (SqlDerivedTable)query.From[0];
        await Assert.That(derived.Query).IsTypeOf<SqlSetOperationQuery>();
    }

    [Test]
    public async Task SetOperation_WorksInInPredicate()
    {
        var query = (SqlSelectQuery)ParseQuery("select * from t where id in (select 1 union select 2)");

        var inExpression = (SqlInExpression)query.Where!;
        var subquery = (SqlSubqueryExpression)inExpression.Values[0];
        await Assert.That(subquery.Query).IsTypeOf<SqlSetOperationQuery>();
    }

    [Test]
    public async Task UnionWithoutRightOperand_ReportsError()
    {
        var success = Sql.TryParse("select 1 union", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Severity == SqlDiagnosticSeverity.Error)).IsTrue();
    }
}
