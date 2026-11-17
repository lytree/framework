namespace Framework.SqlParser.Ast;

/// <summary>
/// 所有 AST 节点的基类。节点不可变，可安全共享；改写通过 <c>with</c> 产生新节点。
/// </summary>
public abstract record SqlNode
{
    /// <summary>节点种类。</summary>
    public abstract SqlNodeKind Kind { get; }

    /// <summary>节点在原始 SQL 中的区间；合成节点可能为零宽区间。</summary>
    public SqlSpan Span { get; init; }

    /// <summary>按声明顺序收集直接子节点，供遍历器复用列表，避免迭代器分配。</summary>
    /// <param name="children">子节点收集列表。</param>
    public abstract void CollectChildren(List<SqlNode> children);
}
