using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>INSERT / UPDATE / DELETE 语句解析测试。</summary>
public sealed class DmlStatementTests
{
    [Test]
    public async Task Insert_WithColumnsAndValues_IsParsed()
    {
        var statement = (SqlInsertStatement)Sql.Parse(
            "insert into t (a, b) values (1, 'x'), (2, 'y')");

        await Assert.That(statement.Target.Name.Value).IsEqualTo("t");
        await Assert.That(statement.Columns.Length).IsEqualTo(2);
        await Assert.That(statement.Columns[1].Value).IsEqualTo("b");

        var values = (SqlValuesQuery)statement.Source!;
        await Assert.That(values.Rows.Length).IsEqualTo(2);
        await Assert.That(values.Rows[1].Values.Length).IsEqualTo(2);
        await Assert.That(statement.DefaultValues).IsFalse();
    }

    [Test]
    public async Task Insert_FromSelect_IsParsed()
    {
        var statement = (SqlInsertStatement)Sql.Parse(
            "insert into db.schema.t select * from u where u.id > 0");

        await Assert.That(statement.Target.Name.Value).IsEqualTo("t");
        await Assert.That(statement.Target.Schema!.Value).IsEqualTo("schema");
        await Assert.That(statement.Source).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task Insert_DefaultValues_IsParsed()
    {
        var statement = (SqlInsertStatement)Sql.Parse("insert into t default values");

        await Assert.That(statement.DefaultValues).IsTrue();
        await Assert.That(statement.Source).IsNull();
    }

    [Test]
    public async Task Insert_WithoutIntoAndSource_ReportsError()
    {
        var success = Sql.TryParse("insert into t (a, b)", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task Update_ParsesAssignmentsAndWhere()
    {
        var statement = (SqlUpdateStatement)Sql.Parse(
            "update t set a = 1, b = b + 1 where id = 3");

        await Assert.That(statement.Target.Name.Value).IsEqualTo("t");
        await Assert.That(statement.Assignments.Length).IsEqualTo(2);
        await Assert.That(((SqlLiteral)statement.Assignments[0].Value).Value).IsEqualTo("1");
        await Assert.That(statement.Assignments[1].Value).IsTypeOf<SqlBinaryExpression>();
        await Assert.That(statement.Where).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task Update_WithAliasAndFrom_IsParsed()
    {
        var statement = (SqlUpdateStatement)Sql.Parse(
            "update db.t x set a = 1 from u where x.id = u.id");

        await Assert.That(statement.Target.Schema!.Value).IsEqualTo("db");
        await Assert.That(statement.Target.Alias!.Value).IsEqualTo("x");
        await Assert.That(statement.From.Length).IsEqualTo(1);
        await Assert.That(statement.From[0]).IsTypeOf<SqlNamedTable>();
        await Assert.That(statement.Where).IsNotNull();
    }

    [Test]
    public async Task Update_WithoutSet_ReportsError()
    {
        var success = Sql.TryParse("update t where id = 1", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task Delete_WithWhere_IsParsed()
    {
        var statement = (SqlDeleteStatement)Sql.Parse("delete from t where id = 1");

        await Assert.That(statement.Target.Name.Value).IsEqualTo("t");
        await Assert.That(statement.Where).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task Delete_WithAlias_IsParsed()
    {
        var statement = (SqlDeleteStatement)Sql.Parse("delete from db.t x where x.id = 1");

        await Assert.That(statement.Target.Schema!.Value).IsEqualTo("db");
        await Assert.That(statement.Target.Alias!.Value).IsEqualTo("x");
        await Assert.That(statement.Where).IsNotNull();
    }

    [Test]
    public async Task Delete_WithoutFrom_ReportsError()
    {
        var success = Sql.TryParse("delete t where id = 1", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
