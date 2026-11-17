namespace Framework.SqlParser.Tests;

/// <summary>基础词法行为测试。</summary>
public sealed class BasicTokenizerTests
{
    [Test]
    public async Task SimpleSelect_ProducesExpectedTokens()
    {
        var result = SqlTokenizer.Tokenize("select * from users");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Tokens.Length).IsEqualTo(5);
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(SqlTokenKind.Keyword);
        await Assert.That(result.Tokens[0].Keyword).IsEqualTo(SqlKeywordId.Select);
        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.Star);
        await Assert.That(result.Tokens[2].Keyword).IsEqualTo(SqlKeywordId.From);
        await Assert.That(result.Tokens[3].Kind).IsEqualTo(SqlTokenKind.Identifier);
        await Assert.That(result.Tokens[3].Value).IsEqualTo("users");
        await Assert.That(result.Tokens[4].Kind).IsEqualTo(SqlTokenKind.EndOfFile);
    }

    [Test]
    public async Task Keywords_AreCaseInsensitive()
    {
        var result = SqlTokenizer.Tokenize("SeLeCt 1");

        await Assert.That(result.Tokens[0].Kind).IsEqualTo(SqlTokenKind.Keyword);
        await Assert.That(result.Tokens[0].Keyword).IsEqualTo(SqlKeywordId.Select);
        await Assert.That(result.Tokens[0].Value).IsNull();
    }

    [Test]
    public async Task Tokens_TrackLineAndColumn()
    {
        var result = SqlTokenizer.Tokenize("select\n  id\nfrom t");

        await Assert.That(result.Tokens[0].Line).IsEqualTo(1);
        await Assert.That(result.Tokens[0].Column).IsEqualTo(1);
        await Assert.That(result.Tokens[1].Line).IsEqualTo(2);
        await Assert.That(result.Tokens[1].Column).IsEqualTo(3);
        await Assert.That(result.Tokens[2].Keyword).IsEqualTo(SqlKeywordId.From);
        await Assert.That(result.Tokens[2].Line).IsEqualTo(3);
        await Assert.That(result.Tokens[2].Column).IsEqualTo(1);
        await Assert.That(result.Tokens[3].Column).IsEqualTo(6);
    }

    [Test]
    public async Task String_DecodesDoubledQuote()
    {
        var result = SqlTokenizer.Tokenize("select 'it''s'");

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.String);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("it's");
        await Assert.That(result.Tokens[1].Span.Start).IsEqualTo(7);
        await Assert.That(result.Tokens[1].Span.Length).IsEqualTo(7);
    }

    [Test]
    public async Task Numbers_AreRecognized()
    {
        var result = SqlTokenizer.Tokenize("select 1, 1.5, .5, 1e10, 0xFF");

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.Number);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("1");
        await Assert.That(result.Tokens[3].Value).IsEqualTo("1.5");
        await Assert.That(result.Tokens[5].Value).IsEqualTo(".5");
        await Assert.That(result.Tokens[7].Value).IsEqualTo("1e10");
        await Assert.That(result.Tokens[9].Kind).IsEqualTo(SqlTokenKind.HexString);
        await Assert.That(result.Tokens[9].Value).IsEqualTo("0xFF");
    }

    [Test]
    public async Task Operators_AreRecognized()
    {
        var result = SqlTokenizer.Tokenize("a<>b != c <= d >= e || f :: g -> h ->> i");

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.NotEqual);
        await Assert.That(result.Tokens[3].Kind).IsEqualTo(SqlTokenKind.NotEqual);
        await Assert.That(result.Tokens[5].Kind).IsEqualTo(SqlTokenKind.LessThanOrEqual);
        await Assert.That(result.Tokens[7].Kind).IsEqualTo(SqlTokenKind.GreaterThanOrEqual);
        await Assert.That(result.Tokens[9].Kind).IsEqualTo(SqlTokenKind.Concat);
        await Assert.That(result.Tokens[11].Kind).IsEqualTo(SqlTokenKind.ColonColon);
        await Assert.That(result.Tokens[13].Kind).IsEqualTo(SqlTokenKind.Arrow);
        await Assert.That(result.Tokens[15].Kind).IsEqualTo(SqlTokenKind.LongArrow);
    }

    [Test]
    public async Task ParametersAndVariables_AreRecognized()
    {
        var result = SqlTokenizer.Tokenize(
            "select * from t where a = ? and b = @p and c = :name and d = $1");

        await Assert.That(result.Tokens[7].Kind).IsEqualTo(SqlTokenKind.Parameter);
        await Assert.That(result.Tokens[7].Value).IsEqualTo("?");
        await Assert.That(result.Tokens[11].Kind).IsEqualTo(SqlTokenKind.Variable);
        await Assert.That(result.Tokens[11].Value).IsEqualTo("@p");
        await Assert.That(result.Tokens[15].Kind).IsEqualTo(SqlTokenKind.Parameter);
        await Assert.That(result.Tokens[15].Value).IsEqualTo(":name");
        await Assert.That(result.Tokens[19].Kind).IsEqualTo(SqlTokenKind.Parameter);
        await Assert.That(result.Tokens[19].Value).IsEqualTo("$1");
    }

    [Test]
    public async Task UnicodeIdentifier_IsSingleToken()
    {
        var result = SqlTokenizer.Tokenize("select 用户_$1 from 表");

        await Assert.That(result.Tokens[1].Kind).IsEqualTo(SqlTokenKind.Identifier);
        await Assert.That(result.Tokens[1].Value).IsEqualTo("用户_$1");
        await Assert.That(result.Tokens[3].Value).IsEqualTo("表");
    }

    [Test]
    public async Task EmptyInput_ProducesOnlyEof()
    {
        var result = SqlTokenizer.Tokenize(string.Empty);

        await Assert.That(result.Tokens.Length).IsEqualTo(1);
        await Assert.That(result.Tokens[0].Kind).IsEqualTo(SqlTokenKind.EndOfFile);
        await Assert.That(result.Success).IsTrue();
    }
}
