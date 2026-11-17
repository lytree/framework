using Framework.SqlParser.Formatting;

namespace Framework.SqlParser.Tests;

/// <summary>SqlFormatter 多行格式化测试。</summary>
public sealed class FormatterTests
{
    [Test]
    public async Task Select_BreaksTopLevelClauses()
    {
        var text = SqlFormatter.Format("select a, b from t where a = 1 and b = 2 order by a");

        await Assert.That(text).IsEqualTo(
            "SELECT a, b\n"
            + "FROM t\n"
            + "WHERE a = 1 AND b = 2\n"
            + "ORDER BY a");
    }

    [Test]
    public async Task Joins_AreIndented()
    {
        var text = SqlFormatter.Format(
            "select u.id from users u left join orders o on u.id = o.user_id where o.id is null");

        await Assert.That(text).IsEqualTo(
            "SELECT u.id\n"
            + "FROM users AS u\n"
            + "  LEFT JOIN orders AS o ON u.id = o.user_id\n"
            + "WHERE o.id IS NULL");
    }

    [Test]
    public async Task Joins_AreNotIndented_WhenDisabled()
    {
        var options = new SqlFormatterOptions { IndentJoins = false };
        var text = SqlFormatter.Format(
            "select * from t cross apply (select * from u) x",
            options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT *\n"
            + "FROM t\n"
            + "CROSS APPLY (SELECT * FROM u) AS x");
    }

    [Test]
    public async Task IndentSize_IsApplied()
    {
        var options = new SqlFormatterOptions { IndentSize = 4 };
        var text = SqlFormatter.Format(
            "select * from a join b on a.id = b.id",
            options: options);

        await Assert.That(text).IsEqualTo(
            "SELECT *\n"
            + "FROM a\n"
            + "    INNER JOIN b ON a.id = b.id");
    }

    [Test]
    public async Task CustomNewLine_IsUsed()
    {
        var options = new SqlFormatterOptions { NewLine = "\r\n" };
        var text = SqlFormatter.Format("select a from t", options: options);

        await Assert.That(text).IsEqualTo("SELECT a\r\nFROM t");
    }

    [Test]
    public async Task Insert_BreaksIntoValuesAndReturning()
    {
        var text = SqlFormatter.Format("insert into t (a, b) values (1, 'x') returning id");

        await Assert.That(text).IsEqualTo(
            "INSERT INTO t (a, b)\n"
            + "VALUES (1, 'x')\n"
            + "RETURNING id");
    }

    [Test]
    public async Task Update_BreaksSetAndWhere()
    {
        var text = SqlFormatter.Format("update t set a = 1, b = 2 where id = 3");

        await Assert.That(text).IsEqualTo(
            "UPDATE t\n"
            + "SET a = 1, b = 2\n"
            + "WHERE id = 3");
    }

    [Test]
    public async Task Cte_KeepsSubqueryOnSameLine()
    {
        var text = SqlFormatter.Format("with c as (select id from users) select * from c where id > 0");

        await Assert.That(text).IsEqualTo(
            "WITH c AS (SELECT id FROM users)\n"
            + "SELECT *\n"
            + "FROM c\n"
            + "WHERE id > 0");
    }

    [Test]
    public async Task DerivedTable_StaysInline()
    {
        var text = SqlFormatter.Format("select * from (select id from users) u where id > 0");

        await Assert.That(text).IsEqualTo(
            "SELECT *\n"
            + "FROM (SELECT id FROM users) AS u\n"
            + "WHERE id > 0");
    }

    [Test]
    public async Task Merge_KeepsActionOnWhenLine()
    {
        var text = SqlFormatter.Format(
            "merge into t using s on t.id = s.id "
            + "when matched then update set v = s.v "
            + "when not matched then insert (id, v) values (s.id, s.v)");

        await Assert.That(text).IsEqualTo(
            "MERGE INTO t USING s ON t.id = s.id\n"
            + "WHEN MATCHED THEN UPDATE SET v = s.v\n"
            + "WHEN NOT MATCHED THEN INSERT (id, v) VALUES (s.id, s.v)");
    }

    [Test]
    public async Task CaseExpression_IsNotBroken()
    {
        var text = SqlFormatter.Format(
            "select case when a = 1 then 'x' else 'y' end as c from t");

        await Assert.That(text).IsEqualTo(
            "SELECT CASE WHEN a = 1 THEN 'x' ELSE 'y' END AS c\n"
            + "FROM t");
    }

    [Test]
    public async Task KeywordsInsideString_AreIgnored()
    {
        var text = SqlFormatter.Format("select 'a) where b' as x from t");

        await Assert.That(text).IsEqualTo(
            "SELECT 'a) where b' AS x\n"
            + "FROM t");
    }

    [Test]
    public async Task Ddl_StaysOnSingleLine()
    {
        var text = SqlFormatter.Format("create table t (id int not null, primary key (id))");

        await Assert.That(text).IsEqualTo("CREATE TABLE t (id int NOT NULL, PRIMARY KEY (id))");
    }

    [Test]
    public async Task Expression_IsSingleLine()
    {
        var text = SqlFormatter.Format(Sql.ParseExpression("a and b or c"));

        await Assert.That(text).IsEqualTo("a AND b OR c");
    }

    [Test]
    public async Task Format_IsIdempotent()
    {
        const string sql = "with c as (select id from users) select u.id "
            + "from c u left join orders o on u.id = o.user_id where o.id is null order by u.id";

        var once = SqlFormatter.Format(sql);
        var twice = SqlFormatter.Format(once);

        await Assert.That(twice).IsEqualTo(once);
    }
}
