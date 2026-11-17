using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>WITH 子句。</summary>
public sealed record SqlWithClause(ImmutableArray<SqlCommonTableExpression> Ctes) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.WithClause;

    /// <summary>是否为 <c>WITH RECURSIVE</c>。</summary>
    public bool Recursive { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var cte in Ctes)
        {
            children.Add(cte);
        }
    }
}

/// <summary>公共表表达式，如 <c>cte(a, b) AS (select ...)</c>。</summary>
public sealed record SqlCommonTableExpression(SqlIdentifier Name, SqlQueryExpression Query) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.CommonTableExpression;

    /// <summary>列别名列表。</summary>
    public ImmutableArray<SqlIdentifier> Columns { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <summary>物化提示。</summary>
    public SqlCteMaterialization Materialization { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Name);

        foreach (var column in Columns)
        {
            children.Add(column);
        }

        children.Add(Query);
    }
}

/// <summary>CTE 物化提示。</summary>
public enum SqlCteMaterialization : byte
{
    /// <summary>未指定。</summary>
    Default = 0,

    /// <summary><c>AS MATERIALIZED</c>。</summary>
    Materialized,

    /// <summary><c>AS NOT MATERIALIZED</c>。</summary>
    NotMaterialized,
}
