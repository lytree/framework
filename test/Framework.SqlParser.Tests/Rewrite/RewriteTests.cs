using Framework.SqlParser.Ast;
using Framework.SqlParser.Deparse;
using Framework.SqlParser.Rewrite;

namespace Framework.SqlParser.Tests;

/// <summary>SqlRewriter 改写测试。</summary>
public sealed class RewriteTests
{
    private sealed class RenameUsersRewriter : SqlRewriter
    {
        public override SqlTableSource RewriteTableSource(SqlTableSource source) =>
            source is SqlNamedTable named && named.Schema is null && named.Name.Value == "users"
                ? named with { Name = new SqlIdentifier("members") }
                : base.RewriteTableSource(source);
    }

    private sealed class ParameterizeTwoRewriter : SqlRewriter
    {
        public override SqlExpression RewriteExpression(SqlExpression expression) =>
            expression is SqlLiteral { LiteralKind: SqlLiteralKind.Number, Value: "2" }
                ? new SqlParameterRef("?")
                : base.RewriteExpression(expression);
    }

    [Test]
    public async Task RenameTable_IsApplied()
    {
        var statement = new RenameUsersRewriter().RewriteStatement(
            Sql.Parse("select id from users where id > 0"));

        await Assert.That(SqlDeparser.Deparse(statement))
            .IsEqualTo("SELECT id FROM members WHERE id > 0");
    }

    [Test]
    public async Task RenameTable_InsideDerivedTable_IsApplied()
    {
        var statement = new RenameUsersRewriter().RewriteStatement(
            Sql.Parse("select * from (select * from users) u"));

        await Assert.That(SqlDeparser.Deparse(statement).Contains("FROM (SELECT * FROM members) AS u")).IsTrue();
    }

    [Test]
    public async Task RenameTable_InsideCte_IsApplied()
    {
        var statement = new RenameUsersRewriter().RewriteStatement(
            Sql.Parse("with c as (select * from users) select * from c"));

        var text = SqlDeparser.Deparse(statement);
        await Assert.That(text.Contains("SELECT * FROM members")).IsTrue();
        await Assert.That(text.Contains("FROM c")).IsTrue();
    }

    [Test]
    public async Task ParameterizeLiteral_InUpdate_IsApplied()
    {
        var statement = new ParameterizeTwoRewriter().RewriteStatement(
            Sql.Parse("update t set a = 1 where id = 2"));

        await Assert.That(SqlDeparser.Deparse(statement))
            .IsEqualTo("UPDATE t SET a = 1 WHERE id = ?");
    }

    [Test]
    public async Task Rewriter_DoesNotMutateOriginal()
    {
        var original = Sql.Parse("select id from users");
        _ = new RenameUsersRewriter().RewriteStatement(original);

        await Assert.That(SqlDeparser.Deparse(original)).IsEqualTo("SELECT id FROM users");
    }
}
