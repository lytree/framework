using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>UNNEST 表函数解析测试。</summary>
public sealed class UnnestTests
{
    private static SqlSelectQuery SelectQuery(string sql) =>
        (SqlSelectQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task Unnest_ParsesArgumentsAndAliasColumns()
    {
        var query = SelectQuery("select * from unnest(arr) as u(x)");

        var unnest = (SqlUnnestTable)query.From[0];
        await Assert.That(unnest.Arguments.Length).IsEqualTo(1);
        await Assert.That(unnest.Alias!.Value).IsEqualTo("u");
        await Assert.That(unnest.ColumnAliases.Length).IsEqualTo(1);
        await Assert.That(unnest.ColumnAliases[0].Value).IsEqualTo("x");
        await Assert.That(unnest.WithOrdinality).IsFalse();
    }

    [Test]
    public async Task Unnest_WithOrdinalityAndMultipleArguments_IsParsed()
    {
        var query = SelectQuery("select * from unnest(a, b) with ordinality as u");

        var unnest = (SqlUnnestTable)query.From[0];
        await Assert.That(unnest.Arguments.Length).IsEqualTo(2);
        await Assert.That(unnest.WithOrdinality).IsTrue();
        await Assert.That(unnest.Alias!.Value).IsEqualTo("u");
    }

    [Test]
    public async Task Unnest_InCrossJoin_IsParsed()
    {
        var query = SelectQuery("select * from t cross join unnest(t.tags) tag");

        var join = (SqlJoinedTable)query.From[0];
        await Assert.That(join.JoinType).IsEqualTo(SqlJoinType.Cross);

        var unnest = (SqlUnnestTable)join.Right;
        await Assert.That(unnest.Alias!.Value).IsEqualTo("tag");
        await Assert.That(unnest.Arguments[0]).IsTypeOf<SqlColumnRef>();
    }

    [Test]
    public async Task Unnest_WithoutArguments_ReportsError()
    {
        var success = Sql.TryParse("select * from unnest()", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
