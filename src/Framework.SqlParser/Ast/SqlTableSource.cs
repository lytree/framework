using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>数据源基类。</summary>
public abstract record SqlTableSource : SqlNode;

/// <summary>命名表，支持 <c>catalog.schema.name</c> 与别名。</summary>
public sealed record SqlNamedTable(SqlIdentifier Name) : SqlTableSource
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.NamedTable;

    /// <summary>目录名（三段式名称的第一段）。</summary>
    public SqlIdentifier? Catalog { get; init; }

    /// <summary>模式名。</summary>
    public SqlIdentifier? Schema { get; init; }

    /// <summary>表别名。</summary>
    public SqlIdentifier? Alias { get; init; }

    /// <summary>列别名列表，如 <c>t(a, b)</c>。</summary>
    public ImmutableArray<SqlIdentifier> ColumnAliases { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Catalog is not null)
        {
            children.Add(Catalog);
        }

        if (Schema is not null)
        {
            children.Add(Schema);
        }

        children.Add(Name);

        if (Alias is not null)
        {
            children.Add(Alias);
        }

        foreach (var columnAlias in ColumnAliases)
        {
            children.Add(columnAlias);
        }
    }
}

/// <summary>派生表，即 FROM 中的子查询。</summary>
public sealed record SqlDerivedTable(SqlQueryExpression Query) : SqlTableSource
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.DerivedTable;

    /// <summary>子查询别名。</summary>
    public SqlIdentifier? Alias { get; init; }

    /// <summary>列别名列表。</summary>
    public ImmutableArray<SqlIdentifier> ColumnAliases { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <summary>是否为 LATERAL 子查询。</summary>
    public bool Lateral { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Query);

        if (Alias is not null)
        {
            children.Add(Alias);
        }

        foreach (var columnAlias in ColumnAliases)
        {
            children.Add(columnAlias);
        }
    }
}

/// <summary>连接表。</summary>
public sealed record SqlJoinedTable(SqlTableSource Left, SqlJoinType JoinType, SqlTableSource Right) : SqlTableSource
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.JoinedTable;

    /// <summary>ON 条件。</summary>
    public SqlExpression? On { get; init; }

    /// <summary>USING 列列表。</summary>
    public ImmutableArray<SqlIdentifier> Using { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <summary>是否为 NATURAL 连接。</summary>
    public bool Natural { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Left);
        children.Add(Right);

        if (On is not null)
        {
            children.Add(On);
        }

        foreach (var column in Using)
        {
            children.Add(column);
        }
    }
}

/// <summary>连接类型。</summary>
public enum SqlJoinType : byte
{
    /// <summary>内连接（<c>JOIN</c> 或 <c>INNER JOIN</c>）。</summary>
    Inner = 0,

    /// <summary>左外连接。</summary>
    Left,

    /// <summary>右外连接。</summary>
    Right,

    /// <summary>全外连接。</summary>
    Full,

    /// <summary>交叉连接。</summary>
    Cross,

    /// <summary>MySQL <c>STRAIGHT_JOIN</c>。</summary>
    Straight,

    /// <summary>T-SQL <c>CROSS APPLY</c>。</summary>
    CrossApply,

    /// <summary>T-SQL <c>OUTER APPLY</c>。</summary>
    OuterApply,
}

/// <summary>PIVOT 表，如 <c>t PIVOT (SUM(x) FOR k IN (1, 2)) p</c>。</summary>
public sealed record SqlPivotTable(
    SqlTableSource Source,
    SqlExpression Aggregate,
    SqlIdentifier ForColumn,
    ImmutableArray<SqlPivotItem> Values) : SqlTableSource
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.PivotTable;

    /// <summary>PIVOT 结果别名。</summary>
    public SqlIdentifier? Alias { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Source);
        children.Add(Aggregate);
        children.Add(ForColumn);

        foreach (var value in Values)
        {
            children.Add(value);
        }

        if (Alias is not null)
        {
            children.Add(Alias);
        }
    }
}

/// <summary>PIVOT 的 IN 列表项，可带别名。</summary>
public sealed record SqlPivotItem(SqlExpression Value) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.PivotItem;

    /// <summary>列别名。</summary>
    public SqlIdentifier? Alias { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Value);

        if (Alias is not null)
        {
            children.Add(Alias);
        }
    }
}

/// <summary>UNPIVOT 表，如 <c>t UNPIVOT (value FOR name IN (a, b)) p</c>。</summary>
public sealed record SqlUnpivotTable(
    SqlTableSource Source,
    SqlIdentifier ValueColumn,
    SqlIdentifier ForColumn,
    ImmutableArray<SqlIdentifier> InColumns) : SqlTableSource
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.UnpivotTable;

    /// <summary>UNPIVOT 结果别名。</summary>
    public SqlIdentifier? Alias { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Source);
        children.Add(ValueColumn);
        children.Add(ForColumn);

        foreach (var column in InColumns)
        {
            children.Add(column);
        }

        if (Alias is not null)
        {
            children.Add(Alias);
        }
    }
}

/// <summary>UNNEST 表函数，如 <c>UNNEST(t.tags) WITH ORDINALITY AS x(tag, ord)</c>。</summary>
public sealed record SqlUnnestTable(ImmutableArray<SqlExpression> Arguments) : SqlTableSource
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.UnnestTable;

    /// <summary>是否带 <c>WITH ORDINALITY</c>。</summary>
    public bool WithOrdinality { get; init; }

    /// <summary>表别名。</summary>
    public SqlIdentifier? Alias { get; init; }

    /// <summary>列别名列表。</summary>
    public ImmutableArray<SqlIdentifier> ColumnAliases { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var argument in Arguments)
        {
            children.Add(argument);
        }

        if (Alias is not null)
        {
            children.Add(Alias);
        }

        foreach (var columnAlias in ColumnAliases)
        {
            children.Add(columnAlias);
        }
    }
}
