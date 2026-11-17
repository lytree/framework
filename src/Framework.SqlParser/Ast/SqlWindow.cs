using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>WINDOW 子句，如 <c>WINDOW w AS (PARTITION BY a)</c>。</summary>
public sealed record SqlWindowClause(ImmutableArray<SqlNamedWindow> Windows) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.WindowClause;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var window in Windows)
        {
            children.Add(window);
        }
    }
}

/// <summary>命名窗口定义。</summary>
public sealed record SqlNamedWindow(SqlIdentifier Name, SqlWindowSpecification Specification) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.NamedWindow;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Name);
        children.Add(Specification);
    }
}

/// <summary>
/// 窗口定义。既表示 <c>OVER (...)</c> 中的完整定义，也表示 <c>OVER w</c> 的窗口名引用。
/// </summary>
public sealed record SqlWindowSpecification : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.WindowSpecification;

    /// <summary>基础窗口名引用；无则为 <c>null</c>。</summary>
    public SqlIdentifier? Name { get; init; }

    /// <summary>PARTITION BY 表达式列表。</summary>
    public ImmutableArray<SqlExpression> PartitionBy { get; init; } = ImmutableArray<SqlExpression>.Empty;

    /// <summary>窗口内的 ORDER BY。</summary>
    public SqlOrderByClause? OrderBy { get; init; }

    /// <summary>窗口帧。</summary>
    public SqlWindowFrame? Frame { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Name is not null)
        {
            children.Add(Name);
        }

        foreach (var partition in PartitionBy)
        {
            children.Add(partition);
        }

        if (OrderBy is not null)
        {
            children.Add(OrderBy);
        }

        if (Frame is not null)
        {
            children.Add(Frame);
        }
    }
}

/// <summary>窗口帧。</summary>
public sealed record SqlWindowFrame(SqlWindowFrameUnit Unit, SqlWindowFrameBound Start) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.WindowFrame;

    /// <summary>结束边界；单边界形式为 <c>null</c>（隐含 CURRENT ROW）。</summary>
    public SqlWindowFrameBound? End { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Start);

        if (End is not null)
        {
            children.Add(End);
        }
    }
}

/// <summary>窗口帧边界。</summary>
public sealed record SqlWindowFrameBound(SqlWindowFrameBoundKind BoundKind) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.WindowFrameBound;

    /// <summary>边界偏移量；<c>UNBOUNDED</c> 与 <c>CURRENT ROW</c> 为 <c>null</c>。</summary>
    public SqlExpression? Value { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Value is not null)
        {
            children.Add(Value);
        }
    }
}

/// <summary>窗口帧单位。</summary>
public enum SqlWindowFrameUnit : byte
{
    /// <summary><c>ROWS</c>。</summary>
    Rows,

    /// <summary><c>RANGE</c>。</summary>
    Range,

    /// <summary><c>GROUPS</c>。</summary>
    Groups,
}

/// <summary>窗口帧边界种类。</summary>
public enum SqlWindowFrameBoundKind : byte
{
    /// <summary><c>UNBOUNDED PRECEDING</c>。</summary>
    UnboundedPreceding,

    /// <summary><c>n PRECEDING</c>。</summary>
    Preceding,

    /// <summary><c>CURRENT ROW</c>。</summary>
    CurrentRow,

    /// <summary><c>n FOLLOWING</c>。</summary>
    Following,

    /// <summary><c>UNBOUNDED FOLLOWING</c>。</summary>
    UnboundedFollowing,
}
