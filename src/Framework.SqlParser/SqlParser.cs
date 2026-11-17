using System.Collections.Immutable;
using Framework.SqlParser.Ast;
using Framework.SqlParser.Parsing;

namespace Framework.SqlParser;

/// <summary>
/// SQL 解析器门面。实例不可变、线程安全，可单例复用。
/// </summary>
public sealed class SqlParser
{
    /// <summary>默认解析器（Generic 方言）。</summary>
    public static SqlParser Default { get; } = new(SqlParserOptions.Default);

    /// <summary>使用指定选项创建解析器。</summary>
    public SqlParser(SqlParserOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>当前解析选项。</summary>
    public SqlParserOptions Options { get; }

    /// <summary>解析单条语句；存在错误级诊断时抛 <see cref="SqlParseException"/>。</summary>
    public SqlStatement Parse(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var outcome = SqlParserEngine.ParseStatement(sql, Options);
        if (!outcome.Success || outcome.Statement is null)
        {
            throw new SqlParseException(DescribeError("SQL 解析失败", outcome.Diagnostics), outcome.Diagnostics);
        }

        return outcome.Statement;
    }

    /// <summary>尝试解析单条语句，不抛异常；通过 <paramref name="diagnostics"/> 返回诊断与部分 AST。</summary>
    public bool TryParse(
        string sql,
        out SqlStatement? statement,
        out IReadOnlyList<SqlDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var outcome = SqlParserEngine.ParseStatement(sql, Options);
        statement = outcome.Statement;
        diagnostics = outcome.Diagnostics;
        return outcome.Success;
    }

    /// <summary>解析孤立表达式；存在错误级诊断时抛 <see cref="SqlParseException"/>。</summary>
    public SqlExpression ParseExpression(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var outcome = SqlParserEngine.ParseExpression(sql, Options);
        if (!outcome.Success || outcome.Expression is null)
        {
            throw new SqlParseException(DescribeError("SQL 表达式解析失败", outcome.Diagnostics), outcome.Diagnostics);
        }

        return outcome.Expression;
    }

    internal static string DescribeError(string prefix, IReadOnlyList<SqlDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity == SqlDiagnosticSeverity.Error)
            {
                return $"{prefix}：{diagnostic.Code} ({diagnostic.Line},{diagnostic.Column}) {diagnostic.Message}";
            }
        }

        return prefix + "。";
    }
}
