using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>表达式基类。</summary>
public abstract record SqlExpression : SqlNode;

/// <summary>标识符，单独出现时表示列或常量名。</summary>
public sealed record SqlIdentifier(string Value) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.Identifier;

    /// <summary>原始引号：<c>"</c>、<c>`</c> 或 <c>[</c>；未加引号时为 <c>null</c>。</summary>
    public char? Quote { get; init; }

    /// <summary>是否大小写敏感（引号标识符）。</summary>
    public bool CaseSensitive { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
    }
}

/// <summary>字面量。</summary>
public sealed record SqlLiteral(string Value, SqlLiteralKind LiteralKind) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.Literal;

    /// <summary>字面量前缀，如 <c>N</c>、<c>E</c>、<c>$</c>、<c>q</c>。</summary>
    public string? Prefix { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
    }
}

/// <summary>列引用，如 <c>u.name</c>、<c>t.*</c>、<c>*</c>。</summary>
public sealed record SqlColumnRef(ImmutableArray<SqlIdentifier> Parts) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.ColumnRef;

    /// <summary>是否为星号引用。</summary>
    public bool IsStar { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var part in Parts)
        {
            children.Add(part);
        }
    }
}

/// <summary>参数或变量引用，保留原始前缀。</summary>
public sealed record SqlParameterRef(string Prefix) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.ParameterRef;

    /// <summary>参数名；<c>?</c> 时为 <c>null</c>。</summary>
    public string? Name { get; init; }

    /// <summary>位置参数序号，如 <c>$1</c> 的 1。</summary>
    public int? Index { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
    }
}

/// <summary>二元运算。</summary>
public sealed record SqlBinaryExpression(SqlExpression Left, SqlBinaryOperator Operator, SqlExpression Right) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.BinaryExpression;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Left);
        children.Add(Right);
    }
}

/// <summary>一元运算。</summary>
public sealed record SqlUnaryExpression(SqlUnaryOperator Operator, SqlExpression Operand) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.UnaryExpression;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Operand);
}

/// <summary>函数调用。</summary>
public sealed record SqlFunctionCall(SqlIdentifier Name) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.FunctionCall;

    /// <summary>实参列表。</summary>
    public ImmutableArray<SqlExpression> Arguments { get; init; } = ImmutableArray<SqlExpression>.Empty;

    /// <summary>是否带 <c>DISTINCT</c> 修饰。</summary>
    public bool Distinct { get; init; }

    /// <summary>是否为 <c>count(*)</c> 形式的星号参数。</summary>
    public bool Star { get; init; }

    /// <summary>WITHIN GROUP 的有序聚合排序。</summary>
    public SqlOrderByClause? WithinGroup { get; init; }

    /// <summary>聚合过滤条件。</summary>
    public SqlFilterClause? Filter { get; init; }

    /// <summary>窗口定义（OVER 子句）。</summary>
    public SqlWindowSpecification? Over { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Name);

        foreach (var argument in Arguments)
        {
            children.Add(argument);
        }

        if (WithinGroup is not null)
        {
            children.Add(WithinGroup);
        }

        if (Filter is not null)
        {
            children.Add(Filter);
        }

        if (Over is not null)
        {
            children.Add(Over);
        }
    }
}

/// <summary>CASE 表达式。</summary>
public sealed record SqlCaseExpression : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.CaseExpression;

    /// <summary>简单 CASE 的比较操作数。</summary>
    public SqlExpression? Operand { get; init; }

    /// <summary>WHEN 分支列表。</summary>
    public ImmutableArray<SqlWhenClause> Whens { get; init; } = ImmutableArray<SqlWhenClause>.Empty;

    /// <summary>ELSE 结果。</summary>
    public SqlExpression? Else { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Operand is not null)
        {
            children.Add(Operand);
        }

        foreach (var when in Whens)
        {
            children.Add(when);
        }

        if (Else is not null)
        {
            children.Add(Else);
        }
    }
}

/// <summary>CAST / TRY_CAST 表达式。</summary>
public sealed record SqlCastExpression(SqlExpression Expression, SqlDataType TargetType) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.CastExpression;

    /// <summary>是否为 <c>TRY_CAST</c>。</summary>
    public bool TryCast { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Expression);
        children.Add(TargetType);
    }
}

/// <summary>子查询表达式。</summary>
public sealed record SqlSubqueryExpression(SqlQueryExpression Query) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.SubqueryExpression;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Query);
}

/// <summary>量化子查询，如 <c>x = ANY (select ...)</c> 中的右侧。</summary>
public sealed record SqlQuantifiedSubqueryExpression(SqlQuantifier Quantifier, SqlQueryExpression Query) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.QuantifiedSubquery;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Query);
}

/// <summary>量化比较量词。</summary>
public enum SqlQuantifier : byte
{
    /// <summary><c>ANY</c>。</summary>
    Any,

    /// <summary><c>SOME</c>。</summary>
    Some,

    /// <summary><c>ALL</c>。</summary>
    All,
}

