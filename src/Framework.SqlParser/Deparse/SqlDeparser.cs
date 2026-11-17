using System.Collections.Immutable;
using System.Text;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Deparse;

/// <summary>Deparser 无法回写节点时抛出。</summary>
public sealed class SqlDeparseException : Exception
{
    /// <summary>使用消息构造。</summary>
    public SqlDeparseException(string message)
        : base(message)
    {
    }
}

/// <summary>Deparser 回写选项。</summary>
public sealed class SqlDeparseOptions
{
    /// <summary>默认选项。</summary>
    public static SqlDeparseOptions Default { get; } = new();

    /// <summary>
    /// 分页子句的目标方言。为 <c>null</c> 时沿用 AST 中的原始写法，
    /// 保证 <c>Parse → Deparse</c> 结构等价。
    /// </summary>
    public SqlDialect? Dialect { get; init; }
}

/// <summary>
/// AST 回写器：把语法树还原为规范化 SQL 文本。关键字统一大写，
/// 表达式按优先级补全必要括号，输出为单行。
/// </summary>
public static class SqlDeparser
{
    /// <summary>回写语句。</summary>
    public static string Deparse(SqlStatement statement, SqlDeparseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(statement);

        var writer = new Writer(options);
        writer.WriteStatement(statement);
        return writer.ToString();
    }

    /// <summary>回写查询表达式。</summary>
    public static string Deparse(SqlQueryExpression query, SqlDeparseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(query);

        var writer = new Writer(options);
        writer.WriteQuery(query);
        return writer.ToString();
    }

    /// <summary>回写数据源。</summary>
    public static string Deparse(SqlTableSource source, SqlDeparseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var writer = new Writer(options);
        writer.WriteTableSource(source);
        return writer.ToString();
    }

    /// <summary>回写表达式。</summary>
    public static string Deparse(SqlExpression expression, SqlDeparseOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var writer = new Writer(options);
        writer.WriteExpression(expression);
        return writer.ToString();
    }

    private sealed class Writer
    {
        private readonly StringBuilder _builder = new();
        private readonly SqlDialect? _dialect;

        public Writer(SqlDeparseOptions? options)
        {
            _dialect = options?.Dialect;
        }

        public override string ToString() => _builder.ToString().Trim();

        // ---------- 基础输出 ----------

        private void Token(string text)
        {
            if (_builder.Length > 0 && _builder[^1] is not ' ' and not '(' and not '.')
            {
                _builder.Append(' ');
            }

            _builder.Append(text);
        }

        private void Open()
        {
            _builder.Append('(');
        }

        private void OpenSpaced()
        {
            if (_builder.Length > 0 && _builder[^1] != ' ')
            {
                _builder.Append(' ');
            }

            _builder.Append('(');
        }

        private void Close()
        {
            if (_builder.Length > 0 && _builder[^1] == ' ')
            {
                _builder.Length--;
            }

            _builder.Append(')');
        }

        private void Comma()
        {
            if (_builder.Length > 0 && _builder[^1] == ' ')
            {
                _builder.Length--;
            }

            _builder.Append(", ");
        }

        private void Dot()
        {
            if (_builder.Length > 0 && _builder[^1] == ' ')
            {
                _builder.Length--;
            }

            _builder.Append('.');
        }

        private void Identifier(SqlIdentifier identifier)
        {
            var text = RenderIdentifier(identifier);
            if (_builder.Length > 0 && _builder[^1] == '.')
            {
                _builder.Append(text);
            }
            else
            {
                Token(text);
            }
        }

        private void IdentifierList(IEnumerable<SqlIdentifier> identifiers)
        {
            var first = true;
            foreach (var identifier in identifiers)
            {
                if (!first)
                {
                    Comma();
                }

                Identifier(identifier);
                first = false;
            }
        }

        private static string RenderIdentifier(SqlIdentifier identifier)
        {
            if (identifier.Quote is not { } quote)
            {
                return identifier.Value;
            }

            return quote switch
            {
                '[' => "[" + identifier.Value.Replace("]", "]]") + "]",
                '`' => "`" + identifier.Value.Replace("`", "``") + "`",
                _ => "\"" + identifier.Value.Replace("\"", "\"\"") + "\"",
            };
        }

        // ---------- 语句 ----------

        public void WriteStatement(SqlStatement statement)
        {
            switch (statement)
            {
                case SqlSelectStatement select:
                    WriteWith(select.With);
                    WriteQuery(select.Query);
                    break;

                case SqlInsertStatement insert:
                    WriteInsert(insert);
                    break;

                case SqlUpdateStatement update:
                    WriteUpdate(update);
                    break;

                case SqlDeleteStatement delete:
                    WriteDelete(delete);
                    break;

                case SqlMergeStatement merge:
                    WriteMerge(merge);
                    break;

                case SqlCreateTableStatement create:
                    WriteCreateTable(create);
                    break;

                case SqlDropTableStatement drop:
                    WriteDropTable(drop);
                    break;

                case SqlTruncateTableStatement truncate:
                    WriteTruncateTable(truncate);
                    break;

                default:
                    throw new SqlDeparseException($"不支持回写的语句节点：{statement.GetType().Name}。");
            }
        }

