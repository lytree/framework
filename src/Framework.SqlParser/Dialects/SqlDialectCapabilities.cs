namespace Framework.SqlParser;

/// <summary>
/// 方言能力位。解析器与词法器通过这些标志决定可接受的语法，而不是到处写数据库类型判断。
/// </summary>
[Flags]
public enum SqlDialectCapabilities
{
    /// <summary>无特殊能力。</summary>
    None = 0,

    /// <summary>支持 <c>[标识符]</c>。</summary>
    BracketIdentifiers = 1 << 0,

    /// <summary>支持 <c>`标识符`</c>。</summary>
    BacktickIdentifiers = 1 << 1,

    /// <summary>支持 <c>$$...$$</c> 与 <c>$tag$...$tag$</c> 字符串。</summary>
    DollarQuotedStrings = 1 << 2,

    /// <summary>支持 <c>E'...'</c> 转义字符串。</summary>
    EscapeStringPrefix = 1 << 3,

    /// <summary>支持 <c>N'...'</c> 国家字符集字符串。</summary>
    NationalStringPrefix = 1 << 4,

    /// <summary>字符串内支持反斜杠转义。</summary>
    BackslashEscapes = 1 << 5,

    /// <summary>支持 Oracle <c>q'[...]'</c> 字符串。</summary>
    OracleQuotedStrings = 1 << 6,

    /// <summary><c>#</c> 起始行注释（MySQL）。</summary>
    HashComments = 1 << 7,

    /// <summary>块注释支持嵌套（PostgreSQL）。</summary>
    NestedBlockComments = 1 << 8,

    /// <summary>支持 <c>LIMIT</c>。</summary>
    LimitClause = 1 << 9,

    /// <summary>支持 <c>LIMIT offset, count</c>。</summary>
    LimitOffsetComma = 1 << 10,

    /// <summary>支持 <c>TOP (n)</c>。</summary>
    TopClause = 1 << 11,

    /// <summary>支持 <c>OFFSET ... FETCH</c>。</summary>
    OffsetFetch = 1 << 12,

    /// <summary>支持 <c>RETURNING</c>。</summary>
    Returning = 1 << 13,

    /// <summary>支持 <c>OUTPUT</c>。</summary>
    OutputClause = 1 << 14,

    /// <summary>支持 <c>DISTINCT ON</c>。</summary>
    DistinctOn = 1 << 15,

    /// <summary>支持 <c>LATERAL</c>。</summary>
    Lateral = 1 << 16,

    /// <summary>支持 Oracle <c>CONNECT BY</c>。</summary>
    ConnectBy = 1 << 17,

    /// <summary>支持 <c>::</c> 类型转换。</summary>
    DoubleColonCast = 1 << 18,

    /// <summary>支持 <c>ILIKE</c>。</summary>
    CaseInsensitiveLike = 1 << 19,

    /// <summary>支持 <c>-&gt;</c> / <c>-&gt;&gt;</c> JSON 运算符。</summary>
    JsonArrowOperators = 1 << 20,

    /// <summary><c>[...]</c> 表示下标而非标识符。</summary>
    SquareBracketSubscript = 1 << 21,

    /// <summary>支持 <c>AT TIME ZONE</c>。</summary>
    AtTimeZone = 1 << 22,

    /// <summary>支持 <c>QUALIFY</c>。</summary>
    QualifyClause = 1 << 23,

    /// <summary>支持 <c>#临时表</c> 形式的标识符（TSql）。</summary>
    HashPrefixedIdentifiers = 1 << 24,
}
