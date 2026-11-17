namespace Framework.SqlParser;

/// <summary>
/// SQL 文本中的半开区间 <c>[Start, End)</c>，单位为字符偏移。
/// </summary>
/// <param name="Start">起始偏移（包含）。</param>
/// <param name="Length">区间长度。</param>
public readonly record struct SqlSpan(int Start, int Length)
{
    /// <summary>结束偏移（不包含）。</summary>
    public int End => Start + Length;

    /// <summary>空区间。</summary>
    public static SqlSpan Empty => new(0, 0);
}
