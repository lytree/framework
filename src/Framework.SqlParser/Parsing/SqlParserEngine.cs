using System.Collections.Immutable;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Parsing;

/// <summary>
/// 解析引擎。语句（递归下降）与表达式（Pratt）的解析逻辑按职责拆分在多个 partial 文件中。
/// 实例只在一次解析期间存活，不共享状态。
/// </summary>
internal sealed partial class SqlParserEngine
{
    private readonly ParserContext _context;
    private readonly SqlDialect _dialect;

    private SqlParserEngine(ParserContext context)
    {
        _context = context;
        _dialect = context.Dialect;
    }

    /// <summary>解析单条语句。</summary>
    public static SqlStatementOutcome ParseStatement(string sql, SqlParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var tokens = SqlTokenizer.Tokenize(sql, options.Dialect, options);
        var context = new ParserContext(sql, tokens.Tokens, options, tokens.Diagnostics);
        var engine = new SqlParserEngine(context);
        var statement = engine.ParseStatementCore();
        return new SqlStatementOutcome(
            statement,
            ImmutableArray.CreateRange(context.Diagnostics),
            !HasError(context.Diagnostics));
    }

    /// <summary>解析孤立表达式。</summary>
    public static SqlExpressionOutcome ParseExpression(string sql, SqlParserOptions options)
    {
        ArgumentNullException.ThrowIfNull(sql);
        var tokens = SqlTokenizer.Tokenize(sql, options.Dialect, options);
        var context = new ParserContext(sql, tokens.Tokens, options, tokens.Diagnostics);
        var engine = new SqlParserEngine(context);

        var expression = engine.ParseExpressionCore(0);
        if (expression is not null)
        {
            context.SkipToEndIfTrailing();
        }

        return new SqlExpressionOutcome(
            expression,
            ImmutableArray.CreateRange(context.Diagnostics),
            !HasError(context.Diagnostics));
    }

    private static bool HasError(IReadOnlyList<SqlDiagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity == SqlDiagnosticSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>语句解析结果。</summary>
internal readonly record struct SqlStatementOutcome(
    SqlStatement? Statement,
    ImmutableArray<SqlDiagnostic> Diagnostics,
    bool Success);

/// <summary>表达式解析结果。</summary>
internal readonly record struct SqlExpressionOutcome(
    SqlExpression? Expression,
    ImmutableArray<SqlDiagnostic> Diagnostics,
    bool Success);
