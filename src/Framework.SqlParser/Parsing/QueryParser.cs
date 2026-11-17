using System.Collections.Immutable;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Parsing;

internal sealed partial class SqlParserEngine
{
    /// <summary>解析查询表达式：查询块 + ORDER BY / LIMIT / OFFSET。</summary>
    private SqlQueryExpression? ParseQueryExpressionCore()
    {
        if (!_context.TryEnterDepth())
        {
            return null;
        }

        try
        {
            var startIndex = _context.Index;
            var body = ParseQueryBody();
            if (body is null)
            {
                return null;
            }

            SqlOrderByClause? orderBy = null;
            SqlLimitClause? limit = null;
            SqlOffsetClause? offset = null;

            if (_context.AtKeyword(SqlKeywordId.Order))
            {
                var orderStart = _context.Index;
                _context.Advance();
                _context.ExpectKeyword(SqlKeywordId.By, "BY");
                orderBy = ParseOrderByClause(orderStart);
            }

            if (_context.MatchKeyword(SqlKeywordId.Limit))
            {
                var limitStart = _context.Index - 1;
                var first = ParseExpressionCore(0);
                if (first is not null)
                {
                    if (_context.Match(SqlTokenKind.Comma))
                    {
                        var second = ParseExpressionCore(0);
                        if (second is not null)
                        {
                            offset = new SqlOffsetClause(first) { Span = first.Span };
                            limit = new SqlLimitClause(second) { Span = _context.SpanFrom(limitStart) };
                        }
                    }
                    else
                    {
                        limit = new SqlLimitClause(first) { Span = _context.SpanFrom(limitStart) };
                    }
                }
            }

            if (_context.MatchKeyword(SqlKeywordId.Offset))
            {
                var offsetStart = _context.Index - 1;
                var count = ParseExpressionCore(0);
                if (count is not null)
                {
                    if (!_context.MatchKeyword(SqlKeywordId.Row))
                    {
                        _context.MatchKeyword(SqlKeywordId.Rows);
                    }

                    offset = new SqlOffsetClause(count) { Span = _context.SpanFrom(offsetStart) };
                }
            }

            SqlFetchClause? fetch = null;
            if (_context.MatchKeyword(SqlKeywordId.Fetch))
            {
                fetch = ParseFetchClause(_context.Index - 1);
            }

            if (orderBy is null && limit is null && offset is null && fetch is null)
            {
                return body;
            }

            return body with
            {
                OrderBy = orderBy,
                Limit = limit,
                Offset = offset,
                Fetch = fetch,
                Span = _context.SpanFrom(startIndex),
            };
        }
        finally
        {
            _context.ExitDepth();
        }
    }

    private SqlQueryExpression? ParseQueryBody() => ParseSetExpression();

    /// <summary>
    /// 集合运算表达式。UNION / EXCEPT / MINUS 左结合，INTERSECT 优先级更高：
    /// <c>a UNION b INTERSECT c</c> 解析为 <c>a UNION (b INTERSECT c)</c>。
    /// </summary>
    private SqlQueryExpression? ParseSetExpression()
    {
        var left = ParseSetTerm();
        if (left is null)
        {
            return null;
        }

        while (true)
        {
            SqlSetOperator setOperator;
            if (_context.AtKeyword(SqlKeywordId.Union))
            {
                setOperator = SqlSetOperator.Union;
            }
            else if (_context.AtKeyword(SqlKeywordId.Except) || _context.AtKeyword(SqlKeywordId.Minus))
            {
                setOperator = SqlSetOperator.Except;
            }
            else
            {
                break;
            }

            _context.Advance();
            var quantifier = ParseSetQuantifier();
            var right = ParseSetTerm();
            if (right is null)
            {
                break;
            }

            left = new SqlSetOperationQuery(left, setOperator, quantifier, right)
            {
                Span = CombineSpans(left, right),
            };
        }

        return left;
    }