        private void WriteWith(SqlWithClause? with)
        {
            if (with is null)
            {
                return;
            }

            Token("WITH");

            if (with.Recursive)
            {
                Token("RECURSIVE");
            }

            var first = true;
            foreach (var cte in with.Ctes)
            {
                if (!first)
                {
                    Comma();
                }

                Identifier(cte.Name);

                if (!cte.Columns.IsDefaultOrEmpty)
                {
                    Open();
                    IdentifierList(cte.Columns);
                    Close();
                }

                Token("AS");

                switch (cte.Materialization)
                {
                    case SqlCteMaterialization.Materialized:
                        Token("MATERIALIZED");
                        break;
                    case SqlCteMaterialization.NotMaterialized:
                        Token("NOT");
                        Token("MATERIALIZED");
                        break;
                }

                OpenSpaced();
                WriteQuery(cte.Query);
                Close();
                first = false;
            }
        }

        private void WriteInsert(SqlInsertStatement insert)
        {
            WriteWith(insert.With);
            Token("INSERT");
            Token("INTO");
            WriteNamedTable(insert.Target);

            if (!insert.Columns.IsDefaultOrEmpty)
            {
                OpenSpaced();
                IdentifierList(insert.Columns);
                Close();
            }

            WriteOutput(insert.Output);

            if (insert.DefaultValues)
            {
                Token("DEFAULT");
                Token("VALUES");
            }
            else if (insert.Source is not null)
            {
                WriteQuery(insert.Source);
            }

            WriteReturning(insert.Returning);
        }

        private void WriteUpdate(SqlUpdateStatement update)
        {
            WriteWith(update.With);
            Token("UPDATE");
            WriteNamedTable(update.Target);
            Token("SET");

            var first = true;
            foreach (var assignment in update.Assignments)
            {
                if (!first)
                {
                    Comma();
                }

                Identifier(assignment.Column);
                Token("=");
                WriteExpression(assignment.Value);
                first = false;
            }

            WriteOutput(update.Output);

            if (!update.From.IsDefaultOrEmpty)
            {
                Token("FROM");
                WriteTableSourceList(update.From);
            }

            if (update.Where is not null)
            {
                Token("WHERE");
                WriteExpression(update.Where);
            }

            WriteReturning(update.Returning);
        }

        private void WriteDelete(SqlDeleteStatement delete)
        {
            WriteWith(delete.With);
            Token("DELETE");
            Token("FROM");
            WriteNamedTable(delete.Target);
            WriteOutput(delete.Output);

            if (delete.Where is not null)
            {
                Token("WHERE");
                WriteExpression(delete.Where);
            }

            WriteReturning(delete.Returning);
        }

        private void WriteMerge(SqlMergeStatement merge)
        {
            WriteWith(merge.With);
            Token("MERGE");
            Token("INTO");
            WriteNamedTable(merge.Target);
            Token("USING");
            WriteTableSource(merge.Source);
            Token("ON");
            WriteExpression(merge.On);

            foreach (var clause in merge.Clauses)
            {
                Token("WHEN");

                switch (clause.MatchKind)
                {
                    case SqlMergeMatchKind.Matched:
                        Token("MATCHED");
                        break;
                    case SqlMergeMatchKind.NotMatchedBySource:
                        Token("NOT");
                        Token("MATCHED");
                        Token("BY");
                        Token("SOURCE");
                        break;
                    default:
                        Token("NOT");
                        Token("MATCHED");
                        break;
                }

                if (clause.Condition is not null)
                {
                    Token("AND");
                    WriteExpression(clause.Condition);
                }

                Token("THEN");
                WriteMergeAction(clause.Action);
            }

            WriteOutput(merge.Output);
        }

        private void WriteMergeAction(SqlMergeAction action)
        {
            switch (action)
            {
                case SqlMergeUpdateAction update:
                    Token("UPDATE");
                    Token("SET");

                    var first = true;
                    foreach (var assignment in update.Assignments)
                    {
                        if (!first)
                        {
                            Comma();
                        }

                        Identifier(assignment.Column);
                        Token("=");
                        WriteExpression(assignment.Value);
                        first = false;
                    }

                    break;

                case SqlMergeInsertAction insert:
                    Token("INSERT");

                    if (!insert.Columns.IsDefaultOrEmpty)
                    {
                        OpenSpaced();
                        IdentifierList(insert.Columns);
                        Close();
                    }

                    Token("VALUES");
                    OpenSpaced();

                    var valueFirst = true;
                    foreach (var value in insert.Values)
                    {
                        if (!valueFirst)
                        {
                            Comma();
                        }

                        WriteExpression(value);
                        valueFirst = false;
                    }

                    Close();
                    break;

                case SqlMergeDeleteAction:
                    Token("DELETE");
                    break;

                default:
                    throw new SqlDeparseException($"不支持回写的 MERGE 操作：{action.GetType().Name}。");
            }
        }

        private void WriteOutput(SqlOutputClause? output)
        {
            if (output is null)
            {
                return;
            }

            Token("OUTPUT");
            WriteSelectItems(output.Items);

            if (output.Into is not null)
            {
                Token("INTO");
                WriteNamedTable(output.Into);

                if (!output.IntoColumns.IsDefaultOrEmpty)
                {
                    OpenSpaced();
                    IdentifierList(output.IntoColumns);
                    Close();
                }
            }
        }

        private void WriteReturning(IEnumerable<SqlSelectItem> returning)
        {
            var items = returning.ToArray();
            if (items.Length == 0)
            {
                return;
            }

            Token("RETURNING");
            WriteSelectItems(items);
        }

        // ---------- DDL ----------

