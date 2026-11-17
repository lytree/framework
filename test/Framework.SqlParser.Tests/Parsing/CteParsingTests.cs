using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>CTE（WITH 子句）解析测试。</summary>
public sealed class CteParsingTests
{
    private static SqlSelectStatement ParseStatement(string sql, SqlDialect? dialect = null) =>
        (SqlSelectStatement)(dialect is null ? Sql.Parse(sql) : Sql.Parse(sql, dialect));

    [Test]
    public async Task SingleCte_IsAttachedToStatement()
    {
        var statement = ParseStatement("with cte as (select id from t) select * from cte");

        await Assert.That(statement.With).IsNotNull();
        var with = statement.With!;
        await Assert.That(with.Recursive).IsFalse();
        await Assert.That(with.Ctes.Length).IsEqualTo(1);

        var cte = with.Ctes[0];
        await Assert.That(cte.Name.Value).IsEqualTo("cte");
        await Assert.That(cte.Query).IsTypeOf<SqlSelectQuery>();

        var query = (SqlSelectQuery)statement.Query;
        await Assert.That(((SqlNamedTable)query.From[0]).Name.Value).IsEqualTo("cte");
    }

    [Test]
    public async Task MultipleCtes_AreParsedInOrder()
    {
        var statement = ParseStatement("with a as (select 1), b as (select 2) select * from a, b");

        var with = statement.With!;
        await Assert.That(with.Ctes.Length).IsEqualTo(2);
        await Assert.That(with.Ctes[0].Name.Value).IsEqualTo("a");
        await Assert.That(with.Ctes[1].Name.Value).IsEqualTo("b");
        await Assert.That(((SqlSelectQuery)statement.Query).From.Length).IsEqualTo(2);
    }

    [Test]
    public async Task RecursiveCteWithColumns_ParsesUnionAllBody()
    {
        var statement = ParseStatement(
            "with recursive t(n) as (select 1 union all select n + 1 from t) select * from t");

        var with = statement.With!;
        await Assert.That(with.Recursive).IsTrue();

        var cte = with.Ctes[0];
        await Assert.That(cte.Columns.Length).IsEqualTo(1);
        await Assert.That(cte.Columns[0].Value).IsEqualTo("n");

        var setOperation = (SqlSetOperationQuery)cte.Query;
        await Assert.That(setOperation.Operator).IsEqualTo(SqlSetOperator.Union);
        await Assert.That(setOperation.Quantifier).IsEqualTo(SqlSetQuantifier.All);
    }

    [Test]
    public async Task MaterializedHint_IsCaptured()
    {
        var materialized = ParseStatement(
            "with x as materialized (select 1) select * from x",
            SqlDialect.PostgreSql);
        await Assert.That(materialized.With!.Ctes[0].Materialization)
            .IsEqualTo(SqlCteMaterialization.Materialized);

        var notMaterialized = ParseStatement(
            "with x as not materialized (select 1) select * from x",
            SqlDialect.PostgreSql);
        await Assert.That(notMaterialized.With!.Ctes[0].Materialization)
            .IsEqualTo(SqlCteMaterialization.NotMaterialized);
    }

    [Test]
    public async Task CteBody_CanBeValuesQuery()
    {
        var statement = ParseStatement("with v as (values (1), (2)) select * from v");

        await Assert.That(statement.With!.Ctes[0].Query).IsTypeOf<SqlValuesQuery>();
    }

    [Test]
    public async Task MissingQueryAfterWith_ReportsSyntaxError()
    {
        var success = Sql.TryParse("with cte as (select 1)", out var statement, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(statement).IsTypeOf<SqlInvalidStatement>();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task WithFollowedByDml_IsParsed()
    {
        var statement = (SqlInsertStatement)Sql.Parse(
            "with cte as (select 1) insert into t select * from cte");

        await Assert.That(statement.With).IsNotNull();
        await Assert.That(statement.With!.Ctes.Length).IsEqualTo(1);
        await Assert.That(statement.Source).IsTypeOf<SqlSelectQuery>();
    }

    [Test]
    public async Task MissingCteBody_ReportsError()
    {
        var success = Sql.TryParse("with cte as () select 1", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Severity == SqlDiagnosticSeverity.Error)).IsTrue();
    }

    [Test]
    public async Task StatementSpan_IncludesWithClause()
    {
        const string sql = "with cte as (select 1) select * from cte";
        var statement = ParseStatement(sql);

        await Assert.That(statement.Span.Start).IsEqualTo(0);
        await Assert.That(statement.Span.Length).IsEqualTo(sql.Length);
    }
}
