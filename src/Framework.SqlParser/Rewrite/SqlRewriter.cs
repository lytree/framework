using System.Collections.Immutable;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Rewrite;

/// <summary>
/// AST 改写器。默认深拷贝整棵树并返回新节点，子类通过重写
/// <see cref="RewriteExpression"/>、<see cref="RewriteTableSource"/> 等钩子实现规则。
/// </summary>
public abstract class SqlRewriter
{
    /// <summary>改写语句。</summary>
    public virtual SqlStatement RewriteStatement(SqlStatement statement) => statement switch
    {
        SqlSelectStatement select => select with
        {
            With = RewriteWith(select.With),
            Query = RewriteQuery(select.Query),
        },

        SqlInsertStatement insert => insert with
        {
            With = RewriteWith(insert.With),
            Source = insert.Source is null ? null : RewriteQuery(insert.Source),
            Returning = RewriteSelectItems(insert.Returning),
            Output = RewriteOutput(insert.Output),
        },

        SqlUpdateStatement update => update with
        {
            With = RewriteWith(update.With),
            Assignments = update.Assignments
                .Select(assignment => assignment with { Value = RewriteExpression(assignment.Value) })
                .ToImmutableArray(),
            From = update.From.Select(RewriteTableSource).ToImmutableArray(),
            Where = update.Where is null ? null : RewriteExpression(update.Where),
            Returning = RewriteSelectItems(update.Returning),
            Output = RewriteOutput(update.Output),
        },

        SqlDeleteStatement delete => delete with
        {
            With = RewriteWith(delete.With),
            Where = delete.Where is null ? null : RewriteExpression(delete.Where),
            Returning = RewriteSelectItems(delete.Returning),
            Output = RewriteOutput(delete.Output),
        },

        SqlMergeStatement merge => merge with
        {
            With = RewriteWith(merge.With),
            Source = RewriteTableSource(merge.Source),
            On = RewriteExpression(merge.On),
            Clauses = merge.Clauses
                .Select(clause => clause with
                {
                    Condition = clause.Condition is null ? null : RewriteExpression(clause.Condition),
                    Action = RewriteMergeAction(clause.Action),
                })
                .ToImmutableArray(),
            Output = RewriteOutput(merge.Output),
        },

        _ => statement,
    };

    /// <summary>改写查询表达式。</summary>
    public virtual SqlQueryExpression RewriteQuery(SqlQueryExpression query) => query switch
    {
        SqlSelectQuery select => RewriteSelectQuery(select),

        SqlValuesQuery values => values with
        {
            Rows = values.Rows
                .Select(row => row with
                {
                    Values = row.Values.Select(RewriteExpression).ToImmutableArray(),
                })
                .ToImmutableArray(),
            OrderBy = RewriteOrderBy(values.OrderBy),
            Limit = RewriteLimit(values.Limit),
            Offset = RewriteOffset(values.Offset),
            Fetch = RewriteFetch(values.Fetch),
        },

        SqlParenthesizedQuery parenthesized => parenthesized with
        {
            Inner = RewriteQuery(parenthesized.Inner),
            OrderBy = RewriteOrderBy(parenthesized.OrderBy),
            Limit = RewriteLimit(parenthesized.Limit),
            Offset = RewriteOffset(parenthesized.Offset),
            Fetch = RewriteFetch(parenthesized.Fetch),
        },

        SqlSetOperationQuery set => set with
        {
            Left = RewriteQuery(set.Left),
            Right = RewriteQuery(set.Right),
            OrderBy = RewriteOrderBy(set.OrderBy),
            Limit = RewriteLimit(set.Limit),
            Offset = RewriteOffset(set.Offset),
            Fetch = RewriteFetch(set.Fetch),
        },

        _ => query,
    };

