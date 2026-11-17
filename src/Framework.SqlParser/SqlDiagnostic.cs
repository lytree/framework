namespace Framework.SqlParser;

/// <summary>诊断级别。</summary>
public enum SqlDiagnosticSeverity
{
    /// <summary>提示信息。</summary>
    Info,

    /// <summary>警告，解析可以继续。</summary>
    Warning,

    /// <summary>错误，结果不可信。</summary>
    Error,
}

/// <summary>
/// 一条解析诊断，包含稳定错误码与源码位置。
/// </summary>
/// <param name="Code">稳定诊断码，见 <see cref="SqlDiagnosticCodes"/>。</param>
/// <param name="Severity">级别。</param>
/// <param name="Message">可读消息。</param>
/// <param name="Span">源码区间。</param>
/// <param name="Line">起始行号（1 基）。</param>
/// <param name="Column">起始列号（1 基）。</param>
public sealed record SqlDiagnostic(
    string Code,
    SqlDiagnosticSeverity Severity,
    string Message,
    SqlSpan Span,
    int Line,
    int Column)
{
    /// <inheritdoc />
    public override string ToString() => $"{Code} ({Line},{Column}): {Message}";
}

/// <summary>诊断码常量。</summary>
public static class SqlDiagnosticCodes
{
    /// <summary>无法识别的字符或标记。</summary>
    public const string UnknownCharacter = "SQL1001";

    /// <summary>未闭合的字符串、注释或引号标识符。</summary>
    public const string UnterminatedToken = "SQL1002";

    /// <summary>非法数字字面量。</summary>
    public const string InvalidNumber = "SQL1003";

    /// <summary>非法参数写法。</summary>
    public const string InvalidParameter = "SQL1004";

    /// <summary>意外的输入结束。</summary>
    public const string UnexpectedEndOfInput = "SQL1005";

    /// <summary>嵌套深度超限。</summary>
    public const string NestingTooDeep = "SQL1006";

    /// <summary>输入长度超限。</summary>
    public const string InputTooLong = "SQL1007";

    /// <summary>不支持的语句类型。</summary>
    public const string UnsupportedStatement = "SQL2001";

    /// <summary>当前方言不支持该语法。</summary>
    public const string UnsupportedSyntax = "SQL2002";

    /// <summary>已按原始文本保留的片段。</summary>
    public const string RawFragment = "SQL2003";

    /// <summary>非结合运算符连用，需要括号。</summary>
    public const string NonAssociativeOperator = "SQL2004";

    /// <summary>冗余或缺失的括号（可自动修复）。</summary>
    public const string RedundantParentheses = "SQL2005";

    /// <summary>语法错误：意外的标记或缺失的标记。</summary>
    public const string SyntaxError = "SQL2006";
}
