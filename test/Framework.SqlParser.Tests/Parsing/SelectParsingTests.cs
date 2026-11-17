using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>SELECT 查询的 AST 解析测试。</summary>
public sealed class SelectParsingTests
{
    private static SqlSelectQuery SelectQuery(string sql, SqlDialect? dialect = null)
    {
        var statement = (SqlSelectStatement)(dialect is null ? Sql.Parse(sql) : Sql.Parse(sql, dialect));
        return (SqlSelectQuery)statement.Query;
    }

    [Test]
    public async Task StarProjection_ParsesSingleStarColumnRef()
    {
        var query = SelectQuery("select * from users");

        await Assert.That(query.Items.Length).IsEqualTo(1);
        var star = (SqlColumnRef)query.Items[0].Expression;
        await Assert.That(star.IsStar).IsTrue();
        await Assert.That(star.Parts.Length).IsEqualTo(0);
        await Assert.That(query.From.Length).IsEqualTo(1);
        await Assert.That(((SqlNamedTable)query.From[0]).Name.Value).IsEqualTo("users");
    }

    [Test]
    public async Task ProjectionWithAliasesAndSchema_ParsesQualifiedParts()
    {
        var query = SelectQuery("select id, u.name as n from public.users u");

        await Assert.That(query.Items.Length).IsEqualTo(2);
        await Assert.That(query.Items[0].Expression).IsTypeOf<SqlIdentifier>();

        var column = (SqlColumnRef)query.Items[1].Expression;
        await Assert.That(column.Parts.Length).IsEqualTo(2);
        await Assert.That(column.Parts[0].Value).IsEqualTo("u");
        await Assert.That(column.Parts[1].Value).IsEqualTo("name");
        await Assert.That(query.Items[1].Alias!.Value).IsEqualTo("n");

        var table = (SqlNamedTable)query.From[0];
        await Assert.That(table.Schema!.Value).IsEqualTo("public");
        await Assert.That(table.Name.Value).IsEqualTo("users");
        await Assert.That(table.Alias!.Value).IsEqualTo("u");
    }

    [Test]
    public async Task DistinctQuantifier_IsCaptured()
    {
        var query = SelectQuery("select distinct a from t");

        await Assert.That(query.Quantifier).IsEqualTo(SelectQuantifier.Distinct);
    }

    [Test]
    public async Task FunctionCalls_CaptureStarAndArguments()
    {
        var query = SelectQuery("select count(*), sum(x) as s from t");

        var count = (SqlFunctionCall)query.Items[0].Expression;
        await Assert.That(count.Name.Value).IsEqualTo("count");
        await Assert.That(count.Star).IsTrue();
        await Assert.That(count.Arguments.Length).IsEqualTo(0);

        var sum = (SqlFunctionCall)query.Items[1].Expression;
        await Assert.That(sum.Arguments.Length).IsEqualTo(1);
        await Assert.That(query.Items[1].Alias!.Value).IsEqualTo("s");
    }

    [Test]
    public async Task Where_BooleanPrecedence_IsOrThenAndThenNot()
    {
        var query = SelectQuery("select * from t where a or b and not c");

        var or = (SqlBinaryExpression)query.Where!;
        await Assert.That(or.Operator).IsEqualTo(SqlBinaryOperator.Or);

        var and = (SqlBinaryExpression)or.Right;
        await Assert.That(and.Operator).IsEqualTo(SqlBinaryOperator.And);

        var not = (SqlUnaryExpression)and.Right;
        await Assert.That(not.Operator).IsEqualTo(SqlUnaryOperator.Not);
    }

    [Test]
    public async Task Where_BetweenConsumesAnd_ThenBooleanAndApplies()
    {
        var query = SelectQuery("select * from t where x between 1 and 3 and y is not null");

        var and = (SqlBinaryExpression)query.Where!;
        await Assert.That(and.Operator).IsEqualTo(SqlBinaryOperator.And);

        var between = (SqlBetweenExpression)and.Left;
        await Assert.That(((SqlLiteral)between.Low).Value).IsEqualTo("1");
        await Assert.That(((SqlLiteral)between.High).Value).IsEqualTo("3");
        await Assert.That(between.Negated).IsFalse();

        var isNull = (SqlIsNullExpression)and.Right;
        await Assert.That(isNull.Negated).IsTrue();
    }

    [Test]
    public async Task Where_InListAndLike_AreParsed()
    {
        var query = SelectQuery("select * from t where id in (1, 2) and name like 'a%'");

        var and = (SqlBinaryExpression)query.Where!;
        var inExpression = (SqlInExpression)and.Left;
        await Assert.That(inExpression.Values.Length).IsEqualTo(2);

        var like = (SqlLikeExpression)and.Right;
        await Assert.That(like.CaseInsensitive).IsFalse();
        await Assert.That(((SqlLiteral)like.Pattern).Value).IsEqualTo("a%");
    }