        private void WriteCreateTable(SqlCreateTableStatement create)
        {
            Token("CREATE");

            if (create.Temporary)
            {
                Token("TEMPORARY");
            }

            Token("TABLE");

            if (create.IfNotExists)
            {
                Token("IF");
                Token("NOT");
                Token("EXISTS");
            }

            WriteNamedTable(create.Table);
            OpenSpaced();

            var first = true;
            foreach (var column in create.Columns)
            {
                if (!first)
                {
                    Comma();
                }

                WriteColumnDefinition(column);
                first = false;
            }

            foreach (var constraint in create.Constraints)
            {
                if (!first)
                {
                    Comma();
                }

                WriteTableConstraint(constraint);
                first = false;
            }

            Close();
        }

        private void WriteColumnDefinition(SqlColumnDefinition column)
        {
            Identifier(column.Name);
            WriteDataType(column.DataType);

            switch (column.Nullability)
            {
                case SqlNullability.NotNull:
                    Token("NOT");
                    Token("NULL");
                    break;
                case SqlNullability.Null:
                    Token("NULL");
                    break;
            }

            if (column.Default is not null)
            {
                Token("DEFAULT");
                WriteExpression(column.Default);
            }

            if (column.PrimaryKey)
            {
                Token("PRIMARY");
                Token("KEY");
            }

            if (column.Unique)
            {
                Token("UNIQUE");
            }

            if (column.Check is not null)
            {
                Token("CHECK");
                OpenSpaced();
                WriteExpression(column.Check);
                Close();
            }

            if (column.References is not null)
            {
                Token("REFERENCES");
                WriteNamedTable(column.References);

                if (!column.ReferencedColumns.IsDefaultOrEmpty)
                {
                    OpenSpaced();
                    IdentifierList(column.ReferencedColumns);
                    Close();
                }
            }
        }

        private void WriteTableConstraint(SqlTableConstraint constraint)
        {
            if (constraint.Name is not null)
            {
                Token("CONSTRAINT");
                Identifier(constraint.Name);
            }

            switch (constraint.ConstraintKind)
            {
                case SqlTableConstraintKind.PrimaryKey:
                    Token("PRIMARY");
                    Token("KEY");
                    OpenSpaced();
                    IdentifierList(constraint.Columns);
                    Close();
                    break;

                case SqlTableConstraintKind.Unique:
                    Token("UNIQUE");
                    OpenSpaced();
                    IdentifierList(constraint.Columns);
                    Close();
                    break;

                case SqlTableConstraintKind.Check:
                    Token("CHECK");
                    OpenSpaced();
                    WriteExpression(constraint.Check!);
                    Close();
                    break;

                case SqlTableConstraintKind.ForeignKey:
                    Token("FOREIGN");
                    Token("KEY");
                    OpenSpaced();
                    IdentifierList(constraint.Columns);
                    Close();
                    Token("REFERENCES");
                    WriteNamedTable(constraint.References!);

                    if (!constraint.ReferencedColumns.IsDefaultOrEmpty)
                    {
                        OpenSpaced();
                        IdentifierList(constraint.ReferencedColumns);
                        Close();
                    }

                    break;
            }
        }

        private void WriteDropTable(SqlDropTableStatement drop)
        {
            Token("DROP");
            Token("TABLE");

            if (drop.IfExists)
            {
                Token("IF");
                Token("EXISTS");
            }

            var first = true;
            foreach (var table in drop.Tables)
            {
                if (!first)
                {
                    Comma();
                }

                WriteNamedTable(table);
                first = false;
            }

            if (drop.Cascade)
            {
                Token("CASCADE");
            }
        }

        private void WriteTruncateTable(SqlTruncateTableStatement truncate)
        {
            Token("TRUNCATE");
            Token("TABLE");

            var first = true;
            foreach (var table in truncate.Tables)
            {
                if (!first)
                {
                    Comma();
                }

                WriteNamedTable(table);
                first = false;
            }
        }

        // ---------- 查询 ----------

        internal void WriteQuery(SqlQueryExpression query)
        {
            switch (query)
            {
                case SqlSelectQuery select:
                    WriteSelectQuery(select);
                    break;

                case SqlValuesQuery values:
                    Token("VALUES");
                    var rowFirst = true;
                    foreach (var row in values.Rows)
                    {
                        if (!rowFirst)
                        {
                            Comma();
                        }

                        OpenSpaced();
                        var valueFirst = true;
                        foreach (var value in row.Values)
                        {
                            if (!valueFirst)
                            {
                                Comma();
                            }

                            WriteExpression(value);
                            valueFirst = false;
                        }

                        Close();
                        rowFirst = false;
                    }

                    WriteQueryTail(values);
                    break;

                case SqlParenthesizedQuery parenthesized:
                    Open();
                    WriteQuery(parenthesized.Inner);
                    Close();
                    WriteQueryTail(parenthesized);
                    break;

                case SqlSetOperationQuery set:
                    WriteSetOperand(set.Left);
                    Token(SetOperatorText(set.Operator));

                    switch (set.Quantifier)
                    {
                        case SqlSetQuantifier.All:
                            Token("ALL");
                            break;
                        case SqlSetQuantifier.Distinct:
                            Token("DISTINCT");
                            break;
                    }

                    WriteSetOperand(set.Right);
                    WriteQueryTail(set);
                    break;

                default:
                    throw new SqlDeparseException($"不支持回写的查询节点：{query.GetType().Name}。");
            }
        }

        private void WriteSetOperand(SqlQueryExpression operand)
        {
            if (operand is SqlSetOperationQuery)
            {
                Open();
                WriteQuery(operand);
                Close();
            }
            else
            {
                WriteQuery(operand);
            }
        }

