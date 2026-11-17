using System.Collections.Immutable;

namespace Framework.SqlParser;

/// <summary>
/// 词法分析结果：Token 序列与诊断集合。
/// </summary>
public sealed class SqlTokenizeResult
{
    internal SqlTokenizeResult(
        string source,
        ImmutableArray<SqlToken> tokens,
        ImmutableArray<SqlDiagnostic> diagnostics)
    {
        Source = source;
        Tokens = tokens;
        Diagnostics = diagnostics;
    }

    /// <summary>原始 SQL 文本。</summary>
    public string Source { get; }

    /// <summary>Token 序列，始终以 <see cref="SqlTokenKind.EndOfFile"/> 结尾（输入超限时为空）。</summary>
    public ImmutableArray<SqlToken> Tokens { get; }

    /// <summary>诊断集合。</summary>
    public ImmutableArray<SqlDiagnostic> Diagnostics { get; }

    /// <summary>是否没有错误级诊断。</summary>
    public bool Success
    {
        get
        {
            foreach (var diagnostic in Diagnostics)
            {
                if (diagnostic.Severity == SqlDiagnosticSeverity.Error)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
