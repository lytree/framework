namespace Framework.SqlParser.Ast;

/// <summary>AST 节点种类。</summary>
public enum SqlNodeKind : byte
{
    /// <summary>未指定。</summary>
    None = 0,

    // 语句
    /// <summary>SELECT 语句。</summary>
    SelectStatement,

    /// <summary>INSERT 语句。</summary>
    InsertStatement,

    /// <summary>UPDATE 语句。</summary>
    UpdateStatement,

    /// <summary>DELETE 语句。</summary>
    DeleteStatement,

    /// <summary>MERGE 语句。</summary>
    MergeStatement,

    /// <summary>CREATE TABLE 语句。</summary>
    CreateTableStatement,

    /// <summary>DROP TABLE 语句。</summary>
    DropTableStatement,

    /// <summary>TRUNCATE TABLE 语句。</summary>
    TruncateTableStatement,

    /// <summary>无法解析的语句（错误恢复产物）。</summary>
    InvalidStatement,

    // 查询
    /// <summary>SELECT 查询块。</summary>
    SelectQuery,

    /// <summary>括号查询。</summary>
    ParenthesizedQuery,

    /// <summary>集合运算查询（UNION / INTERSECT / EXCEPT）。</summary>
    SetOperationQuery,

    /// <summary>VALUES 查询。</summary>
    ValuesQuery,

    /// <summary>VALUES 数据行。</summary>
    ValuesRow,

    // 数据源
    /// <summary>命名表。</summary>
    NamedTable,

    /// <summary>派生表（子查询）。</summary>
    DerivedTable,

    /// <summary>连接表。</summary>
    JoinedTable,

    /// <summary>PIVOT 表。</summary>
    PivotTable,

    /// <summary>PIVOT 的 IN 列表项。</summary>
    PivotItem,

    /// <summary>UNPIVOT 表。</summary>
    UnpivotTable,

    /// <summary>UNNEST 表函数。</summary>
    UnnestTable,

    // 子句
    /// <summary>WITH 子句。</summary>
    WithClause,

    /// <summary>公共表表达式。</summary>
    CommonTableExpression,

    /// <summary>SELECT 列表项。</summary>
    SelectItem,

    /// <summary>TOP 子句。</summary>
    TopClause,

    /// <summary>FILTER 子句。</summary>
    FilterClause,

    /// <summary>UPDATE 的 SET 赋值。</summary>
    SetAssignment,

    /// <summary>MERGE 的 WHEN 分支。</summary>
    MergeClause,

    /// <summary>MERGE 的 UPDATE 操作。</summary>
    MergeUpdateAction,

    /// <summary>MERGE 的 INSERT 操作。</summary>
    MergeInsertAction,

    /// <summary>MERGE 的 DELETE 操作。</summary>
    MergeDeleteAction,

    /// <summary>OUTPUT 子句。</summary>
    OutputClause,

    /// <summary>列定义。</summary>
    ColumnDefinition,

    /// <summary>表级约束。</summary>
    TableConstraint,

    /// <summary>ORDER BY 子句。</summary>
    OrderByClause,

    /// <summary>ORDER BY 单项。</summary>
    OrderByItem,

    /// <summary>GROUP BY 子句。</summary>
    GroupByClause,

    /// <summary>LIMIT 子句。</summary>
    LimitClause,

    /// <summary>OFFSET 子句。</summary>
    OffsetClause,

    /// <summary>FETCH 子句。</summary>
    FetchClause,

    /// <summary>WINDOW 子句。</summary>
    WindowClause,

    /// <summary>命名窗口定义。</summary>
    NamedWindow,

    /// <summary>窗口定义。</summary>
    WindowSpecification,

    /// <summary>窗口帧。</summary>
    WindowFrame,

    /// <summary>窗口帧边界。</summary>
    WindowFrameBound,

    /// <summary>CASE 的 WHEN 分支。</summary>
    WhenClause,

    /// <summary>数据类型引用。</summary>
    DataType,

    // 表达式
    /// <summary>标识符。</summary>
    Identifier,

    /// <summary>字面量。</summary>
    Literal,

    /// <summary>列引用。</summary>
    ColumnRef,

    /// <summary>参数或变量引用。</summary>
    ParameterRef,

    /// <summary>二元运算。</summary>
    BinaryExpression,

    /// <summary>一元运算。</summary>
    UnaryExpression,

    /// <summary>函数调用。</summary>
    FunctionCall,

    /// <summary>CASE 表达式。</summary>
    CaseExpression,

    /// <summary>类型转换。</summary>
    CastExpression,

    /// <summary>子查询表达式。</summary>
    SubqueryExpression,

    /// <summary>量化子查询（ANY / SOME / ALL）。</summary>
    QuantifiedSubquery,

    /// <summary>EXISTS 谓词。</summary>
    ExistsExpression,

    /// <summary>IN 谓词。</summary>
    InExpression,

    /// <summary>BETWEEN 谓词。</summary>
    BetweenExpression,

    /// <summary>LIKE 谓词。</summary>
    LikeExpression,

    /// <summary>IS NULL 谓词。</summary>
    IsNullExpression,

    /// <summary>行构造。</summary>
    TupleExpression,

    /// <summary>COLLATE 表达式。</summary>
    CollateExpression,

    /// <summary>AT TIME ZONE 表达式。</summary>
    AtTimeZoneExpression,

    /// <summary>按原文保留的片段（错误恢复产物）。</summary>
    RawExpression,
}
