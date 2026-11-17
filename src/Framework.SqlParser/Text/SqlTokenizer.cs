using System.Collections.Immutable;
using System.Text;

namespace Framework.SqlParser;

/// <summary>
/// SQL 词法分析器。单遍扫描，识别关键字、标识符、字面量、参数与运算符；
/// 空白与注释被跳过，未闭合结构会产出诊断而不是抛异常。
/// </summary>
public static class SqlTokenizer
{
    /// <summary>对 SQL 文本进行词法分析。</summary>
    /// <param name="sql">SQL 文本。</param>
    /// <param name="dialect">目标方言；为 <c>null</c> 时使用 <paramref name="options"/> 的方言。</param>
    /// <param name="options">解析选项；为 <c>null</c> 时使用默认值。</param>
    public static SqlTokenizeResult Tokenize(
        string sql,
        SqlDialect? dialect = null,
        SqlParserOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sql);
        options ??= SqlParserOptions.Default;

        if (options.MaxInputLength > 0 && sql.Length > options.MaxInputLength)
        {
            var diagnostic = new SqlDiagnostic(
                SqlDiagnosticCodes.InputTooLong,
                SqlDiagnosticSeverity.Error,
                $"SQL 文本长度 {sql.Length} 超过上限 {options.MaxInputLength}。",
                new SqlSpan(0, sql.Length),
                1,
                1);
            return new SqlTokenizeResult(
                sql,
                ImmutableArray<SqlToken>.Empty,
                ImmutableArray.Create(diagnostic));
        }

