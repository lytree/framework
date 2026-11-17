using Framework.SqlParser.Ast;
using Framework.SqlParser.Formatting;

namespace Framework.SqlParser;

/// <summary>静态门面，等价于 <see cref="SqlParser"/> 的便捷入口。</summary>
public static class Sql
{
    /// <summary>解析单条语句。</summary>
    public static SqlStatement Parse(string sql, SqlDialect? dialect = null) =>
        Create(dialect).Parse(sql);

    /// <summary>尝试解析单条语句。</summary>
    public static bool TryParse(
        string sql,
        out SqlStatement? statement,
        out IReadOnlyList<SqlDiagnostic> diagnostics,
        SqlDialect? dialect = null) =>
        Create(dialect).TryParse(sql, out statement, out diagnostics);

    /// <summary>解析孤立表达式。</summary>
    public static SqlExpression ParseExpression(string sql, SqlDialect? dialect = null) =>
        Create(dialect).ParseExpression(sql);

    /// <summary>格式化单条语句。</summary>
    public static string Format(string sql, SqlFormatterOptions? options = null) =>
        SqlFormatter.Format(sql, null, options);

    /// <summary>格式化多条以分号分隔的语句。</summary>
    public static string FormatBatch(
        string sql,
        SqlDialect? dialect = null,
        SqlFormatterOptions? options = null) =>
        SqlFormatter.FormatBatch(sql, dialect, options);

    private static SqlParser Create(SqlDialect? dialect) =>
        dialect is null ? SqlParser.Default : new SqlParser(new SqlParserOptions { Dialect = dialect });
}

/// <summary>表达式解析静态入口。</summary>
public static class SqlExpressionParser
{
    /// <summary>解析表达式文本。</summary>
    public static SqlExpression Parse(string expressionText, SqlDialect? dialect = null) =>
        Sql.ParseExpression(expressionText, dialect);
}
