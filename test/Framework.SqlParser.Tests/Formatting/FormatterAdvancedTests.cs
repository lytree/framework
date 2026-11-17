using Framework.SqlParser.Ast;
using Framework.SqlParser.Formatting;

namespace Framework.SqlParser.Tests;

/// <summary>Formatter 选项、批量、节点重载与方言分页测试。</summary>
public sealed class FormatterAdvancedTests
{
    [Test]
    public async Task KeywordCase_Lower_PreservesIdentifiersAndStrings()
    {
        var options = new SqlFormatterOptions { KeywordCase = SqlKeywordCase.Lower };
        var text = SqlFormatter.Format(
            "select A, B from T where A = 'From' order by A",
            options: options);

        await Assert.That(text).IsEqualTo(
            "select A, B\n"
            + "from T\n"
            + "where A = 'From'\n"
            + "order by A");
    }

    [Test]
    public async Task KeywordCase_Preserve_KeepsDeparserOutput()
    {
        var options = new SqlFormatterOptions { KeywordCase = SqlKeywordCase.Preserve };
        var text = SqlFormatter.Format("select a from t", options: options);

        await Assert.That(text).IsEqualTo("SELECT a\nFROM t");
    }

    [Test]
    public async Task NewLineBeforeFrom_Disabled_KeepsSelectAndFromTogether()
    {
        var options = new SqlFormatterOptions { NewLineBeforeFrom = false };
        var text = SqlFormatter.Format("select a, b from t where a = 1", options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT a, b FROM t\n"
            + "WHERE a = 1");
    }

    [Test]
    public async Task AlignSelectItems_BreaksEachItem()
    {
        var options = new SqlFormatterOptions { AlignSelectItems = true };
        var text = SqlFormatter.Format(
            "select a, b as x, count(*) as c from t",
            options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT\n"
            + "  a,\n"
            + "  b AS x,\n"
            + "  count(*) AS c\n"
            + "FROM t");
    }

    [Test]
    public async Task AlignSelectItems_WithDistinct_KeepsModifierOnSelectLine()
    {
        var options = new SqlFormatterOptions { AlignSelectItems = true };
        var text = SqlFormatter.Format("select distinct a, b from t", options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT DISTINCT\n"
            + "  a,\n"
            + "  b\n"
            + "FROM t");
    }

    [Test]
    public async Task AlignSelectItems_CommaFirst_PutsCommaAtLineStart()
    {
        var options = new SqlFormatterOptions { AlignSelectItems = true, CommaFirst = true };
        var text = SqlFormatter.Format("select a, b from t", options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT\n"
            + "  a\n"
            + "  , b\n"
            + "FROM t");
    }

    [Test]
    public async Task AlignSelectItems_KeepsSubqueryInline()
    {
        var options = new SqlFormatterOptions { AlignSelectItems = true };
        var text = SqlFormatter.Format(
            "select (select max(x) from u) as m, a from t",
            options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT\n"
            + "  (SELECT max(x) FROM u) AS m,\n"
            + "  a\n"
            + "FROM t");
    }

    [Test]
    public async Task FormatBatch_FormatsEachStatement()
    {
        var text = SqlFormatter.FormatBatch(
            "select 1; select a from t where a > 0;");

        await Assert.That(text).IsEqualTo(
            "SELECT 1;\n"
            + "SELECT a\n"
            + "FROM t\n"
            + "WHERE a > 0;");
    }

    [Test]
    public async Task FormatBatch_IgnoresSemicolonInsideString()
    {
        var text = SqlFormatter.FormatBatch("select 'a;b' as x");

        await Assert.That(text).IsEqualTo("SELECT 'a;b' AS x;");
    }

    [Test]
    public async Task FormatBatch_IgnoresSemicolonInsideComment()
    {
        var text = SqlFormatter.FormatBatch("select 1; -- a;b\nselect 2");

        await Assert.That(text).IsEqualTo("SELECT 1;\nSELECT 2;");
    }

    [Test]
    public async Task FormatQuery_OmitsStatementLevelWith()
    {
        var statement = (SqlSelectStatement)Sql.Parse("with c as (select 1) select * from c");
        var text = SqlFormatter.Format(statement.Query);

        await Assert.That(text).IsEqualTo(
            "SELECT *\n"
            + "FROM c");
    }

    [Test]
    public async Task FormatTableSource_ProducesSingleLine()
    {
        var statement = (SqlSelectStatement)Sql.Parse("select * from users u");
        var query = (SqlSelectQuery)statement.Query;
        var text = SqlFormatter.Format(query.From[0]);

        await Assert.That(text).IsEqualTo("users AS u");
    }

    [Test]
    public async Task Dialect_LimitToTSql_BecomesTop()
    {
        var options = new SqlFormatterOptions { Dialect = SqlDialect.TSql };
        var text = SqlFormatter.Format(
            "select * from t order by a limit 10",
            options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT TOP (10) *\n"
            + "FROM t\n"
            + "ORDER BY a");
    }

    [Test]
    public async Task Dialect_OffsetFetchToMySql_BecomesLimit()
    {
        var options = new SqlFormatterOptions { Dialect = SqlDialect.MySql };
        var text = SqlFormatter.Format(
            "select * from t order by a offset 5 rows fetch next 10 rows only",
            options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT *\n"
            + "FROM t\n"
            + "ORDER BY a\n"
            + "LIMIT 10\n"
            + "OFFSET 5");
    }

    [Test]
    public async Task Dialect_TopToMySql_BecomesLimit()
    {
        var options = new SqlFormatterOptions { Dialect = SqlDialect.MySql };
        var text = SqlFormatter.Format(
            "select top (5) id from t",
            SqlDialect.TSql,
            options);

        await Assert.That(text).IsEqualTo(
            "SELECT id\n"
            + "FROM t\n"
            + "LIMIT 5");
    }

    [Test]
    public async Task Dialect_LimitOffsetToTSql_BecomesOffsetFetch()
    {
        var options = new SqlFormatterOptions { Dialect = SqlDialect.TSql };
        var text = SqlFormatter.Format(
            "select * from t limit 10 offset 5",
            SqlDialect.MySql,
            options);

        await Assert.That(text).IsEqualTo(
            "SELECT *\n"
            + "FROM t\n"
            + "OFFSET 5 ROWS\n"
            + "FETCH NEXT 10 ROWS ONLY");
    }

    [Test]
    public async Task Dialect_NullKeepsOriginalPagination()
    {
        var text = SqlFormatter.Format("select * from t limit 10");

        await Assert.That(text).IsEqualTo(
            "SELECT *\n"
            + "FROM t\n"
            + "LIMIT 10");
    }

    [Test]
    public async Task SqlFacade_FormatAndFormatBatch()
    {
        await Assert.That(Sql.Format("select a from t")).IsEqualTo("SELECT a\nFROM t");
        await Assert.That(Sql.FormatBatch("select 1; select 2")).IsEqualTo("SELECT 1;\nSELECT 2;");
    }
}
