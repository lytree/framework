using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>DDL 语句解析测试。</summary>
public sealed class DdlStatementTests
{
    [Test]
    public async Task CreateTable_ParsesColumnsAndConstraints()
    {
        var statement = (SqlCreateTableStatement)Sql.Parse(
            "create table users (id int not null, name varchar(50), age int default 0, primary key (id))");

        await Assert.That(statement.Table.Name.Value).IsEqualTo("users");
        await Assert.That(statement.Columns.Length).IsEqualTo(3);
        await Assert.That(statement.Columns[0].Nullability).IsEqualTo(SqlNullability.NotNull);
        await Assert.That(statement.Columns[1].DataType.Name.Value).IsEqualTo("varchar");
        await Assert.That(statement.Columns[2].Default).IsTypeOf<SqlLiteral>();

        await Assert.That(statement.Constraints.Length).IsEqualTo(1);
        await Assert.That(statement.Constraints[0].ConstraintKind).IsEqualTo(SqlTableConstraintKind.PrimaryKey);
        await Assert.That(statement.Constraints[0].Columns.Length).IsEqualTo(1);
    }

    [Test]
    public async Task CreateTable_IfNotExistsAndColumnFlags_AreParsed()
    {
        var statement = (SqlCreateTableStatement)Sql.Parse(
            "create table if not exists t (id bigint primary key, email varchar(100) unique)");

        await Assert.That(statement.IfNotExists).IsTrue();
        await Assert.That(statement.Columns[0].PrimaryKey).IsTrue();
        await Assert.That(statement.Columns[1].Unique).IsTrue();
    }

    [Test]
    public async Task CreateTemporaryTable_IsParsed()
    {
        var statement = (SqlCreateTableStatement)Sql.Parse("create temporary table tmp (x int)");

        await Assert.That(statement.Temporary).IsTrue();
        await Assert.That(statement.Table.Name.Value).IsEqualTo("tmp");
    }

    [Test]
    public async Task CreateTable_CheckAndForeignKey_AreParsed()
    {
        var statement = (SqlCreateTableStatement)Sql.Parse(
            "create table child (id int, parent_id int, check (parent_id > 0), "
            + "foreign key (parent_id) references parent (id))");

        await Assert.That(statement.Constraints.Length).IsEqualTo(2);

        var check = statement.Constraints[0];
        await Assert.That(check.ConstraintKind).IsEqualTo(SqlTableConstraintKind.Check);
        await Assert.That(check.Check).IsTypeOf<SqlBinaryExpression>();

        var foreignKey = statement.Constraints[1];
        await Assert.That(foreignKey.ConstraintKind).IsEqualTo(SqlTableConstraintKind.ForeignKey);
        await Assert.That(foreignKey.Columns[0].Value).IsEqualTo("parent_id");
        await Assert.That(foreignKey.References!.Name.Value).IsEqualTo("parent");
        await Assert.That(foreignKey.ReferencedColumns[0].Value).IsEqualTo("id");
    }

    [Test]
    public async Task DropTable_IsParsed()
    {
        var statement = (SqlDropTableStatement)Sql.Parse("drop table if exists a, b cascade");

        await Assert.That(statement.IfExists).IsTrue();
        await Assert.That(statement.Tables.Length).IsEqualTo(2);
        await Assert.That(statement.Cascade).IsTrue();
    }

    [Test]
    public async Task TruncateTable_IsParsed()
    {
        var statement = (SqlTruncateTableStatement)Sql.Parse("truncate table a, b");

        await Assert.That(statement.Tables.Length).IsEqualTo(2);
        await Assert.That(statement.Tables[1].Name.Value).IsEqualTo("b");
    }

    [Test]
    public async Task CreateIndex_IsUnsupported()
    {
        var success = Sql.TryParse("create index ix on t (a)", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.UnsupportedStatement)).IsTrue();
    }

    [Test]
    public async Task CreateTable_WithoutType_ReportsError()
    {
        var success = Sql.TryParse("create table t (id)", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }
}
