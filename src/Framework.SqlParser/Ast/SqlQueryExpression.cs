using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>查询表达式基类，承载 ORDER BY / LIMIT / OFFSET 等查询级尾部子句。</summary>
public abstract record SqlQueryExpression : SqlNode
{
    /// <summary>ORDER BY 子句。</summary>
    public SqlOrderByClause? OrderBy { get; init; }

    /// <summary>LIMIT 子句。</summary>
    public SqlLimitClause? Limit { get; init; }

    /// <summary>OFFSET 子句。</summary>
    public SqlOffsetClause? Offset { get; init; }

    /// <summary>FETCH 子句。</summary>
    public SqlFetchClause? Fetch { get; init; }

    /// <summary>收集查询级尾部子句，供派生类型在 <see cref="SqlNode.CollectChildren"/> 中调用。</summary>
    protected void CollectQueryTail(List<SqlNode> children)
    {
        if (OrderBy is not null)
        {
            children.Add(OrderBy);
        }

        if (Limit is not null)
        {
            children.Add(Limit);
        }

        if (Offset is not null)
        {
            children.Add(Offset);
        }

        if (Fetch is not null)
        {
            children.Add(Fetch);
        }
    }
}

/// <summary>SELECT 查询块。</summary>
public sealed record SqlSelectQuery : SqlQueryExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.SelectQuery;

    /// <summary>集合量词（DISTINCT / ALL）。</summary>
    public SelectQuantifier Quantifier { get; init; }

    /// <summary>TOP 子句。</summary>
    public SqlTopClause? Top { get; init; }

    /// <summary>SELECT 列表。</summary>
    public ImmutableArray<SqlSelectItem> Items { get; init; } = ImmutableArray<SqlSelectItem>.Empty;

    /// <summary>FROM 数据源列表。</summary>
    public ImmutableArray<SqlTableSource> From { get; init; } = ImmutableArray<SqlTableSource>.Empty;

    /// <summary>WHERE 条件。</summary>
    public SqlExpression? Where { get; init; }

    /// <summary>GROUP BY 子句。</summary>
    public SqlGroupByClause? GroupBy { get; init; }

    /// <summary>HAVING 条件。</summary>
    public SqlExpression? Having { get; init; }

    /// <summary>QUALIFY 条件。</summary>
    public SqlExpression? Qualify { get; init; }

    /// <summary>WINDOW 子句。</summary>
    public SqlWindowClause? Windows { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Top is not null)
        {
            children.Add(Top);
        }

        foreach (var item in Items)
        {
            children.Add(item);
        }

        foreach (var source in From)
        {
            children.Add(source);
        }

        if (Where is not null)
        {
            children.Add(Where);
        }

        if (GroupBy is not null)
        {
            children.Add(GroupBy);
        }

        if (Having is not null)
        {
            children.Add(Having);
        }

        if (Qualify is not null)
        {
            children.Add(Qualify);
        }

        if (Windows is not null)
        {
            children.Add(Windows);
        }

        CollectQueryTail(children);
    }
}

/// <summary>括号包裹的查询，如 <c>(select ...)</c>。</summary>
public sealed record SqlParenthesizedQuery(SqlQueryExpression Inner) : SqlQueryExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.ParenthesizedQuery;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Inner);
        CollectQueryTail(children);
    }
}

/// <summary>集合运算查询。Intersect 的优先级高于 Union / Except，解析器会按此构造左右子树。</summary>
public sealed record SqlSetOperationQuery(
    SqlQueryExpression Left,
    SqlSetOperator Operator,
    SqlSetQuantifier Quantifier,
    SqlQueryExpression Right) : SqlQueryExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.SetOperationQuery;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Left);
        children.Add(Right);
        CollectQueryTail(children);
    }
}

/// <summary>VALUES 查询。</summary>
public sealed record SqlValuesQuery(ImmutableArray<SqlValuesRow> Rows) : SqlQueryExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.ValuesQuery;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var row in Rows)
        {
            children.Add(row);
        }

        CollectQueryTail(children);
    }
}

/// <summary>VALUES 数据行。</summary>
public sealed record SqlValuesRow(ImmutableArray<SqlExpression> Values) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.ValuesRow;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var value in Values)
        {
            children.Add(value);
        }
    }
}

/// <summary>集合运算符。</summary>
public enum SqlSetOperator : byte
{
    /// <summary><c>UNION</c>。</summary>
    Union,

    /// <summary><c>INTERSECT</c>。</summary>
    Intersect,

    /// <summary><c>EXCEPT</c> / <c>MINUS</c>。</summary>
    Except,
}

/// <summary>集合运算量词。</summary>
public enum SqlSetQuantifier : byte
{
    /// <summary>未显式指定（UNION 语义等价于 DISTINCT）。</summary>
    Default = 0,

    /// <summary><c>ALL</c>。</summary>
    All,

    /// <summary><c>DISTINCT</c>。</summary>
    Distinct,
}

/// <summary>SELECT 集合量词。</summary>
public enum SelectQuantifier : byte
{
    /// <summary>未指定。</summary>
    None = 0,

    /// <summary><c>ALL</c>。</summary>
    All,

    /// <summary><c>DISTINCT</c>。</summary>
    Distinct,

    /// <summary><c>DISTINCTROW</c>。</summary>
    DistinctRow,
}
