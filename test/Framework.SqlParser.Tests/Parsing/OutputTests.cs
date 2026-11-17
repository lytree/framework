using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>OUTPUT 子句解析测试。</summary>
public sealed class OutputTests
{
    [Test]
    public async Task Insert_Output_IsParsedBeforeValues()
    {
        var statement = (SqlInsertStatement)Sql.Parse(
            "insert into t (a) output inserted.id, inserted.a as new_a values (1)");

        var output = statement.Output!;
        await Assert.That(output.Items.Length).IsEqualTo(2);

        var column = (SqlColumnRef)output.Items[0].Expression;
        await Assert.That(column.Parts.Length).IsEqualTo(2);
        await Assert.That(column.Parts[0].Value).IsEqualTo("inserted");
        await Assert.That(output.Items[1].Alias!.Value).IsEqualTo("new_a");
        await Assert.That(statement.Source).IsTypeOf<SqlValuesQuery>();
    }

    [Test]
    public async Task Update_OutputInto_IsParsed()
    {
        var statement = (SqlUpdateStatement)Sql.Parse(
            "update t set a = 1 output deleted.a into archive (a) where id = 1");

        var output = statement.Output!;
        await Assert.That(output.Items.Length).IsEqualTo(1);
        await Assert.That(output.Into!.Name.Value).IsEqualTo("archive");
        await Assert.That(output.IntoColumns.Length).IsEqualTo(1);
        await Assert.That(statement.Where).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task Delete_Output_IsParsed()
    {
        var statement = (SqlDeleteStatement)Sql.Parse(
            "delete from t output deleted.id where id = 1");

        await Assert.That(statement.Output!.Items.Length).IsEqualTo(1);
        await Assert.That(statement.Where).IsNotNull();
    }

    [Test]
    public async Task Merge_Output_IsParsed()
    {
        var merge = (SqlMergeStatement)Sql.Parse(
            "merge into t using s on t.id = s.id when matched then delete output deleted.id");

        await Assert.That(merge.Output).IsNotNull();
        await Assert.That(merge.Output!.Items.Length).IsEqualTo(1);
    }

    [Test]
    public async Task OutputIntoWithoutTable_ReportsError()
    {
        var success = Sql.TryParse(
            "delete from t output deleted.id into where id = 1",
            out _,
            out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
