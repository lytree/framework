using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>WITH + DML 解析测试。</summary>
public sealed class WithDmlTests
{
    [Test]
    public async Task With_Update_IsParsed()
    {
        var statement = (SqlUpdateStatement)Sql.Parse(
            "with cte as (select 1) update t set a = 1 where id in (select * from cte)");

        await Assert.That(statement.With).IsNotNull();
        await Assert.That(statement.With!.Ctes.Length).IsEqualTo(1);
        await Assert.That(statement.Where).IsTypeOf<SqlInExpression>();
    }

    [Test]
    public async Task With_Delete_IsParsed()
    {
        var statement = (SqlDeleteStatement)Sql.Parse(
            "with cte as (select 1) delete from t where id in (select * from cte)");

        await Assert.That(statement.With).IsNotNull();
        await Assert.That(statement.With!.Ctes[0].Name.Value).IsEqualTo("cte");
        await Assert.That(statement.Where).IsTypeOf<SqlInExpression>();
    }

    [Test]
    public async Task With_Merge_IsParsed()
    {
        var merge = (SqlMergeStatement)Sql.Parse(
            "with cte as (select 1) merge into t using cte on t.id = cte.id when matched then delete");

        await Assert.That(merge.With).IsNotNull();
        await Assert.That(merge.Clauses.Length).IsEqualTo(1);
        await Assert.That(merge.Clauses[0].Action).IsTypeOf<SqlMergeDeleteAction>();
    }

    [Test]
    public async Task WithDml_StatementSpan_IncludesWith()
    {
        const string sql = "with cte as (select 1) delete from t";
        var statement = (SqlDeleteStatement)Sql.Parse(sql);

        await Assert.That(statement.Span.Start).IsEqualTo(0);
        await Assert.That(statement.Span.Length).IsEqualTo(sql.Length);
    }

    [Test]
    public async Task With_UnsupportedStatement_ReportsError()
    {
        var success = Sql.TryParse("with cte as (select 1) create table x (id int)", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