/// <summary>EXISTS 谓词。</summary>
public sealed record SqlExistsExpression(SqlQueryExpression Query) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.ExistsExpression;

    /// <summary>是否带 <c>NOT</c>。</summary>
    public bool Negated { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Query);
}

/// <summary>IN 谓词。</summary>
public sealed record SqlInExpression(SqlExpression Expression) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.InExpression;

    /// <summary>候选值；子查询以 <see cref="SqlSubqueryExpression"/> 单元素表示。</summary>
    public ImmutableArray<SqlExpression> Values { get; init; } = ImmutableArray<SqlExpression>.Empty;

    /// <summary>是否带 <c>NOT</c>。</summary>
    public bool Negated { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Expression);

        foreach (var value in Values)
        {
            children.Add(value);
        }
    }
}

/// <summary>BETWEEN 谓词。</summary>
public sealed record SqlBetweenExpression(SqlExpression Expression, SqlExpression Low, SqlExpression High) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.BetweenExpression;

    /// <summary>是否带 <c>NOT</c>。</summary>
    public bool Negated { get; init; }

    /// <summary>是否为 <c>BETWEEN SYMMETRIC</c>。</summary>
    public bool Symmetric { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Expression);
        children.Add(Low);
        children.Add(High);
    }
}

/// <summary>LIKE / ILIKE 谓词。</summary>
public sealed record SqlLikeExpression(SqlExpression Expression, SqlExpression Pattern) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.LikeExpression;

    /// <summary>ESCAPE 字符表达式。</summary>
    public SqlExpression? Escape { get; init; }

    /// <summary>是否带 <c>NOT</c>。</summary>
    public bool Negated { get; init; }

    /// <summary>是否为大小写不敏感的 <c>ILIKE</c>。</summary>
    public bool CaseInsensitive { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Expression);
        children.Add(Pattern);

        if (Escape is not null)
        {
            children.Add(Escape);
        }
    }
}

/// <summary>IS [NOT] NULL 谓词。</summary>
public sealed record SqlIsNullExpression(SqlExpression Expression) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.IsNullExpression;

    /// <summary>是否带 <c>NOT</c>。</summary>
    public bool Negated { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children) => children.Add(Expression);
}

/// <summary>行构造，如 <c>(a, b)</c>。</summary>
public sealed record SqlTupleExpression(ImmutableArray<SqlExpression> Items) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.TupleExpression;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var item in Items)
        {
            children.Add(item);
        }
    }
}

/// <summary>COLLATE 表达式。</summary>
public sealed record SqlCollateExpression(SqlExpression Expression, SqlIdentifier Collation) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.CollateExpression;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Expression);
        children.Add(Collation);
    }
}

/// <summary>AT TIME ZONE 表达式。</summary>
public sealed record SqlAtTimeZoneExpression(SqlExpression Expression, SqlExpression Zone) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.AtTimeZoneExpression;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Expression);
        children.Add(Zone);
    }
}

/// <summary>按原文保留的表达式片段，仅在错误恢复时产生。</summary>
public sealed record SqlRawExpression(SqlSpan RawSpan) : SqlExpression
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.RawExpression;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
    }
}

/// <summary>二元运算符。</summary>
public enum SqlBinaryOperator : byte
{
    /// <summary><c>OR</c>。</summary>
    Or,

    /// <summary><c>AND</c>。</summary>
    And,

    /// <summary><c>=</c>。</summary>
    Equal,

    /// <summary><c>&lt;&gt;</c> / <c>!=</c>。</summary>
    NotEqual,

    /// <summary><c>&lt;</c>。</summary>
    LessThan,

    /// <summary><c>&lt;=</c>。</summary>
    LessThanOrEqual,

    /// <summary><c>&gt;</c>。</summary>
    GreaterThan,

    /// <summary><c>&gt;=</c>。</summary>
    GreaterThanOrEqual,

    /// <summary><c>||</c>。</summary>
    Concat,

    /// <summary><c>+</c>。</summary>
    Add,

    /// <summary><c>-</c>。</summary>
    Subtract,

    /// <summary><c>*</c>。</summary>
    Multiply,

    /// <summary><c>/</c>。</summary>
    Divide,

    /// <summary><c>%</c>。</summary>
    Modulo,
}

/// <summary>一元运算符。</summary>
public enum SqlUnaryOperator : byte
{
    /// <summary><c>NOT</c>。</summary>
    Not,

    /// <summary><c>+</c>。</summary>
    Plus,

    /// <summary><c>-</c>。</summary>
    Minus,

    /// <summary><c>~</c>。</summary>
    BitwiseNot,
}

/// <summary>字面量种类。</summary>
public enum SqlLiteralKind : byte
{
    /// <summary>数字。</summary>
    Number,

    /// <summary>字符串。</summary>
    String,

    /// <summary>十六进制串。</summary>
    HexString,

    /// <summary>位串。</summary>
    BitString,

    /// <summary>布尔值。</summary>
    Boolean,

    /// <summary><c>NULL</c>。</summary>
    Null,
}
