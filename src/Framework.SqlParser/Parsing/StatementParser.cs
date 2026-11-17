using System.Collections.Immutable;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Parsing;

internal sealed partial class SqlParserEngine
{
    private SqlStatement? ParseStatementCore()
    {
        while (_context.Match(SqlTokenKind.Semicolon))
        {
        }

        if (_context.IsAtEnd)
        {
            _context.Report(SqlDiagnosticCodes.UnexpectedEndOfInput, _context.Current, "SQL 语句为空。");
            return null;
        }

        var token = _context.Current;
        if (token.Is(SqlKeywordId.Select)
            || token.Is(SqlKeywordId.Values)
            || token.Kind == SqlTokenKind.LeftParen
            || token.Is(SqlKeywordId.With))
        {
            var startIndex = _context.Index;
            SqlWithClause? with = null;

            if (token.Is(SqlKeywordId.With))
            {
                with = ParseWithClause();

                if (IsDmlKeyword(_context.Current))
                {
                    SqlStatement? dml = _context.Current.Keyword switch
                    {
                        SqlKeywordId.Insert => ParseInsertStatement(startIndex, with),
                        SqlKeywordId.Update => ParseUpdateStatement(startIndex, with),
                        SqlKeywordId.Delete => ParseDeleteStatement(startIndex, with),
                        _ => ParseMergeStatement(startIndex, with),
                    };

                    if (dml is null)
                    {
                        _context.SkipToEnd();
                        return null;
                    }

                    FinishStatement();
                    return dml;
                }

                if (!IsQueryStartAfterWith())
                {
                    _context.Report(
                        SqlDiagnosticCodes.SyntaxError,
                        _context.Current,
                        "WITH 子句之后缺少查询或 DML 语句。");

                    _context.SkipToStatementEnd();
                    var invalidSpan = _context.SpanFrom(startIndex);

                    return new SqlInvalidStatement(invalidSpan)
                    {
                        Dialect = _dialect,
                        Span = invalidSpan,
                    };
                }
            }

            var query = ParseQueryExpressionCore();
            if (query is null)
            {
                _context.SkipToEnd();
                return null;
            }

            var statement = new SqlSelectStatement(query)
            {
                With = with,
                Dialect = _dialect,
                Span = _context.SpanFrom(startIndex),
            };

            FinishStatement();
            return statement;
        }

        if (token.Is(SqlKeywordId.Insert)
            || token.Is(SqlKeywordId.Update)
            || token.Is(SqlKeywordId.Delete)
            || token.Is(SqlKeywordId.Merge))
        {
            var startIndex = _context.Index;
            SqlStatement? dml = token.Keyword switch
            {
                SqlKeywordId.Insert => ParseInsertStatement(startIndex),
                SqlKeywordId.Update => ParseUpdateStatement(startIndex),
                SqlKeywordId.Delete => ParseDeleteStatement(startIndex),
                _ => ParseMergeStatement(startIndex),
            };

            if (dml is null)
            {
                _context.SkipToEnd();
                return null;
            }

            FinishStatement();
            return dml;
        }

        if (token.Is(SqlKeywordId.Create)
            || token.Is(SqlKeywordId.Drop)
            || token.Is(SqlKeywordId.Truncate))
        {
            var startIndex = _context.Index;
            SqlStatement? ddl = token.Keyword switch
            {
                SqlKeywordId.Create => ParseCreateStatement(startIndex),
                SqlKeywordId.Drop => ParseDropTableStatement(startIndex),
                _ => ParseTruncateTableStatement(startIndex),
            };

            if (ddl is null)
            {
                _context.SkipToEnd();
                return null;
            }

            FinishStatement();
            return ddl;
        }

        _context.Report(SqlDiagnosticCodes.UnsupportedStatement, token, "不支持的语句类型。");

        var rawStart = _context.Index;
        _context.SkipToStatementEnd();
        var span = _context.SpanFrom(rawStart);

        return new SqlInvalidStatement(span)
        {
            Dialect = _dialect,
            Span = span,
        };
    }

