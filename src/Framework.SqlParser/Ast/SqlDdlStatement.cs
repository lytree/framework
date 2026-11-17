using System.Collections.Immutable;

namespace Framework.SqlParser.Ast;

/// <summary>CREATE TABLE 语句。</summary>
public sealed record SqlCreateTableStatement(SqlNamedTable Table) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.CreateTableStatement;

    /// <summary>是否为 <c>IF NOT EXISTS</c>。</summary>
    public bool IfNotExists { get; init; }

    /// <summary>是否为临时表。</summary>
    public bool Temporary { get; init; }

    /// <summary>列定义。</summary>
    public ImmutableArray<SqlColumnDefinition> Columns { get; init; } = ImmutableArray<SqlColumnDefinition>.Empty;

    /// <summary>表级约束。</summary>
    public ImmutableArray<SqlTableConstraint> Constraints { get; init; } = ImmutableArray<SqlTableConstraint>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Table);

        foreach (var column in Columns)
        {
            children.Add(column);
        }

        foreach (var constraint in Constraints)
        {
            children.Add(constraint);
        }
    }
}

/// <summary>列定义，如 <c>id INT NOT NULL DEFAULT 0</c>。</summary>
public sealed record SqlColumnDefinition(SqlIdentifier Name, SqlDataType DataType) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.ColumnDefinition;

    /// <summary>可空性。</summary>
    public SqlNullability Nullability { get; init; }

    /// <summary>默认值。</summary>
    public SqlExpression? Default { get; init; }

    /// <summary>是否为主键。</summary>
    public bool PrimaryKey { get; init; }

    /// <summary>是否唯一。</summary>
    public bool Unique { get; init; }

    /// <summary>CHECK 约束表达式。</summary>
    public SqlExpression? Check { get; init; }

    /// <summary>REFERENCES 目标表。</summary>
    public SqlNamedTable? References { get; init; }

    /// <summary>REFERENCES 目标列。</summary>
    public ImmutableArray<SqlIdentifier> ReferencedColumns { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        children.Add(Name);
        children.Add(DataType);

        if (Default is not null)
        {
            children.Add(Default);
        }

        if (Check is not null)
        {
            children.Add(Check);
        }

        if (References is not null)
        {
            children.Add(References);
        }

        foreach (var column in ReferencedColumns)
        {
            children.Add(column);
        }
    }
}

/// <summary>列的可空性。</summary>
public enum SqlNullability : byte
{
    /// <summary>未声明。</summary>
    Unspecified,

    /// <summary><c>NULL</c>。</summary>
    Null,

    /// <summary><c>NOT NULL</c>。</summary>
    NotNull,
}

/// <summary>表级约束。</summary>
public sealed record SqlTableConstraint(
    SqlTableConstraintKind ConstraintKind,
    ImmutableArray<SqlIdentifier> Columns) : SqlNode
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.TableConstraint;

    /// <summary>约束名。</summary>
    public SqlIdentifier? Name { get; init; }

    /// <summary>CHECK 约束表达式。</summary>
    public SqlExpression? Check { get; init; }

    /// <summary>外键引用表。</summary>
    public SqlNamedTable? References { get; init; }

    /// <summary>外键引用列。</summary>
    public ImmutableArray<SqlIdentifier> ReferencedColumns { get; init; } = ImmutableArray<SqlIdentifier>.Empty;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        if (Name is not null)
        {
            children.Add(Name);
        }

        foreach (var column in Columns)
        {
            children.Add(column);
        }

        if (Check is not null)
        {
            children.Add(Check);
        }

        if (References is not null)
        {
            children.Add(References);
        }

        foreach (var column in ReferencedColumns)
        {
            children.Add(column);
        }
    }
}

/// <summary>表级约束类型。</summary>
public enum SqlTableConstraintKind : byte
{
    /// <summary>主键。</summary>
    PrimaryKey,

    /// <summary>唯一。</summary>
    Unique,

    /// <summary>检查约束。</summary>
    Check,

    /// <summary>外键。</summary>
    ForeignKey,
}

/// <summary>DROP TABLE 语句。</summary>
public sealed record SqlDropTableStatement(ImmutableArray<SqlNamedTable> Tables) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.DropTableStatement;

    /// <summary>是否为 <c>IF EXISTS</c>。</summary>
    public bool IfExists { get; init; }

    /// <summary>是否为 <c>CASCADE</c>。</summary>
    public bool Cascade { get; init; }

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var table in Tables)
        {
            children.Add(table);
        }
    }
}

/// <summary>TRUNCATE TABLE 语句。</summary>
public sealed record SqlTruncateTableStatement(ImmutableArray<SqlNamedTable> Tables) : SqlStatement
{
    /// <inheritdoc />
    public override SqlNodeKind Kind => SqlNodeKind.TruncateTableStatement;

    /// <inheritdoc />
    public override void CollectChildren(List<SqlNode> children)
    {
        foreach (var table in Tables)
        {
            children.Add(table);
        }
    }
}