        private void WriteSelectQuery(SqlSelectQuery select)
        {
            Token("SELECT");

            switch (select.Quantifier)
            {
                case SelectQuantifier.All:
                    Token("ALL");
                    break;
                case SelectQuantifier.Distinct:
                    Token("DISTINCT");
                    break;
                case SelectQuantifier.DistinctRow:
                    Token("DISTINCTROW");
                    break;
            }

            WriteSelectTop(select);

            if (select.Items.IsDefaultOrEmpty)
            {
                Token("*");
            }
            else
            {
                WriteSelectItems(select.Items);
            }

            if (!select.From.IsDefaultOrEmpty)
            {
                Token("FROM");
                WriteTableSourceList(select.From);
            }

            if (select.Where is not null)
            {
                Token("WHERE");
                WriteExpression(select.Where);
            }

            if (select.GroupBy is not null)
            {
                Token("GROUP");
                Token("BY");
                WriteExpressionList(select.GroupBy.Items);
            }

            if (select.Having is not null)
            {
                Token("HAVING");
                WriteExpression(select.Having);
            }

            if (select.Qualify is not null)
            {
                Token("QUALIFY");
                WriteExpression(select.Qualify);
            }

            if (select.Windows is not null)
            {
                Token("WINDOW");
                var first = true;
                foreach (var window in select.Windows.Windows)
                {
                    if (!first)
                    {
                        Comma();
                    }

                    Identifier(window.Name);
                    Token("AS");
                    OpenSpaced();
                    WriteWindowBody(window.Specification);
                    Close();
                    first = false;
                }
            }

            WriteQueryTail(select);
        }

        /// <summary>
        /// 输出 SELECT 位置的 TOP。目标方言不支持 TOP 时不输出，
        /// 由查询尾部改写为 LIMIT / FETCH。
        /// </summary>
        private void WriteSelectTop(SqlSelectQuery select)
        {
            if (_dialect is null)
            {
                WriteTop(select.Top);
                return;
            }

            if (!_dialect.Supports(SqlDialectCapabilities.TopClause))
            {
                return;
            }

            var top = select.Top;

            // 目标方言支持 TOP 时，把「无 OFFSET/FETCH 的 LIMIT」提升为 TOP。
            if (top is null && select.Limit is not null && select.Offset is null && select.Fetch is null)
            {
                top = new SqlTopClause(select.Limit.Count);
            }

            WriteTop(top);
        }

        private void WriteTop(SqlTopClause? top)
        {
            if (top is null)
            {
                return;
            }

            Token("TOP");

            if (top.Count is not null)
            {
                OpenSpaced();
                WriteExpression(top.Count);
                Close();
            }

            if (top.Percent)
            {
                Token("PERCENT");
            }

            if (top.WithTies)
            {
                Token("WITH");
                Token("TIES");
            }
        }

        private void WriteSelectItems(IEnumerable<SqlSelectItem> items)
        {
            var first = true;
            foreach (var item in items)
            {
                if (!first)
                {
                    Comma();
                }

                WriteExpression(item.Expression);

                if (item.Alias is not null)
                {
                    Token("AS");
                    Identifier(item.Alias);
                }

                first = false;
            }
        }

        private void WriteQueryTail(SqlQueryExpression query)
        {
            if (query.OrderBy is not null)
            {
                WriteOrderBy(query.OrderBy);
            }

            if (_dialect is null || !TryWriteDialectPagination(query))
            {
                WriteOriginalPagination(query);
            }
        }

        private void WriteOriginalPagination(SqlQueryExpression query)
        {
            if (query.Limit is not null)
            {
                Token("LIMIT");
                WriteExpression(query.Limit.Count);
            }

            if (query.Offset is not null)
            {
                Token("OFFSET");
                WriteExpression(query.Offset.Count);

                if (query.Fetch is not null)
                {
                    Token("ROWS");
                }
            }

            if (query.Fetch is not null)
            {
                Token("FETCH");

                if (query.Fetch.Count is not null)
                {
                    Token("NEXT");
                    WriteExpression(query.Fetch.Count);

                    if (query.Fetch.Percent)
                    {
                        Token("PERCENT");
                    }

                    Token("ROWS");
                }

                if (query.Fetch.WithTies)
                {
                    Token("WITH");
                    Token("TIES");
                }
                else
                {
                    Token("ONLY");
                }
            }
        }

