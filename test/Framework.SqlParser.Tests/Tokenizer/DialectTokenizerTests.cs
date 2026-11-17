namespace Framework.SqlParser.Tests;

/// <summary>方言相关词法差异测试。</summary>
public sealed class DialectTokenizerTests
{
    [Test]
    public async Task DoubleQuotedIdentifier_IsDecoded()
    {
        var result = SqlTokenizer.Tokenize("select \"a\"\"b\" from t");

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.QuotedIdentifier);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("a\"b");
        await Assert.That(result.Tokens[1].Flags.HasFlag(SqlTokenFlags.DoubleQuotedIdentifier)).IsTrue();
    }

    [Test]
    public async Task TSql_BracketIdentifier_IsRecognized()
    {
        var result = SqlTokenizer.Tokenize("select [my col] from [dbo].[t]", SqlDialect.TSql);

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.QuotedIdentifier);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("my col");
        await Assert.That(result.Tokens[1].Flags.HasFlag(SqlTokenFlags.BracketIdentifier)).IsTrue();
        await Assert.That(result.Tokens[3].Value).IsEqualTo("dbo");
        await Assert.That(result.Tokens[5].Value).IsEqualTo("t");
    }

    [Test]
    public async Task TSql_HashPrefixedIdentifier_IsRecognized()
    {
        var result = SqlTokenizer.Tokenize("select * from #temp", SqlDialect.TSql);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Tokens[3].Kind).IsEqualTo(SqlTokenKind.Identifier);
        await Assert.That(result.Tokens[3].Value).IsEqualTo("#temp");
    }

    [Test]
    public async Task MySql_BacktickIdentifier_IsRecognized()
    {
        var result = SqlTokenizer.Tokenize("select `a``b` from `t`", SqlDialect.MySql);

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.QuotedIdentifier);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("a`b");
        await Assert.That(result.Tokens[3].Value).IsEqualTo("t");
    }

    [Test]
    public async Task MySql_HashStartsComment()
    {
        var result = SqlTokenizer.Tokenize("select 1 # comment\nfrom t", SqlDialect.MySql);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Tokens.Length).IsEqualTo(5);
        await Assert.That(result.Tokens[2].Keyword).IsEqualTo(SqlKeywordId.From);
    }

    [Test]
    public async Task PostgreSql_HashIsUnknownCharacter()
    {
        var result = SqlTokenizer.Tokenize("select 1 # 2", SqlDialect.PostgreSql);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostics.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnknownCharacter);
        await Assert.That(result.Tokens[2].Kind).IsEqualTo(SqlTokenKind.Unknown);
    }

    [Test]
    public async Task NationalStringPrefix_IsPreserved()
    {
        var result = SqlTokenizer.Tokenize("select N'abc'", SqlDialect.TSql);

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.String);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("abc");
        await Assert.That(result.Tokens[1].Flags.HasFlag(SqlTokenFlags.NationalString)).IsTrue();
    }

    [Test]
    public async Task PostgreSql_EscapeString_InterpretsBackslash()
    {
        var result = SqlTokenizer.Tokenize("select E'a\\nb'", SqlDialect.PostgreSql);

        await Assert.That(result.Tokens[1].Value).IsEqualTo("a\nb");
        await Assert.That(result.Tokens[1].Flags.HasFlag(SqlTokenFlags.EscapeString)).IsTrue();
    }

    [Test]
    public async Task MySql_BackslashEscape_IsInterpretted()
    {
        var result = SqlTokenizer.Tokenize("select 'a\\nb'", SqlDialect.MySql);

        await Assert.That(result.Tokens[1].Value).IsEqualTo("a\nb");
    }

    [Test]
    public async Task PostgreSql_DollarQuotedString_IsRecognized()
    {
        var result = SqlTokenizer.Tokenize("select $$a 'b' c$$", SqlDialect.PostgreSql);

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.String);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("a 'b' c");
        await Assert.That(result.Tokens[1].Flags.HasFlag(SqlTokenFlags.DollarQuotedString)).IsTrue();
    }

    [Test]
    public async Task PostgreSql_TaggedDollarQuotedString_IsRecognized()
    {
        var result = SqlTokenizer.Tokenize("select $tag$body$tag$", SqlDialect.PostgreSql);

        await Assert.That(result.Tokens[1].Value).IsEqualTo("body");
    }

    [Test]
    public async Task Oracle_QQuotedString_IsRecognized()
    {
        var result = SqlTokenizer.Tokenize("select q'[a'b]' from dual", SqlDialect.Oracle);

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.String);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("a'b");
        await Assert.That(result.Tokens[1].Flags.HasFlag(SqlTokenFlags.OracleQuotedString)).IsTrue();
    }

    [Test]
    public async Task PostgreSql_BlockCommentsNest()
    {
        var result = SqlTokenizer.Tokenize("select 1 /* a /* b */ c */ from t", SqlDialect.PostgreSql);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Tokens.Length).IsEqualTo(5);
        await Assert.That(result.Tokens[2].Keyword).IsEqualTo(SqlKeywordId.From);
    }

    [Test]
    public async Task Generic_BacktickIsUnknown()
    {
        var result = SqlTokenizer.Tokenize("select `x`");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Diagnostics.Length).IsEqualTo(2);
        await Assert.That(result.Diagnostics[0].Code).IsEqualTo(SqlDiagnosticCodes.UnknownCharacter);
    }
}
