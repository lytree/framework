using System.Collections.Immutable;

namespace Framework.SqlParser.Analysis;

/// <summary>三值化的语句特征。</summary>
public enum SqlFeatureState : byte
{
    /// <summary>确定不满足。</summary>
    No = 0,

    /// <summary>无法确定。</summary>
    Maybe,

    /// <summary>确定满足。</summary>
    Proven,
}

/// <summary>表引用。</summary>
public sealed record SqlTableReference(
    string? Catalog,
    string? Schema,
    string Name,
    string? Alias,
    SqlSpan Span);

/// <summary>列引用。</summary>
public sealed record SqlColumnReference(ImmutableArray<string> Parts, bool IsStar, SqlSpan Span);

/// <summary>参数引用。</summary>
public sealed record SqlParameterReference(string Prefix, string? Name, int? Index, SqlSpan Span);

/// <summary>语句特征。</summary>
public sealed record SqlStatementFeatures(
    SqlFeatureState ReadsData,
    SqlFeatureState ModifiesData,
    SqlFeatureState ReturnsResultSet);

/// <summary>语句分析结果。</summary>
public sealed record SqlAnalysis(
    ImmutableArray<SqlTableReference> Tables,
    ImmutableArray<SqlColumnReference> Columns,
    ImmutableArray<SqlParameterReference> Parameters,
    SqlStatementFeatures Features);