        /// <summary>
        /// 按目标方言改写分页子句。无法无损表达时返回 <c>false</c>，
        /// 由调用方回退到原始写法。
        /// </summary>
        private bool TryWriteDialectPagination(SqlQueryExpression query)
        {
            var dialect = _dialect!;
            var top = (query as SqlSelectQuery)?.Top;
            var limit = query.Limit?.Count;
            var offset = query.Offset?.Count;
            var fetch = query.Fetch;

            if (dialect.Supports(SqlDialectCapabilities.TopClause))
            {
                // 无 OFFSET/FETCH 的 LIMIT 已由 WriteSelectTop 输出为 TOP。
                if (limit is not null && offset is null && fetch is null && query is SqlSelectQuery)
                {
                    return true;
                }

                if (top is not null && offset is null && fetch is null && limit is null)
                {
                    return true;
                }

                if (offset is null && limit is null && fetch is null)
                {
                    return true;
                }

                if (fetch?.Percent == true)
                {
                    return false;
                }

                Token("OFFSET");
                if (offset is not null)
                {
                    WriteExpression(offset);
                }
                else
                {
                    Token("0");
                }

                Token("ROWS");

                var count = limit ?? fetch?.Count;
                if (count is null)
                {
                    return true;
                }

                Token("FETCH");
                Token("NEXT");
                WriteExpression(count);
                Token("ROWS");

                if (fetch?.WithTies == true)
                {
                    Token("WITH");
                    Token("TIES");
                }
                else
                {
                    Token("ONLY");
                }

                return true;
            }

            // TOP 在目标方言中不存在，转为行数限制。
            if (top?.Percent == true || fetch?.Percent == true || fetch?.WithTies == true)
            {
                return false;
            }

            limit ??= top?.Count;
            limit ??= fetch?.Count;

            if (dialect.Supports(SqlDialectCapabilities.LimitClause))
            {
                if (limit is not null)
                {
                    Token("LIMIT");
                    WriteExpression(limit);

                    if (offset is not null)
                    {
                        Token("OFFSET");
                        WriteExpression(offset);
                    }

                    return true;
                }

                if (offset is null)
                {
                    return true;
                }

                if (!dialect.Supports(SqlDialectCapabilities.OffsetFetch))
                {
                    return false;
                }
            }

            if (!dialect.Supports(SqlDialectCapabilities.OffsetFetch))
            {
                return false;
            }

            if (offset is not null)
            {
                Token("OFFSET");
                WriteExpression(offset);
                Token("ROWS");
            }

            if (limit is not null)
            {
                Token("FETCH");
                Token("NEXT");
                WriteExpression(limit);
                Token("ROWS");
                Token("ONLY");
            }

            return true;
        }

        private void WriteOrderBy(SqlOrderByClause orderBy)
        {
            Token("ORDER");
            Token("BY");

            var first = true;
            foreach (var item in orderBy.Items)
            {
                if (!first)
                {
                    Comma();
                }

                WriteExpression(item.Expression);

                switch (item.Direction)
                {
                    case SqlSortOrder.Ascending:
                        Token("ASC");
                        break;
                    case SqlSortOrder.Descending:
                        Token("DESC");
                        break;
                }

                switch (item.Nulls)
                {
                    case SqlNullsOrder.First:
                        Token("NULLS");
                        Token("FIRST");
                        break;
                    case SqlNullsOrder.Last:
                        Token("NULLS");
                        Token("LAST");
                        break;
                }

                first = false;
            }
        }

        private void WriteExpressionList(IEnumerable<SqlExpression> expressions)
        {
            var first = true;
            foreach (var expression in expressions)
            {
                if (!first)
                {
                    Comma();
                }

                WriteExpression(expression);
                first = false;
            }
        }

        // ---------- 数据源 ----------

        private void WriteTableSourceList(IEnumerable<SqlTableSource> sources)
        {
            var first = true;
            foreach (var source in sources)
            {
                if (!first)
                {
                    Comma();
                }

                WriteTableSource(source);
                first = false;
            }
        }

        internal void WriteTableSource(SqlTableSource source)
        {
            switch (source)
            {
                case SqlNamedTable named:
                    WriteNamedTable(named);
                    break;

                case SqlDerivedTable derived:
                    if (derived.Lateral)
                    {
                        Token("LATERAL");
                    }

                    OpenSpaced();
                    WriteQuery(derived.Query);
                    Close();
                    WriteTableAlias(derived.Alias, derived.ColumnAliases);
                    break;

                case SqlJoinedTable joined:
                    WriteTableSource(joined.Left);
                    WriteJoin(joined);
                    break;

                case SqlPivotTable pivot:
                    WriteTableSource(pivot.Source);
                    Token("PIVOT");
                    OpenSpaced();
                    WriteExpression(pivot.Aggregate);
                    Token("FOR");
                    Identifier(pivot.ForColumn);
                    Token("IN");
                    OpenSpaced();

                    var pivotFirst = true;
                    foreach (var value in pivot.Values)
                    {
                        if (!pivotFirst)
                        {
                            Comma();
                        }

                        WriteExpression(value.Value);

                        if (value.Alias is not null)
                        {
                            Token("AS");
                            Identifier(value.Alias);
                        }

                        pivotFirst = false;
                    }

                    Close();
                    Close();
                    WriteTableAlias(pivot.Alias, default);
                    break;

                case SqlUnpivotTable unpivot:
                    WriteTableSource(unpivot.Source);
                    Token("UNPIVOT");
                    OpenSpaced();
                    Identifier(unpivot.ValueColumn);
                    Token("FOR");
                    Identifier(unpivot.ForColumn);
                    Token("IN");
                    OpenSpaced();
                    IdentifierList(unpivot.InColumns);
                    Close();
                    Close();
                    WriteTableAlias(unpivot.Alias, default);
                    break;

                case SqlUnnestTable unnest:
                    Token("UNNEST");
                    Open();
                    WriteExpressionList(unnest.Arguments);
                    Close();

                    if (unnest.WithOrdinality)
                    {
                        Token("WITH");
                        Token("ORDINALITY");
                    }

                    WriteTableAlias(unnest.Alias, unnest.ColumnAliases);
                    break;

                default:
                    throw new SqlDeparseException($"不支持回写的数据源：{source.GetType().Name}。");
            }
        }

        private void WriteNamedTable(SqlNamedTable table)
        {
            if (table.Catalog is not null)
            {
                Identifier(table.Catalog);
                Dot();
            }

            if (table.Schema is not null)
            {
                Identifier(table.Schema);
                Dot();
            }

            Identifier(table.Name);
            WriteTableAlias(table.Alias, table.ColumnAliases);
        }

