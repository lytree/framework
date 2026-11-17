using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>MERGE 语句解析测试。</summary>
public sealed class MergeTests
{
    [Test]
    public async Task Merge_MatchedUpdate_IsParsed()
    {
        var merge = (SqlMergeStatement)Sql.Parse(
            "merge into t using s on t.id = s.id when matched then update set v = s.v");

        await Assert.That(merge.Target.Name.Value).IsEqualTo("t");
        await Assert.That(merge.Source).IsTypeOf<SqlNamedTable>();
        await Assert.That(merge.On).IsTypeOf<SqlBinaryExpression>();
        await Assert.That(merge.Clauses.Length).IsEqualTo(1);

        var clause = merge.Clauses[0];
        await Assert.That(clause.MatchKind).IsEqualTo(SqlMergeMatchKind.Matched);

        var update = (SqlMergeUpdateAction)clause.Action;
        await Assert.That(update.Assignments.Length).IsEqualTo(1);
        await Assert.That(update.Assignments[0].Column.Value).IsEqualTo("v");
    }

    [Test]
    public async Task Merge_NotMatchedInsert_IsParsed()
    {
        var merge = (SqlMergeStatement)Sql.Parse(
            "merge into t using s on t.id = s.id when not matched then insert (id, v) values (s.id, s.v)");

        var clause = merge.Clauses[0];
        await Assert.That(clause.MatchKind).IsEqualTo(SqlMergeMatchKind.NotMatchedByTarget);

        var insert = (SqlMergeInsertAction)clause.Action;
        await Assert.That(insert.Columns.Length).IsEqualTo(2);
        await Assert.That(insert.Values.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Merge_NotMatchedBySourceDelete_IsParsed()
    {
        var merge = (SqlMergeStatement)Sql.Parse(
            "merge into t using s on t.id = s.id when not matched by source then delete");

        var clause = merge.Clauses[0];
        await Assert.That(clause.MatchKind).IsEqualTo(SqlMergeMatchKind.NotMatchedBySource);
        await Assert.That(clause.Action).IsTypeOf<SqlMergeDeleteAction>();
    }

    [Test]
    public async Task Merge_MultipleClausesAndCondition_AreParsed()
    {
        var merge = (SqlMergeStatement)Sql.Parse(
            "merge into t using s on t.id = s.id "
            + "when matched and s.flag = 1 then delete "
            + "when not matched by target then insert (id) values (s.id)");

        await Assert.That(merge.Clauses.Length).IsEqualTo(2);
        await Assert.That(merge.Clauses[0].Condition).IsTypeOf<SqlBinaryExpression>();
        await Assert.That(merge.Clauses[0].Action).IsTypeOf<SqlMergeDeleteAction>();
        await Assert.That(merge.Clauses[1].Action).IsTypeOf<SqlMergeInsertAction>();
    }

    [Test]
    public async Task Merge_WithDerivedSource_IsParsed()
    {
        var merge = (SqlMergeStatement)Sql.Parse(
            "merge into t using (select * from s) x on t.id = x.id when matched then delete");

        var source = (SqlDerivedTable)merge.Source;
        await Assert.That(source.Alias!.Value).IsEqualTo("x");
        await Assert.That(source.Query).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task Merge_WithoutWhen_ReportsError()
    {
        var success = Sql.TryParse("merge into t using s on t.id = s.id", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task Merge_WithoutOn_ReportsError()
    {
        var success = Sql.TryParse(
            "merge into t using s when matched then delete",
            out _,
            out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Severity == SqlDiagnosticSeverity.Error)).IsTrue();
    }
}
