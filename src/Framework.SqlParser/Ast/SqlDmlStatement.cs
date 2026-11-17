using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>INSERT 语句。</summary>
public sealed record SqlInsertStatement(SqlNamedTable Target) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.InsertStatement;

    /// <summary>WITH 子句。</summary>
    public SqlWithClause? With { get; init; }

    /// <summary>目标列列表。</summary>
    public ImmutableArray<SqlIdentifier> Columns { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <summary>数据来源：VALUES 查询或 SELECT 查询。</summary>
    public SqlQueryExpression? Source { get; init; }

    /// <summary>是否为 <c>DEFAULT VALUES</c>。</summary>
    public bool DefaultValues { get; init; }

    /// <summary>OUTPUT 子句。</summary>
    public SqlOutputClause? Output { get; init; }

    /// <summary>RETURNING 列表。</summary>
    public ImmutableArray<SqlSelectItem> Returning { get; init; } = ImmutableArray<SqlSelectItem>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (With is not null)
        {
            children.Add(With);
        }

        children.Add(Target);

        foreach (var column in Columns)
        {
            children.Add(column);
        }

        if (Output is not null)
        {
            children.Add(Output);
        }

        if (Source is not null)
        {
            children.Add(Source);
        }

        foreach (var item in Returning)
        {
            children.Add(item);
        }
    }
}

/// <summary>UPDATE 语句。</summary>
public sealed record SqlUpdateStatement(SqlNamedTable Target) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.UpdateStatement;

    /// <summary>WITH 子句。</summary>
    public SqlWithClause? With { get; init; }

    /// <summary>SET 赋值列表。</summary>
    public ImmutableArray<SqlSetAssignment> Assignments { get; init; } = ImmutableArray<SqlSetAssignment>.Empty;

    /// <summary>OUTPUT 子句。</summary>
    public SqlOutputClause? Output { get; init; }

    /// <summary>FROM 数据源（T-SQL / PostgreSQL 扩展）。</summary>
    public ImmutableArray<SqlTableSource> From { get; init; } = ImmutableArray<SqlTableSource>.Empty;

    /// <summary>WHERE 条件。</summary>
    public SqlExpression? Where { get; init; }

    /// <summary>RETURNING 列表。</summary>
    public ImmutableArray<SqlSelectItem> Returning { get; init; } = ImmutableArray<SqlSelectItem>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (With is not null)
        {
            children.Add(With);
        }

        children.Add(Target);

        foreach (var assignment in Assignments)
        {
            children.Add(assignment);
        }

        if (Output is not null)
        {
            children.Add(Output);
        }

        foreach (var source in From)
        {
            children.Add(source);
        }

        if (Where is not null)
        {
            children.Add(Where);
        }

        foreach (var item in Returning)
        {
            children.Add(item);
        }
    }
}

/// <summary>UPDATE 的 SET 赋值，如 <c>a = 1</c>。</summary>
public sealed record SqlSetAssignment(SqlIdentifier Column, SqlExpression Value) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.SetAssignment;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Column);
        children.Add(Value);
    }
}

/// <summary>DELETE 语句。</summary>
public sealed record SqlDeleteStatement(SqlNamedTable Target) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.DeleteStatement;

    /// <summary>WITH 子句。</summary>
    public SqlWithClause? With { get; init; }

    /// <summary>OUTPUT 子句。</summary>
    public SqlOutputClause? Output { get; init; }

    /// <summary>WHERE 条件。</summary>
    public SqlExpression? Where { get; init; }

    /// <summary>RETURNING 列表。</summary>
    public ImmutableArray<SqlSelectItem> Returning { get; init; } = ImmutableArray<SqlSelectItem>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (With is not null)
        {
            children.Add(With);
        }

        children.Add(Target);

        if (Output is not null)
        {
            children.Add(Output);
        }

        if (Where is not null)
        {
            children.Add(Where);
        }

        foreach (var item in Returning)
        {
            children.Add(item);
        }
    }
}

/// <summary>OUTPUT 子句（T-SQL），如 <c>OUTPUT INSERTED.id INTO archive (id)</c>。</summary>
public sealed record SqlOutputClause(ImmutableArray<SqlSelectItem> Items) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.OutputClause;

    /// <summary>OUTPUT INTO 的目标表。</summary>
    public SqlNamedTable? Into { get; init; }

    /// <summary>OUTPUT INTO 的目标列。</summary>
    public ImmutableArray<SqlIdentifier> IntoColumns { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var item in Items)
        {
            children.Add(item);
        }

        if (Into is not null)
        {
            children.Add(Into);
        }

        foreach (var column in IntoColumns)
        {
            children.Add(column);
        }
    }
}
