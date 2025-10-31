using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SqlKata;

namespace Framework.SqlKata;


public static class FluentQuery
{


    // 静态字典，缓存 Type 到生成的 Columns 字典的映射
    private static readonly ConcurrentDictionary<Type, IDictionary<string, string>> ColumnsCache =
        new ConcurrentDictionary<Type, IDictionary<string, string>>();

    #region Query/From

    public static Query Query()
    {
        return new FluentQueryWrapper();
    }

    public static Query Query<A>()
    {
        return new FluentQueryWrapper($"{Table<A>()}");
    }

    public static Query Query<A>(Expression<Func<A>> alias)
    {
        return Query(Table<A>(), alias);
    }

    public static Query Query<A>(string table, Expression<Func<A>> alias)
    {
        return new FluentQueryWrapper($"{table} AS {Alias(alias)}");
    }

    public static Query From<A>(this Query query)
    {
        return query.From($"{Table<A>()}");
    }

    public static Join From<A>(this Join query)
    {
        return query.From($"{Table<A>()}");
    }

    public static Q From<Q, A>(this Q query, Expression<Func<A>> alias) where Q : BaseQuery<Q>
    {
        return query.From(Table<A>(), alias);
    }

    public static Q From<Q, A>(this Q query, string table, Expression<Func<A>> alias) where Q : BaseQuery<Q>
    {
        return query.From($"{table} AS {Alias(alias)}");
    }