    private SqlWithClause ParseWithClause()
    {
        var startIndex = _context.Index;
        _context.Advance(); // WITH

        var recursive = _context.MatchKeyword(SqlKeywordId.Recursive);
        var ctes = ImmutableArray.CreateBuilder<SqlCommonTableExpression>();

        while (true)
        {
            var cteStart = _context.Index;
            var name = ParseIdentifier();
            if (name is null)
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "WITH 后缺少 CTE 名称。");
                break;
            }

            var columns = ImmutableArray<SqlIdentifier>.Empty;
            if (_context.At(SqlTokenKind.LeftParen))
            {
                _context.Advance();
                var columnBuilder = ImmutableArray.CreateBuilder<SqlIdentifier>();
                while (true)
                {
                    var column = ParseIdentifier();
                    if (column is null)
                    {
                        break;
                    }

                    columnBuilder.Add(column);
                    if (!_context.Match(SqlTokenKind.Comma))
                    {
                        break;
                    }
                }

                if (columnBuilder.Count == 0)
                {
                    _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "CTE 列别名列表不能为空。");
                }

                _context.Expect(SqlTokenKind.RightParen, "')'");
                columns = columnBuilder.ToImmutable();
            }

            _context.ExpectKeyword(SqlKeywordId.As, "AS");

            var materialization = SqlCteMaterialization.Default;
            if (_context.MatchKeyword(SqlKeywordId.Materialized))
            {
                materialization = SqlCteMaterialization.Materialized;
            }
            else if (_context.AtKeyword(SqlKeywordId.Not) && _context.Peek().Is(SqlKeywordId.Materialized))
            {
                _context.Advance();
                _context.Advance();
                materialization = SqlCteMaterialization.NotMaterialized;
            }

            _context.Expect(SqlTokenKind.LeftParen, "'('");
            var query = ParseQueryExpressionCore();
            _context.Expect(SqlTokenKind.RightParen, "')'");

            if (query is null)
            {
                break;
            }

            ctes.Add(new SqlCommonTableExpression(name, query)
            {
                Columns = columns,
                Materialization = materialization,
                Span = _context.SpanFrom(cteStart),
            });

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return new SqlWithClause(ctes.ToImmutable())
        {
            Recursive = recursive,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private bool IsQueryStartAfterWith() =>
        _context.AtKeyword(SqlKeywordId.Select)
        || _context.AtKeyword(SqlKeywordId.Values)
        || _context.At(SqlTokenKind.LeftParen);

    private SqlInsertStatement? ParseInsertStatement(int startIndex, SqlWithClause? with = null)
    {
        _context.Advance(); // INSERT
        _context.MatchKeyword(SqlKeywordId.Into);

        var target = ParseNamedTableTarget(allowAlias: false);
        if (target is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "INSERT 缺少目标表。");
            return null;
        }

        var columns = _context.At(SqlTokenKind.LeftParen)
            ? ParseIdentifierList()
            : ImmutableArray<SqlIdentifier>.Empty;

        var output = ParseOutputClause();

        SqlQueryExpression? source = null;
        var defaultValues = false;

        if (_context.AtKeyword(SqlKeywordId.Default))
        {
            _context.Advance();
            _context.ExpectKeyword(SqlKeywordId.Values, "VALUES");
            defaultValues = true;
        }
        else if (_context.AtKeyword(SqlKeywordId.Values))
        {
            source = ParseValuesQuery();
        }
        else if (IsQueryStart())
        {
            source = ParseQueryExpressionCore();
        }
        else
        {
            _context.Report(
                SqlDiagnosticCodes.SyntaxError,
                _context.Current,
                "INSERT 需要 VALUES 或 SELECT 子句。");
        }

        return new SqlInsertStatement(target)
        {
            With = with,
            Columns = columns,
            Output = output,
            Source = source,
            DefaultValues = defaultValues,
            Returning = ParseReturning(),
            Dialect = _dialect,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlUpdateStatement? ParseUpdateStatement(int startIndex, SqlWithClause? with = null)
    {
        _context.Advance(); // UPDATE

        var target = ParseNamedTableTarget();
        if (target is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "UPDATE 缺少目标表。");
            return null;
        }

        var assignments = ImmutableArray<SqlSetAssignment>.Empty;
        if (_context.MatchKeyword(SqlKeywordId.Set))
        {
            assignments = ParseSetAssignments();
        }
        else
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "UPDATE 需要 SET 子句。");
        }

        var output = ParseOutputClause();

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

        return new SqlUpdateStatement(target)
        {
            With = with,
            Assignments = assignments,
            Output = output,
            From = from,
            Where = where,
            Returning = ParseReturning(),
            Dialect = _dialect,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlDeleteStatement? ParseDeleteStatement(int startIndex, SqlWithClause? with = null)
    {
        _context.Advance(); // DELETE
        _context.ExpectKeyword(SqlKeywordId.From, "FROM");

        var target = ParseNamedTableTarget();
        if (target is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "DELETE 缺少目标表。");
            return null;
        }

        var output = ParseOutputClause();

        SqlExpression? where = null;
        if (_context.MatchKeyword(SqlKeywordId.Where))
        {
            where = ParseExpressionCore(0);
        }

        return new SqlDeleteStatement(target)
        {
            With = with,
            Output = output,
            Where = where,
            Returning = ParseReturning(),
            Dialect = _dialect,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlNamedTable? ParseNamedTableTarget(bool allowAlias = true)
    {
        var startIndex = _context.Index;
        var parts = ParseQualifiedName();
        if (parts.IsDefaultOrEmpty)
        {
            return null;
        }

        SqlIdentifier? alias = null;
        var columnAliases = ImmutableArray<SqlIdentifier>.Empty;

        if (allowAlias)
        {
            (alias, columnAliases) = ParseTableAlias();
        }

        return CreateNamedTable(parts, alias, columnAliases, _context.SpanFrom(startIndex));
    }

    private ImmutableArray<SqlSelectItem> ParseReturning()
    {
        return _context.MatchKeyword(SqlKeywordId.Returning)
            ? ParseSelectItems()
            : ImmutableArray<SqlSelectItem>.Empty;
    }

    private ImmutableArray<SqlIdentifier> ParseIdentifierList()
    {
        _context.Expect(SqlTokenKind.LeftParen, "'('");
        var builder = ImmutableArray.CreateBuilder<SqlIdentifier>();

        while (true)
        {
            var identifier = ParseIdentifier();
            if (identifier is null)
            {
                break;
            }

            builder.Add(identifier);
            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        if (builder.Count == 0)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "列列表不能为空。");
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");
        return builder.ToImmutable();
    }

    private ImmutableArray<SqlSetAssignment> ParseSetAssignments()
    {
        var builder = ImmutableArray.CreateBuilder<SqlSetAssignment>();

        while (true)
        {
            var assignmentStart = _context.Index;
            var column = ParseIdentifier();
            if (column is null)
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "SET 缺少列名。");
                break;
            }

            _context.Expect(SqlTokenKind.Equal, "'='");
            var value = ParseExpressionCore(0);
            if (value is null)
            {
                break;
            }

            builder.Add(new SqlSetAssignment(column, value) { Span = _context.SpanFrom(assignmentStart) });

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return builder.ToImmutable();
    }

    private SqlMergeStatement? ParseMergeStatement(int startIndex, SqlWithClause? with = null)
    {
        _context.Advance(); // MERGE
        _context.MatchKeyword(SqlKeywordId.Into);

        var target = ParseNamedTableTarget();
        if (target is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "MERGE 缺少目标表。");
            return null;
        }

        _context.ExpectKeyword(SqlKeywordId.Using, "USING");
        var source = ParseTablePrimary();
        if (source is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "MERGE 缺少 USING 数据源。");
            return null;
        }

        _context.ExpectKeyword(SqlKeywordId.On, "ON");
        var on = ParseExpressionCore(0);
        if (on is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "MERGE 缺少 ON 条件。");
            return null;
        }

        var clauses = ImmutableArray.CreateBuilder<SqlMergeClause>();

        while (_context.MatchKeyword(SqlKeywordId.When))
        {
            var clauseStart = _context.Index - 1;

            SqlMergeMatchKind matchKind;
            if (_context.MatchKeyword(SqlKeywordId.Matched))
            {
                matchKind = SqlMergeMatchKind.Matched;
            }
            else
            {
                _context.ExpectKeyword(SqlKeywordId.Not, "NOT");
                _context.ExpectKeyword(SqlKeywordId.Matched, "MATCHED");

                if (_context.MatchKeyword(SqlKeywordId.By) && _context.MatchKeyword(SqlKeywordId.Source))
                {
                    matchKind = SqlMergeMatchKind.NotMatchedBySource;
                }
                else
                {
                    _context.MatchKeyword(SqlKeywordId.Target);
                    matchKind = SqlMergeMatchKind.NotMatchedByTarget;
                }
            }

            SqlExpression? condition = null;
            if (_context.MatchKeyword(SqlKeywordId.And))
            {
                condition = ParseExpressionCore(0);
            }

            _context.ExpectKeyword(SqlKeywordId.Then, "THEN");
            var action = ParseMergeAction();
            if (action is null)
            {
                break;
            }

            clauses.Add(new SqlMergeClause(matchKind, action)
            {
                Condition = condition,
                Span = _context.SpanFrom(clauseStart),
            });
        }

        if (clauses.Count == 0)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "MERGE 至少需要一个 WHEN 子句。");
        }

        return new SqlMergeStatement(target, source, on)
        {
            With = with,
            Clauses = clauses.ToImmutable(),
            Output = ParseOutputClause(),
            Dialect = _dialect,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlMergeAction? ParseMergeAction()
    {
        var startIndex = _context.Index;

        if (_context.MatchKeyword(SqlKeywordId.Update))
        {
            _context.ExpectKeyword(SqlKeywordId.Set, "SET");
            return new SqlMergeUpdateAction(ParseSetAssignments())
            {
                Span = _context.SpanFrom(startIndex),
            };
        }

        if (_context.MatchKeyword(SqlKeywordId.Insert))
        {
            var columns = _context.At(SqlTokenKind.LeftParen)
                ? ParseIdentifierList()
                : ImmutableArray<SqlIdentifier>.Empty;

            _context.ExpectKeyword(SqlKeywordId.Values, "VALUES");
            _context.Expect(SqlTokenKind.LeftParen, "'('");
            var values = ParseExpressionList();
            _context.Expect(SqlTokenKind.RightParen, "')'");

            return new SqlMergeInsertAction(columns, values)
            {
                Span = _context.SpanFrom(startIndex),
            };
        }

        if (_context.MatchKeyword(SqlKeywordId.Delete))
        {
            return new SqlMergeDeleteAction { Span = _context.SpanFrom(startIndex) };
        }

        _context.Report(
            SqlDiagnosticCodes.SyntaxError,
            _context.Current,
            "MERGE 操作需要 UPDATE / INSERT / DELETE。");
        return null;
    }

    private SqlOutputClause? ParseOutputClause()
    {
        if (!_context.AtKeyword(SqlKeywordId.Output))
        {
            return null;
        }

        var startIndex = _context.Index;
        _context.Advance(); // OUTPUT

        var items = ParseSelectItems();

        SqlNamedTable? into = null;
        var intoColumns = ImmutableArray<SqlIdentifier>.Empty;

        if (_context.MatchKeyword(SqlKeywordId.Into))
        {
            into = ParseNamedTableTarget(allowAlias: false);
            if (into is null)
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "OUTPUT INTO 缺少目标表。");
            }

            if (_context.At(SqlTokenKind.LeftParen))
            {
                intoColumns = ParseIdentifierList();
            }
        }

        return new SqlOutputClause(items)
        {
            Into = into,
            IntoColumns = intoColumns,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlStatement? ParseCreateStatement(int startIndex)
    {
        _context.Advance(); // CREATE

        var temporary = _context.MatchKeyword(SqlKeywordId.Temporary) || _context.MatchKeyword(SqlKeywordId.Temp);
        if (!_context.MatchKeyword(SqlKeywordId.Table))
        {
            _context.Report(SqlDiagnosticCodes.UnsupportedStatement, _context.Current, "仅支持 CREATE TABLE 语句。");
            return null;
        }

        var ifNotExists = false;
        if (_context.MatchKeyword(SqlKeywordId.If))
        {
            _context.ExpectKeyword(SqlKeywordId.Not, "NOT");
            _context.ExpectKeyword(SqlKeywordId.Exists, "EXISTS");
            ifNotExists = true;
        }

        var table = ParseNamedTableTarget(allowAlias: false);
        if (table is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "CREATE TABLE 缺少表名。");
            return null;
        }

        _context.Expect(SqlTokenKind.LeftParen, "'('");

        var columns = ImmutableArray.CreateBuilder<SqlColumnDefinition>();
        var constraints = ImmutableArray.CreateBuilder<SqlTableConstraint>();

        while (!_context.At(SqlTokenKind.RightParen) && !_context.At(SqlTokenKind.EndOfFile))
        {
            if (_context.AtKeyword(SqlKeywordId.Primary)
                || _context.AtKeyword(SqlKeywordId.Unique)
                || _context.AtKeyword(SqlKeywordId.Check)
                || _context.AtKeyword(SqlKeywordId.Foreign)
                || _context.AtKeyword(SqlKeywordId.Constraint))
            {
                var constraint = ParseTableConstraint();
                if (constraint is null)
                {
                    break;
                }

                constraints.Add(constraint);
            }
            else
            {
                var column = ParseColumnDefinition();
                if (column is null)
                {
                    break;
                }

                columns.Add(column);
            }

            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");

        return new SqlCreateTableStatement(table)
        {
            IfNotExists = ifNotExists,
            Temporary = temporary,
            Columns = columns.ToImmutable(),
            Constraints = constraints.ToImmutable(),
            Dialect = _dialect,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlColumnDefinition? ParseColumnDefinition()
    {
        var startIndex = _context.Index;
        var name = ParseIdentifier();
        if (name is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "列定义缺少列名。");
            return null;
        }

        var dataType = ParseDataType();
        if (dataType is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "列定义缺少数据类型。");
            return null;
        }

        var nullability = SqlNullability.Unspecified;
        var primaryKey = false;
        var unique = false;
        SqlExpression? defaultValue = null;
        SqlExpression? check = null;
        SqlNamedTable? references = null;
        var referencedColumns = ImmutableArray<SqlIdentifier>.Empty;

        while (true)
        {
            if (_context.MatchKeyword(SqlKeywordId.Not))
            {
                _context.ExpectKeyword(SqlKeywordId.Null, "NULL");
                nullability = SqlNullability.NotNull;
                continue;
            }

            if (_context.MatchKeyword(SqlKeywordId.Null))
            {
                nullability = SqlNullability.Null;
                continue;
            }

            if (_context.MatchKeyword(SqlKeywordId.Default))
            {
                defaultValue = ParseExpressionCore(0);
                continue;
            }

            if (_context.MatchKeyword(SqlKeywordId.Primary))
            {
                _context.ExpectKeyword(SqlKeywordId.Key, "KEY");
                primaryKey = true;
                continue;
            }

            if (_context.MatchKeyword(SqlKeywordId.Unique))
            {
                unique = true;
                continue;
            }

            if (_context.MatchKeyword(SqlKeywordId.Check))
            {
                _context.Expect(SqlTokenKind.LeftParen, "'('");
                check = ParseExpressionCore(0);
                _context.Expect(SqlTokenKind.RightParen, "')'");
                continue;
            }

            if (_context.MatchKeyword(SqlKeywordId.References))
            {
                references = ParseReferencesTarget(out referencedColumns);
                continue;
            }

            break;
        }

        return new SqlColumnDefinition(name, dataType)
        {
            Nullability = nullability,
            Default = defaultValue,
            PrimaryKey = primaryKey,
            Unique = unique,
            Check = check,
            References = references,
            ReferencedColumns = referencedColumns,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlTableConstraint? ParseTableConstraint()
    {
        var startIndex = _context.Index;
        SqlIdentifier? name = null;

        if (_context.MatchKeyword(SqlKeywordId.Constraint))
        {
            name = ParseIdentifier();
        }

        if (_context.MatchKeyword(SqlKeywordId.Primary))
        {
            _context.ExpectKeyword(SqlKeywordId.Key, "KEY");
            return new SqlTableConstraint(SqlTableConstraintKind.PrimaryKey, ParseIdentifierList())
            {
                Name = name,
                Span = _context.SpanFrom(startIndex),
            };
        }

        if (_context.MatchKeyword(SqlKeywordId.Unique))
        {
            return new SqlTableConstraint(SqlTableConstraintKind.Unique, ParseIdentifierList())
            {
                Name = name,
                Span = _context.SpanFrom(startIndex),
            };
        }

        if (_context.MatchKeyword(SqlKeywordId.Check))
        {
            _context.Expect(SqlTokenKind.LeftParen, "'('");
            var expression = ParseExpressionCore(0);
            _context.Expect(SqlTokenKind.RightParen, "')'");

            return new SqlTableConstraint(SqlTableConstraintKind.Check, ImmutableArray<SqlIdentifier>.Empty)
            {
                Name = name,
                Check = expression,
                Span = _context.SpanFrom(startIndex),
            };
        }

        if (_context.MatchKeyword(SqlKeywordId.Foreign))
        {
            _context.ExpectKeyword(SqlKeywordId.Key, "KEY");
            var columns = ParseIdentifierList();
            _context.ExpectKeyword(SqlKeywordId.References, "REFERENCES");
            var references = ParseReferencesTarget(out var referencedColumns);

            return new SqlTableConstraint(SqlTableConstraintKind.ForeignKey, columns)
            {
                Name = name,
                References = references,
                ReferencedColumns = referencedColumns,
                Span = _context.SpanFrom(startIndex),
            };
        }

        _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "无法识别的表约束。");
        return null;
    }

    private SqlNamedTable? ParseReferencesTarget(out ImmutableArray<SqlIdentifier> columns)
    {
        columns = ImmutableArray<SqlIdentifier>.Empty;

        var table = ParseNamedTableTarget(allowAlias: false);
        if (table is null)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "REFERENCES 缺少目标表。");
            return null;
        }

        if (_context.At(SqlTokenKind.LeftParen))
        {
            columns = ParseIdentifierList();
        }

        return table;
    }

    private SqlDropTableStatement? ParseDropTableStatement(int startIndex)
    {
        _context.Advance(); // DROP
        if (!_context.MatchKeyword(SqlKeywordId.Table))
        {
            _context.Report(SqlDiagnosticCodes.UnsupportedStatement, _context.Current, "仅支持 DROP TABLE 语句。");
            return null;
        }

        var ifExists = false;
        if (_context.MatchKeyword(SqlKeywordId.If))
        {
            _context.ExpectKeyword(SqlKeywordId.Exists, "EXISTS");
            ifExists = true;
        }

        var tables = ParseTableNameList();
        if (tables.IsDefaultOrEmpty)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "DROP TABLE 缺少表名。");
        }

        var cascade = _context.MatchKeyword(SqlKeywordId.Cascade);
        if (!cascade)
        {
            _context.MatchKeyword(SqlKeywordId.Restrict);
        }

        return new SqlDropTableStatement(tables)
        {
            IfExists = ifExists,
            Cascade = cascade,
            Dialect = _dialect,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlTruncateTableStatement? ParseTruncateTableStatement(int startIndex)
    {
        _context.Advance(); // TRUNCATE
        _context.MatchKeyword(SqlKeywordId.Table);

        var tables = ParseTableNameList();
        if (tables.IsDefaultOrEmpty)
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "TRUNCATE 缺少表名。");
        }

        return new SqlTruncateTableStatement(tables)
        {
            Dialect = _dialect,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private ImmutableArray<SqlNamedTable> ParseTableNameList()
    {
        var builder = ImmutableArray.CreateBuilder<SqlNamedTable>();

        while (true)
        {
            var table = ParseNamedTableTarget(allowAlias: false);
            if (table is null)
            {
                break;
            }

            builder.Add(table);
            if (!_context.Match(SqlTokenKind.Comma))
            {
                break;
            }
        }

        return builder.ToImmutable();
    }

    private static bool IsDmlKeyword(SqlToken token) =>
        token.Kind == SqlTokenKind.Keyword
        && token.Keyword is SqlKeywordId.Insert or SqlKeywordId.Update or SqlKeywordId.Delete or SqlKeywordId.Merge;

    private void FinishStatement()
    {
        while (_context.Match(SqlTokenKind.Semicolon))
        {
        }

        if (_context.IsAtEnd)
        {
            return;
        }

        _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "语句结束后存在无法解析的标记。");
        _context.SkipToEnd();
    }
}