    /// <summary>改写 SELECT 查询。</summary>
    public virtual SqlSelectQuery RewriteSelectQuery(SqlSelectQuery query) => query with
    {
        Items = query.Items
            .Select(item => item with { Expression = RewriteExpression(item.Expression) })
            .ToImmutableArray(),
        Top = query.Top is null
            ? null
            : query.Top with
            {
                Count = query.Top.Count is null ? null : RewriteExpression(query.Top.Count),
            },
        From = query.From.Select(RewriteTableSource).ToImmutableArray(),
        Where = query.Where is null ? null : RewriteExpression(query.Where),
        GroupBy = query.GroupBy is null
            ? null
            : query.GroupBy with
            {
                Items = query.GroupBy.Items.Select(RewriteExpression).ToImmutableArray(),
            },
        Having = query.Having is null ? null : RewriteExpression(query.Having),
        Qualify = query.Qualify is null ? null : RewriteExpression(query.Qualify),
        Windows = query.Windows is null
            ? null
            : query.Windows with
            {
                Windows = query.Windows.Windows
                    .Select(window => window with
                    {
                        Specification = RewriteWindowSpecification(window.Specification),
                    })
                    .ToImmutableArray(),
            },
        OrderBy = RewriteOrderBy(query.OrderBy),
        Limit = RewriteLimit(query.Limit),
        Offset = RewriteOffset(query.Offset),
        Fetch = RewriteFetch(query.Fetch),
    };

    /// <summary>改写数据源。</summary>
    public virtual SqlTableSource RewriteTableSource(SqlTableSource source) => source switch
    {
        SqlDerivedTable derived => derived with { Query = RewriteQuery(derived.Query) },

        SqlJoinedTable joined => joined with
        {
            Left = RewriteTableSource(joined.Left),
            Right = RewriteTableSource(joined.Right),
            On = joined.On is null ? null : RewriteExpression(joined.On),
        },

        SqlPivotTable pivot => pivot with
        {
            Source = RewriteTableSource(pivot.Source),
            Aggregate = RewriteExpression(pivot.Aggregate),
            Values = pivot.Values
                .Select(value => value with { Value = RewriteExpression(value.Value) })
                .ToImmutableArray(),
        },

        SqlUnpivotTable unpivot => unpivot with
        {
            Source = RewriteTableSource(unpivot.Source),
        },

        SqlUnnestTable unnest => unnest with
        {
            Arguments = unnest.Arguments.Select(RewriteExpression).ToImmutableArray(),
        },

        _ => source,
    };

    /// <summary>改写表达式。基类实现深度优先重建所有子表达式。</summary>
    public virtual SqlExpression RewriteExpression(SqlExpression expression) => expression switch
    {
        SqlBinaryExpression binary => binary with
        {
            Left = RewriteExpression(binary.Left),
            Right = RewriteExpression(binary.Right),
        },

        SqlUnaryExpression unary => unary with
        {
            Operand = RewriteExpression(unary.Operand),
        },

        SqlFunctionCall call => call with
        {
            Arguments = call.Arguments.Select(RewriteExpression).ToImmutableArray(),
            WithinGroup = RewriteOrderBy(call.WithinGroup),
            Filter = call.Filter is null
                ? null
                : call.Filter with { Condition = RewriteExpression(call.Filter.Condition) },
            Over = call.Over is null ? null : RewriteWindowSpecification(call.Over),
        },

        SqlCaseExpression caseExpression => caseExpression with
        {
            Operand = caseExpression.Operand is null
                ? null
                : RewriteExpression(caseExpression.Operand),
            Whens = caseExpression.Whens
                .Select(branch => branch with
                {
                    Condition = RewriteExpression(branch.Condition),
                    Result = RewriteExpression(branch.Result),
                })
                .ToImmutableArray(),
            Else = caseExpression.Else is null ? null : RewriteExpression(caseExpression.Else),
        },

        SqlCastExpression cast => cast with
        {
            Expression = RewriteExpression(cast.Expression),
            TargetType = cast.TargetType with
            {
                Arguments = cast.TargetType.Arguments.Select(RewriteExpression).ToImmutableArray(),
            },
        },

        SqlSubqueryExpression subquery => subquery with
        {
            Query = RewriteQuery(subquery.Query),
        },

        SqlQuantifiedSubqueryExpression quantified => quantified with
        {
            Query = RewriteQuery(quantified.Query),
        },

        SqlExistsExpression exists => exists with
        {
            Query = RewriteQuery(exists.Query),
        },

        SqlInExpression inExpression => inExpression with
        {
            Expression = RewriteExpression(inExpression.Expression),
            Values = inExpression.Values.Select(RewriteExpression).ToImmutableArray(),
        },

        SqlBetweenExpression between => between with
        {
            Expression = RewriteExpression(between.Expression),
            Low = RewriteExpression(between.Low),
            High = RewriteExpression(between.High),
        },

        SqlLikeExpression like => like with
        {
            Expression = RewriteExpression(like.Expression),
            Pattern = RewriteExpression(like.Pattern),
            Escape = like.Escape is null ? null : RewriteExpression(like.Escape),
        },

        SqlIsNullExpression isNull => isNull with
        {
            Expression = RewriteExpression(isNull.Expression),
        },

        SqlTupleExpression tuple => tuple with
        {
            Items = tuple.Items.Select(RewriteExpression).ToImmutableArray(),
        },

        SqlCollateExpression collate => collate with
        {
            Expression = RewriteExpression(collate.Expression),
        },

        SqlAtTimeZoneExpression atTimeZone => atTimeZone with
        {
            Expression = RewriteExpression(atTimeZone.Expression),
            Zone = RewriteExpression(atTimeZone.Zone),
        },

        _ => expression,
    };

