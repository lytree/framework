using System.Collections.Immutable;

namespace Framework.SqlParser.Parsing;

/// <summary>
/// 解析上下文：token 游标、诊断收集、嵌套深度与错误计数。
/// 构造失败（Expect 不匹配）时只记诊断，不抛异常，由上层决定是否停止。
/// </summary>
internal sealed class ParserContext
{
    private readonly ImmutableArray<SqlToken> _tokens;
    private readonly List<SqlDiagnostic> _diagnostics;
    private int _index;
    private int _depth;
    private int _errorCount;

    public ParserContext(
        string source,
        ImmutableArray<SqlToken> tokens,
        SqlParserOptions options,
        IEnumerable<SqlDiagnostic>? diagnostics = null)
    {
        Source = source;
        Options = options;
        Dialect = options.Dialect;
        _tokens = tokens.Length == 0
            ? ImmutableArray.Create(new SqlToken(
                SqlTokenKind.EndOfFile,
                SqlKeywordId.None,
                new SqlSpan(source.Length, 0),
                1,
                1))
            : tokens;

        _diagnostics = diagnostics is null ? [] : [.. diagnostics];
        foreach (var diagnostic in _diagnostics)
        {
            if (diagnostic.Severity == SqlDiagnosticSeverity.Error)
            {
                _errorCount++;
            }
        }
    }

    /// <summary>原始 SQL 文本。</summary>
    public string Source { get; }

    /// <summary>解析选项。</summary>
    public SqlParserOptions Options { get; }

    /// <summary>目标方言。</summary>
    public SqlDialect Dialect { get; }

    /// <summary>已收集的诊断。</summary>
    public IReadOnlyList<SqlDiagnostic> Diagnostics => _diagnostics;

    /// <summary>是否已达到错误上限。</summary>
    public bool TooManyErrors => Options.MaxErrors > 0 && _errorCount >= Options.MaxErrors;

    /// <summary>当前 token 下标；赋值用于前瞻回退。</summary>
    public int Index
    {
        get => _index;
        set => _index = Math.Clamp(value, 0, _tokens.Length - 1);
    }

    /// <summary>当前 token。</summary>
    public SqlToken Current => _tokens[_index];

    /// <summary>是否到达输入末尾。</summary>
    public bool IsAtEnd => Current.Kind == SqlTokenKind.EndOfFile;

    /// <summary>向前查看第 <paramref name="offset"/> 个 token（1 表示下一个）。</summary>
    public SqlToken Peek(int offset = 1) => _tokens[Math.Min(_index + offset, _tokens.Length - 1)];

    /// <summary>消费当前 token。</summary>
    public SqlToken Advance()
    {
        var token = Current;
        if (_index < _tokens.Length - 1)
        {
            _index++;
        }

        return token;
    }

    /// <summary>当前 token 是否为指定类型。</summary>
    public bool At(SqlTokenKind kind) => Current.Kind == kind;

    /// <summary>当前 token 是否为指定关键字。</summary>
    public bool AtKeyword(SqlKeywordId keyword) => Current.Is(keyword);

    /// <summary>匹配并消费指定类型。</summary>
    public bool Match(SqlTokenKind kind)
    {
        if (!At(kind))
        {
            return false;
        }

        Advance();
        return true;
    }

    /// <summary>匹配并消费指定关键字。</summary>
    public bool MatchKeyword(SqlKeywordId keyword)
    {
        if (!AtKeyword(keyword))
        {
            return false;
        }

        Advance();
        return true;
    }

    /// <summary>期望指定类型；失败时记诊断且不消费。</summary>
    public SqlToken Expect(SqlTokenKind kind, string? expected = null)
    {
        if (At(kind))
        {
            return Advance();
        }

        Report(
            SqlDiagnosticCodes.SyntaxError,
            Current,
            $"预期 {expected ?? Describe(kind)}，实际为 {Describe(Current)}。");
        return Current;
    }

    /// <summary>期望指定关键字；失败时记诊断且不消费。</summary>
    public SqlToken ExpectKeyword(SqlKeywordId keyword, string? expected = null)
    {
        if (AtKeyword(keyword))
        {
            return Advance();
        }

        Report(
            SqlDiagnosticCodes.SyntaxError,
            Current,
            $"预期 {expected ?? keyword.ToString().ToUpperInvariant()}，实际为 {Describe(Current)}。");
        return Current;
    }