    [Test]
    public async Task NotInAndNotLike_AreNegated()
    {
        var query = SelectQuery("select * from t where id not in (1) and name not like 'a%'");

        var and = (SqlBinaryExpression)query.Where!;
        await Assert.That(((SqlInExpression)and.Left).Negated).IsTrue();
        await Assert.That(((SqlLikeExpression)and.Right).Negated).IsTrue();
    }

    [Test]
    public async Task JoinChain_IsLeftAssociative()
    {
        var query = SelectQuery("select * from a left join b on a.id = b.id join c on b.id = c.id");

        var outer = (SqlJoinedTable)query.From[0];
        await Assert.That(outer.JoinType).IsEqualTo(SqlJoinType.Inner);
        await Assert.That(outer.Right).IsTypeOf<SqlNamedTable>();

        var inner = (SqlJoinedTable)outer.Left;
        await Assert.That(inner.JoinType).IsEqualTo(SqlJoinType.Left);
        await Assert.That(inner.On).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task JoinUsing_CapturesColumnList()
    {
        var query = SelectQuery("select * from a join b using (id, tenant)");

        var join = (SqlJoinedTable)query.From[0];
        await Assert.That(join.Using.Length).IsEqualTo(2);
        await Assert.That(join.Using[1].Value).IsEqualTo("tenant");
    }

    [Test]
    public async Task DerivedTable_ParsesInnerQueryAndAlias()
    {
        var query = SelectQuery("select * from (select id from u) x");

        var derived = (SqlDerivedTable)query.From[0];
        await Assert.That(derived.Alias!.Value).IsEqualTo("x");
        await Assert.That(derived.Query).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task InSubquery_ProducesSubqueryExpression()
    {
        var query = SelectQuery("select * from t where id in (select id from u)");

        var inExpression = (SqlInExpression)query.Where!;
        await Assert.That(inExpression.Values.Length).IsEqualTo(1);

        var subquery = (SqlSubqueryExpression)inExpression.Values[0];
        await Assert.That(subquery.Query).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task OrderByLimitOffset_AreCaptured()
    {
        var query = SelectQuery("select * from t order by a desc, b nulls last limit 10 offset 5");

        await Assert.That(query.OrderBy!.Items.Length).IsEqualTo(2);
        await Assert.That(query.OrderBy.Items[0].Direction).IsEqualTo(SqlSortOrder.Descending);
        await Assert.That(query.OrderBy.Items[1].Nulls).IsEqualTo(SqlNullsOrder.Last);
        await Assert.That(((SqlLiteral)query.Limit!.Count).Value).IsEqualTo("10");
        await Assert.That(((SqlLiteral)query.Offset!.Count).Value).IsEqualTo("5");
    }

    [Test]
    public async Task LimitCommaForm_SplitsOffsetAndCount()
    {
        var query = SelectQuery("select * from t limit 5, 10", SqlDialect.MySql);

        await Assert.That(((SqlLiteral)query.Offset!.Count).Value).IsEqualTo("5");
        await Assert.That(((SqlLiteral)query.Limit!.Count).Value).IsEqualTo("10");
    }

    [Test]
    public async Task ThreePartTableName_CapturesCatalogAndSchema()
    {
        var query = SelectQuery("select * from db.dbo.users");

        var table = (SqlNamedTable)query.From[0];
        await Assert.That(table.Catalog!.Value).IsEqualTo("db");
        await Assert.That(table.Schema!.Value).IsEqualTo("dbo");
        await Assert.That(table.Name.Value).IsEqualTo("users");
    }

    [Test]
    public async Task TableColumnAliases_AreCaptured()
    {
        var query = SelectQuery("select * from t as x(a, b)");

        var table = (SqlNamedTable)query.From[0];
        await Assert.That(table.Alias!.Value).IsEqualTo("x");
        await Assert.That(table.ColumnAliases.Length).IsEqualTo(2);
        await Assert.That(table.ColumnAliases[0].Value).IsEqualTo("a");
    }

    [Test]
    public async Task GroupByAndHaving_AreCaptured()
    {
        var query = SelectQuery("select a, count(*) from t group by a having count(*) > 1");

        await Assert.That(query.GroupBy!.Items.Length).IsEqualTo(1);
        await Assert.That(query.Having).IsTypeOf<SqlBinaryExpression>();
    }

    [Test]
    public async Task StatementSpan_CoversWholeInput()
    {
        const string sql = "select * from users";
        var statement = Sql.Parse(sql);

        await Assert.That(statement.Span.Start).IsEqualTo(0);
        await Assert.That(statement.Span.Length).IsEqualTo(sql.Length);
    }
}
