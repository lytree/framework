using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>FETCH 子句解析测试。</summary>
public sealed class FetchClauseTests
{
    private static SqlSelectQuery SelectQuery(string sql, SqlDialect? dialect = null) =>
        (SqlSelectQuery)((SqlSelectStatement)(dialect is null ? Sql.Parse(sql) : Sql.Parse(sql, dialect))).Query;

    [Test]
    public async Task OffsetAndFetch_AreParsed()
    {
        var query = SelectQuery("select * from t order by id offset 10 rows fetch first 5 rows only");

        await Assert.That(((SqlLiteral)query.Offset!.Count).Value).IsEqualTo("10");

        var fetch = query.Fetch!;
        await Assert.That(((SqlLiteral)fetch.Count!).Value).IsEqualTo("5");
        await Assert.That(fetch.WithTies).IsFalse();
        await Assert.That(fetch.Percent).IsFalse();
    }

    [Test]
    public async Task FetchNextRow_IsParsed()
    {
        var query = SelectQuery("select * from t order by id fetch next 1 row only");

        await Assert.That(((SqlLiteral)query.Fetch!.Count!).Value).IsEqualTo("1");
    }

    [Test]
    public async Task FetchFirstRowOnly_LeavesCountNull()
    {
        var query = SelectQuery("select * from t order by id fetch first row only");

        await Assert.That(query.Fetch).IsNotNull();
        await Assert.That(query.Fetch!.Count).IsNull();
    }

    [Test]
    public async Task FetchWithTies_IsCaptured()
    {
        var query = SelectQuery("select * from t order by id fetch first 5 rows with ties");

        await Assert.That(query.Fetch!.WithTies).IsTrue();
    }

    [Test]
    public async Task FetchPercent_IsCaptured()
    {
        var query = SelectQuery("select * from t order by id fetch first 10 percent rows only");

        var fetch = query.Fetch!;
        await Assert.That(fetch.Percent).IsTrue();
        await Assert.That(((SqlLiteral)fetch.Count!).Value).IsEqualTo("10");
    }

    [Test]
    public async Task FetchWithoutOnlyOrWithTies_ReportsError()
    {
        var success = Sql.TryParse("select * from t fetch first 5 rows", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
