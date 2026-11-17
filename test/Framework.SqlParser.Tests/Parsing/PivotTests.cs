using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>PIVOT 解析测试。</summary>
public sealed class PivotTests
{
    private static SqlSelectQuery SelectQuery(string sql, SqlDialect? dialect = null) =>
        (SqlSelectQuery)((SqlSelectStatement)(dialect is null ? Sql.Parse(sql) : Sql.Parse(sql, dialect))).Query;

    [Test]
    public async Task Pivot_ParsesAggregateForAndInList()
    {
        var query = SelectQuery(
            "select * from sales pivot (sum(amount) for month in ([Jan], [Feb])) as p",
            SqlDialect.TSql);

        var pivot = (SqlPivotTable)query.From[0];
        await Assert.That(pivot.Source).IsTypeOf<SqlNamedTable>();

        var aggregate = (SqlFunctionCall)pivot.Aggregate;
        await Assert.That(aggregate.Name.Value).IsEqualTo("sum");

        await Assert.That(pivot.ForColumn.Value).IsEqualTo("month");
        await Assert.That(pivot.Values.Length).IsEqualTo(2);
        await Assert.That(((SqlIdentifier)pivot.Values[0].Value).Value).IsEqualTo("Jan");
        await Assert.That(pivot.Alias!.Value).IsEqualTo("p");
    }

    [Test]
    public async Task Pivot_ParsesItemAliases()
    {
        var query = SelectQuery("select * from t pivot (count(*) for k in (1 as one, 2 as two)) p");

        var pivot = (SqlPivotTable)query.From[0];
        await Assert.That(((SqlFunctionCall)pivot.Aggregate).Star).IsTrue();
        await Assert.That(pivot.Values[0].Alias!.Value).IsEqualTo("one");
        await Assert.That(pivot.Values[1].Alias!.Value).IsEqualTo("two");
    }

    [Test]
    public async Task Pivot_OnDerivedTable_IsParsed()
    {
        var query = SelectQuery(
            "select * from (select region, amount from sales) s pivot (sum(amount) for region in ('east')) p");

        var pivot = (SqlPivotTable)query.From[0];
        var derived = (SqlDerivedTable)pivot.Source;
        await Assert.That(derived.Alias!.Value).IsEqualTo("s");
        await Assert.That(pivot.Alias!.Value).IsEqualTo("p");
    }

    [Test]
    public async Task Pivot_WithoutIn_ReportsError()
    {
        var success = Sql.TryParse(
            "select * from t pivot (sum(x) for k (1)) p",
            out _,
            out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
