using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>UNPIVOT 解析测试。</summary>
public sealed class UnpivotTests
{
    private static SqlSelectQuery SelectQuery(string sql) =>
        (SqlSelectQuery)((SqlSelectStatement)Sql.Parse(sql)).Query;

    [Test]
    public async Task Unpivot_ParsesValueForAndInColumns()
    {
        var query = SelectQuery(
            "select * from sales unpivot (amount for month in (jan, feb, mar)) as u");

        var unpivot = (SqlUnpivotTable)query.From[0];
        await Assert.That(unpivot.Source).IsTypeOf<SqlNamedTable>();
        await Assert.That(unpivot.ValueColumn.Value).IsEqualTo("amount");
        await Assert.That(unpivot.ForColumn.Value).IsEqualTo("month");
        await Assert.That(unpivot.InColumns.Length).IsEqualTo(3);
        await Assert.That(unpivot.InColumns[2].Value).IsEqualTo("mar");
        await Assert.That(unpivot.Alias!.Value).IsEqualTo("u");
    }

    [Test]
    public async Task Unpivot_OnDerivedTable_IsParsed()
    {
        var query = SelectQuery(
            "select * from (select jan, feb from sales) s unpivot (v for k in (jan, feb)) u");

        var unpivot = (SqlUnpivotTable)query.From[0];
        var derived = (SqlDerivedTable)unpivot.Source;
        await Assert.That(derived.Alias!.Value).IsEqualTo("s");
        await Assert.That(unpivot.Alias!.Value).IsEqualTo("u");
    }

    [Test]
    public async Task Unpivot_EmptyInList_ReportsError()
    {
        var success = Sql.TryParse(
            "select * from t unpivot (v for k in ()) p",
            out _,
            out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
