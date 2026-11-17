using System.Collections.Immutable;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Analysis;

/// <summary>
/// AST 分析器：提取表引用、列引用、参数引用，并推断语句特征。
/// </summary>
public static class SqlAnalyzer
{
    /// <summary>分析一条语句。</summary>
    /// <param name="statement">已解析的语句。</param>
    public static SqlAnalysis Analyze(SqlStatement statement)
    {
        ArgumentNullException.ThrowIfNull(statement);

        var cteNames = CollectCteNames(statement);
        var collector = new Collector(cteNames);
        collector.Visit(statement);

        return new SqlAnalysis(
            collector.Tables.ToImmutableArray(),
            collector.Columns.ToImmutableArray(),
            collector.Parameters.ToImmutableArray(),
            ComputeFeatures(statement, collector.Tables.Count > 0));
    }

    private static HashSet<string> CollectCteNames(SqlStatement statement)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Walk(statement, node =>
        {
            if (node is SqlCommonTableExpression cte)
            {
                names.Add(cte.Name.Value);
            }
        });

        return names;
    }

    private static void Walk(SqlNode node, Action<SqlNode> visit)
    {
        visit(node);

        var children = new List<SqlNode>(4);
        node.CollectChildren(children);

        foreach (var child in children)
        {
            Walk(child, visit);
        }
    }

    private static SqlStatementFeatures ComputeFeatures(SqlStatement statement, bool hasTables) => statement switch
    {
        SqlSelectStatement => new SqlStatementFeatures(
            hasTables ? SqlFeatureState.Proven : SqlFeatureState.No,
            SqlFeatureState.No,
            SqlFeatureState.Proven),

        SqlInsertStatement insert => new SqlStatementFeatures(
            insert.Source is null or SqlValuesQuery ? SqlFeatureState.No : SqlFeatureState.Proven,
            SqlFeatureState.Proven,
            insert.Returning.Length > 0 ? SqlFeatureState.Proven : SqlFeatureState.No),

        SqlUpdateStatement update => new SqlStatementFeatures(
            SqlFeatureState.Proven,
            SqlFeatureState.Proven,
            update.Returning.Length > 0 ? SqlFeatureState.Proven : SqlFeatureState.No),

        SqlDeleteStatement delete => new SqlStatementFeatures(
            SqlFeatureState.Proven,
            SqlFeatureState.Proven,
            delete.Returning.Length > 0 ? SqlFeatureState.Proven : SqlFeatureState.No),

        SqlMergeStatement merge => new SqlStatementFeatures(
            SqlFeatureState.Proven,
            SqlFeatureState.Proven,
            merge.Output is null ? SqlFeatureState.No : SqlFeatureState.Proven),

        SqlCreateTableStatement => new SqlStatementFeatures(
            SqlFeatureState.No,
            SqlFeatureState.No,
            SqlFeatureState.No),

        SqlDropTableStatement => new SqlStatementFeatures(
            SqlFeatureState.No,
            SqlFeatureState.No,
            SqlFeatureState.No),

        SqlTruncateTableStatement => new SqlStatementFeatures(
            SqlFeatureState.No,
            SqlFeatureState.Proven,
            SqlFeatureState.No),

        _ => new SqlStatementFeatures(
            SqlFeatureState.Maybe,
            SqlFeatureState.Maybe,
            SqlFeatureState.Maybe),
    };

    private sealed class Collector(HashSet<string> cteNames)
    {
        public List<SqlTableReference> Tables { get; } = [];

        public List<SqlColumnReference> Columns { get; } = [];

        public List<SqlParameterReference> Parameters { get; } = [];

        public void Visit(SqlNode node)
        {
            switch (node)
            {
                case SqlNamedTable table when !IsCteReference(table):
                    Tables.Add(new SqlTableReference(
                        table.Catalog?.Value,
                        table.Schema?.Value,
                        table.Name.Value,
                        table.Alias?.Value,
                        table.Span));
                    break;

                case SqlColumnRef column:
                    Columns.Add(new SqlColumnReference(
                        column.Parts.Select(part => part.Value).ToImmutableArray(),
                        column.IsStar,
                        column.Span));
                    break;

                case SqlParameterRef parameter:
                    Parameters.Add(new SqlParameterReference(
                        parameter.Prefix,
                        parameter.Name,
                        parameter.Index,
                        parameter.Span));
                    break;
            }

            var children = new List<SqlNode>(4);
            node.CollectChildren(children);

            foreach (var child in children)
            {
                Visit(child);
            }
        }

        private bool IsCteReference(SqlNamedTable table) =>
            table.Catalog is null && table.Schema is null && cteNames.Contains(table.Name.Value);
    }
}
