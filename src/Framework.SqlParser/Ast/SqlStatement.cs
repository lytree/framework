namespace Framework.SqlParser.Ast;

/// <summary>语句基类。</summary>
public abstract record SqlStatement : SqlNode
{
    /// <summary>解析该语句时使用的方言。</summary>
    public required SqlDialect Dialect { get; init; }
}

/// <summary>SELECT 语句。</summary>
public sealed record SqlSelectStatement(SqlQueryExpression Query) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.SelectStatement;

    /// <summary>WITH 子句。</summary>
    public SqlWithClause? With { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (With is not null)
        {
            children.Add(With);
        }

        children.Add(Query);
    }
}

/// <summary>无法解析的语句，保留原始区间供调用方诊断或回写。</summary>
public sealed record SqlInvalidStatement(SqlSpan RawSpan) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.InvalidStatement;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
    }
}
