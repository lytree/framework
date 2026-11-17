using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>CROSS APPLY / OUTER APPLY 解析测试。</summary>
public sealed class ApplyTests
{
    private static SqlSelectQuery SelectQuery(string sql) =>
        (SqlSelectQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task CrossApply_ParsesDerivedTableAsLateral()
    {
        var query = SelectQuery(
            "select * from t cross apply (select top 1 * from u where u.id = t.id) x");

        var join = (SqlJoinedTable)query.From[0];
        await Assert.That(join.JoinType).IsEqualTo(SqlJoinType.CrossApply);

        var derived = (SqlDerivedTable)join.Right;
        await Assert.That(derived.Lateral).IsTrue();
        await Assert.That(derived.Alias!.Value).IsEqualTo("x");
        await Assert.That(derived.Query).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task OuterApply_IsParsed()
    {
        var query = SelectQuery("select * from t outer apply (select * from u) x");

        var join = (SqlJoinedTable)query.From[0];
        await Assert.That(join.JoinType).IsEqualTo(SqlJoinType.OuterApply);
    }

    [Test]
    public async Task CrossApply_WithValuesQuery_IsParsed()
    {
        var query = SelectQuery("select * from t cross apply (values (1), (2)) v(n)");

        var join = (SqlJoinedTable)query.From[0];
        await Assert.That(join.JoinType).IsEqualTo(SqlJoinType.CrossApply);

        var derived = (SqlDerivedTable)join.Right;
        await Assert.That(derived.Query).IsTypeOf<SqlValuesQuery>();
        await Assert.That(derived.ColumnAliases.Length).IsEqualTo(1);
    }

    [Test]
    public async Task CrossJoin_IsNotMistakenForApply()
    {
        var query = SelectQuery("select * from a cross join b");

        var join = (SqlJoinedTable)query.From[0];
        await Assert.That(join.JoinType).IsEqualTo(SqlJoinType.Cross);
    }
}
