namespace Framework.SqlParser;

/// <summary>
/// SQL 方言配置。一个实例描述一种数据库在词法/语法上的差异。
/// </summary>
public sealed class SqlDialect
{
    private SqlDialect(string name, SqlDialectCapabilities capabilities)
    {
        Name = name;
        Capabilities = capabilities;
    }

    /// <summary>通用方言（ANSI SQL 子集）。</summary>
    public static SqlDialect Generic { get; } = new(
        "Generic",
        SqlDialectCapabilities.NationalStringPrefix |
        SqlDialectCapabilities.LimitClause |
        SqlDialectCapabilities.OffsetFetch);

    /// <summary>Microsoft SQL Server。</summary>
    public static SqlDialect TSql { get; } = new(
        "TSql",
        SqlDialectCapabilities.BracketIdentifiers |
        SqlDialectCapabilities.NationalStringPrefix |
        SqlDialectCapabilities.HashPrefixedIdentifiers |
        SqlDialectCapabilities.TopClause |
        SqlDialectCapabilities.OffsetFetch |
        SqlDialectCapabilities.OutputClause);

    /// <summary>MySQL / MariaDB。</summary>
    public static SqlDialect MySql { get; } = new(
        "MySql",
        SqlDialectCapabilities.BacktickIdentifiers |
        SqlDialectCapabilities.BackslashEscapes |
        SqlDialectCapabilities.HashComments |
        SqlDialectCapabilities.NationalStringPrefix |
        SqlDialectCapabilities.LimitClause |
        SqlDialectCapabilities.LimitOffsetComma);

    /// <summary>PostgreSQL。</summary>
    public static SqlDialect PostgreSql { get; } = new(
        "PostgreSql",
        SqlDialectCapabilities.DollarQuotedStrings |
        SqlDialectCapabilities.EscapeStringPrefix |
        SqlDialectCapabilities.NestedBlockComments |
        SqlDialectCapabilities.DoubleColonCast |
        SqlDialectCapabilities.Returning |
        SqlDialectCapabilities.DistinctOn |
        SqlDialectCapabilities.Lateral |
        SqlDialectCapabilities.CaseInsensitiveLike |
        SqlDialectCapabilities.JsonArrowOperators |
        SqlDialectCapabilities.SquareBracketSubscript |
        SqlDialectCapabilities.AtTimeZone |
        SqlDialectCapabilities.OffsetFetch);

    /// <summary>Oracle。</summary>
    public static SqlDialect Oracle { get; } = new(
        "Oracle",
        SqlDialectCapabilities.NationalStringPrefix |
        SqlDialectCapabilities.OracleQuotedStrings |
        SqlDialectCapabilities.ConnectBy |
        SqlDialectCapabilities.Returning |
        SqlDialectCapabilities.OffsetFetch);

    /// <summary>SQLite。</summary>
    public static SqlDialect Sqlite { get; } = new(
        "Sqlite",
        SqlDialectCapabilities.BracketIdentifiers |
        SqlDialectCapabilities.BacktickIdentifiers |
        SqlDialectCapabilities.LimitClause |
        SqlDialectCapabilities.Returning |
        SqlDialectCapabilities.NationalStringPrefix);

    /// <summary>方言名称。</summary>
    public string Name { get; }

    /// <summary>能力位。</summary>
    public SqlDialectCapabilities Capabilities { get; }

    /// <summary>判断是否具备全部指定能力。</summary>
    public bool Supports(SqlDialectCapabilities capability) => (Capabilities & capability) == capability;

    /// <inheritdoc />
    public override string ToString() => Name;
}
