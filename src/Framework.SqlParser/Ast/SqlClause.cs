using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>SELECT 列表项。</summary>
public sealed record SqlSelectItem(SqlExpression Expression) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.SelectItem;

    /// <summary>列别名。</summary>
    public SqlIdentifier? Alias { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Expression);

        if (Alias is not null)
        {
            children.Add(Alias);
        }
    }
}

/// <summary>TOP 子句，如 <c>TOP (10) PERCENT WITH TIES</c>。</summary>
public sealed record SqlTopClause(SqlExpression? Count) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.TopClause;

    /// <summary>是否为 <c>PERCENT</c> 形式。</summary>
    public bool Percent { get; init; }

    /// <summary>是否为 <c>WITH TIES</c>。</summary>
    public bool WithTies { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Count is not null)
        {
            children.Add(Count);
        }
    }
}

/// <summary>聚合函数的 FILTER 子句，如 <c>count(*) FILTER (WHERE x &gt; 0)</c>。</summary>
public sealed record SqlFilterClause(SqlExpression Condition) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.FilterClause;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Condition);
}

/// <summary>ORDER BY 子句。</summary>
public sealed record SqlOrderByClause(ImmutableArray<SqlOrderByItem> Items) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.OrderByClause;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var item in Items)
        {
            children.Add(item);
        }
    }
}

/// <summary>ORDER BY 单项。</summary>
public sealed record SqlOrderByItem(SqlExpression Expression) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.OrderByItem;

    /// <summary>排序方向。</summary>
    public SqlSortOrder Direction { get; init; }

    /// <summary>NULL 排序位置。</summary>
    public SqlNullsOrder Nulls { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Expression);
}

/// <summary>GROUP BY 子句。</summary>
public sealed record SqlGroupByClause(ImmutableArray<SqlExpression> Items) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.GroupByClause;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var item in Items)
        {
            children.Add(item);
        }
    }
}

/// <summary>LIMIT 子句。MySQL <c>LIMIT offset, count</c> 的 offset 记录在查询级 <see cref="SqlQueryExpression.Offset"/>。</summary>
public sealed record SqlLimitClause(SqlExpression Count) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.LimitClause;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Count);
}

/// <summary>OFFSET 子句。</summary>
public sealed record SqlOffsetClause(SqlExpression Count) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.OffsetClause;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Count);
}

/// <summary>FETCH 子句，如 <c>FETCH FIRST 10 ROWS WITH TIES</c>。</summary>
public sealed record SqlFetchClause(SqlExpression? Count) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.FetchClause;

    /// <summary>是否为 <c>WITH TIES</c>；否则为 <c>ONLY</c>。</summary>
    public bool WithTies { get; init; }

    /// <summary>是否为 <c>PERCENT</c> 形式。</summary>
    public bool Percent { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Count is not null)
        {
            children.Add(Count);
        }
    }
}

/// <summary>CASE 的 WHEN ... THEN ... 分支。</summary>
public sealed record SqlWhenClause(SqlExpression Condition, SqlExpression Result) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.WhenClause;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Condition);
        children.Add(Result);
    }
}

/// <summary>数据类型引用，如 <c>decimal(10, 2)</c>。</summary>
public sealed record SqlDataType(SqlIdentifier Name) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.DataType;

    /// <summary>类型参数，如长度、精度、标度。</summary>
    public ImmutableArray<SqlExpression> Arguments { get; init; } = ImmutableArray<SqlExpression>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Name);

        foreach (var argument in Arguments)
        {
            children.Add(argument);
        }
    }
}

/// <summary>排序方向。</summary>
public enum SqlSortOrder : byte
{
    /// <summary>未指定。</summary>
    Unspecified = 0,

    /// <summary>升序。</summary>
    Ascending,

    /// <summary>降序。</summary>
    Descending,
}

/// <summary>NULL 值排序位置。</summary>
public enum SqlNullsOrder : byte
{
    /// <summary>未指定。</summary>
    Unspecified = 0,

    /// <summary>NULL 在前。</summary>
    First,

    /// <summary>NULL 在后。</summary>
    Last,
}
