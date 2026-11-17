using System.Text;
using Framework.SqlParser.Ast;
using Framework.SqlParser.Deparse;

namespace Framework.SqlParser.Formatting;

/// <summary>关键字大小写风格。</summary>
public enum SqlKeywordCase : byte
{
    /// <summary>大写（Deparser 默认输出）。</summary>
    Upper = 0,

    /// <summary>小写。</summary>
    Lower,

    /// <summary>保持 Deparser 输出原样。</summary>
    Preserve,
}

/// <summary>SQL 格式化选项。</summary>
public sealed class SqlFormatterOptions
{
    /// <summary>默认选项。</summary>
    public static SqlFormatterOptions Default { get; } = new();

    /// <summary>缩进空格数。</summary>
    public int IndentSize { get; init; } = 2;

    /// <summary>换行符。</summary>
    public string NewLine { get; init; } = "\n";

    /// <summary>是否缩进 JOIN 子句。</summary>
    public bool IndentJoins { get; init; } = true;

    /// <summary>是否在 FROM 前换行；关闭后 <c>SELECT ... FROM ...</c> 保持同一行。</summary>
    public bool NewLineBeforeFrom { get; init; } = true;

    /// <summary>关键字大小写风格。</summary>
    public SqlKeywordCase KeywordCase { get; init; } = SqlKeywordCase.Upper;

    /// <summary>是否把 SELECT 列表拆成每项一行。</summary>
    public bool AlignSelectItems { get; init; }

    /// <summary>拆行时逗号是否放在行首（需同时开启 <see cref="AlignSelectItems"/>）。</summary>
    public bool CommaFirst { get; init; }

    /// <summary>
    /// 分页子句的目标方言。为 <c>null</c> 时沿用 AST 中的原始分页写法
    /// （LIMIT / TOP / OFFSET ... FETCH）；设置后按目标方言输出。
    /// </summary>
    public SqlDialect? Dialect { get; init; }
}

/// <summary>
/// 多行格式化器：基于 Deparser 的规范化输出，把每个顶层子句拆到独立行，
/// JOIN 子句按 <see cref="SqlFormatterOptions.IndentSize"/> 缩进。
/// 子查询、字符串与括号内的内容保持原样。
/// </summary>
public static class SqlFormatter
{
    /// <summary>格式化单条 SQL 文本。</summary>
    public static string Format(
        string sql,
        SqlDialect? dialect = null,
        SqlFormatterOptions? options = null) =>
        Format(Sql.Parse(sql, dialect), options);

    /// <summary>格式化多条以分号分隔的 SQL 文本。</summary>
    public static string FormatBatch(
        string sql,
        SqlDialect? dialect = null,
        SqlFormatterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sql);

        var resolved = options ?? SqlFormatterOptions.Default;
        var formatted = new List<string>();

        foreach (var statementText in SplitStatements(sql))
        {
            if (string.IsNullOrWhiteSpace(statementText))
            {
                continue;
            }

            formatted.Add(Format(statementText, dialect, resolved) + ";");
        }

