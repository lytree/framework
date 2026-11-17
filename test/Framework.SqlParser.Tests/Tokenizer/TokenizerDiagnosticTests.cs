namespace Framework.SqlParser.Tests;

/// <summary>词法错误与诊断测试。</summary>
public sealed class TokenizerDiagnosticTests
{
    [Test]
    public async Task UnterminatedString_ReportsDiagnostic()
    {
        var result = SqlTokenizer.Tokenize("select 'abc");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostics.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnterminatedToken);
        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.String);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("abc");
    }

    [Test]
    public async Task UnterminatedBlockComment_ReportsDiagnostic()
    {
        var result = SqlTokenizer.Tokenize("select /* abc");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostics.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnterminatedToken);
        await Assert.That(result.Tokens.Length).IsEqualTo(2);
        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.EndOfFile);
    }

    [Test]
    public async Task UnterminatedQuotedIdentifier_ReportsDiagnostic()
    {
        var result = SqlTokenizer.Tokenize("select \"abc");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostics.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnterminatedToken);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("abc");
    }

    [Test]
    public async Task UnknownCharacter_ReportsDiagnosticAndContinues()
    {
        var result = SqlTokenizer.Tokenize("select 1 \\ 2");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostics.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnknownCharacter);
        await Assert.That(result.Diagnostics[0].Column).IsEqualTo(10);
        await Assert.That(result.Tokens.Length).IsEqualTo(5);
        await Assert.That(result.Tokens[2].Kind).IsEqualTo(SqlTokenKind.Unknown);
        await Assert.That(result.Tokens[4].Kind).IsEqualTo(SqlTokenKind.EndOfFile);
    }

    [Test]
    public async Task InputTooLong_ReturnsDiagnosticWithoutTokens()
    {
        var options = new SqlParserOptions { MaxInputLength = 4 };
        var result = SqlTokenizer.Tokenize("select 1", options: options);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Tokens.Length).IsEqualTo(0);
        await Assert.That(result.Diagnostics.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.InputTooLong);
    }

    [Test]
    public async Task MaxErrors_StopsFurtherScanning()
    {
        var options = new SqlParserOptions { MaxErrors = 1 };
        var result = SqlTokenizer.Tokenize("\\ \\ \\", null, options);

        await Assert.That(result.Diagnostics.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnknownCharacter);
        await Assert.That(result.Tokens.Length).IsEqualTo(2);
        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.EndOfFile);
    }
}
