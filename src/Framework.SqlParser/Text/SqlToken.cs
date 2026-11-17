namespace Framework.SqlParser;

/// <summary>
/// 一个词法单元。只保存区间、位置与解码后的值；关键字不分配字符串。
/// </summary>
public readonly struct SqlToken
{
    /// <summary>构造词法单元。</summary>
    public SqlToken(
        SqlTokenKind kind,
        SqlKeywordId keyword,
        SqlSpan span,
        int line,
        int column,
        string? value = null,
        SqlTokenFlags flags = SqlTokenFlags.None)
    {
        Kind = kind;
        Keyword = keyword;
        Span = span;
        Line = line;
        Column = column;
        Value = value;
        Flags = flags;
    }

    /// <summary>词法单元类型。</summary>
    public SqlTokenKind Kind { get; }

    /// <summary>关键字标识；仅当 <see cref="Kind"/> 为 <see cref="SqlTokenKind.Keyword"/> 时有效。</summary>
    public SqlKeywordId Keyword { get; }

    /// <summary>源码区间。</summary>
    public SqlSpan Span { get; }

    /// <summary>起始行号（1 基）。</summary>
    public int Line { get; }

    /// <summary>起始列号（1 基）。</summary>
    public int Column { get; }

    /// <summary>解码后的文本；关键字与标点通常为 <c>null</c>。</summary>
    public string? Value { get; }

    /// <summary>附加标志（引号/字面量前缀等）。</summary>
    public SqlTokenFlags Flags { get; }

    /// <summary>判断是否为指定关键字。</summary>
    public bool Is(SqlKeywordId keyword) => Kind == SqlTokenKind.Keyword && Keyword == keyword;

    /// <inheritdoc />
    public override string ToString() =>
        Kind == SqlTokenKind.Keyword
            ? $"{Kind}({Keyword}) @{Span.Start}"
            : $"{Kind}('{Value}') @{Span.Start}";
}