        return string.Join(resolved.NewLine, formatted);
    }

    /// <summary>格式化语句。</summary>
    public static string Format(SqlStatement statement, SqlFormatterOptions? options = null) =>
        FormatNode(statement, options ?? SqlFormatterOptions.Default);

    /// <summary>格式化查询表达式。</summary>
    public static string Format(SqlQueryExpression query, SqlFormatterOptions? options = null) =>
        FormatNode(query, options ?? SqlFormatterOptions.Default);

    /// <summary>格式化数据源。</summary>
    public static string Format(SqlTableSource source, SqlFormatterOptions? options = null) =>
        FormatNode(source, options ?? SqlFormatterOptions.Default);

    /// <summary>格式化表达式。</summary>
    public static string Format(SqlExpression expression, SqlFormatterOptions? options = null) =>
        FormatNode(expression, options ?? SqlFormatterOptions.Default);

    private static string FormatNode(SqlNode node, SqlFormatterOptions options)
    {
        var deparseOptions = options.Dialect is null
            ? SqlDeparseOptions.Default
            : new SqlDeparseOptions { Dialect = options.Dialect };

        var text = node switch
        {
            SqlStatement statement => SqlDeparser.Deparse(statement, deparseOptions),
            SqlQueryExpression query => SqlDeparser.Deparse(query, deparseOptions),
            SqlTableSource source => SqlDeparser.Deparse(source, deparseOptions),
            SqlExpression expression => SqlDeparser.Deparse(expression, deparseOptions),
            _ => throw new SqlDeparseException($"不支持格式化的节点：{node.GetType().Name}。"),
        };

        return ApplyKeywordCase(FormatNormalized(text, options), options.KeywordCase);
    }

    private static readonly string[] Phrases =
    [
        "CREATE TEMPORARY TABLE",
        "NATURAL LEFT OUTER JOIN",
        "NATURAL RIGHT OUTER JOIN",
        "NATURAL FULL OUTER JOIN",
        "NATURAL INNER JOIN",
        "NATURAL CROSS JOIN",
        "NATURAL LEFT JOIN",
        "NATURAL RIGHT JOIN",
        "NATURAL FULL JOIN",
        "LEFT OUTER JOIN",
        "RIGHT OUTER JOIN",
        "FULL OUTER JOIN",
        "NATURAL JOIN",
        "INNER JOIN",
        "LEFT JOIN",
        "RIGHT JOIN",
        "FULL JOIN",
        "CROSS JOIN",
        "CROSS APPLY",
        "OUTER APPLY",
        "STRAIGHT_JOIN",
        "INSERT INTO",
        "DELETE FROM",
        "MERGE INTO",
        "GROUP BY",
        "ORDER BY",
        "UNION ALL",
        "UNION DISTINCT",
        "UNION",
        "EXCEPT",
        "INTERSECT",
        "CREATE TABLE",
        "DROP TABLE",
        "TRUNCATE TABLE",
        "SELECT",
        "FROM",
        "WHERE",
        "HAVING",
        "QUALIFY",
        "WINDOW",
        "LIMIT",
        "OFFSET",
        "FETCH",
        "RETURNING",
        "OUTPUT",
        "VALUES",
        "SET",
        "WHEN",
        "UPDATE",
        "MERGE",
        "INSERT",
        "CREATE",
        "DROP",
        "TRUNCATE",
        "WITH",
    ];

    private static readonly string[] SelectModifiers = ["DISTINCTROW", "DISTINCT", "ALL"];

    private static string FormatNormalized(string text, SqlFormatterOptions options)
    {
        var output = new StringBuilder();
        var current = new StringBuilder();
        var depth = 0;
        var caseDepth = 0;
        var indent = 0;
        var inSelectList = false;

        void Flush()
        {
            var line = current.ToString().Trim();
            current.Clear();

            if (line.Length == 0)
            {
                return;
            }

            if (output.Length > 0)
            {
                output.Append(options.NewLine);
            }

            output.Append(' ', Math.Max(0, indent * options.IndentSize));
            output.Append(line);
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            switch (c)
            {
                case '\'':
                case '"':
                case '`':
                    CopyQuoted(text, ref i, current, c);
                    continue;

                case '[':
                    CopyBracket(text, ref i, current);
                    continue;

                case '$' when i + 1 < text.Length && text[i + 1] == '$':
                    CopyDollarQuoted(text, ref i, current);
                    continue;

                case '(':
                    depth++;
                    current.Append(c);
                    continue;

                case ')':
                    if (depth > 0)
                    {
                        depth--;
                    }

                    current.Append(c);
                    continue;
            }

            if (depth == 0 && c == ',' && inSelectList && options.AlignSelectItems)
            {
                if (options.CommaFirst)
                {
                    Flush();
                    indent = 1;
                    current.Append(", ");
                }
                else
                {
                    current.Append(',');
                    Flush();
                    indent = 1;
                }

                // 跳过紧随逗号的空格，避免重复缩进。
                if (i + 1 < text.Length && text[i + 1] == ' ')
                {
                    i++;
                }

                continue;
            }

            if (depth == 0 && (i == 0 || text[i - 1] == ' '))
            {
                if (MatchWord(text, i, "CASE"))
                {
                    caseDepth++;
                }
                else if (MatchWord(text, i, "END"))
                {
                    if (caseDepth > 0)
                    {
                        caseDepth--;
                    }
                }
                else if (caseDepth == 0)
                {
                    var phrase = MatchPhrase(text, i);
                    if (phrase is not null)
                    {
                        var line = current.ToString().TrimEnd();

                        if (line.Length > 0)
                        {
                            if (!ShouldSkipBreak(phrase, line, options))
                            {
                                Flush();
                                indent = options.IndentJoins && IsJoinPhrase(phrase) ? 1 : 0;
                            }
                        }
                        else
                        {
                            indent = 0;
                        }

                        if (phrase == "SELECT")
                        {
                            inSelectList = true;

                            if (options.AlignSelectItems)
                            {
                                current.Append(phrase);

                                var next = i + phrase.Length;
                                AppendSelectModifiers(text, ref next, current);
                                Flush();
                                indent = 1;
                                i = next - 1;
                                continue;
                            }
                        }
                        else
                        {
                            inSelectList = false;
                        }
                    }
                }
            }

            current.Append(c);
        }

        Flush();
        return output.ToString();
    }

    private static void AppendSelectModifiers(string text, ref int index, StringBuilder builder)
    {
        while (true)
        {
            var start = index;
            while (start < text.Length && text[start] == ' ')
            {
                start++;
            }

            if (start >= text.Length)
            {
                index = start;
                return;
            }

            var matched = false;
            foreach (var modifier in SelectModifiers)
            {
                if (!MatchWord(text, start, modifier))
                {
                    continue;
                }

                builder.Append(' ').Append(modifier);
                index = start + modifier.Length;
                matched = true;
                break;
            }

            if (matched)
            {
                continue;
            }

            if (!MatchWord(text, start, "TOP"))
            {
                index = start;
                return;
            }

            builder.Append(" TOP");
            index = start + 3;

            var open = index;
            while (open < text.Length && text[open] == ' ')
            {
                open++;
            }

            if (open < text.Length && text[open] == '(')
            {
                index = CopyParenthesized(text, open, builder);
            }
        }
    }

    /// <summary>复制从 <paramref name="index"/> 开始的括号段，返回右括号之后的位置。</summary>
    private static int CopyParenthesized(string text, int index, StringBuilder builder)
    {
        var depth = 0;

        while (index < text.Length)
        {
            var c = text[index];

            switch (c)
            {
                case '\'':
                case '"':
                case '`':
                    CopyQuoted(text, ref index, builder, c);
                    index++;
                    continue;

                case '[':
                    CopyBracket(text, ref index, builder);
                    index++;
                    continue;

                case '(':
                    depth++;
                    builder.Append(c);
                    index++;
                    continue;

                case ')':
                    depth--;
                    builder.Append(c);
                    index++;

                    if (depth == 0)
                    {
                        return index;
                    }

                    continue;

                default:
                    builder.Append(c);
                    index++;
                    continue;
            }
        }

        return index;
    }

    private static bool ShouldSkipBreak(string phrase, string currentLine, SqlFormatterOptions options)
    {
        if (phrase == "FROM" && !options.NewLineBeforeFrom)
        {
            return true;
        }

        if (phrase == "VALUES" && currentLine.EndsWith("DEFAULT", StringComparison.Ordinal))
        {
            return true;
        }

        var isActionKeyword = phrase is "UPDATE" or "SET" or "VALUES"
            || phrase.StartsWith("INSERT", StringComparison.Ordinal)
            || phrase.StartsWith("DELETE", StringComparison.Ordinal);

        return isActionKeyword && currentLine.StartsWith("WHEN ", StringComparison.Ordinal);
    }

    private static bool IsJoinPhrase(string phrase) =>
        phrase.Contains("JOIN", StringComparison.Ordinal)
        || phrase.EndsWith("APPLY", StringComparison.Ordinal)
        || phrase == "STRAIGHT_JOIN";

    private static string? MatchPhrase(string text, int index)
    {
        foreach (var phrase in Phrases)
        {
            if (MatchWord(text, index, phrase))
            {
                return phrase;
            }
        }

        return null;
    }

    private static bool MatchWord(string text, int index, string word)
    {
        if (index + word.Length > text.Length)
        {
            return false;
        }

        if (string.CompareOrdinal(text, index, word, 0, word.Length) != 0)
        {
            return false;
        }

        var end = index + word.Length;
        return end >= text.Length || !IsWordCharacter(text[end]);
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static string ApplyKeywordCase(string text, SqlKeywordCase keywordCase)
    {
        if (keywordCase == SqlKeywordCase.Upper || text.Length == 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            switch (c)
            {
                case '\'':
                case '"':
                case '`':
                    CopyQuoted(text, ref i, builder, c);
                    continue;

                case '[':
                    CopyBracket(text, ref i, builder);
                    continue;

                case '$' when i + 1 < text.Length && text[i + 1] == '$':
                    CopyDollarQuoted(text, ref i, builder);
                    continue;
            }

            if (!IsWordCharacter(c))
            {
                builder.Append(c);
                continue;
            }

            var start = i;
            while (i < text.Length && IsWordCharacter(text[i]))
            {
                i++;
            }

            var span = text.AsSpan(start, i - start);
            if (keywordCase == SqlKeywordCase.Lower && SqlKeywords.TryGet(span, out _))
            {
                builder.Append(text.Substring(start, i - start).ToLowerInvariant());
            }
            else
            {
                builder.Append(span);
            }

            i--;
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SplitStatements(string text)
    {
        var start = 0;
        var scratch = new StringBuilder();

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            switch (c)
            {
                case '\'':
                case '"':
                case '`':
                    CopyQuoted(text, ref i, scratch, c);
                    scratch.Clear();
                    continue;

                case '[':
                    CopyBracket(text, ref i, scratch);
                    scratch.Clear();
                    continue;

                case '$' when i + 1 < text.Length && text[i + 1] == '$':
                    CopyDollarQuoted(text, ref i, scratch);
                    scratch.Clear();
                    continue;

                case '-' when i + 1 < text.Length && text[i + 1] == '-':
                    while (i < text.Length && text[i] != '\n')
                    {
                        i++;
                    }

                    continue;

                case '/' when i + 1 < text.Length && text[i + 1] == '*':
                {
                    var commentDepth = 0;
                    while (i < text.Length)
                    {
                        if (text[i] == '/' && i + 1 < text.Length && text[i + 1] == '*')
                        {
                            commentDepth++;
                            i += 2;
                            continue;
                        }

                        if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '/')
                        {
                            commentDepth--;
                            i += 2;

                            if (commentDepth == 0)
                            {
                                break;
                            }

                            continue;
                        }

                        i++;
                    }

                    i--;
                    continue;
                }

                case ';':
                    yield return text[start..i];
                    start = i + 1;
                    continue;
            }
        }

        if (start < text.Length)
        {
            yield return text[start..];
        }
    }

    private static void CopyQuoted(string text, ref int index, StringBuilder builder, char quote)
    {
        builder.Append(quote);
        index++;

        while (index < text.Length)
        {
            var c = text[index];
            builder.Append(c);

            if (c != quote)
            {
                index++;
                continue;
            }

            if (index + 1 < text.Length && text[index + 1] == quote)
            {
                builder.Append(quote);
                index += 2;
                continue;
            }

            return;
        }
    }

    private static void CopyBracket(string text, ref int index, StringBuilder builder)
    {
        builder.Append('[');
        index++;

        while (index < text.Length)
        {
            var c = text[index];
            builder.Append(c);

            if (c != ']')
            {
                index++;
                continue;
            }

            if (index + 1 < text.Length && text[index + 1] == ']')
            {
                builder.Append(']');
                index += 2;
                continue;
            }

            return;
        }
    }

    private static void CopyDollarQuoted(string text, ref int index, StringBuilder builder)
    {
        builder.Append("$$");
        index += 2;

        while (index < text.Length)
        {
            if (text[index] == '$' && index + 1 < text.Length && text[index + 1] == '$')
            {
                builder.Append("$$");
                index++;
                return;
            }

            builder.Append(text[index++]);
        }
    }
}