    public static Q FromRawFormat<Q>(this Q query, string queryFormat, params Expression<Func<object>>[] columns) where Q : BaseQuery<Q>
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.FromRaw(queryRaw);
    }

    public static Q FromRawFormat<Q>(this Q query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings) where Q : BaseQuery<Q>
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.FromRaw(queryRaw, bindings: bindings);
    }

    public static Query As<A>(this Query query, Expression<Func<A>> alias)
    {
        var aliasName = Alias(alias);
        return query.As(aliasName);
    }

    public static Query WithRawFormat<A>(this Query query, Expression<Func<A>> alias, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.WithRaw(Alias(alias), queryRaw);
    }

    public static Query WithRawFormat<A>(this Query query, Expression<Func<A>> alias, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.WithRaw(Alias(alias), queryRaw, bindings: bindings);
    }

    public static Query CombineRawFormat(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.CombineRaw(queryRaw);
    }

    public static Query CombineRawFormat(this Query query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.CombineRaw(queryRaw, bindings: bindings);
    }

    public static Query ExceptRawFormat(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.ExceptRaw(queryRaw);
    }

    public static Query ExceptRawFormat(this Query query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.ExceptRaw(queryRaw, bindings: bindings);
    }

    public static Query IntersectRawFormat(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.IntersectRaw(queryRaw);
    }

    public static Query IntersectRawFormat(this Query query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.IntersectRaw(queryRaw, bindings: bindings);
    }

    public static Query UnionRawFormat(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.UnionRaw(queryRaw);
    }

    public static Query UnionRawFormat(this Query query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return query.UnionRaw(queryRaw, bindings: bindings);
    }

    #endregion Query/From

    #region Selects

    public static Query Select<A>(this Query query, Expression<Func<A>> alias, Query subquery)
    {
        var aliasName = Alias(alias);
        query.Select(subquery, aliasName);
        return query;
    }

    /// <summary>
    /// Example: SelectRawFormat(() => dto.FullName, queryFormat: "{0} + ' ' + {1}", () => cnt.FirstName, () => cnt.LastName)
    /// Results: SELECT FirstName + ' ' + LastName AS FullName
    /// </summary>
    public static Query SelectRawFormat<A>(this Query query, Expression<Func<A>> alias, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var aliasName = Alias(alias);
        return query.SelectRawFormat(aliasName, queryFormat, columns: columns);
    }

    /// <summary>
    /// Example: SelectRawFormat(() => dto.Name, queryFormat: "ISNULL({0}, ?)", new[] { FluentQuery.Expression(() => cnt.FirstName) }, new[] { "John" })
    /// Results: SELECT ISNULL(FirstName, 'John') AS Name
    /// </summary>
    public static Query SelectRawFormat<A>(this Query query, Expression<Func<A>> alias, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var aliasName = Alias(alias);
        return query.SelectRawFormat(aliasName, queryFormat, columns: columns, bindings: bindings);
    }

    /// <summary>
    /// Example: SelectRawFormat("FullName", queryFormat: "{0} + ' ' + {1}", () => cnt.FirstName, () => cnt.LastName)
    /// Results: SELECT FirstName + ' ' + LastName AS FullName
    /// </summary>
    public static Query SelectRawFormat(this Query query, string alias, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        query.GetWrapper().SelectsRaw.Add(alias, queryRaw);
        query.SelectRaw($"{queryRaw} AS {alias}");
        return query;
    }

    /// <summary>
    /// Example: SelectRawFormat("Name", queryFormat: "ISNULL({0}, ?)", new[] { FluentQuery.Expression(() => cnt.FirstName) }, new[] { "John" })
    /// Results: SELECT ISNULL(FirstName, 'John') AS Name
    /// </summary>
    public static Query SelectRawFormat(this Query query, string alias, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        query.GetWrapper().SelectsRaw.Add(alias, queryRaw);
        query.SelectRaw($"{queryRaw} AS {alias}", bindings: bindings);
        return query;
    }

    public static Query Select<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return query.Select(aliasName, column);
    }

    public static Query Select<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GetWrapper().Selects.Add(alias, columnName);
        query.Select($"{columnName} AS {alias}");
        return query;
    }

    public static Query Select<T>(this Query query, Expression<Func<T>> column)
    {
        var columnName = Property(column);
        var fullName = $"{AliasFromColumn(column)}.{columnName}";
        query.GetWrapper().Selects.Add(columnName, fullName);
        return query.SelectRaw(fullName);
    }

    public static Query SelectAll<T>(this Query query)
    {
        query.From<T>();

        foreach (var col in GetColumns<T>())
        {
            var columnName = $"{col.Value}";
            query.GetWrapper().Selects.Add(col.Key, columnName);
            query.Select($"{columnName} AS {col.Key}");
        }

        return query;
    }

    public static Query SelectAll<T>(this Query query, Expression<Func<T>> alias)
    {
        query.From(alias);

        foreach (var col in GetColumns<T>())
        {
            var columnName = $"{Alias(alias)}.{col.Value}";
            query.GetWrapper().Selects.Add(col.Key, columnName);
            query.Select($"{columnName} AS {col.Key}");
        }

        return query;
    }

    public static Query SelectFunc<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column, string func, bool aggregate = false)
    {
        var aliasName = Alias<A>(alias);
        query.SelectFunc(aliasName, column, func, aggregate);
        return query;
    }

    public static Query SelectFunc<T>(this Query query, string alias, Expression<Func<T>> column, string func, bool aggregate = false)
    {
        var columnName = $"{func}({AliasFromColumn(column)}.{Property(column)})";
        if (aggregate)
            query.GetWrapper().SelectAggrs.Add(alias, columnName);
        else
            query.GetWrapper().Selects.Add(alias, columnName);
        query.SelectRaw($"{columnName} AS {alias}");
        return query;
    }

    #endregion Selects

    #region Where

    /// <summary>
    /// Example: WhereRawFormat("(LEN({0}) + LEN({1})) > 0", () => cnt.FirstName, () => cnt.LastName)
    /// Results: WHERE (LEN(FirstName) + LEN(LastName)) > 0
    /// </summary>
    public static Q WhereRawFormat<Q>(this Q query, string queryFormat, params Expression<Func<object>>[] columns) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.WhereRaw(queryFormat);
        return query;
    }

    /// <summary>
    /// Example: WhereRawFormat("(LEN({0}) + LEN({1})) > 0", () => cnt.FirstName, () => cnt.LastName)
    /// Results: WHERE (LEN(FirstName) + LEN(LastName)) > 0
    /// </summary>
    public static Q OrWhereRawFormat<Q>(this Q query, string queryFormat, params Expression<Func<object>>[] columns) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.OrWhereRaw(queryFormat);
        return query;
    }

    /// <summary>
    /// Example: WhereRawFormat("LEN({0}) > ?", new[] { FluentQuery.Expression(() => cnt.FirstName) }, new[] { 5 })
    /// Results: WHERE LEN(FirstName) > 5
    /// </summary>
    public static Q WhereRawFormat<Q>(this Q query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.WhereRaw(queryFormat, bindings: bindings);
        return query;
    }

    /// <summary>
    /// Example: WhereRawFormat("LEN({0}) > ?", new[] { FluentQuery.Expression(() => cnt.FirstName) }, new[] { 5 })
    /// Results: WHERE LEN(FirstName) > 5
    /// </summary>
    public static Q OrWhereRawFormat<Q>(this Q query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.OrWhereRaw(queryFormat, bindings: bindings);
        return query;
    }

    public static Q Where<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.Where($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhere<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhere($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q WhereNot<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhereNot<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q WhereColumns<Q, T1, T2>(this Q query, Expression<Func<T1>> column1, Expression<Func<T2>> column2, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereColumns(
            $"{AliasFromColumn(column1)}.{Property(column1)}",
            op,
            $"{AliasFromColumn(column2)}.{Property(column2)}");
        return query;
    }

    public static Q WhereColumns<Q, T1>(this Q query, Expression<Func<T1>> column1, string second, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereColumns($"{AliasFromColumn(column1)}.{Property(column1)}", op, second);
        return query;
    }

    public static Q OrWhereColumns<Q, T1, T2>(this Q query, Expression<Func<T1>> column1, Expression<Func<T2>> column2, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereColumns(
            $"{AliasFromColumn(column1)}.{Property(column1)}",
            op,
            $"{AliasFromColumn(column2)}.{Property(column2)}");
        return query;
    }

    public static Q OrWhereColumns<Q, T1>(this Q query, Expression<Func<T1>> column1, string second, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereColumns($"{AliasFromColumn(column1)}.{Property(column1)}", op, second);
        return query;
    }

    public static Q WhereNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.WhereNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q OrWhereNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.OrWhereNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q WhereNotNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.WhereNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q OrWhereNotNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.OrWhereNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q WhereTrue<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.WhereTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q OrWhereTrue<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.OrWhereTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q WhereFalse<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.WhereFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q OrWhereFalse<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        query.OrWhereFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Q WhereLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereNotLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereNotLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereNotStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereNotStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereNotEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereNotEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereNotContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.WhereNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q OrWhereNotContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        query.OrWhereNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return query;
    }

    public static Q WhereBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        query.WhereBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Q OrWhereBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        query.OrWhereBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Q WhereNotBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        query.WhereNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Q OrWhereNotBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        query.OrWhereNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Q WhereIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        query.WhereIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Q OrWhereIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        query.OrWhereIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Q WhereNotIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        query.WhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Q OrWhereNotIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        query.OrWhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Q WhereIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        query.WhereIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return query;
    }

    public static Q OrWhereIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        query.OrWhereIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return query;
    }

    public static Q WhereNotIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        query.WhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return query;
    }

    public static Q OrWhereNotIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        query.OrWhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return query;
    }

    public static Q WhereDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhereDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q WhereNotDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhereNotDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q WhereDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhereDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q WhereNotDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhereNotDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q WhereTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhereTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q WhereNotTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.WhereNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Q OrWhereNotTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        query.OrWhereNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    #endregion Where

    #region Joins

    public static Query Join<A, J1, J2>(this Query query, Expression<Func<A>> alias, Expression<Func<J1>> column1, Expression<Func<J2>> column2, string op = "=")
    {
        return query.Join(Table<A>(), alias, column1, column2, op);
    }

    public static Query Join<A, J1, J2>(this Query query, string table, Expression<Func<A>> alias, Expression<Func<J1>> column1, Expression<Func<J2>> column2, string op = "=")
    {
        query.Join(
            $"{table} AS {Alias(alias)}",
            $"{AliasFromColumn(column1)}.{Property(column1)}",
            $"{AliasFromColumn(column2)}.{Property(column2)}",
            op: op
        );
        return query;
    }

    public static Query Join<A>(this Query query, Expression<Func<A>> alias, Func<Join, Join> joinQuery, string type = "inner join")
    {
        return query.Join(Table<A>(), alias, joinQuery, type);
    }

    public static Query Join<A>(this Query query, string table, Expression<Func<A>> alias, Func<Join, Join> joinQuery, string type = "inner join")
    {
        query.Join(
            $"{table} AS {Alias(alias)}",
            joinQuery,
            type: type
        );
        return query;
    }

    public static Query LeftJoin<A, J1, J2>(this Query query, Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        return query.LeftJoin(Table<A>(), alias, firstColumn, secondColumn, op);
    }

    public static Query LeftJoin<A, J1, J2>(this Query query, string table, Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        query.LeftJoin(
            $"{table} AS {Alias(alias)}",
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op: op
        );
        return query;
    }

    public static Query LeftJoin<A>(this Query query, Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        return query.LeftJoin(Table<A>(), alias, joinQuery);
    }

    public static Query LeftJoin<A>(this Query query, string table, Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        query.LeftJoin(
            $"{table} AS {Alias(alias)}",
            joinQuery
        );
        return query;
    }

    public static Query RightJoin<A, J1, J2>(this Query query, Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        return query.RightJoin(Table<A>(), alias, firstColumn, secondColumn, op);
    }

    public static Query RightJoin<A, J1, J2>(this Query query, string table, Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        query.RightJoin(
            $"{table} AS {Alias(alias)}",
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op: op
        );
        return query;
    }

    public static Query RightJoin<A>(this Query query, Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        return query.RightJoin(Table<A>(), alias, joinQuery);
    }

    public static Query RightJoin<A>(this Query query, string table, Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        query.RightJoin(
            $"{table} AS {Alias(alias)}",
            joinQuery
        );
        return query;
    }

    public static Query CrossJoin<A>(this Query query, Expression<Func<A>> alias)
    {
        query.CrossJoin(Alias(alias));
        return query;
    }

    public static Query CrossJoin<A>(this Query query, string table)
    {
        query.CrossJoin(table);
        return query;
    }

    public static Join On<J1, J2>(this Join join, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        join.On(
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op);
        return join;
    }

    public static Join OrOn<J1, J2>(this Join join, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        join.OrOn(
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op);
        return join;
    }

    public static Join JoinWith<A>(this Join join, Expression<Func<A>> alias)
    {
        join.JoinWith(Table<A>(), alias);
        return join;
    }

    public static Join JoinWith<A>(this Join join, string table, Expression<Func<A>> alias)
    {
        join.JoinWith($"{table} AS {Alias(alias)}");
        return join;
    }

    #endregion Joins

    #region Orders

    public static Query OrderByColumn<T>(this Query query, Expression<Func<T>> column)
    {
        query.OrderBy($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query OrderByAlias<T>(this Query query, Expression<Func<T>> alias)
    {
        var aliasName = Alias(alias);
        query.OrderByAlias(aliasName);
        return query;
    }

    public static Query OrderByAlias(this Query query, string alias)
    {
        if (query.GetWrapper().Selects.TryGetValue(alias, out var select))
        {
            query.OrderBy($"{select}");
        }
        else if (query.GetWrapper().SelectsRaw.TryGetValue(alias, out var selectRaw))
        {
            query.OrderByRaw(selectRaw);
        }
        else if (query.GetWrapper().SelectAggrs.TryGetValue(alias, out var selectAggr))
        {
            query.OrderByRaw(selectAggr);
        }
        else
        {
            throw new ArgumentException($"The alias name '{alias}' not found or not supported.");
        }

        return query;
    }

    public static Query OrderByColumnDesc<T>(this Query query, Expression<Func<T>> column)
    {
        query.OrderByDesc($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query OrderByAliasDesc<T>(this Query query, Expression<Func<T>> alias)
    {
        var aliasName = Alias(alias);
        query.OrderByAliasDesc(aliasName);
        return query;
    }

    public static Query OrderByAliasDesc(this Query query, string alias)
    {
        if (query.GetWrapper().Selects.TryGetValue(alias, out var select))
        {
            query.OrderByDesc($"{select}");
        }
        else if (query.GetWrapper().SelectsRaw.TryGetValue(alias, out var selectRaw))
        {
            query.OrderByRaw(selectRaw + " desc");
        }
        else if (query.GetWrapper().SelectAggrs.TryGetValue(alias, out var selectAggr))
        {
            query.OrderByRaw(selectAggr + " desc");
        }
        else
        {
            throw new ArgumentException($"The alias '{alias}' not found or not supported.");
        }

        return query;
    }

    public static Query OrderByRawFormat(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns);
        query.OrderByRaw(queryFormat);
        return query;
    }

    public static Query OrderByRawFormat(this Query query, string queryFormat, Expression<Func<object>>[] columns, params object[] bindings)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns);
        query.OrderByRaw(queryFormat, bindings: bindings);
        return query;
    }

    #endregion Orders

    #region Aggregations

    public static Query SelectCount<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "COUNT", aggregate: true);
        return query;
    }

    public static Query SelectCount<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "COUNT", aggregate: true);
        return query;
    }

    public static Query SelectCount<T>(this Query query, Expression<Func<T>> column)
    {
        query.SelectCount(column, column);
        return query;
    }

    public static Query SelectMin<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "MIN", aggregate: true);
        return query;
    }

    public static Query SelectMin<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "MIN", aggregate: true);
        return query;
    }

    public static Query SelectMin<T>(this Query query, Expression<Func<T>> column)
    {
        query.SelectMin(column, column);
        return query;
    }

    public static Query SelectMax<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "MAX", aggregate: true);
        return query;
    }

    public static Query SelectMax<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "MAX", aggregate: true);
        return query;
    }

    public static Query SelectMax<T>(this Query query, Expression<Func<T>> column)
    {
        query.SelectMax(column, column);
        return query;
    }

    public static Query SelectAvg<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "AVG", aggregate: true);
        return query;
    }

    public static Query SelectAvg<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "AVG", aggregate: true);
        return query;
    }

    public static Query SelectAvg<T>(this Query query, Expression<Func<T>> column)
    {
        query.SelectAvg(column, column);
        return query;
    }

    public static Query SelectSum<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "SUM", aggregate: true);
        return query;
    }

    public static Query SelectSum<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        query.SelectFunc(alias, column, "SUM", aggregate: true);
        return query;
    }

    public static Query SelectSum<T>(this Query query, Expression<Func<T>> column)
    {
        query.SelectSum(column, column);
        return query;
    }

    public static Query AsCount<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return query.AsCount(aliasName, column);
    }

    public static Query AsCount<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GetWrapper().SelectAggrs.Add(alias, columnName);
        query.AsCount(new[] { $"{columnName} AS {alias}" });
        return query;
    }

    public static Query AsAvg<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return query.AsAvg(aliasName, alias);
    }

    public static Query AsAvg<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GetWrapper().SelectAggrs.Add(alias, columnName);
        query.AsAvg($"{columnName} AS {alias}");
        return query;
    }

    public static Query AsAverage<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return query.AsAverage(aliasName, column);
    }

    public static Query AsAverage<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GetWrapper().SelectAggrs.Add(alias, columnName);
        query.AsAverage($"{columnName} AS {alias}");
        return query;
    }

    public static Query AsSum<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return query.AsSum(aliasName, column);
    }

    public static Query AsSum<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GetWrapper().SelectAggrs.Add(alias, columnName);
        query.AsSum($"{columnName} AS {alias}");
        return query;
    }

    public static Query AsMax<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return query.AsMax(aliasName, column);
    }

    public static Query AsMax<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GetWrapper().SelectAggrs.Add(alias, columnName);
        query.AsMax($"{columnName} AS {alias}");
        return query;
    }

    public static Query AsMin<A, T>(this Query query, Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias<A>(alias);
        return query.AsMin(aliasName, column);
    }

    public static Query AsMin<T>(this Query query, string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GetWrapper().SelectAggrs.Add(alias, columnName);
        query.AsMin($"{columnName} AS {alias}");
        return query;
    }

    public static Query GroupBy<T>(this Query query, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        query.GroupBy(new[] { columnName });
        return query;
    }

    public static Query GroupByRaw<T>(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns);
        query.GroupByRaw(queryRaw);
        return query;
    }

    public static Query GroupByRaw<T>(this Query query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns);
        query.GroupByRaw(queryRaw, bindings: bindings);
        return query;
    }

    #endregion Aggregations

    #region Havings

    public static Query Having<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.Having($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query HavingNot<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.HavingNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query OrHaving<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.OrHaving($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query OrHavingNot<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.OrHavingNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query HavingColumns<T1, T2>(this Query query, Expression<Func<T1>> firstColumn, Expression<Func<T2>> secondColumn, string op = "=")
    {
        query.HavingColumns($"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}", op, $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}");
        return query;
    }

    public static Query OrHavingColumns<T1, T2>(this Query query, Expression<Func<T1>> firstColumn, Expression<Func<T2>> secondColumn, string op = "=")
    {
        query.OrHavingColumns($"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}", op, $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}");
        return query;
    }

    public static Query HavingContains<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingContains<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingDate<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.HavingDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query OrHavingDate<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.OrHavingDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query HavingDatePart<T>(this Query query, Expression<Func<T>> column, object value, string part)
    {
        query.HavingDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return query;
    }

    public static Query OrHavingDatePart<T>(this Query query, Expression<Func<T>> column, object value, string part)
    {
        query.OrHavingDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return query;
    }

    public static Query HavingEnds<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingEnds<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingFalse<T>(this Query query, Expression<Func<T>> column)
    {
        query.HavingFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query OrHavingFalse<T>(this Query query, Expression<Func<T>> column)
    {
        query.OrHavingFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query HavingTrue<T>(this Query query, Expression<Func<T>> column)
    {
        query.HavingTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query OrHavingTrue<T>(this Query query, Expression<Func<T>> column)
    {
        query.OrHavingTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query HavingIn<T>(this Query query, Expression<Func<T>> column, IEnumerable<T> values)
    {
        query.HavingIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Query HavingIn<T>(this Query query, Expression<Func<T>> column, Query subQuery)
    {
        query.HavingIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return query;
    }

    public static Query OrHavingIn<T>(this Query query, Expression<Func<T>> column, IEnumerable<T> values)
    {
        query.OrHavingIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Query OrHavingIn<T>(this Query query, Expression<Func<T>> column, Query subQuery)
    {
        query.OrHavingIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return query;
    }

    public static Query HavingLike<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingLike<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingBetween<T>(this Query query, Expression<Func<T>> column, T lower, T higher)
    {
        query.HavingBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Query OrHavingBetween<T>(this Query query, Expression<Func<T>> column, T lower, T higher)
    {
        query.OrHavingBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Query HavingNotBetween<T>(this Query query, Expression<Func<T>> column, T lower, T higher)
    {
        query.HavingNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Query OrHavingNotBetween<T>(this Query query, Expression<Func<T>> column, T lower, T higher)
    {
        query.OrHavingNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return query;
    }

    public static Query HavingNotContains<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingNotContains<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingNotDate<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.HavingNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query OrHavingNotDate<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.OrHavingNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query HavingNotDatePart<T>(this Query query, Expression<Func<T>> column, object value, string part)
    {
        query.HavingNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return query;
    }

    public static Query OrHavingNotDatePart<T>(this Query query, Expression<Func<T>> column, object value, string part)
    {
        query.OrHavingNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return query;
    }

    public static Query HavingNotEnds<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingNotEnds<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingNotIn<T>(this Query query, Expression<Func<T>> column, IEnumerable<T> values)
    {
        query.HavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Query HavingNotIn<T>(this Query query, Expression<Func<T>> column, Query subQuery)
    {
        query.HavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return query;
    }

    public static Query OrHavingNotIn<T>(this Query query, Expression<Func<T>> column, IEnumerable<T> values)
    {
        query.OrHavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return query;
    }

    public static Query OrHavingNotIn<T>(this Query query, Expression<Func<T>> column, Query subQuery)
    {
        query.OrHavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return query;
    }

    public static Query HavingNotLike<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingNotLike<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingNull<T>(this Query query, Expression<Func<T>> column)
    {
        query.HavingNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query HavingNotNull<T>(this Query query, Expression<Func<T>> column)
    {
        query.HavingNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query OrHavingNull<T>(this Query query, Expression<Func<T>> column)
    {
        query.OrHavingNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query OrHavingNotNull<T>(this Query query, Expression<Func<T>> column)
    {
        query.OrHavingNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return query;
    }

    public static Query HavingStarts<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingNotStarts<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.HavingNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingStarts<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query OrHavingNotStarts<T>(this Query query, Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        query.OrHavingNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return query;
    }

    public static Query HavingTime<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.HavingTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query OrHavingTime<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.OrHavingTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query HavingNotTime<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.HavingNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query OrHavingNotTime<T>(this Query query, Expression<Func<T>> column, object value, string op = "=")
    {
        query.OrHavingNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return query;
    }

    public static Query HavingRawFormat<T>(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.HavingRaw(queryFormat);
        return query;
    }

    public static Query HavingRawFormat<T>(this Query query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.HavingRaw(queryFormat, bindings: bindings);
        return query;
    }

    public static Query OrHavingRawFormat<T>(this Query query, string queryFormat, params Expression<Func<object>>[] columns)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.OrHavingRaw(queryFormat);
        return query;
    }

    public static Query OrHavingRawFormat<T>(this Query query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        query.OrHavingRaw(queryFormat, bindings: bindings);
        return query;
    }

    #endregion Havings

    #region Misc

    public static Q If<Q>(this Q query, bool condition, Func<Q, Q> ifTrue, Func<Q, Q> ifFalse = null) where Q : BaseQuery<Q>
    {
        if (ifTrue == null)
            throw new ArgumentNullException(nameof(ifTrue));

        if (condition)
            return ifTrue.Invoke(query);
        else
            return ifFalse != null ? ifFalse.Invoke(query) : query;
    }

    public static Query WithVariable(this Query query, string key, object value)
    {
        query.Variables.Add(key, value);

        return query;
    }

    #endregion Misc

    #region Public Methods

    /// <summary>
    /// Gets a column name from the entity (poco) property (eg. Column(() => cnt.FirstName) gives 'FirstName')
    /// </summary>
    public static string Column<T>(Expression<Func<T>> column)
    {
        return Property<T>(column);
    }

    /// <summary>
    /// Gets the first part of the full column name (eg. AliasFromColumn(() => cnt.FirstName) gives 'cnt')
    /// </summary>
    public static string AliasFromColumn<T>(Expression<Func<T>> property)
    {
        return Property(property, parent: true);
    }

    /// <summary>
    /// Gets full column name from the entity (poco) property (eg. ColumnWithAlias(() => cnt.FirstName) gives 'cnt.FirstName')
    /// </summary>
    public static string ColumnWithAlias<T>(Expression<Func<T>> column)
    {
        return $"{AliasFromColumn(column)}.{Column(column)}";
    }

    /// <summary>
    /// Gets alias name from populated dto model (eg. Alias(() => model.FullName) gives 'FullName')
    /// </summary>
    public static string Alias<A>(Expression<Func<A>> model)
    {
        return Property(model);
    }

    /// <summary>
    /// Gets a table name from the entity (poco) property (eg. Table(() => cnt) gives 'Contacts')
    /// </summary>
    public static string Table<A>(Expression<Func<A>> alias)
    {
        return Table<A>();
    }

    /// <summary>
    /// Gets a table name from the entity (poco) property (eg. Table<Contact>() gives 'Contacts')
    /// </summary>
    public static string Table<A>()
    {
        var attribute = typeof(A).GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.TableAttribute>();

        var tableName = attribute?.Name;
        var schemaName = attribute?.Schema;

        if (string.IsNullOrWhiteSpace(tableName))
            tableName = typeof(A).Name;

        if (!string.IsNullOrWhiteSpace(schemaName))
            tableName = schemaName + "." + tableName;

        return tableName;
    }

    /// <summary>
    /// Used to describe an array of column expressions in a short way (eg. .WhereRawFormat("{0} LIKE 'John'", columns: new[] { FluentQuery.Expression(() => cnt.FirstName) }))
    /// </summary>
    public static Expression<Func<object>> Expression(Expression<Func<object>> func)
    {
        return func;
    }

    #endregion Public Methods

    #region Private Methods

    private static FluentQueryWrapper GetWrapper(this Query query)
    {
        return query as FluentQueryWrapper ?? throw new Exception("Cannot execute operation because SqlKata query wasn't instantiated from the FluentQuery. Use 'FluentQuery.Query()' instead of 'new Query()'.");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="snake"></param>
    /// <param name="parent">Get parent member name (eg. customer.Id will return "customer" instead of "ID")</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static string Property<T>(Expression<Func<T>> property, bool parent = false)
    {
        var memberName = GetMemberName(property, parent: parent);

        if (memberName != null)
            return memberName.Replace(".", "_");
        else
            throw new ArgumentException($"The expression cannot be evaluated");
    }

    private static string GetMemberName(Expression expression, bool parent = false)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        switch (expression.NodeType)
        {
            case ExpressionType.MemberAccess:
                return parent
                    ? ((MemberExpression)((MemberExpression)expression).Expression).Member.Name
                    : ((MemberExpression)expression).Member.Name;

            case ExpressionType.Convert:
                return GetMemberName(((UnaryExpression)expression).Operand, parent: parent);

            case ExpressionType.Lambda:
                if (((LambdaExpression)expression).Body.NodeType == ExpressionType.Convert)
                    return GetMemberName((((LambdaExpression)expression).Body as UnaryExpression).Operand, parent: parent);

                var memberExpression = ((LambdaExpression)expression).Body as MemberExpression
                    ?? throw new NotSupportedException(expression.NodeType.ToString(), new Exception($"Cannot get member name from expression {expression}."));

                if (parent)
                    return (memberExpression.Expression as MemberExpression)?.Member.Name
                        ?? throw new NotSupportedException(expression.NodeType.ToString(), new Exception($"Cannot get parent member name from expression {expression}."));
                else
                    return memberExpression.Member.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()?.Name
                        ?? FormatNestedMemberName(memberExpression);

            default:
                throw new NotSupportedException(expression.NodeType.ToString(),
                    new Exception($"Cannot get member name from expression {expression}."));
        }
    }

    private static string FormatNestedMemberName(MemberExpression expression)
    {
        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        var name = expression.Member.Name;

        while (expression.Expression is MemberExpression memberExpression)
        {
            if (memberExpression.Member.MemberType == MemberTypes.Property)
            {
                name = memberExpression.Member.Name + "." + name;
                expression = memberExpression;
            }
            else if (memberExpression.Member.MemberType == MemberTypes.Field && memberExpression.Member.DeclaringType.Name.StartsWith("ValueTuple`") && memberExpression.Member.Name == "Rest")
            {
                throw new ArgumentException("Only maximum 7 items of Tuple are supported. See https://learn.microsoft.com/en-us/dotnet/api/system.tuple-8?view=net-9.0");
            }
            else
            {
                break;
            }
        }

        return name;
    }

    private static string FormatQueryRaw(string queryFormat, params Expression<Func<object>>[] columns)
    {
        if (columns != null && columns.Length > 0)
            queryFormat = string.Format(queryFormat, columns.Select(x => $"{AliasFromColumn(x)}.{Alias(x)}").ToArray());

        return queryFormat;
    }
    private static IDictionary<string, string> GetColumns<T>()
    {
        var type = typeof(T);

        // 1. 尝试从缓存中读取
        if (ColumnsCache.TryGetValue(type, out var cachedColumns))
        {
            return cachedColumns;
        }

        // 2. 缓存未命中：使用约定和最小反射查找生成的静态类
        var mapType = type.Assembly.GetType($"{type.FullName}Map");

        if (mapType == null)
        {
            // 💥 Throw exception: Force the use of the Source Generator
            throw new InvalidOperationException(
                $"Source Generator mapping failure: The type '{type.FullName}' is missing its expected generated static map class '{type.FullName}Map'. " +
                "Please ensure that the model has been scanned by the Source Generator and that the project configuration is correct."
            );
        }

        // 3. 查找静态字段 'Columns'
        var columnsField = mapType.GetField("Columns", BindingFlags.Public | BindingFlags.Static);

        if (columnsField == null)
        {
            throw new InvalidOperationException(
            $"Source Generator mapping failure: The generated map for '{type.Name}' was found but is missing the required static 'Columns' field."
        );
        }

        var columns = (IDictionary<string, string>)columnsField.GetValue(null);

        // 4. 缓存结果并返回
        ColumnsCache.TryAdd(type, columns);

        return columns;
    }

    #endregion Private Methods
}