        return new Scanner(sql, dialect ?? options.Dialect, options).Run();
    }

    private sealed class Scanner
    {
        private readonly string _text;
        private readonly SqlDialect _dialect;
        private readonly SqlParserOptions _options;
        private readonly List<SqlToken> _tokens = [];
        private readonly List<SqlDiagnostic> _diagnostics = [];
        private int _pos;
        private int _line = 1;
        private int _lineStart;
        private int _errorCount;

        public Scanner(string text, SqlDialect dialect, SqlParserOptions options)
        {
            _text = text;
            _dialect = dialect;
            _options = options;
        }

        private char Current => _pos < _text.Length ? _text[_pos] : '\0';

        public SqlTokenizeResult Run()
        {
            while (true)
            {
                SkipTrivia();
                if (_pos >= _text.Length)
                {
                    break;
                }

                var start = _pos;
                var line = _line;
                var column = _pos - _lineStart + 1;
                var token = NextToken(start, line, column);
                if (token is { } value)
                {
                    _tokens.Add(value);
                }

                if (_options.MaxErrors > 0 && _errorCount >= _options.MaxErrors)
                {
                    break;
                }
            }

            _tokens.Add(new SqlToken(
                SqlTokenKind.EndOfFile,
                SqlKeywordId.None,
                new SqlSpan(_text.Length, 0),
                _line,
                _pos - _lineStart + 1));

            return new SqlTokenizeResult(
                _text,
                ImmutableArray.CreateRange(_tokens),
                ImmutableArray.CreateRange(_diagnostics));
        }

        private SqlToken? NextToken(int start, int line, int column)
        {
            var c = Current;

            // 字符串前缀必须优先于普通标识符判断。
            if ((c is 'N' or 'n') &&
                Peek() == '\'' &&
                _dialect.Supports(SqlDialectCapabilities.NationalStringPrefix))
            {
                return LexQuotedString(start, line, column, 'N', forceBackslash: false);
            }

            if ((c is 'E' or 'e') &&
                Peek() == '\'' &&
                _dialect.Supports(SqlDialectCapabilities.EscapeStringPrefix))
            {
                return LexQuotedString(start, line, column, 'E', forceBackslash: true);
            }

            if ((c is 'Q' or 'q') &&
                Peek() == '\'' &&
                _dialect.Supports(SqlDialectCapabilities.OracleQuotedStrings))
            {
                return LexOracleQuotedString(start, line, column);
            }

            if (c == '\'')
            {
                return LexQuotedString(start, line, column, '\0', forceBackslash: false);
            }

            if (c == '"')
            {
                return LexQuotedIdentifier(start, line, column, '"', SqlTokenFlags.DoubleQuotedIdentifier);
            }

            if (c == '`' && _dialect.Supports(SqlDialectCapabilities.BacktickIdentifiers))
            {
                return LexQuotedIdentifier(start, line, column, '`', SqlTokenFlags.BacktickIdentifier);
            }

            if (c == '[' && _dialect.Supports(SqlDialectCapabilities.BracketIdentifiers))
            {
                return LexBracketIdentifier(start, line, column);
            }

            if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek())))
            {
                return LexNumber(start, line, column);
            }

            if (c == '@')
            {
                return LexVariable(start, line, column);
            }

            if (c == ':')
            {
                if (Peek() == ':')
                {
                    Advance();
                    Advance();
                    return Token(SqlTokenKind.ColonColon, start, line, column);
                }

                if (IsIdentifierStart(Peek()))
                {
                    return LexNamedParameter(start, line, column);
                }
            }

            if (c == '$')
            {
                if (char.IsDigit(Peek()))
                {
                    return LexDollarParameter(start, line, column);
                }

                if (_dialect.Supports(SqlDialectCapabilities.DollarQuotedStrings))
                {
                    return LexDollarQuotedString(start, line, column);
                }
            }

            if (c == '?')
            {
                Advance();
                return Token(SqlTokenKind.Parameter, start, line, column, "?");
            }

            if (IsIdentifierStart(c))
            {
                return LexIdentifier(start, line, column);
            }

            return LexOperatorOrPunctuation(start, line, column);
        }

        private SqlToken LexIdentifier(int start, int line, int column)
        {
            while (IsIdentifierPart(Current))
            {
                Advance();
            }

            var span = _text.AsSpan(start, _pos - start);
            if (SqlKeywords.TryGet(span, out var keyword))
            {
                return new SqlToken(
                    SqlTokenKind.Keyword,
                    keyword,
                    new SqlSpan(start, span.Length),
                    line,
                    column);
            }

            return Token(SqlTokenKind.Identifier, start, line, column, span.ToString());
        }

        private SqlToken LexVariable(int start, int line, int column)
        {
            Advance();
            if (Current == '@')
            {
                Advance();
            }

            while (IsIdentifierPart(Current))
            {
                Advance();
            }

            return Token(SqlTokenKind.Variable, start, line, column, _text[start.._pos]);
        }

        private SqlToken LexNamedParameter(int start, int line, int column)
        {
            Advance();
            while (IsIdentifierPart(Current))
            {
                Advance();
            }

            return Token(SqlTokenKind.Parameter, start, line, column, _text[start.._pos]);
        }

        private SqlToken LexDollarParameter(int start, int line, int column)
        {
            Advance();
            while (char.IsDigit(Current))
            {
                Advance();
            }

            return Token(SqlTokenKind.Parameter, start, line, column, _text[start.._pos]);
        }

        private SqlToken LexNumber(int start, int line, int column)
        {
            if (Current == '0' && (Peek() is 'x' or 'X'))
            {
                Advance();
                Advance();
                while (IsHexDigit(Current))
                {
                    Advance();
                }

                return Token(SqlTokenKind.HexString, start, line, column, _text[start.._pos]);
            }

            while (char.IsDigit(Current))
            {
                Advance();
            }

            if (Current == '.' && char.IsDigit(Peek()))
            {
                Advance();
                while (char.IsDigit(Current))
                {
                    Advance();
                }
            }

            if (Current is 'e' or 'E' &&
                (char.IsDigit(Peek()) ||
                 ((Peek() is '+' or '-') && char.IsDigit(Peek(2)))))
            {
                Advance();
                if (Current is '+' or '-')
                {
                    Advance();
                }

                while (char.IsDigit(Current))
                {
                    Advance();
                }
            }

            return Token(SqlTokenKind.Number, start, line, column, _text[start.._pos]);
        }

        private SqlToken LexQuotedString(
            int start,
            int line,
            int column,
            char prefix,
            bool forceBackslash)
        {
            var flags = SqlTokenFlags.None;
            if (prefix == 'N')
            {
                flags |= SqlTokenFlags.NationalString;
            }
            else if (prefix == 'E')
            {
                flags |= SqlTokenFlags.EscapeString;
            }

            if (prefix != '\0')
            {
                Advance();
            }

            Advance(); // 开引号

            var builder = new StringBuilder();
            var terminated = false;
            var backslash = forceBackslash ||
                            _dialect.Supports(SqlDialectCapabilities.BackslashEscapes);

            while (_pos < _text.Length)
            {
                var ch = Current;
                if (ch == '\'')
                {
                    if (Peek() == '\'')
                    {
                        builder.Append('\'');
                        Advance();
                        Advance();
                        continue;
                    }

                    Advance();
                    terminated = true;
                    break;
                }

                if (ch == '\\' && backslash)
                {
                    Advance();
                    if (_pos >= _text.Length)
                    {
                        break;
                    }

                    var escaped = Current;
                    builder.Append(escaped switch
                    {
                        '0' => '\0',
                        'b' => '\b',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'Z' => '\u001A',
                        _ => escaped,
                    });
                    Advance();
                    continue;
                }

                builder.Append(ch);
                Advance();
            }

            if (!terminated)
            {
                Report(SqlDiagnosticCodes.UnterminatedToken, start, _pos, line, column, "字符串字面量未闭合。");
            }

            return Token(SqlTokenKind.String, start, line, column, builder.ToString(), flags);
        }

        private SqlToken LexOracleQuotedString(int start, int line, int column)
        {
            Advance(); // q
            Advance(); // '

            if (_pos >= _text.Length)
            {
                Report(SqlDiagnosticCodes.UnterminatedToken, start, _pos, line, column, "Oracle q 字符串未闭合。");
                return Token(SqlTokenKind.String, start, line, column, string.Empty, SqlTokenFlags.OracleQuotedString);
            }

            var open = Current;
            var close = open switch
            {
                '[' => ']',
                '{' => '}',
                '(' => ')',
                '<' => '>',
                _ => open,
            };
            Advance();

            var builder = new StringBuilder();
            var terminated = false;
            while (_pos < _text.Length)
            {
                var ch = Current;
                if (ch == close && Peek() == '\'')
                {
                    Advance();
                    Advance();
                    terminated = true;
                    break;
                }

                builder.Append(ch);
                Advance();
            }

            if (!terminated)
            {
                Report(SqlDiagnosticCodes.UnterminatedToken, start, _pos, line, column, "Oracle q 字符串未闭合。");
            }

            return Token(
                SqlTokenKind.String,
                start,
                line,
                column,
                builder.ToString(),
                SqlTokenFlags.OracleQuotedString);
        }

        private SqlToken LexDollarQuotedString(int start, int line, int column)
        {
            var tagEnd = _pos + 1;
            while (tagEnd < _text.Length &&
                   _text[tagEnd] != '$' &&
                   IsIdentifierPart(_text[tagEnd]))
            {
                tagEnd++;
            }

            if (tagEnd >= _text.Length || _text[tagEnd] != '$')
            {
                var unknown = Current;
                Advance();
                Report(
                    SqlDiagnosticCodes.UnknownCharacter,
                    start,
                    _pos,
                    line,
                    column,
                    $"无法识别的字符 '{unknown}'。");
                return Token(SqlTokenKind.Unknown, start, line, column, unknown.ToString());
            }

            var tag = _text.Substring(_pos, tagEnd - _pos + 1);
            var bodyStart = tagEnd + 1;
            var close = _text.IndexOf(tag, bodyStart, StringComparison.Ordinal);

            string value;
            if (close < 0)
            {
                value = _text[bodyStart..];
                AdvanceTo(_text.Length);
                Report(
                    SqlDiagnosticCodes.UnterminatedToken,
                    start,
                    _pos,
                    line,
                    column,
                    "美元引用字符串未闭合。");
            }
            else
            {
                value = _text[bodyStart..close];
                AdvanceTo(close + tag.Length);
            }

            return Token(
                SqlTokenKind.String,
                start,
                line,
                column,
                value,
                SqlTokenFlags.DollarQuotedString);
        }

        private SqlToken LexQuotedIdentifier(
            int start,
            int line,
            int column,
            char quote,
            SqlTokenFlags flags)
        {
            Advance();

            var builder = new StringBuilder();
            var terminated = false;
            while (_pos < _text.Length)
            {
                var ch = Current;
                if (ch == quote)
                {
                    if (Peek() == quote)
                    {
                        builder.Append(quote);
                        Advance();
                        Advance();
                        continue;
                    }

                    Advance();
                    terminated = true;
                    break;
                }

                builder.Append(ch);
                Advance();
            }

            if (!terminated)
            {
                Report(SqlDiagnosticCodes.UnterminatedToken, start, _pos, line, column, "引号标识符未闭合。");
            }

            return Token(
                SqlTokenKind.QuotedIdentifier,
                start,
                line,
                column,
                builder.ToString(),
                flags);
        }

        private SqlToken LexBracketIdentifier(int start, int line, int column)
        {
            Advance();

            var builder = new StringBuilder();
            var terminated = false;
            while (_pos < _text.Length)
            {
                var ch = Current;
                if (ch == ']')
                {
                    if (Peek() == ']')
                    {
                        builder.Append(']');
                        Advance();
                        Advance();
                        continue;
                    }

                    Advance();
                    terminated = true;
                    break;
                }

                builder.Append(ch);
                Advance();
            }

            if (!terminated)
            {
                Report(SqlDiagnosticCodes.UnterminatedToken, start, _pos, line, column, "方括号标识符未闭合。");
            }

            return Token(
                SqlTokenKind.QuotedIdentifier,
                start,
                line,
                column,
                builder.ToString(),
                SqlTokenFlags.BracketIdentifier);
        }

        private SqlToken? LexOperatorOrPunctuation(int start, int line, int column)
        {
            var c = Current;
            switch (c)
            {
                case ',':
                    Advance();
                    return Token(SqlTokenKind.Comma, start, line, column);
                case '.':
                    Advance();
                    return Token(SqlTokenKind.Dot, start, line, column);
                case ';':
                    Advance();
                    return Token(SqlTokenKind.Semicolon, start, line, column);
                case '(':
                    Advance();
                    return Token(SqlTokenKind.LeftParen, start, line, column);
                case ')':
                    Advance();
                    return Token(SqlTokenKind.RightParen, start, line, column);
                case '[':
                    Advance();
                    return Token(SqlTokenKind.LeftBracket, start, line, column);
                case ']':
                    Advance();
                    return Token(SqlTokenKind.RightBracket, start, line, column);
                case '+':
                    Advance();
                    return Token(SqlTokenKind.Plus, start, line, column);
                case '-':
                    if (Peek() == '>')
                    {
                        Advance();
                        Advance();
                        if (Current == '>')
                        {
                            Advance();
                            return Token(SqlTokenKind.LongArrow, start, line, column);
                        }

                        return Token(SqlTokenKind.Arrow, start, line, column);
                    }

                    Advance();
                    return Token(SqlTokenKind.Minus, start, line, column);
                case '*':
                    Advance();
                    return Token(SqlTokenKind.Star, start, line, column);
                case '/':
                    Advance();
                    return Token(SqlTokenKind.Slash, start, line, column);
                case '%':
                    Advance();
                    return Token(SqlTokenKind.Percent, start, line, column);
                case '=':
                    Advance();
                    return Token(SqlTokenKind.Equal, start, line, column);
                case '<':
                    if (Peek() == '=')
                    {
                        Advance();
                        Advance();
                        return Token(SqlTokenKind.LessThanOrEqual, start, line, column);
                    }

                    if (Peek() == '>')
                    {
                        Advance();
                        Advance();
                        return Token(SqlTokenKind.NotEqual, start, line, column);
                    }

                    Advance();
                    return Token(SqlTokenKind.LessThan, start, line, column);
                case '>':
                    if (Peek() == '=')
                    {
                        Advance();
                        Advance();
                        return Token(SqlTokenKind.GreaterThanOrEqual, start, line, column);
                    }

                    Advance();
                    return Token(SqlTokenKind.GreaterThan, start, line, column);
                case '!':
                    if (Peek() == '=')
                    {
                        Advance();
                        Advance();
                        return Token(SqlTokenKind.NotEqual, start, line, column);
                    }

                    break;
                case '|':
                    if (Peek() == '|')
                    {
                        Advance();
                        Advance();
                        return Token(SqlTokenKind.Concat, start, line, column);
                    }

                    Advance();
                    return Token(SqlTokenKind.Pipe, start, line, column);
                case '&':
                    Advance();
                    return Token(SqlTokenKind.Ampersand, start, line, column);
                case '^':
                    Advance();
                    return Token(SqlTokenKind.Caret, start, line, column);
                case '~':
                    Advance();
                    return Token(SqlTokenKind.Tilde, start, line, column);
            }

            var unknownStart = _pos;
            var unknownChar = c;
            Advance();
            Report(
                SqlDiagnosticCodes.UnknownCharacter,
                unknownStart,
                _pos,
                line,
                column,
                $"无法识别的字符 '{unknownChar}'。");
            return Token(SqlTokenKind.Unknown, unknownStart, line, column, unknownChar.ToString());
        }

        private void SkipTrivia()
        {
            while (_pos < _text.Length)
            {
                var c = Current;
                if (char.IsWhiteSpace(c))
                {
                    Advance();
                    continue;
                }

                if (c == '-' && Peek() == '-')
                {
                    Advance();
                    Advance();
                    while (_pos < _text.Length && Current != '\n')
                    {
                        Advance();
                    }

                    continue;
                }

                if (c == '#' && _dialect.Supports(SqlDialectCapabilities.HashComments))
                {
                    while (_pos < _text.Length && Current != '\n')
                    {
                        Advance();
                    }

                    continue;
                }

                if (c == '/' && Peek() == '*')
                {
                    SkipBlockComment();
                    continue;
                }

                break;
            }
        }

        private void SkipBlockComment()
        {
            var start = _pos;
            var line = _line;
            var column = _pos - _lineStart + 1;
            var nested = _dialect.Supports(SqlDialectCapabilities.NestedBlockComments);

            Advance();
            Advance(); // 开头的 /*

            var depth = 1;
            while (_pos < _text.Length)
            {
                if (nested && Current == '/' && Peek() == '*')
                {
                    depth++;
                    Advance();
                    Advance();
                    continue;
                }

                if (Current == '*' && Peek() == '/')
                {
                    depth--;
                    Advance();
                    Advance();
                    if (depth == 0)
                    {
                        return;
                    }

                    continue;
                }

                Advance();
            }

            Report(SqlDiagnosticCodes.UnterminatedToken, start, _pos, line, column, "块注释未闭合。");
        }

        private bool IsIdentifierStart(char c) =>
            c == '_' ||
            char.IsLetter(c) ||
            (c == '#' && _dialect.Supports(SqlDialectCapabilities.HashPrefixedIdentifiers));

        private bool IsIdentifierPart(char c) =>
            c == '_' ||
            c == '$' ||
            char.IsLetterOrDigit(c) ||
            (c == '#' && _dialect.Supports(SqlDialectCapabilities.HashPrefixedIdentifiers));

        private static bool IsHexDigit(char c) =>
            char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

        private char Peek(int offset = 1) =>
            _pos + offset < _text.Length ? _text[_pos + offset] : '\0';

        private void Advance()
        {
            if (_pos >= _text.Length)
            {
                return;
            }

            if (_text[_pos] == '\n')
            {
                _line++;
                _lineStart = _pos + 1;
            }

            _pos++;
        }

        private void AdvanceTo(int target)
        {
            for (var i = _pos; i < target; i++)
            {
                if (_text[i] == '\n')
                {
                    _line++;
                    _lineStart = i + 1;
                }
            }

            _pos = target;
        }

        private SqlToken Token(
            SqlTokenKind kind,
            int start,
            int line,
            int column,
            string? value = null,
            SqlTokenFlags flags = SqlTokenFlags.None) =>
            new(kind, SqlKeywordId.None, new SqlSpan(start, _pos - start), line, column, value, flags);

        private void Report(
            string code,
            int start,
            int end,
            int line,
            int column,
            string message)
        {
            _diagnostics.Add(new SqlDiagnostic(
                code,
                SqlDiagnosticSeverity.Error,
                message,
                new SqlSpan(start, Math.Max(0, end - start)),
                line,
                column));

            _errorCount++;
        }
    }
}
