using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>窗口函数解析测试。</summary>
public sealed class WindowFunctionTests
{
    private static SqlSelectQuery SelectQuery(string sql) =>
        (SqlSelectQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task Over_ParsesPartitionAndOrderBy()
    {
        var query = SelectQuery(
            "select row_number() over (partition by dept order by salary desc) as rn from emp");

        var call = (SqlFunctionCall)query.Items[0].Expression;
        await Assert.That(call.Over).IsNotNull();

        var over = call.Over!;
        await Assert.That(over.Name).IsNull();
        await Assert.That(over.PartitionBy.Length).IsEqualTo(1);
        await Assert.That(over.OrderBy!.Items.Length).IsEqualTo(1);
        await Assert.That(over.OrderBy.Items[0].Direction).IsEqualTo(SqlSortOrder.Descending);
        await Assert.That(over.Frame).IsNull();
    }

    [Test]
    public async Task Over_ParsesRowsBetweenFrame()
    {
        var query = SelectQuery(
            "select sum(x) over (order by id rows between 2 preceding and current row) from t");

        var over = ((SqlFunctionCall)query.Items[0].Expression).Over!;
        var frame = over.Frame!;

        await Assert.That(frame.Unit).IsEqualTo(SqlWindowFrameUnit.Rows);
        await Assert.That(frame.Start.BoundKind).IsEqualTo(SqlWindowFrameBoundKind.Preceding);
        await Assert.That(((SqlLiteral)frame.Start.Value!).Value).IsEqualTo("2");
        await Assert.That(frame.End).IsNotNull();
        await Assert.That(frame.End!.BoundKind).IsEqualTo(SqlWindowFrameBoundKind.CurrentRow);
    }

    [Test]
    public async Task Over_ParsesRangeUnboundedFrame()
    {
        var query = SelectQuery(
            "select sum(x) over (order by id range between unbounded preceding and unbounded following) from t");

        var frame = ((SqlFunctionCall)query.Items[0].Expression).Over!.Frame!;

        await Assert.That(frame.Unit).IsEqualTo(SqlWindowFrameUnit.Range);
        await Assert.That(frame.Start.BoundKind).IsEqualTo(SqlWindowFrameBoundKind.UnboundedPreceding);
        await Assert.That(frame.End!.BoundKind).IsEqualTo(SqlWindowFrameBoundKind.UnboundedFollowing);
    }

    [Test]
    public async Task Over_ParsesGroupsFrame()
    {
        var query = SelectQuery(
            "select sum(x) over (groups between 1 preceding and 1 following) from t");

        var frame = ((SqlFunctionCall)query.Items[0].Expression).Over!.Frame!;

        await Assert.That(frame.Unit).IsEqualTo(SqlWindowFrameUnit.Groups);
        await Assert.That(frame.Start.BoundKind).IsEqualTo(SqlWindowFrameBoundKind.Preceding);
        await Assert.That(frame.End!.BoundKind).IsEqualTo(SqlWindowFrameBoundKind.Following);
        await Assert.That(((SqlLiteral)frame.End.Value!).Value).IsEqualTo("1");
    }

    [Test]
    public async Task Over_SingleFrameBound_LeavesEndNull()
    {
        var query = SelectQuery("select sum(x) over (rows unbounded preceding) from t");

        var frame = ((SqlFunctionCall)query.Items[0].Expression).Over!.Frame!;

        await Assert.That(frame.Unit).IsEqualTo(SqlWindowFrameUnit.Rows);
        await Assert.That(frame.Start.BoundKind).IsEqualTo(SqlWindowFrameBoundKind.UnboundedPreceding);
        await Assert.That(frame.End).IsNull();
    }

    [Test]
    public async Task Over_EmptyParentheses_IsParsed()
    {
        var query = SelectQuery("select count(*) over () from t");

        var over = ((SqlFunctionCall)query.Items[0].Expression).Over!;
        await Assert.That(over.Name).IsNull();
        await Assert.That(over.PartitionBy.Length).IsEqualTo(0);
        await Assert.That(over.OrderBy).IsNull();
        await Assert.That(over.Frame).IsNull();
    }

    [Test]
    public async Task Over_NamedWindowReference_IsCaptured()
    {
        var query = SelectQuery("select sum(x) over w from t window w as (partition by a)");

        var over = ((SqlFunctionCall)query.Items[0].Expression).Over!;
        await Assert.That(over.Name!.Value).IsEqualTo("w");

        await Assert.That(query.Windows).IsNotNull();
        var window = query.Windows!.Windows[0];
        await Assert.That(window.Name.Value).IsEqualTo("w");
        await Assert.That(window.Specification.PartitionBy.Length).IsEqualTo(1);
    }

    [Test]
    public async Task WindowClause_ParsesMultipleDefinitions()
    {
        var query = SelectQuery(
            "select sum(x) over w1, sum(y) over w2 from t window w1 as (partition by a), w2 as (order by b)");

        var windows = query.Windows!;
        await Assert.That(windows.Windows.Length).IsEqualTo(2);
        await Assert.That(windows.Windows[1].Name.Value).IsEqualTo("w2");
        await Assert.That(windows.Windows[1].Specification.OrderBy!.Items.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Over_BaseWindowWithExtraOrderBy_IsParsed()
    {
        var query = SelectQuery(
            "select sum(x) over (w order by b) from t window w as (partition by a)");

        var over = ((SqlFunctionCall)query.Items[0].Expression).Over!;
        await Assert.That(over.Name!.Value).IsEqualTo("w");
        await Assert.That(over.OrderBy!.Items.Length).IsEqualTo(1);
    }

    [Test]
    public async Task Qualify_CanReferenceWindowExpression()
    {
        var query = SelectQuery(
            "select id, row_number() over (partition by g order by id) as rn from t qualify rn = 1");

        await Assert.That(query.Qualify).IsTypeOf<SqlBinaryExpression>();
    }
}