    private SqlWithClause? RewriteWith(SqlWithClause? with) => with is null
        ? null
        : with with
        {
            Ctes = with.Ctes
                .Select(cte => cte with { Query = RewriteQuery(cte.Query) })
                .ToImmutableArray(),
        };

    private SqlOrderByClause? RewriteOrderBy(SqlOrderByClause? orderBy) => orderBy is null
        ? null
        : orderBy with
        {
            Items = orderBy.Items
                .Select(item => item with { Expression = RewriteExpression(item.Expression) })
                .ToImmutableArray(),
        };

    private SqlLimitClause? RewriteLimit(SqlLimitClause? limit) =>
        limit is null ? null : limit with { Count = RewriteExpression(limit.Count) };

    private SqlOffsetClause? RewriteOffset(SqlOffsetClause? offset) =>
        offset is null ? null : offset with { Count = RewriteExpression(offset.Count) };

    private SqlFetchClause? RewriteFetch(SqlFetchClause? fetch) => fetch is null
        ? null
        : fetch with
        {
            Count = fetch.Count is null ? null : RewriteExpression(fetch.Count),
        };

    private SqlWindowSpecification RewriteWindowSpecification(SqlWindowSpecification specification) =>
        specification with
        {
            PartitionBy = specification.PartitionBy.Select(RewriteExpression).ToImmutableArray(),
            OrderBy = RewriteOrderBy(specification.OrderBy),
            Frame = specification.Frame is null
                ? null
                : specification.Frame with
                {
                    Start = RewriteFrameBound(specification.Frame.Start),
                    End = specification.Frame.End is null
                        ? null
                        : RewriteFrameBound(specification.Frame.End),
                },
        };

    private SqlWindowFrameBound RewriteFrameBound(SqlWindowFrameBound bound) =>
        bound.Value is null ? bound : bound with { Value = RewriteExpression(bound.Value) };

    private SqlOutputClause? RewriteOutput(SqlOutputClause? output) => output is null
        ? null
        : output with { Items = RewriteSelectItems(output.Items) };

    private ImmutableArray<SqlSelectItem> RewriteSelectItems(ImmutableArray<SqlSelectItem> items) =>
        items
            .Select(item => item with { Expression = RewriteExpression(item.Expression) })
            .ToImmutableArray();

    private SqlMergeAction RewriteMergeAction(SqlMergeAction action) => action switch
    {
        SqlMergeUpdateAction update => update with
        {
            Assignments = update.Assignments
                .Select(assignment => assignment with { Value = RewriteExpression(assignment.Value) })
                .ToImmutableArray(),
        },

        SqlMergeInsertAction insert => insert with
        {
            Values = insert.Values.Select(RewriteExpression).ToImmutableArray(),
        },

        _ => action,
    };
}