        private void WriteTableAlias(SqlIdentifier? alias, ImmutableArray<SqlIdentifier> columnAliases)
        {
            if (alias is not null)
            {
                Token("AS");
                Identifier(alias);

                if (!columnAliases.IsDefaultOrEmpty)
                {
                    Open();
                    IdentifierList(columnAliases);
                    Close();
                }
            }
        }

        private void WriteApplySource(SqlTableSource source)
        {
            // APPLY 本身即隐含 LATERAL，无需重复输出 LATERAL 关键字。
            if (source is SqlDerivedTable { Lateral: true } derived)
            {
                source = derived with { Lateral = false };
            }

            WriteTableSource(source);
        }

        private void WriteJoin(SqlJoinedTable join)
        {
            switch (join.JoinType)
            {
                case SqlJoinType.CrossApply:
                    Token("CROSS");
                    Token("APPLY");
                    WriteApplySource(join.Right);
                    return;

                case SqlJoinType.OuterApply:
                    Token("OUTER");
                    Token("APPLY");
                    WriteApplySource(join.Right);
                    return;

                case SqlJoinType.Straight:
                    Token("STRAIGHT_JOIN");
                    WriteTableSource(join.Right);
                    break;

                default:
                    if (join.Natural)
                    {
                        Token("NATURAL");
                    }

                    switch (join.JoinType)
                    {
                        case SqlJoinType.Inner:
                            Token("INNER");
                            break;
                        case SqlJoinType.Left:
                            Token("LEFT");
                            break;
                        case SqlJoinType.Right:
                            Token("RIGHT");
                            break;
                        case SqlJoinType.Full:
                            Token("FULL");
                            break;
                        case SqlJoinType.Cross:
                            Token("CROSS");
                            break;
                    }

                    Token("JOIN");
                    WriteTableSource(join.Right);
                    break;
            }

            if (join.On is not null)
            {
                Token("ON");
                WriteExpression(join.On);
            }
            else if (!join.Using.IsDefaultOrEmpty)
            {
                Token("USING");
                Open();
                IdentifierList(join.Using);
                Close();
            }
        }

        // ---------- 表达式 ----------

        public void WriteExpression(SqlExpression expression)
        {
            switch (expression)
            {
                case SqlIdentifier identifier:
                    Identifier(identifier);
                    break;

                case SqlLiteral literal:
                    WriteLiteral(literal);
                    break;

                case SqlColumnRef column:
                    WriteColumnRef(column);
                    break;

                case SqlParameterRef parameter:
                    WriteParameter(parameter);
                    break;

                case SqlBinaryExpression binary:
                    WriteBinary(binary);
                    break;

                case SqlUnaryExpression unary:
                    WriteUnary(unary);
                    break;

                case SqlFunctionCall call:
                    WriteFunction(call);
                    break;

                case SqlCaseExpression caseExpression:
                    WriteCase(caseExpression);
                    break;

                case SqlCastExpression cast:
                    Token(cast.TryCast ? "TRY_CAST" : "CAST");
                    Open();
                    WriteExpression(cast.Expression);
                    Token("AS");
                    WriteDataType(cast.TargetType);
                    Close();
                    break;

                case SqlSubqueryExpression subquery:
                    OpenSpaced();
                    WriteQuery(subquery.Query);
                    Close();
                    break;

                case SqlQuantifiedSubqueryExpression quantified:
                    Token(quantified.Quantifier switch
                    {
                        SqlQuantifier.Any => "ANY",
                        SqlQuantifier.Some => "SOME",
                        _ => "ALL",
                    });
                    OpenSpaced();
                    WriteQuery(quantified.Query);
                    Close();
                    break;

                case SqlExistsExpression exists:
                    if (exists.Negated)
                    {
                        Token("NOT");
                    }

                    Token("EXISTS");
                    OpenSpaced();
                    WriteQuery(exists.Query);
                    Close();
                    break;

                case SqlInExpression inExpression:
                    WritePredicateOperand(inExpression.Expression);

                    if (inExpression.Negated)
                    {
                        Token("NOT");
                    }

                    Token("IN");
                    OpenSpaced();

                    if (inExpression.Values.Length == 1
                        && inExpression.Values[0] is SqlSubqueryExpression subqueryValue)
                    {
                        WriteQuery(subqueryValue.Query);
                    }
                    else
                    {
                        WriteExpressionList(inExpression.Values);
                    }

                    Close();
                    break;

                case SqlBetweenExpression between:
                    WritePredicateOperand(between.Expression);

                    if (between.Negated)
                    {
                        Token("NOT");
                    }

                    Token("BETWEEN");

                    if (between.Symmetric)
                    {
                        Token("SYMMETRIC");
                    }

                    WritePredicateOperand(between.Low);
                    Token("AND");
                    WritePredicateOperand(between.High);
                    break;

                case SqlLikeExpression like:
                    WritePredicateOperand(like.Expression);

                    if (like.Negated)
                    {
                        Token("NOT");
                    }

                    Token(like.CaseInsensitive ? "ILIKE" : "LIKE");
                    WritePredicateOperand(like.Pattern);

                    if (like.Escape is not null)
                    {
                        Token("ESCAPE");
                        WriteExpression(like.Escape);
                    }

                    break;

                case SqlIsNullExpression isNull:
                    WritePredicateOperand(isNull.Expression);
                    Token("IS");

                    if (isNull.Negated)
                    {
                        Token("NOT");
                    }

                    Token("NULL");
                    break;

                case SqlTupleExpression tuple:
                    Open();
                    WriteExpressionList(tuple.Items);
                    Close();
                    break;

                case SqlCollateExpression collate:
                    WriteOperand(collate.Expression, 45, false, SqlBinaryOperator.Add);
                    Token("COLLATE");
                    Identifier(collate.Collation);
                    break;

                case SqlAtTimeZoneExpression atTimeZone:
                    WriteOperand(atTimeZone.Expression, 45, false, SqlBinaryOperator.Add);
                    Token("AT");
                    Token("TIME");
                    Token("ZONE");
                    WriteExpression(atTimeZone.Zone);
                    break;

                case SqlRawExpression:
                    throw new SqlDeparseException("SqlRawExpression 无法回写为 SQL 文本。");

                default:
                    throw new SqlDeparseException($"不支持回写的表达式节点：{expression.GetType().Name}。");
            }
        }

