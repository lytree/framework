using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>聚合函数 FILTER 与 WITHIN GROUP 解析测试。</summary>
public sealed class AggregateClauseTests
{
    private static SqlSelectQuery SelectQuery(string sql) =>
        (SqlSelectQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task Filter_ParsesWhereCondition()
    {
        var query = SelectQuery("select count(*) filter (where x > 0) from t");

        var call = (SqlFunctionCall)query.Items[0].Expression;
        await Assert.That(call.Star).IsTrue();
        await Assert.That(call.Filter).IsNotNull();

        var condition = (SqlBinaryExpression)call.Filter!.Condition;
        await Assert.That(condition.Operator).IsEqualTo(SqlBinaryOperator.GreaterThan);
    }

    [Test]
    public async Task Filter_CanCombineWithOver()
    {
        var query = SelectQuery(
            "select sum(x) filter (where x is not null) over (partition by g) from t");

        var call = (SqlFunctionCall)query.Items[0].Expression;
        await Assert.That(call.Filter).IsNotNull();
        await Assert.That(((SqlIsNullExpression)call.Filter!.Condition).Negated).IsTrue();

        await Assert.That(call.Over).IsNotNull();
        await Assert.That(call.Over!.PartitionBy.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Filter_WithoutWhere_ReportsError()
    {
        var success = Sql.TryParse("select count(*) filter (x > 0) from t", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task WithinGroup_ParsesOrderBy()
    {
        var query = SelectQuery("select percentile_cont(0.5) within group (order by x) from t");

        var call = (SqlFunctionCall)query.Items[0].Expression;
        await Assert.That(call.Arguments.Length).IsEqualTo(1);
        await Assert.That(call.WithinGroup).IsNotNull();
        await Assert.That(call.WithinGroup!.Items.Length).IsEqualTo(1);
    }

    [Test]
    public async Task WithinGroup_ParsesMultipleOrderItems()
    {
        var query = SelectQuery(
            "select percentile_disc(0.5) within group (order by x desc nulls first, y) from t");

        var items = ((SqlFunctionCall)query.Items[0].Expression).WithinGroup!.Items;
        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items[0].Direction).IsEqualTo(SqlSortOrder.Descending);
        await Assert.That(items[0].Nulls).IsEqualTo(SqlNullsOrder.First);
    }

    [Test]
    public async Task WithinGroupFilterAndOver_CanCombine()
    {
        var query = SelectQuery(
            "select percentile_cont(0.5) within group (order by x) filter (where x > 0) over (partition by g) from t");

        var call = (SqlFunctionCall)query.Items[0].Expression;
        await Assert.That(call.WithinGroup).IsNotNull();
        await Assert.That(call.Filter).IsNotNull();
        await Assert.That(call.Over).IsNotNull();
    }

    [Test]
    public async Task WithinGroup_WithoutOrderBy_ReportsError()
    {
        var success = Sql.TryParse("select f() within group (x) from t", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
