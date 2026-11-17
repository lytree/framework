using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>RETURNING 子句解析测试。</summary>
public sealed class ReturningTests
{
    [Test]
    public async Task Insert_Returning_IsParsed()
    {
        var statement = (SqlInsertStatement)Sql.Parse(
            "insert into t (a) values (1) returning id, a as x");

        await Assert.That(statement.Returning.Length).IsEqualTo(2);
        await Assert.That(statement.Returning[0].Expression).IsTypeOf<SqlIdentifier>();
        await Assert.That(statement.Returning[1].Alias!.Value).IsEqualTo("x");
    }

    [Test]
    public async Task Update_ReturningStar_IsParsed()
    {
        var statement = (SqlUpdateStatement)Sql.Parse("update t set a = 1 returning *");

        await Assert.That(statement.Returning.Length).IsEqualTo(1);
        await Assert.That(((SqlColumnRef)statement.Returning[0].Expression).IsStar).IsTrue();
    }

    [Test]
    public async Task Delete_Returning_IsParsed()
    {
        var statement = (SqlDeleteStatement)Sql.Parse(
            "delete from t where id = 1 returning id as old_id");

        await Assert.That(statement.Returning.Length).IsEqualTo(1);
        await Assert.That(statement.Returning[0].Alias!.Value).IsEqualTo("old_id");
    }

    [Test]
    public async Task ReturningExpression_IsParsed()
    {
        var statement = (SqlDeleteStatement)Sql.Parse(
            "delete from t where id = 1 returning id + 1");

        await Assert.That(statement.Returning[0].Expression).IsTypeOf<SqlBinaryExpression>();
    }
}