        private void WriteLiteral(SqlLiteral literal)
        {
            switch (literal.LiteralKind)
            {
                case SqlLiteralKind.String:
                    Token(RenderString(literal));
                    break;

                case SqlLiteralKind.Null:
                    Token("NULL");
                    break;

                case SqlLiteralKind.Boolean:
                    Token(literal.Value.ToUpperInvariant());
                    break;

                default:
                    Token(literal.Value);
                    break;
            }
        }

        private static string RenderString(SqlLiteral literal) => literal.Prefix switch
        {
            "N" or "E" => literal.Prefix + "'" + literal.Value.Replace("'", "''") + "'",
            "$" => "$$" + literal.Value + "$$",
            "q" => "q'[" + literal.Value.Replace("]", "]]") + "]'",
            _ => "'" + literal.Value.Replace("'", "''") + "'",
        };

        private void WriteColumnRef(SqlColumnRef column)
        {
            if (column.Parts.IsDefaultOrEmpty)
            {
                Token("*");
                return;
            }

            for (var i = 0; i < column.Parts.Length; i++)
            {
                if (i > 0)
                {
                    Dot();
                }

                Identifier(column.Parts[i]);
            }

            if (column.IsStar)
            {
                Dot();
                Token("*");
            }
        }

        private void WriteParameter(SqlParameterRef parameter)
        {
            var text = parameter.Prefix switch
            {
                "?" => "?",
                "$" => "$" + parameter.Index?.ToString(),
                _ => parameter.Prefix + parameter.Name,
            };

            // 让 $1 与前面的标识符之间保持一个空格。
            Token(text);
        }

        private void WriteFunction(SqlFunctionCall call)
        {
            Identifier(call.Name);
            Open();

            if (call.Star)
            {
                Token("*");
            }
            else
            {
                if (call.Distinct)
                {
                    Token("DISTINCT");
                }

                WriteExpressionList(call.Arguments);
            }

            Close();

            if (call.WithinGroup is not null)
            {
                Token("WITHIN");
                Token("GROUP");
                OpenSpaced();
                WriteOrderBy(call.WithinGroup);
                Close();
            }

            if (call.Filter is not null)
            {
                Token("FILTER");
                OpenSpaced();
                Token("WHERE");
                WriteExpression(call.Filter.Condition);
                Close();
            }

            if (call.Over is not null)
            {
                Token("OVER");
                WriteWindow(call.Over);
            }
        }

        private void WriteWindow(SqlWindowSpecification specification)
        {
            if (specification.Name is not null
                && specification.PartitionBy.IsDefaultOrEmpty
                && specification.OrderBy is null
                && specification.Frame is null)
            {
                Identifier(specification.Name);
                return;
            }

            OpenSpaced();

            if (specification.Name is not null)
            {
                Identifier(specification.Name);
            }

            if (!specification.PartitionBy.IsDefaultOrEmpty)
            {
                Token("PARTITION");
                Token("BY");
                WriteExpressionList(specification.PartitionBy);
            }

            if (specification.OrderBy is not null)
            {
                WriteOrderBy(specification.OrderBy);
            }

            if (specification.Frame is not null)
            {
                WriteFrame(specification.Frame);
            }

            Close();
        }

        private void WriteWindowBody(SqlWindowSpecification specification)
        {
            if (specification.Name is not null)
            {
                Identifier(specification.Name);
            }

            if (!specification.PartitionBy.IsDefaultOrEmpty)
            {
                Token("PARTITION");
                Token("BY");
                WriteExpressionList(specification.PartitionBy);
            }

            if (specification.OrderBy is not null)
            {
                WriteOrderBy(specification.OrderBy);
            }

            if (specification.Frame is not null)
            {
                WriteFrame(specification.Frame);
            }
        }

        private void WriteFrame(SqlWindowFrame frame)
        {
            Token(frame.Unit switch
            {
                SqlWindowFrameUnit.Rows => "ROWS",
                SqlWindowFrameUnit.Range => "RANGE",
                _ => "GROUPS",
            });

            if (frame.End is not null)
            {
                Token("BETWEEN");
                WriteFrameBound(frame.Start);
                Token("AND");
                WriteFrameBound(frame.End);
            }
            else
            {
                WriteFrameBound(frame.Start);
            }
        }

