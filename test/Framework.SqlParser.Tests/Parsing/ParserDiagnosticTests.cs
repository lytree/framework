using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>解析诊断与错误恢复测试。</summary>
public sealed class ParserDiagnosticTests
{
    [Test]
    public async Task SelectWithoutProjection_ReportsSyntaxErrorAtFrom()
    {
        var success = Sql.TryParse("select from", out var statement, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(statement).IsTypeOf<SqlSelectStatement>();

        var firstError = diagnostics.First(d => d.Severity == SqlDiagnosticSeverity.Error);
        await Assert.That(firstError.Code).IsEqualTo(SqlDiagnosticCodes.SyntaxError);
        await Assert.That(firstError.Column).IsEqualTo(8);
    }

    [Test]
    public async Task MissingWhereExpression_ReportsSyntaxError()
    {
        var success = Sql.TryParse("select * from t where", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task UnsupportedStatement_ProducesInvalidStatement()
    {
        const string sql = "alter table t add column x int";
        var success = Sql.TryParse(sql, out var statement, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(statement).IsTypeOf<SqlInvalidStatement>();
        await Assert.That(statement!.Span.Length).IsEqualTo(sql.Length);
        await Assert.That(diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnsupportedStatement);
    }

    [Test]
    public async Task IncompleteSelect_StillReturnsPartialStatement()
    {
        var success = Sql.TryParse("select id from", out var statement, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(statement).IsTypeOf<SqlSelectStatement>();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.SyntaxError)).IsTrue();
    }

    [Test]
    public async Task TokenizerError_IsPropagated()
    {
        var success = Sql.TryParse("select 'abc", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.UnterminatedToken)).IsTrue();
    }

    [Test]
    public async Task EmptyInput_ReportsUnexpectedEndOfInput()
    {
        var success = Sql.TryParse(string.Empty, out var statement, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(statement).IsNull();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.UnexpectedEndOfInput)).IsTrue();
    }

    [Test]
    public async Task NonAssociativeComparison_ReportsDiagnostic()
    {
        var success = Sql.TryParse("select * from t where a = b = c", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.NonAssociativeOperator)).IsTrue();
    }

    [Test]
    public async Task NestingTooDeep_ReportsDiagnostic()
    {
        var parser = new SqlParser(new SqlParserOptions { MaxDepth = 2 });
        var success = parser.TryParse("select ((((1))))", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.NestingTooDeep)).IsTrue();
    }

    [Test]
    public async Task Parse_ThrowsWhenDiagnosticsContainError()
    {
        await Assert.That(() => { Sql.Parse("select from"); }).Throws<SqlParseException>();
    }

    [Test]
    public async Task DoubleColonCast_UnsupportedDialect_ReportsDiagnostic()
    {
        var success = Sql.TryParse("select x::int from t", out _, out var diagnostics);

        await Assert.That(success).IsFalse();
        await Assert.That(diagnostics.Any(d => d.Code == SqlDiagnosticCodes.UnsupportedSyntax)).IsTrue();
    }
}
