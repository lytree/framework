using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>Pratt 表达式解析测试。</summary>
public sealed class ExpressionParsingTests
{
    [Test]
    public async Task Arithmetic_MultiplicationBindsTighterThanAddition()
    {
        var expression = (SqlBinaryExpression)Sql.ParseExpression("1 + 2 * 3");

        await Assert.That(expression.Operator).IsEqualTo(SqlBinaryOperator.Add);
        var right = (SqlBinaryExpression)expression.Right;
        await Assert.That(right.Operator).IsEqualTo(SqlBinaryOperator.Multiply);
    }

    [Test]
    public async Task Parentheses_OverridePrecedence()
    {
        var expression = (SqlBinaryExpression)Sql.ParseExpression("(1 + 2) * 3");

        await Assert.That(expression.Operator).IsEqualTo(SqlBinaryOperator.Multiply);
        await Assert.That(expression.Left).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task UnaryNot_AppliesToComparison()
    {
        var expression = (SqlUnaryExpression)Sql.ParseExpression("not a = b");

        await Assert.That(expression.Operator).IsEqualTo(SqlUnaryOperator.Not);
        var comparison = (SqlBinaryExpression)expression.Operand;
        await Assert.That(comparison.Operator).IsEqualTo(SqlBinaryOperator.Equal);
    }

    [Test]
    public async Task UnaryMinus_IsParsed()
    {
        var expression = (SqlUnaryExpression)Sql.ParseExpression("-1");

        await Assert.That(expression.Operator).IsEqualTo(SqlUnaryOperator.Minus);
        await Assert.That(expression.Operand).IsTypeOf<SqlLiteral>();
    }

    [Test]
    public async Task Concat_IsLeftAssociative()
    {
        var expression = (SqlBinaryExpression)Sql.ParseExpression("a || b || c");

        await Assert.That(expression.Operator).IsEqualTo(SqlBinaryOperator.Concat);
        var left = (SqlBinaryExpression)expression.Left;
        await Assert.That(left.Operator).IsEqualTo(SqlBinaryOperator.Concat);
    }

    [Test]
    public async Task SearchedCase_CapturesWhensAndElse()
    {
        var expression = (SqlCaseExpression)Sql.ParseExpression("case when a > 0 then 'p' else 'n' end");

        await Assert.That(expression.Operand).IsNull();
        await Assert.That(expression.Whens.Length).IsEqualTo(1);
        await Assert.That(expression.Whens[0].Condition).IsTypeOf<SqlBinaryExpression>();
        await Assert.That(expression.Else).IsTypeOf<SqlLiteral>();
    }

    [Test]
    public async Task SimpleCase_CapturesOperand()
    {
        var expression = (SqlCaseExpression)Sql.ParseExpression("case x when 1 then 'a' end");

        await Assert.That(expression.Operand).IsTypeOf<SqlIdentifier>();
        await Assert.That(expression.Whens.Length).IsEqualTo(1);
        await Assert.That(expression.Else).IsNull();
    }

    [Test]
    public async Task Cast_CapturesTypeArguments()
    {
        var expression = (SqlCastExpression)Sql.ParseExpression("cast(x as decimal(10, 2))");

        await Assert.That(expression.TargetType.Name.Value).IsEqualTo("decimal");
        await Assert.That(expression.TargetType.Arguments.Length).IsEqualTo(2);
        await Assert.That(expression.TryCast).IsFalse();
    }

    [Test]
    public async Task PostgreSqlDoubleColonCast_IsParsed()
    {
        var expression = (SqlCastExpression)Sql.ParseExpression("x::int", SqlDialect.PostgreSql);

        await Assert.That(expression.TargetType.Name.Value).IsEqualTo("int");
    }

    [Test]
    public async Task ExistsAndNotExists_AreParsed()
    {
        var exists = (SqlExistsExpression)Sql.ParseExpression("exists (select 1 from t)");
        await Assert.That(exists.Negated).IsFalse();

        var notExists = (SqlExistsExpression)Sql.ParseExpression("not exists (select 1)");
        await Assert.That(notExists.Negated).IsTrue();
    }

    [Test]
    public async Task Parameters_KeepOriginalPrefix()
    {
        var positional = (SqlParameterRef)Sql.ParseExpression("?");
        await Assert.That(positional.Prefix).IsEqualTo("?");
        await Assert.That(positional.Name).IsNull();

        var named = (SqlParameterRef)Sql.ParseExpression("@id");
        await Assert.That(named.Prefix).IsEqualTo("@");
        await Assert.That(named.Name).IsEqualTo("id");

        var colon = (SqlParameterRef)Sql.ParseExpression(":name");
        await Assert.That(colon.Prefix).IsEqualTo(":");
        await Assert.That(colon.Name).IsEqualTo("name");

        var ordinal = (SqlParameterRef)Sql.ParseExpression("$2");
        await Assert.That(ordinal.Prefix).IsEqualTo("$");
        await Assert.That(ordinal.Index).IsEqualTo(2);
    }

    [Test]
    public async Task NationalStringLiteral_KeepsPrefix()
    {
        var literal = (SqlLiteral)Sql.ParseExpression("N'abc'");

        await Assert.That(literal.Prefix).IsEqualTo("N");
        await Assert.That(literal.Value).IsEqualTo("abc");
        await Assert.That(literal.LiteralKind).IsEqualTo(SqlLiteralKind.String);
    }

    [Test]
    public async Task NullAndBooleanLiterals_AreDistinguished()
    {
        var nullLiteral = (SqlLiteral)Sql.ParseExpression("null");
        await Assert.That(nullLiteral.LiteralKind).IsEqualTo(SqlLiteralKind.Null);

        var boolean = (SqlLiteral)Sql.ParseExpression("true");
        await Assert.That(boolean.LiteralKind).IsEqualTo(SqlLiteralKind.Boolean);
    }

    [Test]
    public async Task ColumnReference_CapturesAllParts()
    {
        var column = (SqlColumnRef)Sql.ParseExpression("a.b.c");

        await Assert.That(column.Parts.Length).IsEqualTo(3);
        await Assert.That(column.Parts[2].Value).IsEqualTo("c");
    }

    [Test]
    public async Task TupleExpression_CapturesItems()
    {
        var tuple = (SqlTupleExpression)Sql.ParseExpression("(a, b)");

        await Assert.That(tuple.Items.Length).IsEqualTo(2);
    }

    [Test]
    public async Task StaticExpressionParser_DelegatesToFacade()
    {
        var expression = SqlExpressionParser.Parse("1 + 1");

        await Assert.That(expression).IsTypeOf<SqlBinaryExpression>();
    }
}
