namespace Framework.SqlParser;

/// <summary>词法单元类型。</summary>
public enum SqlTokenKind : byte
{
    /// <summary>输入结束。</summary>
    EndOfFile,

    /// <summary>普通标识符。</summary>
    Identifier,

    /// <summary>引号标识符（双引号、反引号或方括号）。</summary>
    QuotedIdentifier,

    /// <summary>关键字。</summary>
    Keyword,

    /// <summary>十进制数字。</summary>
    Number,

    /// <summary>字符串字面量。</summary>
    String,

    /// <summary>十六进制字面量。</summary>
    HexString,

    /// <summary>位串字面量。</summary>
    BitString,

    /// <summary>绑定参数，如 <c>?</c>、<c>@p</c>、<c>:name</c>、<c>$1</c>。</summary>
    Parameter,

    /// <summary>变量，如 <c>@@version</c>。</summary>
    Variable,

    /// <summary>逗号。</summary>
    Comma,

    /// <summary>点号。</summary>
    Dot,

    /// <summary>分号。</summary>
    Semicolon,

    /// <summary>左圆括号。</summary>
    LeftParen,

    /// <summary>右圆括号。</summary>
    RightParen,

    /// <summary>左方括号。</summary>
    LeftBracket,

    /// <summary>右方括号。</summary>
    RightBracket,

    /// <summary>加号。</summary>
    Plus,

    /// <summary>减号。</summary>
    Minus,

    /// <summary>星号。</summary>
    Star,

    /// <summary>斜杠。</summary>
    Slash,

    /// <summary>百分号。</summary>
    Percent,

    /// <summary>等号。</summary>
    Equal,

    /// <summary>不等于（<c>&lt;&gt;</c> 或 <c>!=</c>）。</summary>
    NotEqual,

    /// <summary>小于。</summary>
    LessThan,

    /// <summary>小于等于。</summary>
    LessThanOrEqual,

    /// <summary>大于。</summary>
    GreaterThan,

    /// <summary>大于等于。</summary>
    GreaterThanOrEqual,

    /// <summary>字符串拼接 <c>||</c>。</summary>
    Concat,

    /// <summary>类型转换 <c>::</c>。</summary>
    ColonColon,

    /// <summary>JSON 箭头 <c>-&gt;</c>。</summary>
    Arrow,

    /// <summary>JSON 文本箭头 <c>-&gt;&gt;</c>。</summary>
    LongArrow,

    /// <summary>问号。</summary>
    Question,

    /// <summary>按位与 <c>&amp;</c>。</summary>
    Ampersand,

    /// <summary>按位或 <c>|</c>。</summary>
    Pipe,

    /// <summary>按位异或 <c>^</c>。</summary>
    Caret,

    /// <summary>按位取反 <c>~</c>。</summary>
    Tilde,

    /// <summary>无法识别的字符。</summary>
    Unknown,
}

/// <summary>词法单元的附加标志。</summary>
[Flags]
public enum SqlTokenFlags : byte
{
    /// <summary>无。</summary>
    None = 0,

    /// <summary><c>N'...'</c>。</summary>
    NationalString = 1 << 0,

    /// <summary><c>E'...'</c>。</summary>
    EscapeString = 1 << 1,

    /// <summary><c>$$...$$</c> / <c>$tag$...$tag$</c>。</summary>
    DollarQuotedString = 1 << 2,

    /// <summary><c>q'[...]'</c>。</summary>
    OracleQuotedString = 1 << 3,

    /// <summary>双引号标识符。</summary>
    DoubleQuotedIdentifier = 1 << 4,

    /// <summary>反引号标识符。</summary>
    BacktickIdentifier = 1 << 5,

    /// <summary>方括号标识符。</summary>
    BracketIdentifier = 1 << 6,
}
