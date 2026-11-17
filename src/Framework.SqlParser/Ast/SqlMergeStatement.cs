using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>MERGE 语句。</summary>
public sealed record SqlMergeStatement(
    SqlNamedTable Target,
    SqlTableSource Source,
    SqlExpression On) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.MergeStatement;

    /// <summary>WITH 子句。</summary>
    public SqlWithClause? With { get; init; }

    /// <summary>WHEN 分支列表。</summary>
    public ImmutableArray<SqlMergeClause> Clauses { get; init; } = ImmutableArray<SqlMergeClause>.Empty;

    /// <summary>OUTPUT 子句。</summary>
    public SqlOutputClause? Output { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (With is not null)
        {
            children.Add(With);
        }

        children.Add(Target);
        children.Add(Source);
        children.Add(On);

        foreach (var clause in Clauses)
        {
            children.Add(clause);
        }

        if (Output is not null)
        {
            children.Add(Output);
        }
    }
}

/// <summary>MERGE 的 WHEN 分支。</summary>
public sealed record SqlMergeClause(SqlMergeMatchKind MatchKind, SqlMergeAction Action) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.MergeClause;

    /// <summary>WHEN 分支的附加条件。</summary>
    public SqlExpression? Condition { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Condition is not null)
        {
            children.Add(Condition);
        }

        children.Add(Action);
    }
}

/// <summary>MERGE 分支的匹配类型。</summary>
public enum SqlMergeMatchKind : byte
{
    /// <summary><c>WHEN MATCHED</c>。</summary>
    Matched,

    /// <summary><c>WHEN NOT MATCHED</c> / <c>WHEN NOT MATCHED BY TARGET</c>。</summary>
    NotMatchedByTarget,

    /// <summary><c>WHEN NOT MATCHED BY SOURCE</c>。</summary>
    NotMatchedBySource,
}

/// <summary>MERGE 分支执行的操作。</summary>
public abstract record SqlMergeAction : SqlNode;

/// <summary>MERGE 的 UPDATE 操作。</summary>
public sealed record SqlMergeUpdateAction(ImmutableArray<SqlSetAssignment> Assignments) : SqlMergeAction
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.MergeUpdateAction;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var assignment in Assignments)
        {
            children.Add(assignment);
        }
    }
}

/// <summary>MERGE 的 INSERT 操作。</summary>
public sealed record SqlMergeInsertAction(
    ImmutableArray<SqlIdentifier> Columns,
    ImmutableArray<SqlExpression> Values) : SqlMergeAction
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.MergeInsertAction;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var column in Columns)
        {
            children.Add(column);
        }

        foreach (var value in Values)
        {
            children.Add(value);
        }
    }
}

/// <summary>MERGE 的 DELETE 操作。</summary>
public sealed record SqlMergeDeleteAction : SqlMergeAction
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.MergeDeleteAction;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
    }
}
