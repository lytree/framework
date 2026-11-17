namespace Framework.SqlParser;

/// <summary>
/// SQL 解析选项。实例不可变，可在线程间共享。
/// </summary>
public sealed class SqlParserOptions
{
    /// <summary>默认选项（Generic 方言）。</summary>
    public static SqlParserOptions Default { get; } = new();

    /// <summary>目标方言，默认 <see cref="SqlDialect.Generic"/>。</summary>
    public SqlDialect Dialect { get; init; } = SqlDialect.Generic;

    /// <summary>表达式/查询的最大嵌套深度。</summary>
    public int MaxDepth { get; init; } = 128;

    /// <summary>单个语句允许的最大错误数，达到后跳过剩余输入。</summary>
    public int MaxErrors { get; init; } = 8;

    /// <summary>允许的最大输入字符数，默认 1MB。</summary>
    public int MaxInputLength { get; init; } = 1 << 20;

    /// <summary>是否启用错误恢复。</summary>
    public bool EnableRecovery { get; init; } = true;
}
