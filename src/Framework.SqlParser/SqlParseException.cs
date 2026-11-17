using System.Collections.Immutable;

namespace Framework.SqlParser;

/// <summary>解析失败时抛出的异常，携带全部诊断与首个诊断的源码区间。</summary>
public sealed class SqlParseException : Exception
{
    /// <summary>构造异常。</summary>
    public SqlParseException(string message, ImmutableArray<SqlDiagnostic> diagnostics)
        : base(message)
    {
        Diagnostics = diagnostics;
        Span = diagnostics.Length > 0 ? diagnostics[0].Span : SqlSpan.Empty;
    }

    /// <summary>本次解析产生的全部诊断。</summary>
    public ImmutableArray<SqlDiagnostic> Diagnostics { get; }

    /// <summary>首个诊断的源码区间；没有诊断时为空区间。</summary>
    public SqlSpan Span { get; }
}
