using SqlKata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Framework.SqlKata;

public class FluentQuery : Query
{

    internal readonly IDictionary<string, string> Selects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // <alias, column>
    internal readonly IDictionary<string, string> SelectsRaw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // <alias, raw_query>
    internal readonly IDictionary<string, string> SelectAggrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // <alias, aggregation>


    public FluentQuery() : base() { }

    public FluentQuery(string table, string comment = null) : base(table, comment: comment) { }

    #region Query/From

    public Join From<A>(Join query)
    {
        return From($"{Table<A>()}");
    }

    public Q From<Q, A>(Expression<Func<A>> alias) where Q : BaseQuery<Q>
    {
        return From(Table<A>(), alias);
    }

    public Q From<Q, A>(string table, Expression<Func<A>> alias) where Q : BaseQuery<Q>
    {
        return From($"{table} AS {Alias(alias)}");
    }

    public Q FromRawFormat<Q>(this Q query, string queryFormat, params Expression<Func<object>>[] columns) where Q : BaseQuery<Q>
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return FromRaw(queryRaw);
    }

    public Q FromRawFormat<Q>(this Q query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings) where Q : BaseQuery<Q>
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return FromRaw(queryRaw, bindings: bindings);
    }

    public Query As<A>(Expression<Func<A>> alias)
    {
        var aliasName = Alias(alias);
        return As(aliasName);
    }

    public Query WithRawFormat<A>(Expression<Func<A>> alias, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return WithRaw(Alias(alias), queryRaw);
    }

    public Query WithRawFormat<A>(Expression<Func<A>> alias, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return WithRaw(Alias(alias), queryRaw, bindings: bindings);
    }

    public Query CombineRawFormat(string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return CombineRaw(queryRaw);
    }

    public Query CombineRawFormat(string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return CombineRaw(queryRaw, bindings: bindings);
    }

    public Query ExceptRawFormat(string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return ExceptRaw(queryRaw);
    }

    public Query ExceptRawFormat(string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return ExceptRaw(queryRaw, bindings: bindings);
    }

    public Query IntersectRawFormat(string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return IntersectRaw(queryRaw);
    }

    public Query IntersectRawFormat(string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return IntersectRaw(queryRaw, bindings: bindings);
    }

    public Query UnionRawFormat(string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return UnionRaw(queryRaw);
    }

    public Query UnionRawFormat(string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        return UnionRaw(queryRaw, bindings: bindings);
    }

    #endregion Query/From

    #region Selects

    public Query Select<A>(Expression<Func<A>> alias, Query subquery)
    {
        var aliasName = Alias(alias);
        Select(subquery, aliasName);
        return this;
    }

    /// <summary>
    /// Example: SelectRawFormat(() => dto.FullName, queryFormat: "{0} + ' ' + {1}", () => cnt.FirstName, () => cnt.LastName)
    /// Results: SELECT FirstName + ' ' + LastName AS FullName
    /// </summary>
    public Query SelectRawFormat<A>(Expression<Func<A>> alias, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var aliasName = Alias(alias);
        return SelectRawFormat(aliasName, queryFormat, columns: columns);
    }

    /// <summary>
    /// Example: SelectRawFormat(() => dto.Name, queryFormat: "ISNULL({0}, ?)", new[] { FluentExpression(() => cnt.FirstName) }, new[] { "John" })
    /// Results: SELECT ISNULL(FirstName, 'John') AS Name
    /// </summary>
    public Query SelectRawFormat<A>(Expression<Func<A>> alias, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var aliasName = Alias(alias);
        return SelectRawFormat(aliasName, queryFormat, columns: columns, bindings: bindings);
    }

    /// <summary>
    /// Example: SelectRawFormat("FullName", queryFormat: "{0} + ' ' + {1}", () => cnt.FirstName, () => cnt.LastName)
    /// Results: SELECT FirstName + ' ' + LastName AS FullName
    /// </summary>
    public Query SelectRawFormat(string alias, string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        SelectsRaw.Add(alias, queryRaw);
        SelectRaw($"{queryRaw} AS {alias}");
        return this;
    }

    /// <summary>
    /// Example: SelectRawFormat("Name", queryFormat: "ISNULL({0}, ?)", new[] { FluentExpression(() => cnt.FirstName) }, new[] { "John" })
    /// Results: SELECT ISNULL(FirstName, 'John') AS Name
    /// </summary>
    public Query SelectRawFormat(string alias, string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns: columns);
        SelectsRaw.Add(alias, queryRaw);
        SelectRaw($"{queryRaw} AS {alias}", bindings: bindings);
        return this;
    }

    public Query Select<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return Select(aliasName, column);
    }

    public Query Select<T>(string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        Selects.Add(alias, columnName);
        Select($"{columnName} AS {alias}");
        return this;
    }

    public Query Select<T>(Expression<Func<T>> column)
    {
        var columnName = Property(column);
        var fullName = $"{AliasFromColumn(column)}.{columnName}";
        Selects.Add(columnName, fullName);
        return SelectRaw(fullName);
    }

    public Query SelectAll<T>()
    {
        foreach (var col in GetColumns<T>())
        {
            var columnName = $"{col.Value}";
            Selects.Add(col.Key, columnName);
            Select($"{columnName} AS {col.Key}");
        }

        return this;
    }

    public Query SelectAll<T>(Expression<Func<T>> alias)
    {

        foreach (var col in GetColumns<T>())
        {
            var columnName = $"{Alias(alias)}.{col.Value}";
            Selects.Add(col.Key, columnName);
            Select($"{columnName} AS {col.Key}");
        }

        return this;
    }

    public Query SelectFunc<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column, string func, bool aggregate = false)
    {
        var aliasName = Alias<A>(alias);
        SelectFunc(aliasName, column, func, aggregate);
        return this;
    }

    public Query SelectFunc<T>(string alias, Expression<Func<T>> column, string func, bool aggregate = false)
    {
        var columnName = $"{func}({AliasFromColumn(column)}.{Property(column)})";
        if (aggregate)
            SelectAggrs.Add(alias, columnName);
        else
            Selects.Add(alias, columnName);
        SelectRaw($"{columnName} AS {alias}");
        return this;
    }

    #endregion Selects

    #region Where

    /// <summary>
    /// Example: WhereRawFormat("(LEN({0}) + LEN({1})) > 0", () => cnt.FirstName, () => cnt.LastName)
    /// Results: WHERE (LEN(FirstName) + LEN(LastName)) > 0
    /// </summary>
    public Q WhereRawFormat<Q>(this Q query, string queryFormat, params Expression<Func<object>>[] columns) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        WhereRaw(queryFormat);
        return this;
    }

    /// <summary>
    /// Example: WhereRawFormat("(LEN({0}) + LEN({1})) > 0", () => cnt.FirstName, () => cnt.LastName)
    /// Results: WHERE (LEN(FirstName) + LEN(LastName)) > 0
    /// </summary>
    public Q OrWhereRawFormat<Q>(this Q query, string queryFormat, params Expression<Func<object>>[] columns) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        OrWhereRaw(queryFormat);
        return this;
    }

    /// <summary>
    /// Example: WhereRawFormat("LEN({0}) > ?", new[] { FluentExpression(() => cnt.FirstName) }, new[] { 5 })
    /// Results: WHERE LEN(FirstName) > 5
    /// </summary>
    public Q WhereRawFormat<Q>(this Q query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        WhereRaw(queryFormat, bindings: bindings);
        return this;
    }

    /// <summary>
    /// Example: WhereRawFormat("LEN({0}) > ?", new[] { FluentExpression(() => cnt.FirstName) }, new[] { 5 })
    /// Results: WHERE LEN(FirstName) > 5
    /// </summary>
    public Q OrWhereRawFormat<Q>(this Q query, string queryFormat, Expression<Func<object>>[] columns, object[] bindings) where Q : BaseQuery<Q>
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        OrWhereRaw(queryFormat, bindings: bindings);
        return this;
    }

    public Q Where<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        Where($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhere<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhere($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q WhereNot<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        WhereNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhereNot<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q WhereColumns<Q, T1, T2>(this Q query, Expression<Func<T1>> column1, Expression<Func<T2>> column2, string op = "=") where Q : BaseQuery<Q>
    {
        WhereColumns(
            $"{AliasFromColumn(column1)}.{Property(column1)}",
            op,
            $"{AliasFromColumn(column2)}.{Property(column2)}");
        return this;
    }

    public Q WhereColumns<Q, T1>(this Q query, Expression<Func<T1>> column1, string second, string op = "=") where Q : BaseQuery<Q>
    {
        WhereColumns($"{AliasFromColumn(column1)}.{Property(column1)}", op, second);
        return this;
    }

    public Q OrWhereColumns<Q, T1, T2>(this Q query, Expression<Func<T1>> column1, Expression<Func<T2>> column2, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereColumns(
            $"{AliasFromColumn(column1)}.{Property(column1)}",
            op,
            $"{AliasFromColumn(column2)}.{Property(column2)}");
        return this;
    }

    public Q OrWhereColumns<Q, T1>(this Q query, Expression<Func<T1>> column1, string second, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereColumns($"{AliasFromColumn(column1)}.{Property(column1)}", op, second);
        return this;
    }

    public Q WhereNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        WhereNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q OrWhereNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        OrWhereNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q WhereNotNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        WhereNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q OrWhereNotNull<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        OrWhereNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q WhereTrue<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        WhereTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q OrWhereTrue<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        OrWhereTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q WhereFalse<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        WhereFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q OrWhereFalse<Q, T>(this Q query, Expression<Func<T>> column) where Q : BaseQuery<Q>
    {
        OrWhereFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Q WhereLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereNotLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereNotLike<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereNotStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereNotStarts<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereNotEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereNotEnds<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereNotContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        WhereNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q OrWhereNotContains<Q, T>(this Q query, Expression<Func<T>> column, object value, bool caseSensitive = false) where Q : BaseQuery<Q>
    {
        OrWhereNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive: caseSensitive);
        return this;
    }

    public Q WhereBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        WhereBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Q OrWhereBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        OrWhereBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Q WhereNotBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        WhereNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Q OrWhereNotBetween<Q, T, TValue>(this Q query, Expression<Func<T>> column, TValue lower, TValue higher) where Q : BaseQuery<Q>
    {
        OrWhereNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Q WhereIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        WhereIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Q OrWhereIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        OrWhereIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Q WhereNotIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        WhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Q OrWhereNotIn<Q, T, TValue>(this Q query, Expression<Func<T>> column, IEnumerable<TValue> values) where Q : BaseQuery<Q>
    {
        OrWhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Q WhereIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        WhereIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return this;
    }

    public Q OrWhereIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        OrWhereIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return this;
    }

    public Q WhereNotIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        WhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return this;
    }

    public Q OrWhereNotIn<Q, T>(this Q query, Expression<Func<T>> column, Query subquery) where Q : BaseQuery<Q>
    {
        OrWhereNotIn($"{AliasFromColumn(column)}.{Property(column)}", subquery);
        return this;
    }

    public Q WhereDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        WhereDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhereDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q WhereNotDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        WhereNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhereNotDatePart<Q, T>(this Q query, string part, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q WhereDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        WhereDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhereDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q WhereNotDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        WhereNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhereNotDate<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q WhereTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        WhereTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhereTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q WhereNotTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        WhereNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Q OrWhereNotTime<Q, T>(this Q query, Expression<Func<T>> column, object value, string op = "=") where Q : BaseQuery<Q>
    {
        OrWhereNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    #endregion Where



    #region Orders

    public Query OrderByColumn<T>(Expression<Func<T>> column)
    {
        OrderBy($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query OrderByAlias<T>(Expression<Func<T>> alias)
    {
        var aliasName = Alias(alias);
        OrderByAlias(aliasName);
        return this;
    }

    public Query OrderByAlias(string alias)
    {
        if (Selects.TryGetValue(alias, out var select))
        {
            OrderBy($"{select}");
        }
        else if (SelectsRaw.TryGetValue(alias, out var selectRaw))
        {
            OrderByRaw(selectRaw);
        }
        else if (SelectAggrs.TryGetValue(alias, out var selectAggr))
        {
            OrderByRaw(selectAggr);
        }
        else
        {
            throw new ArgumentException($"The alias name '{alias}' not found or not supported.");
        }

        return this;
    }

    public Query OrderByColumnDesc<T>(Expression<Func<T>> column)
    {
        OrderByDesc($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query OrderByAliasDesc<T>(Expression<Func<T>> alias)
    {
        var aliasName = Alias(alias);
        OrderByAliasDesc(aliasName);
        return this;
    }

    public Query OrderByAliasDesc(string alias)
    {
        if (Selects.TryGetValue(alias, out var select))
        {
            OrderByDesc($"{select}");
        }
        else if (SelectsRaw.TryGetValue(alias, out var selectRaw))
        {
            OrderByRaw(selectRaw + " desc");
        }
        else if (SelectAggrs.TryGetValue(alias, out var selectAggr))
        {
            OrderByRaw(selectAggr + " desc");
        }
        else
        {
            throw new ArgumentException($"The alias '{alias}' not found or not supported.");
        }

        return this;
    }

    public Query OrderByRawFormat(string queryFormat, params Expression<Func<object>>[] columns)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns);
        OrderByRaw(queryFormat);
        return this;
    }

    public Query OrderByRawFormat(string queryFormat, Expression<Func<object>>[] columns, params object[] bindings)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns);
        OrderByRaw(queryFormat, bindings: bindings);
        return this;
    }

    #endregion Orders

    #region Aggregations

    public Query SelectCount<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "COUNT", aggregate: true);
        return this;
    }

    public Query SelectCount<T>(string alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "COUNT", aggregate: true);
        return this;
    }

    public Query SelectCount<T>(Expression<Func<T>> column)
    {
        SelectCount(column, column);
        return this;
    }

    public Query SelectMin<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "MIN", aggregate: true);
        return this;
    }

    public Query SelectMin<T>(string alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "MIN", aggregate: true);
        return this;
    }

    public Query SelectMin<T>(Expression<Func<T>> column)
    {
        SelectMin(column, column);
        return this;
    }

    public Query SelectMax<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "MAX", aggregate: true);
        return this;
    }

    public Query SelectMax<T>(string alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "MAX", aggregate: true);
        return this;
    }

    public Query SelectMax<T>(Expression<Func<T>> column)
    {
        SelectMax(column, column);
        return this;
    }

    public Query SelectAvg<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "AVG", aggregate: true);
        return this;
    }

    public Query SelectAvg<T>(string alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "AVG", aggregate: true);
        return this;
    }

    public Query SelectAvg<T>(Expression<Func<T>> column)
    {
        SelectAvg(column, column);
        return this;
    }

    public Query SelectSum<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "SUM", aggregate: true);
        return this;
    }

    public Query SelectSum<T>(string alias, Expression<Func<T>> column)
    {
        SelectFunc(alias, column, "SUM", aggregate: true);
        return this;
    }

    public Query SelectSum<T>(Expression<Func<T>> column)
    {
        SelectSum(column, column);
        return this;
    }

    public Query AsCount<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return AsCount(aliasName, column);
    }

    public Query AsCount<T>(string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        SelectAggrs.Add(alias, columnName);
        AsCount(new[] { $"{columnName} AS {alias}" });
        return this;
    }

    public Query AsAvg<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return AsAvg(aliasName, alias);
    }

    public Query AsAvg<T>(string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        SelectAggrs.Add(alias, columnName);
        AsAvg($"{columnName} AS {alias}");
        return this;
    }

    public Query AsAverage<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return AsAverage(aliasName, column);
    }

    public Query AsAverage<T>(string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        SelectAggrs.Add(alias, columnName);
        AsAverage($"{columnName} AS {alias}");
        return this;
    }

    public Query AsSum<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return AsSum(aliasName, column);
    }

    public Query AsSum<T>(string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        SelectAggrs.Add(alias, columnName);
        AsSum($"{columnName} AS {alias}");
        return this;
    }

    public Query AsMax<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias(alias);
        return AsMax(aliasName, column);
    }

    public Query AsMax<T>(string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        SelectAggrs.Add(alias, columnName);
        AsMax($"{columnName} AS {alias}");
        return this;
    }

    public Query AsMin<A, T>(Expression<Func<A>> alias, Expression<Func<T>> column)
    {
        var aliasName = Alias<A>(alias);
        return AsMin(aliasName, column);
    }

    public Query AsMin<T>(string alias, Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        SelectAggrs.Add(alias, columnName);
        AsMin($"{columnName} AS {alias}");
        return this;
    }

    public Query GroupBy<T>(Expression<Func<T>> column)
    {
        var columnName = $"{AliasFromColumn(column)}.{Property(column)}";
        GroupBy(new[] { columnName });
        return this;
    }

    public Query GroupByRaw<T>(string queryFormat, params Expression<Func<object>>[] columns)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns);
        GroupByRaw(queryRaw);
        return this;
    }

    public Query GroupByRaw<T>(string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        var queryRaw = FormatQueryRaw(queryFormat, columns);
        GroupByRaw(queryRaw, bindings: bindings);
        return this;
    }

    #endregion Aggregations

    #region Havings

    public Query Having<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        Having($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query HavingNot<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        HavingNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query OrHaving<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        OrHaving($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query OrHavingNot<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        OrHavingNot($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query HavingColumns<T1, T2>(Expression<Func<T1>> firstColumn, Expression<Func<T2>> secondColumn, string op = "=")
    {
        HavingColumns($"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}", op, $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}");
        return this;
    }

    public Query OrHavingColumns<T1, T2>(Expression<Func<T1>> firstColumn, Expression<Func<T2>> secondColumn, string op = "=")
    {
        OrHavingColumns($"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}", op, $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}");
        return this;
    }

    public Query HavingContains<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingContains<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingDate<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        HavingDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query OrHavingDate<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        OrHavingDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query HavingDatePart<T>(Expression<Func<T>> column, object value, string part)
    {
        HavingDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return this;
    }

    public Query OrHavingDatePart<T>(Expression<Func<T>> column, object value, string part)
    {
        OrHavingDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return this;
    }

    public Query HavingEnds<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingEnds<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingFalse<T>(Expression<Func<T>> column)
    {
        HavingFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query OrHavingFalse<T>(Expression<Func<T>> column)
    {
        OrHavingFalse($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query HavingTrue<T>(Expression<Func<T>> column)
    {
        HavingTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query OrHavingTrue<T>(Expression<Func<T>> column)
    {
        OrHavingTrue($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query HavingIn<T>(Expression<Func<T>> column, IEnumerable<T> values)
    {
        HavingIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Query HavingIn<T>(Expression<Func<T>> column, Query subQuery)
    {
        HavingIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return this;
    }

    public Query OrHavingIn<T>(Expression<Func<T>> column, IEnumerable<T> values)
    {
        OrHavingIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Query OrHavingIn<T>(Expression<Func<T>> column, Query subQuery)
    {
        OrHavingIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return this;
    }

    public Query HavingLike<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingLike<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingBetween<T>(Expression<Func<T>> column, T lower, T higher)
    {
        HavingBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Query OrHavingBetween<T>(Expression<Func<T>> column, T lower, T higher)
    {
        OrHavingBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Query HavingNotBetween<T>(Expression<Func<T>> column, T lower, T higher)
    {
        HavingNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Query OrHavingNotBetween<T>(Expression<Func<T>> column, T lower, T higher)
    {
        OrHavingNotBetween($"{AliasFromColumn(column)}.{Property(column)}", lower, higher);
        return this;
    }

    public Query HavingNotContains<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingNotContains<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingNotContains($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingNotDate<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        HavingNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query OrHavingNotDate<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        OrHavingNotDate($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query HavingNotDatePart<T>(Expression<Func<T>> column, object value, string part)
    {
        HavingNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return this;
    }

    public Query OrHavingNotDatePart<T>(Expression<Func<T>> column, object value, string part)
    {
        OrHavingNotDatePart(part, $"{AliasFromColumn(column)}.{Property(column)}", value);
        return this;
    }

    public Query HavingNotEnds<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingNotEnds<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingNotEnds($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingNotIn<T>(Expression<Func<T>> column, IEnumerable<T> values)
    {
        HavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Query HavingNotIn<T>(Expression<Func<T>> column, Query subQuery)
    {
        HavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return this;
    }

    public Query OrHavingNotIn<T>(Expression<Func<T>> column, IEnumerable<T> values)
    {
        OrHavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", values);
        return this;
    }

    public Query OrHavingNotIn<T>(Expression<Func<T>> column, Query subQuery)
    {
        OrHavingNotIn($"{AliasFromColumn(column)}.{Property(column)}", subQuery);
        return this;
    }

    public Query HavingNotLike<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingNotLike<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingNotLike($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingNull<T>(Expression<Func<T>> column)
    {
        HavingNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query HavingNotNull<T>(Expression<Func<T>> column)
    {
        HavingNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query OrHavingNull<T>(Expression<Func<T>> column)
    {
        OrHavingNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query OrHavingNotNull<T>(Expression<Func<T>> column)
    {
        OrHavingNotNull($"{AliasFromColumn(column)}.{Property(column)}");
        return this;
    }

    public Query HavingStarts<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingNotStarts<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        HavingNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingStarts<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query OrHavingNotStarts<T>(Expression<Func<T>> column, object value, bool caseSensitive = false, string escapeCharacter = null)
    {
        OrHavingNotStarts($"{AliasFromColumn(column)}.{Property(column)}", value, caseSensitive, escapeCharacter);
        return this;
    }

    public Query HavingTime<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        HavingTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query OrHavingTime<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        OrHavingTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query HavingNotTime<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        HavingNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query OrHavingNotTime<T>(Expression<Func<T>> column, object value, string op = "=")
    {
        OrHavingNotTime($"{AliasFromColumn(column)}.{Property(column)}", op, value);
        return this;
    }

    public Query HavingRawFormat<T>(string queryFormat, params Expression<Func<object>>[] columns)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        HavingRaw(queryFormat);
        return this;
    }

    public Query HavingRawFormat<T>(string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        HavingRaw(queryFormat, bindings: bindings);
        return this;
    }

    public Query OrHavingRawFormat<T>(string queryFormat, params Expression<Func<object>>[] columns)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        OrHavingRaw(queryFormat);
        return this;
    }

    public Query OrHavingRawFormat<T>(string queryFormat, Expression<Func<object>>[] columns, object[] bindings)
    {
        queryFormat = FormatQueryRaw(queryFormat, columns: columns);
        OrHavingRaw(queryFormat, bindings: bindings);
        return this;
    }

    #endregion Havings

    #region Misc

    public Q If<Q>(this Q query, bool condition, Func<Q, Q> ifTrue, Func<Q, Q> ifFalse = null) where Q : BaseQuery<Q>
    {
        if (ifTrue == null)
            throw new ArgumentNullException(nameof(ifTrue));

        if (condition)
            return ifTrue.Invoke(query);
        else
            return ifFalse != null ? ifFalse.Invoke(query) : query;
    }

    public Query WithVariable(string key, object value)
    {
        Variables.Add(key, value);

        return this;
    }

    #endregion Misc

    #region Public Methods

    /// <summary>
    /// Gets a column name from the entity (poco) property (eg. Column(() => cnt.FirstName) gives 'FirstName')
    /// </summary>
    public string Column<T>(Expression<Func<T>> column)
    {
        return Property<T>(column);
    }

    /// <summary>
    /// Gets the first part of the full column name (eg. AliasFromColumn(() => cnt.FirstName) gives 'cnt')
    /// </summary>
    public string AliasFromColumn<T>(Expression<Func<T>> property)
    {
        return Property(property, parent: true);
    }

    /// <summary>
    /// Gets full column name from the entity (poco) property (eg. ColumnWithAlias(() => cnt.FirstName) gives 'cnt.FirstName')
    /// </summary>
    public string ColumnWithAlias<T>(Expression<Func<T>> column)
    {
        return $"{AliasFromColumn(column)}.{Column(column)}";
    }

    /// <summary>
    /// Gets alias name from populated dto model (eg. Alias(() => model.FullName) gives 'FullName')
    /// </summary>
    public string Alias<A>(Expression<Func<A>> model)
    {
        return Property(model);
    }

    /// <summary>
    /// Gets a table name from the entity (poco) property (eg. Table(() => cnt) gives 'Contacts')
    /// </summary>
    public string Table<A>(Expression<Func<A>> alias)
    {
        return Table<A>();
    }

    /// <summary>
    /// Gets a table name from the entity (poco) property (eg. Table<Contact>() gives 'Contacts')
    /// </summary>
    public string Table<A>()
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
    /// Used to describe an array of column expressions in a short way (eg. .WhereRawFormat("{0} LIKE 'John'", columns: new[] { FluentExpression(() => cnt.FirstName) }))
    /// </summary>
    public Expression<Func<object>> Expression(Expression<Func<object>> func)
    {
        return func;
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="snake"></param>
    /// <param name="parent">Get parent member name (eg. customer.Id will return "customer" instead of "ID")</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private string Property<T>(Expression<Func<T>> property, bool parent = false)
    {
        var memberName = GetMemberName(property, parent: parent);

        if (memberName != null)
            return memberName.Replace(".", "_");
        else
            throw new ArgumentException($"The expression cannot be evaluated");
    }

    private string GetMemberName(Expression expression, bool parent = false)
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
                        ?? memberExpression.Member.GetCustomAttribute<ColumnAttribute>()?.Name
                        ?? FormatNestedMemberName(memberExpression);

            default:
                throw new NotSupportedException(expression.NodeType.ToString(),
                    new Exception($"Cannot get member name from expression {expression}."));
        }
    }

    private string FormatNestedMemberName(MemberExpression expression)
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

    private string FormatQueryRaw(string queryFormat, params Expression<Func<object>>[] columns)
    {
        if (columns != null && columns.Length > 0)
            queryFormat = string.Format(queryFormat, columns.Select(x => $"{AliasFromColumn(x)}.{Alias(x)}").ToArray());

        return queryFormat;
    }

    private IDictionary<string, string> GetColumns<T>()
    {
        var columns = new Dictionary<string, string>();



        return columns;
    }

    #endregion Private Methods
}