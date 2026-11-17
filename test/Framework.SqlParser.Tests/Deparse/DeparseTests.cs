using Framework.SqlParser.Ast;
using Framework.SqlParser.Deparse;

namespace Framework.SqlParser.Tests;

/// <summary>Deparser 回写与往返测试。</summary>
public sealed class DeparseTests
{
    [Test]
    public async Task Select_IsDeparsed()
    {
        const string sql = "select u.id, u.name from db.users u join orders o on u.id = o.user_id "
            + "where u.active = 1 order by u.name desc limit 10";

        var text = SqlDeparser.Deparse(Sql.Parse(sql));

        await Assert.That(text.Contains("FROM db.users AS u INNER JOIN orders AS o")).IsTrue();
        await Assert.That(text.Contains("WHERE u.active = 1")).IsTrue();
        await Assert.That(text.Contains("ORDER BY u.name DESC LIMIT 10")).IsTrue();
        await Assert.That(Sql.Parse(text)).IsTypeOf<SqlSelectStatement>();
    }

    [Test]
    public async Task Expression_PrecedenceIsPreserved()
    {
        await Assert.That(SqlDeparser.Deparse(Sql.ParseExpression("1 + 2 * 3"))).IsEqualTo("1 + 2 * 3");
        await Assert.That(SqlDeparser.Deparse(Sql.ParseExpression("(1 + 2) * 3"))).IsEqualTo("(1 + 2) * 3");
        await Assert.That(SqlDeparser.Deparse(Sql.ParseExpression("a or b and c"))).IsEqualTo("a OR b AND c");
        await Assert.That(SqlDeparser.Deparse(Sql.ParseExpression("not (a and b)"))).IsEqualTo("NOT(a AND b)");
    }

    [Test]
    public async Task WindowFunction_IsDeparsed()
    {
        const string sql = "select sum(x) over (partition by a order by b rows between 1 preceding and current row) as s from t";
        var text = SqlDeparser.Deparse(Sql.Parse(sql));

        await Assert.That(text.Contains(
            "sum(x) OVER (PARTITION BY a ORDER BY b ROWS BETWEEN 1 PRECEDING AND CURRENT ROW) AS s")).IsTrue();
    }

    [Test]
    public async Task Dml_IsDeparsed()
    {
        await Assert.That(SqlDeparser.Deparse(Sql.Parse(
            "insert into t (a, b) values (1, 'x') returning id")))
            .IsEqualTo("INSERT INTO t (a, b) VALUES (1, 'x') RETURNING id");

        await Assert.That(SqlDeparser.Deparse(Sql.Parse("update t set a = 1 where id = 2")))
            .IsEqualTo("UPDATE t SET a = 1 WHERE id = 2");

        await Assert.That(SqlDeparser.Deparse(Sql.Parse("delete from t where id = 2")))
            .IsEqualTo("DELETE FROM t WHERE id = 2");
    }

    [Test]
    public async Task Ddl_IsDeparsed()
    {
        const string sql = "create table t (id int not null, name varchar(50) default 'x', primary key (id))";
        var text = SqlDeparser.Deparse(Sql.Parse(sql));

        await Assert.That(text).IsEqualTo(
            "CREATE TABLE t (id int NOT NULL, name varchar(50) DEFAULT 'x', PRIMARY KEY (id))");
    }

    [Test]
    public async Task Deparse_IsIdempotent()
    {
        var statements = new[]
        {
            "select 1",
            "select * from t",
            "select distinct top (5) a, b from t where a is not null and b between 1 and 2 order by a",
            "select u.id from users u left join orders o on u.id = o.user_id where o.id is null",
            "with cte (x) as (select 1) select * from cte",
            "select 1 union all select 2 except select 3",
            "values (1, 'a'), (2, 'b')",
            "select sum(x) over (partition by a order by b rows 1 preceding) from t",
            "select case when a = 1 then 'x' else 'y' end from t",
            "select cast(a as decimal(10, 2)) from t",
            "select * from t where id in (select id from u) and exists (select 1 from v)",
            "insert into t(a) values (1) returning a",
            "update t set a = 1, b = b + 1 where id = 2",
            "delete from t where id = 2 returning id",
            "merge into t using s on t.id = s.id when matched then update set v = s.v "
                + "when not matched then insert (id) values (s.id)",
            "create table if not exists t (id int primary key, name varchar(20) unique)",
            "drop table if exists a, b cascade",
            "truncate table a, b",
            "select * from t pivot (sum(x) for k in (1, 2)) p",
            "select * from t unpivot (v for k in (a, b)) u",
            "select * from t cross apply (select * from u) x",
            "select * from unnest(arr) with ordinality as u(x, i)",
        };

        foreach (var sql in statements)
        {
            var first = SqlDeparser.Deparse(Sql.Parse(sql));
            var second = SqlDeparser.Deparse(Sql.Parse(first));
            await Assert.That(second).IsEqualTo(first);
        }
    }
}