        private void WriteFrameBound(SqlWindowFrameBound bound)
        {
            switch (bound.BoundKind)
            {
                case SqlWindowFrameBoundKind.UnboundedPreceding:
                    Token("UNBOUNDED");
                    Token("PRECEDING");
                    break;
                case SqlWindowFrameBoundKind.Preceding:
                    WriteExpression(bound.Value!);
                    Token("PRECEDING");
                    break;
                case SqlWindowFrameBoundKind.CurrentRow:
                    Token("CURRENT");
                    Token("ROW");
                    break;
                case SqlWindowFrameBoundKind.Following:
                    WriteExpression(bound.Value!);
                    Token("FOLLOWING");
                    break;
                case SqlWindowFrameBoundKind.UnboundedFollowing:
                    Token("UNBOUNDED");
                    Token("FOLLOWING");
                    break;
            }
        }

        private void WriteDataType(SqlDataType dataType)
        {
            Identifier(dataType.Name);

            if (!dataType.Arguments.IsDefaultOrEmpty)
            {
                Open();
                WriteExpressionList(dataType.Arguments);
                Close();
            }
        }

        private void WriteCase(SqlCaseExpression caseExpression)
        {
            Token("CASE");

            if (caseExpression.Operand is not null)
            {
                WriteExpression(caseExpression.Operand);
            }

            foreach (var when in caseExpression.Whens)
            {
                Token("WHEN");
                WriteExpression(when.Condition);
                Token("THEN");
                WriteExpression(when.Result);
            }

            if (caseExpression.Else is not null)
            {
                Token("ELSE");
                WriteExpression(caseExpression.Else);
            }

            Token("END");
        }

        private void WriteBinary(SqlBinaryExpression binary)
        {
            var precedence = BinaryPrecedence(binary.Operator);
            WriteOperand(binary.Left, precedence, false, binary.Operator);
            Token(OperatorText(binary.Operator));
            WriteOperand(binary.Right, precedence, true, binary.Operator);
        }

        private void WriteOperand(
            SqlExpression operand,
            int precedence,
            bool right,
            SqlBinaryOperator binaryOperator)
        {
            var childPrecedence = PrecedenceOf(operand);
            var needsParentheses = childPrecedence < precedence
                || (right
                    && childPrecedence == precedence
                    && binaryOperator is SqlBinaryOperator.Subtract
                        or SqlBinaryOperator.Divide
                        or SqlBinaryOperator.Modulo);

            if (needsParentheses)
            {
                Open();
                WriteExpression(operand);
                Close();
            }
            else
            {
                WriteExpression(operand);
            }
        }

        private void WritePredicateOperand(SqlExpression operand)
        {
            if (PrecedenceOf(operand) < 40)
            {
                Open();
                WriteExpression(operand);
                Close();
            }
            else
            {
                WriteExpression(operand);
            }
        }

        private void WriteUnary(SqlUnaryExpression unary)
        {
            Token(unary.Operator switch
            {
                SqlUnaryOperator.Not => "NOT",
                SqlUnaryOperator.Plus => "+",
                SqlUnaryOperator.Minus => "-",
                _ => "~",
            });

            var precedence = unary.Operator == SqlUnaryOperator.Not ? 30 : 80;
            if (PrecedenceOf(unary.Operand) < precedence)
            {
                Open();
                WriteExpression(unary.Operand);
                Close();
            }
            else
            {
                WriteExpression(unary.Operand);
            }
        }

        private static int PrecedenceOf(SqlExpression expression) => expression switch
        {
            SqlBinaryExpression binary => BinaryPrecedence(binary.Operator),
            SqlUnaryExpression unary => unary.Operator == SqlUnaryOperator.Not ? 30 : 80,
            SqlIsNullExpression => 40,
            SqlInExpression => 40,
            SqlBetweenExpression => 40,
            SqlLikeExpression => 40,
            SqlExistsExpression => 40,
            SqlCollateExpression => 45,
            SqlAtTimeZoneExpression => 45,
            SqlCastExpression => 90,
            SqlQuantifiedSubqueryExpression => 90,
            _ => 100,
        };

        private static int BinaryPrecedence(SqlBinaryOperator binaryOperator) => binaryOperator switch
        {
            SqlBinaryOperator.Or => 10,
            SqlBinaryOperator.And => 20,
            SqlBinaryOperator.Equal
                or SqlBinaryOperator.NotEqual
                or SqlBinaryOperator.LessThan
                or SqlBinaryOperator.LessThanOrEqual
                or SqlBinaryOperator.GreaterThan
                or SqlBinaryOperator.GreaterThanOrEqual => 40,
            SqlBinaryOperator.Concat => 50,
            SqlBinaryOperator.Add or SqlBinaryOperator.Subtract => 60,
            _ => 70,
        };

        private static string OperatorText(SqlBinaryOperator binaryOperator) => binaryOperator switch
        {
            SqlBinaryOperator.Or => "OR",
            SqlBinaryOperator.And => "AND",
            SqlBinaryOperator.Equal => "=",
            SqlBinaryOperator.NotEqual => "<>",
            SqlBinaryOperator.LessThan => "<",
            SqlBinaryOperator.LessThanOrEqual => "<=",
            SqlBinaryOperator.GreaterThan => ">",
            SqlBinaryOperator.GreaterThanOrEqual => ">=",
            SqlBinaryOperator.Concat => "||",
            SqlBinaryOperator.Add => "+",
            SqlBinaryOperator.Subtract => "-",
            SqlBinaryOperator.Multiply => "*",
            SqlBinaryOperator.Divide => "/",
            _ => "%",
        };

        private static string SetOperatorText(SqlSetOperator setOperator) => setOperator switch
        {
            SqlSetOperator.Union => "UNION",
            SqlSetOperator.Intersect => "INTERSECT",
            _ => "EXCEPT",
        };
    }
}