    /// <summary>记录错误级诊断。</summary>
    public void Report(string code, SqlToken token, string message)
    {
        _diagnostics.Add(new SqlDiagnostic(
            code,
            SqlDiagnosticSeverity.Error,
            message,
            token.Span,
            token.Line,
            token.Column));
        _errorCount++;
    }

    /// <summary>记录警告级诊断。</summary>
    public void ReportWarning(string code, SqlToken token, string message)
    {
        _diagnostics.Add(new SqlDiagnostic(
            code,
            SqlDiagnosticSeverity.Warning,
            message,
            token.Span,
            token.Line,
            token.Column));
    }

    /// <summary>进入一层嵌套；超过上限时记诊断并返回 <c>false</c>。</summary>
    public bool TryEnterDepth()
    {
        if (_depth >= Options.MaxDepth)
        {
            Report(
                SqlDiagnosticCodes.NestingTooDeep,
                Current,
                $"表达式嵌套深度超过上限 {Options.MaxDepth}。");
            return false;
        }

        _depth++;
        return true;
    }

    /// <summary>退出一层嵌套。</summary>
    public void ExitDepth() => _depth--;

    /// <summary>取得从 <paramref name="startIndex"/> 到已消费末尾的区间。</summary>
    public SqlSpan SpanFrom(int startIndex, int endExclusive = -1)
    {
        if (endExclusive < 0)
        {
            endExclusive = _index;
        }

        var first = TokenAt(startIndex);
        if (endExclusive <= startIndex)
        {
            return new SqlSpan(first.Span.Start, 0);
        }

        var last = TokenAt(endExclusive - 1);
        return new SqlSpan(first.Span.Start, Math.Max(0, last.Span.End - first.Span.Start));
    }

    /// <summary>获取 token 的原始文本：标识符取值，关键字与标点从源文本切片。</summary>
    public string TokenText(SqlToken token)
    {
        if (token.Value is not null)
        {
            return token.Value;
        }

        return token.Span.Length > 0 && token.Span.End <= Source.Length
            ? Source.Substring(token.Span.Start, token.Span.Length)
            : string.Empty;
    }

    /// <summary>跳过到语句结束（分号或末尾）。</summary>
    public void SkipToStatementEnd()
    {
        while (!IsAtEnd && !At(SqlTokenKind.Semicolon))
        {
            Advance();
        }
    }

    /// <summary>跳过剩余全部 token。</summary>
    public void SkipToEnd()
    {
        while (!IsAtEnd)
        {
            Advance();
        }
    }

    /// <summary>消费尾部分号；若仍有剩余 token，记语法错误并跳过。</summary>
    public void SkipToEndIfTrailing(string message = "表达式结束后存在无法解析的标记。")
    {
        while (Match(SqlTokenKind.Semicolon))
        {
        }

        if (IsAtEnd)
        {
            return;
        }

        Report(SqlDiagnosticCodes.SyntaxError, Current, message);
        SkipToEnd();
    }

    private SqlToken TokenAt(int index) => _tokens[Math.Clamp(index, 0, _tokens.Length - 1)];

    private static string Describe(SqlToken token) => token.Kind switch
    {
        SqlTokenKind.EndOfFile => "输入结束",
        SqlTokenKind.Keyword => $"关键字 {token.Keyword.ToString().ToUpperInvariant()}",
        SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier => $"标识符 '{token.Value}'",
        SqlTokenKind.Number => $"数字 '{token.Value}'",
        SqlTokenKind.String => "字符串字面量",
        _ => token.Kind.ToString(),
    };

    private static string Describe(SqlTokenKind kind) => kind switch
    {
        SqlTokenKind.Identifier => "标识符",
        SqlTokenKind.QuotedIdentifier => "引号标识符",
        SqlTokenKind.Number => "数字",
        SqlTokenKind.String => "字符串",
        SqlTokenKind.EndOfFile => "输入结束",
        SqlTokenKind.LeftParen => "'('",
        SqlTokenKind.RightParen => "')'",
        SqlTokenKind.Comma => "','",
        SqlTokenKind.Dot => "'.'",
        SqlTokenKind.Semicolon => "';'",
        _ => kind.ToString(),
    };
}