    private SqlQueryExpression? ParseSetTerm()
    {
        var left = ParseQueryPrimary();
        if (left is null)
        {
            return null;
        }

        while (_context.AtKeyword(SqlKeywordId.Intersect))
        {
            _context.Advance();
            var quantifier = ParseSetQuantifier();
            var right = ParseQueryPrimary();
            if (right is null)
            {
                break;
            }

            left = new SqlSetOperationQuery(left, SqlSetOperator.Intersect, quantifier, right)
            {
                Span = CombineSpans(left, right),
            };
        }

        return left;
    }

    private SqlSetQuantifier ParseSetQuantifier()
    {
        if (_context.MatchKeyword(SqlKeywordId.All))
        {
            return SqlSetQuantifier.All;
        }

        return _context.MatchKeyword(SqlKeywordId.Distinct)
            ? SqlSetQuantifier.Distinct
            : SqlSetQuantifier.Default;
    }

    private SqlQueryExpression? ParseQueryPrimary()
    {
        if (_context.At(SqlTokenKind.LeftParen))
        {
            var startIndex = _context.Index;
            _context.Advance();
            var inner = ParseQueryExpressionCore();
            _context.Expect(SqlTokenKind.RightParen, "')'");
            return inner is null
                ? null
                : new SqlParenthesizedQuery(inner) { Span = _context.SpanFrom(startIndex) };
        }

        if (_context.AtKeyword(SqlKeywordId.Select))
        {
            return ParseSelectQuery();
        }

        if (_context.AtKeyword(SqlKeywordId.Values))
        {
            return ParseValuesQuery();
        }

        if (_context.AtKeyword(SqlKeywordId.With))
        {
            _context.Report(SqlDiagnosticCodes.UnsupportedSyntax, _context.Current, "子查询中的 WITH（CTE）尚未支持。");
            return null;
        }

        _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "预期 SELECT、VALUES 或括号查询。");
        return null;
    }

    private SqlValuesQuery ParseValuesQuery()
    {
        var startIndex = _context.Index;
        _context.Advance(); // VALUES

        var rows = ImmutableArray.CreateBuilder<SqlValuesRow>();

        while (true)
        {
            var rowStart = _context.Index;
            if (!_context.At(SqlTokenKind.LeftParen))
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "VALUES 行必须以 '(' 开始。");
                break;
            }

            _context.Advance();
            var values = ImmutableArray.CreateBuilder<SqlExpression>();

            if (_context.At(SqlTokenKind.RightParen))
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "VALUES 行不能为空。");
            }
            else
            {
                while (true)
                {
                    var value = ParseExpressionCore(0);
                    if (value is null)
                    {
                        break;
                    }

                    values.Add(value);
                    if (!_context.Match(SqlTokenKind.Comma))
                    {
                        break;
                    }
                }
            }

            _context.Expect(SqlTokenKind.RightParen, "')'");
            rows.Add(new SqlValuesRow(values.ToImmutable()) { Span = _context.SpanFrom(rowStart) });

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return new SqlValuesQuery(rows.ToImmutable()) { Span = _context.SpanFrom(startIndex) };
    }

    private static SqlSpan CombineSpans(SqlNode left, SqlNode right) =>
        new(left.Span.Start, Math.Max(0, right.Span.End - left.Span.Start));

    private SqlSelectQuery ParseSelectQuery()
    {
        var startIndex = _context.Index;
        _context.Advance(); // SELECT

        var quantifier = SelectQuantifier.None;
        if (_context.MatchKeyword(SqlKeywordId.Distinct))
        {
            quantifier = SelectQuantifier.Distinct;
        }
        else if (_context.MatchKeyword(SqlKeywordId.DistinctRow))
        {
            quantifier = SelectQuantifier.DistinctRow;
        }
        else if (_context.MatchKeyword(SqlKeywordId.All))
        {
            quantifier = SelectQuantifier.All;
        }

        var top = ParseTopClause();

        var items = ParseSelectItems();

        var from = ImmutableArray<SqlTableSource>.Empty;
        if (_context.MatchKeyword(SqlKeywordId.From))
        {
            from = ParseFromList();
        }

        SqlExpression? where = null;
        if (_context.MatchKeyword(SqlKeywordId.Where))
        {
            where = ParseExpressionCore(0);
        }

        SqlGroupByClause? groupBy = null;
        if (_context.AtKeyword(SqlKeywordId.Group))
        {
            var groupStart = _context.Index;
            _context.Advance();
            _context.ExpectKeyword(SqlKeywordId.By, "BY");
            groupBy = ParseGroupByClause(groupStart);
        }

        SqlExpression? having = null;
        if (_context.MatchKeyword(SqlKeywordId.Having))
        {
            having = ParseExpressionCore(0);
        }

        SqlWindowClause? windows = null;
        if (_context.AtKeyword(SqlKeywordId.Window))
        {
            var windowStart = _context.Index;
            _context.Advance();
            windows = ParseWindowClause(windowStart);
        }

        SqlExpression? qualify = null;
        if (_context.MatchKeyword(SqlKeywordId.Qualify))
        {
            qualify = ParseExpressionCore(0);
        }

        return new SqlSelectQuery
        {
            Quantifier = quantifier,
            Top = top,
            Items = items,
            From = from,
            Where = where,
            GroupBy = groupBy,
            Having = having,
            Qualify = qualify,
            Windows = windows,
            Span = _context.SpanFrom(startIndex),
        };
    }

    /// <summary>解析 T-SQL <c>TOP (n) [PERCENT] [WITH TIES]</c>。</summary>
    private SqlTopClause? ParseTopClause()
    {
        if (!_context.AtKeyword(SqlKeywordId.Top))
        {
            return null;
        }

        var startIndex = _context.Index;
        _context.Advance(); // TOP

        SqlExpression? count;
        if (_context.Match(SqlTokenKind.LeftParen))
        {
            count = ParseExpressionCore(0);
            _context.Expect(SqlTokenKind.RightParen, "')'");
        }
        else if (_context.Current.Kind is SqlTokenKind.Number or SqlTokenKind.Parameter or SqlTokenKind.Variable)
        {
            // 旧式 TOP n 只接受单个数值/参数，避免把后面的 * 当作乘法。
            count = ParsePrefixExpression();
        }
        else
        {
            count = null;
        }

        if (count is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "TOP 后缺少行数。");
        }

        var percent = _context.MatchKeyword(SqlKeywordId.Percent);
        var withTies = false;
        if (_context.MatchKeyword(SqlKeywordId.With))
        {
            _context.ExpectKeyword(SqlKeywordId.Ties, "TIES");
            withTies = true;
        }

        return new SqlTopClause(count)
        {
            Percent = percent,
            WithTies = withTies,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private ImmutableArray<SqlSelectItem> ParseSelectItems()
    {
        var items = ImmutableArray.CreateBuilder<SqlSelectItem>();

        while (true)
        {
            if (_context.TooManyErrors)
            {
                break;
            }

            var itemStart = _context.Index;
            var expression = ParseExpressionCore(0);
            if (expression is null)
            {
                break;
            }

            var alias = ParseSelectItemAlias();
            items.Add(new SqlSelectItem(expression)
            {
                Alias = alias,
                Span = _context.SpanFrom(itemStart),
            });

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        if (items.Count == 0)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "SELECT 列表为空。");
        }

        return items.ToImmutable();
    }

    private SqlIdentifier? ParseSelectItemAlias()
    {
        if (_context.MatchKeyword(SqlKeywordId.As))
        {
            var alias = ParseIdentifier();
            if (alias is null)
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "AS 后缺少别名。");
            }

            return alias;
        }

        return _context.Current.Kind is SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier
            ? ParseIdentifier()
            : null;
    }

    private ImmutableArray<SqlTableSource> ParseFromList()
    {
        var sources = ImmutableArray.CreateBuilder<SqlTableSource>();

        while (true)
        {
            if (_context.TooManyErrors)
            {
                break;
            }

            var source = ParseTableSource();
            if (source is null)
            {
                break;
            }

            sources.Add(source);
            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return sources.ToImmutable();
    }

    private SqlTableSource? ParseTableSource()
    {
        var left = ParseTablePrimary();
        if (left is null)
        {
            return null;
        }

        while (true)
        {
            var applyType = TryParseApply();
            if (applyType is not null)
            {
                var applyRight = ParseTablePrimary();
                if (applyRight is null)
                {
                    break;
                }

                if (applyRight is SqlDerivedTable derivedRight)
                {
                    applyRight = derivedRight with { Lateral = true };
                }

                var applySpan = new SqlSpan(left.Span.Start, Math.Max(0, applyRight.Span.End - left.Span.Start));
                left = new SqlJoinedTable(left, applyType.Value, applyRight) { Span = applySpan };
                continue;
            }

            var joinType = TryParseJoinType(out var natural);
            if (joinType is null)
            {
                break;
            }

            var right = ParseTablePrimary();
            if (right is null)
            {
                break;
            }

            SqlExpression? on = null;
            var usingColumns = ImmutableArray<SqlIdentifier>.Empty;

            if (_context.MatchKeyword(SqlKeywordId.On))
            {
                on = ParseExpressionCore(0);
            }
            else if (_context.MatchKeyword(SqlKeywordId.Using))
            {
                _context.Expect(SqlTokenKind.LeftParen, "'('");
                var columns = ImmutableArray.CreateBuilder<SqlIdentifier>();
                while (true)
                {
                    var column = ParseIdentifier();
                    if (column is null)
                    {
                        break;
                    }

                    columns.Add(column);
                    if (!_context.Match(SqlTokenKind.Comma))
                    {
                        break;
                    }
                }

                _context.Expect(SqlTokenKind.RightParen, "')'");
                usingColumns = columns.ToImmutable();
            }

            var span = new SqlSpan(left.Span.Start, Math.Max(0, right.Span.End - left.Span.Start));
            left = new SqlJoinedTable(left, joinType.Value, right)
            {
                On = on,
                Using = usingColumns,
                Natural = natural,
                Span = span,
            };
        }

        return left;
    }

    private SqlTableSource? ParseTablePrimary()
    {
        var startIndex = _context.Index;
        var lateral = _context.MatchKeyword(SqlKeywordId.Lateral);

        if (_context.AtKeyword(SqlKeywordId.Unnest))
        {
            return ApplyPivots(ParseUnnestTable());
        }

        if (_context.At(SqlTokenKind.LeftParen))
        {
            _context.Advance();

            if (IsQueryStart())
            {
                var query = ParseQueryExpressionCore();
                _context.Expect(SqlTokenKind.RightParen, "')'");
                if (query is null)
                {
                    return null;
                }

                var (alias, columnAliases) = ParseTableAlias();
                var derived = new SqlDerivedTable(query)
                {
                    Alias = alias,
                    ColumnAliases = columnAliases,
                    Lateral = lateral,
                    Span = _context.SpanFrom(startIndex),
                };

                return ApplyPivots(derived);
            }

            var inner = ParseTableSource();
            _context.Expect(SqlTokenKind.RightParen, "')'");
            return inner is null ? null : ApplyPivots(inner);
        }

        var parts = ParseQualifiedName();
        if (parts.IsDefaultOrEmpty)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "预期表名。");
            return null;
        }

        var (tableAlias, tableColumnAliases) = ParseTableAlias();
        var tableSpan = _context.SpanFrom(startIndex);

        return ApplyPivots(CreateNamedTable(parts, tableAlias, tableColumnAliases, tableSpan));
    }

    private static SqlNamedTable CreateNamedTable(
        ImmutableArray<SqlIdentifier> parts,
        SqlIdentifier? alias,
        ImmutableArray<SqlIdentifier> columnAliases,
        SqlSpan span) => parts.Length switch
        {
            1 => new SqlNamedTable(parts[0])
            {
                Alias = alias,
                ColumnAliases = columnAliases,
                Span = span,
            },
            2 => new SqlNamedTable(parts[1])
            {
                Schema = parts[0],
                Alias = alias,
                ColumnAliases = columnAliases,
                Span = span,
            },
            _ => new SqlNamedTable(parts[^1])
            {
                Catalog = parts[0],
                Schema = parts[1],
                Alias = alias,
                ColumnAliases = columnAliases,
                Span = span,
            },
        };

    private SqlJoinType? TryParseApply()
    {
        var start = _context.Index;
        SqlJoinType applyType;

        if (_context.MatchKeyword(SqlKeywordId.Cross))
        {
            applyType = SqlJoinType.CrossApply;
        }
        else if (_context.MatchKeyword(SqlKeywordId.Outer))
        {
            applyType = SqlJoinType.OuterApply;
        }
        else
        {
            return null;
        }

        if (_context.MatchKeyword(SqlKeywordId.Apply))
        {
            return applyType;
        }

        _context.Index = start;
        return null;
    }

    private SqlTableSource ApplyPivots(SqlTableSource source)
    {
        while (true)
        {
            SqlTableSource? transformed;
            if (_context.AtKeyword(SqlKeywordId.Pivot))
            {
                transformed = ParsePivot(source);
            }
            else if (_context.AtKeyword(SqlKeywordId.Unpivot))
            {
                transformed = ParseUnpivot(source);
            }
            else
            {
                break;
            }

            if (transformed is null)
            {
                break;
            }

            source = transformed;
        }

        return source;
    }

    private SqlPivotTable? ParsePivot(SqlTableSource source)
    {
        var startIndex = _context.Index;
        _context.Advance(); // PIVOT
        _context.Expect(SqlTokenKind.LeftParen, "'('");

        var aggregate = ParseExpressionCore(0);
        _context.ExpectKeyword(SqlKeywordId.For, "FOR");
        var forColumn = ParseIdentifier();
        if (forColumn is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "PIVOT 缺少 FOR 列名。");
        }

        _context.ExpectKeyword(SqlKeywordId.In, "IN");
        _context.Expect(SqlTokenKind.LeftParen, "'('");

        var values = ImmutableArray.CreateBuilder<SqlPivotItem>();
        while (true)
        {
            var itemStart = _context.Index;
            var value = ParseExpressionCore(0);
            if (value is null)
            {
                break;
            }

            var alias = ParseSelectItemAlias();
            values.Add(new SqlPivotItem(value) { Alias = alias, Span = _context.SpanFrom(itemStart) });

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");
        _context.Expect(SqlTokenKind.RightParen, "')'");

        var (pivotAlias, _) = ParseTableAlias();

        if (aggregate is null || forColumn is null)
        {
            return null;
        }

        var pivotSpan = _context.SpanFrom(startIndex);

        return new SqlPivotTable(source, aggregate, forColumn, values.ToImmutable())
        {
            Alias = pivotAlias,
            Span = new SqlSpan(source.Span.Start, Math.Max(0, pivotSpan.End - source.Span.Start)),
        };
    }

    private SqlUnpivotTable? ParseUnpivot(SqlTableSource source)
    {
        var startIndex = _context.Index;
        _context.Advance(); // UNPIVOT
        _context.Expect(SqlTokenKind.LeftParen, "'('");

        var valueColumn = ParseIdentifier();
        _context.ExpectKeyword(SqlKeywordId.For, "FOR");
        var forColumn = ParseIdentifier();
        _context.ExpectKeyword(SqlKeywordId.In, "IN");
        _context.Expect(SqlTokenKind.LeftParen, "'('");

        var columns = ImmutableArray.CreateBuilder<SqlIdentifier>();
        while (true)
        {
            var column = ParseIdentifier();
            if (column is null)
            {
                break;
            }

            columns.Add(column);
            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        if (columns.Count == 0)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "UNPIVOT 的 IN 列表不能为空。");
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");
        _context.Expect(SqlTokenKind.RightParen, "')'");

        var (alias, _) = ParseTableAlias();

        if (valueColumn is null || forColumn is null)
        {
            return null;
        }

        var transformSpan = _context.SpanFrom(startIndex);

        return new SqlUnpivotTable(source, valueColumn, forColumn, columns.ToImmutable())
        {
            Alias = alias,
            Span = new SqlSpan(source.Span.Start, Math.Max(0, transformSpan.End - source.Span.Start)),
        };
    }

    private SqlUnnestTable ParseUnnestTable()
    {
        var startIndex = _context.Index;
        _context.Advance(); // UNNEST
        _context.Expect(SqlTokenKind.LeftParen, "'('");
        var arguments = ParseExpressionList();
        _context.Expect(SqlTokenKind.RightParen, "')'");

        var ordinality = false;
        if (_context.MatchKeyword(SqlKeywordId.With))
        {
            _context.ExpectKeyword(SqlKeywordId.Ordinality, "ORDINALITY");
            ordinality = true;
        }

        var (alias, columnAliases) = ParseTableAlias();

        if (arguments.IsDefaultOrEmpty)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "UNNEST 缺少参数。");
        }

        return new SqlUnnestTable(arguments)
        {
            WithOrdinality = ordinality,
            Alias = alias,
            ColumnAliases = columnAliases,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private (SqlIdentifier? Alias, ImmutableArray<SqlIdentifier> ColumnAliases) ParseTableAlias()
    {
        SqlIdentifier? alias = null;

        if (_context.MatchKeyword(SqlKeywordId.As))
        {
            alias = ParseIdentifier();
            if (alias is null)
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "AS 后缺少表别名。");
            }
        }
        else if (_context.Current.Kind is SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier)
        {
            alias = ParseIdentifier();
        }

        if (alias is null || !_context.At(SqlTokenKind.LeftParen))
        {
            return (alias, ImmutableArray<SqlIdentifier>.Empty);
        }

        _context.Advance();
        var columns = ImmutableArray.CreateBuilder<SqlIdentifier>();
        while (true)
        {
            var column = ParseIdentifier();
            if (column is null)
            {
                break;
            }

            columns.Add(column);
            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");
        return (alias, columns.ToImmutable());
    }

    private ImmutableArray<SqlIdentifier> ParseQualifiedName()
    {
        var parts = ImmutableArray.CreateBuilder<SqlIdentifier>();

        while (true)
        {
            var part = ParseIdentifier();
            if (part is null)
            {
                break;
            }

            parts.Add(part);
            if (!_context.Match(SqlTokenKind.Dot))
            {
                break;
            }
        }

        return parts.ToImmutable();
    }

    private SqlJoinType? TryParseJoinType(out bool natural)
    {
        var start = _context.Index;
        natural = _context.MatchKeyword(SqlKeywordId.Natural);

        SqlJoinType? joinType = null;
        if (_context.MatchKeyword(SqlKeywordId.Inner))
        {
            joinType = SqlJoinType.Inner;
        }
        else if (_context.MatchKeyword(SqlKeywordId.Left))
        {
            joinType = SqlJoinType.Left;
        }
        else if (_context.MatchKeyword(SqlKeywordId.Right))
        {
            joinType = SqlJoinType.Right;
        }
        else if (_context.MatchKeyword(SqlKeywordId.Full))
        {
            joinType = SqlJoinType.Full;
        }
        else if (_context.MatchKeyword(SqlKeywordId.Cross))
        {
            joinType = SqlJoinType.Cross;
        }
        else if (_context.MatchKeyword(SqlKeywordId.StraightJoin))
        {
            joinType = SqlJoinType.Straight;
        }

        if (joinType is SqlJoinType.Left or SqlJoinType.Right or SqlJoinType.Full)
        {
            _context.MatchKeyword(SqlKeywordId.Outer);
        }

        if (_context.MatchKeyword(SqlKeywordId.Join))
        {
            return joinType ?? SqlJoinType.Inner;
        }

        _context.Index = start;
        natural = false;
        return null;
    }

    private SqlOrderByClause ParseOrderByClause(int startIndex)
    {
        var items = ImmutableArray.CreateBuilder<SqlOrderByItem>();

        while (true)
        {
            var itemStart = _context.Index;
            var expression = ParseExpressionCore(0);
            if (expression is null)
            {
                break;
            }

            var direction = SqlSortOrder.Unspecified;
            if (_context.MatchKeyword(SqlKeywordId.Asc))
            {
                direction = SqlSortOrder.Ascending;
            }
            else if (_context.MatchKeyword(SqlKeywordId.Desc))
            {
                direction = SqlSortOrder.Descending;
            }

            var nulls = SqlNullsOrder.Unspecified;
            if (_context.MatchKeyword(SqlKeywordId.Nulls))
            {
                if (_context.MatchKeyword(SqlKeywordId.First))
                {
                    nulls = SqlNullsOrder.First;
                }
                else if (_context.MatchKeyword(SqlKeywordId.Last))
                {
                    nulls = SqlNullsOrder.Last;
                }
            }

            items.Add(new SqlOrderByItem(expression)
            {
                Direction = direction,
                Nulls = nulls,
                Span = _context.SpanFrom(itemStart),
            });

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return new SqlOrderByClause(items.ToImmutable()) { Span = _context.SpanFrom(startIndex) };
    }

    private SqlGroupByClause ParseGroupByClause(int startIndex)
    {
        var items = ImmutableArray.CreateBuilder<SqlExpression>();

        while (true)
        {
            var item = ParseExpressionCore(0);
            if (item is null)
            {
                break;
            }

            items.Add(item);
            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return new SqlGroupByClause(items.ToImmutable()) { Span = _context.SpanFrom(startIndex) };
    }

    private SqlFetchClause ParseFetchClause(int startIndex)
    {
        if (!_context.MatchKeyword(SqlKeywordId.First))
        {
            _context.ExpectKeyword(SqlKeywordId.Next, "FIRST 或 NEXT");
        }

        SqlExpression? count = null;
        if (!_context.AtKeyword(SqlKeywordId.Row)
            && !_context.AtKeyword(SqlKeywordId.Rows)
            && !_context.AtKeyword(SqlKeywordId.Only))
        {
            count = ParseExpressionCore(0);
        }

        var percent = _context.MatchKeyword(SqlKeywordId.Percent);

        if (!_context.MatchKeyword(SqlKeywordId.Row))
        {
            _context.ExpectKeyword(SqlKeywordId.Rows, "ROW 或 ROWS");
        }

        var withTies = false;
        if (_context.MatchKeyword(SqlKeywordId.With))
        {
            _context.ExpectKeyword(SqlKeywordId.Ties, "TIES");
            withTies = true;
        }
        else
        {
            _context.ExpectKeyword(SqlKeywordId.Only, "ONLY 或 WITH TIES");
        }

        return new SqlFetchClause(count)
        {
            WithTies = withTies,
            Percent = percent,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlWindowClause ParseWindowClause(int startIndex)
    {
        var windows = ImmutableArray.CreateBuilder<SqlNamedWindow>();

        while (true)
        {
            var itemStart = _context.Index;
            var name = ParseIdentifier();
            if (name is null)
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "WINDOW 子句缺少窗口名。");
                break;
            }

            _context.ExpectKeyword(SqlKeywordId.As, "AS");
            var specification = ParseWindowSpecification();
            if (specification is null)
            {
                break;
            }

            windows.Add(new SqlNamedWindow(name, specification) { Span = _context.SpanFrom(itemStart) });

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return new SqlWindowClause(windows.ToImmutable()) { Span = _context.SpanFrom(startIndex) };
    }

    /// <summary>
    /// 解析窗口定义：<c>OVER w</c>、<c>OVER (w ORDER BY ...)</c> 或完整的
    /// <c>OVER (PARTITION BY ... ORDER BY ... ROWS/RANGE/GROUPS ...)</c>。
    /// </summary>
    private SqlWindowSpecification? ParseWindowSpecification()
    {
        var startIndex = _context.Index;

        if (_context.Current.Kind is SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier)
        {
            var name = ParseIdentifier();
            return new SqlWindowSpecification { Name = name, Span = _context.SpanFrom(startIndex) };
        }

        if (!_context.Match(SqlTokenKind.LeftParen))
        {
            _context.Report(
                SqlDiagnosticCodes.SyntaxError,
                _context.Current,
                "OVER 后缺少窗口定义或窗口名。");
            return null;
        }

        SqlIdentifier? baseName = null;
        if (_context.Current.Kind is SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier)
        {
            baseName = ParseIdentifier();
        }

        var partitionBy = ImmutableArray<SqlExpression>.Empty;
        if (_context.MatchKeyword(SqlKeywordId.Partition))
        {
            _context.ExpectKeyword(SqlKeywordId.By, "BY");
            partitionBy = ParseExpressionList();
        }

        SqlOrderByClause? orderBy = null;
        if (_context.AtKeyword(SqlKeywordId.Order))
        {
            var orderStart = _context.Index;
            _context.Advance();
            _context.ExpectKeyword(SqlKeywordId.By, "BY");
            orderBy = ParseOrderByClause(orderStart);
        }

        var frame = ParseWindowFrame();
        _context.Expect(SqlTokenKind.RightParen, "')'");

        return new SqlWindowSpecification
        {
            Name = baseName,
            PartitionBy = partitionBy,
            OrderBy = orderBy,
            Frame = frame,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlWindowFrame? ParseWindowFrame()
    {
        var startIndex = _context.Index;

        SqlWindowFrameUnit unit;
        if (_context.MatchKeyword(SqlKeywordId.Rows))
        {
            unit = SqlWindowFrameUnit.Rows;
        }
        else if (_context.MatchKeyword(SqlKeywordId.Range))
        {
            unit = SqlWindowFrameUnit.Range;
        }
        else if (_context.MatchKeyword(SqlKeywordId.Groups))
        {
            unit = SqlWindowFrameUnit.Groups;
        }
        else
        {
            return null;
        }

        if (_context.MatchKeyword(SqlKeywordId.Between))
        {
            var start = ParseWindowFrameBound();
            _context.ExpectKeyword(SqlKeywordId.And, "AND");
            var end = ParseWindowFrameBound();
            if (start is null)
            {
                return null;
            }

            return new SqlWindowFrame(unit, start)
            {
                End = end,
                Span = _context.SpanFrom(startIndex),
            };
        }

        var single = ParseWindowFrameBound();
        return single is null
            ? null
            : new SqlWindowFrame(unit, single) { Span = _context.SpanFrom(startIndex) };
    }

    private SqlWindowFrameBound? ParseWindowFrameBound()
    {
        var startIndex = _context.Index;

        if (_context.MatchKeyword(SqlKeywordId.Unbounded))
        {
            if (_context.MatchKeyword(SqlKeywordId.Preceding))
            {
                return new SqlWindowFrameBound(SqlWindowFrameBoundKind.UnboundedPreceding)
                {
                    Span = _context.SpanFrom(startIndex),
                };
            }

            if (_context.MatchKeyword(SqlKeywordId.Following))
            {
                return new SqlWindowFrameBound(SqlWindowFrameBoundKind.UnboundedFollowing)
                {
                    Span = _context.SpanFrom(startIndex),
                };
            }

            _context.Report(
                SqlDiagnosticCodes.SyntaxError,
                _context.Current,
                "UNBOUNDED 后缺少 PRECEDING 或 FOLLOWING。");
            return null;
        }

        if (_context.MatchKeyword(SqlKeywordId.Current))
        {
            _context.ExpectKeyword(SqlKeywordId.Row, "ROW");
            return new SqlWindowFrameBound(SqlWindowFrameBoundKind.CurrentRow)
            {
                Span = _context.SpanFrom(startIndex),
            };
        }

        var value = ParseExpressionCore(0);
        if (value is null)
        {
            return null;
        }

        if (_context.MatchKeyword(SqlKeywordId.Preceding))
        {
            return new SqlWindowFrameBound(SqlWindowFrameBoundKind.Preceding)
            {
                Value = value,
                Span = _context.SpanFrom(startIndex),
            };
        }

        if (_context.MatchKeyword(SqlKeywordId.Following))
        {
            return new SqlWindowFrameBound(SqlWindowFrameBoundKind.Following)
            {
                Value = value,
                Span = _context.SpanFrom(startIndex),
            };
        }

        _context.Report(
            SqlDiagnosticCodes.SyntaxError,
            _context.Current,
            "窗口帧边界缺少 PRECEDING 或 FOLLOWING。");
        return null;
    }

    private ImmutableArray<SqlExpression> ParseExpressionList()
    {
        var items = ImmutableArray.CreateBuilder<SqlExpression>();

        while (true)
        {
            var item = ParseExpressionCore(0);
            if (item is null)
            {
                break;
            }

            items.Add(item);
            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return items.ToImmutable();
    }
}
