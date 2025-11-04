


using System.Linq.Expressions;
using SqlKata;

public class FluentJoin : Join
{

    internal readonly IDictionary<string, string> Selects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // <alias, column>
    internal readonly IDictionary<string, string> SelectsRaw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // <alias, raw_query>
    internal readonly IDictionary<string, string> SelectAggrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // <alias, aggregation>


    #region Joins

    public Query Join<A, J1, J2>(Expression<Func<A>> alias, Expression<Func<J1>> column1, Expression<Func<J2>> column2, string op = "=")
    {
        return Join(Table<A>(), alias, column1, column2, op);
    }

    public Query Join<A, J1, J2>(string table, Expression<Func<A>> alias, Expression<Func<J1>> column1, Expression<Func<J2>> column2, string op = "=")
    {
        Join(
            $"{table} AS {Alias(alias)}",
            $"{AliasFromColumn(column1)}.{Property(column1)}",
            $"{AliasFromColumn(column2)}.{Property(column2)}",
            op: op
        );
        return this;
    }

    public Query Join<A>(Expression<Func<A>> alias, Func<Join, Join> joinQuery, string type = "inner join")
    {
        return Join(Table<A>(), alias, joinQuery, type);
    }

    public Query Join<A>(string table, Expression<Func<A>> alias, Func<Join, Join> joinQuery, string type = "inner join")
    {
        Join(
            $"{table} AS {Alias(alias)}",
            joinQuery,
            type: type
        );
        return this;
    }

    public Query LeftJoin<A, J1, J2>(Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        return LeftJoin(Table<A>(), alias, firstColumn, secondColumn, op);
    }

    public Query LeftJoin<A, J1, J2>(string table, Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        LeftJoin(
            $"{table} AS {Alias(alias)}",
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op: op
        );
        return this;
    }

    public Query LeftJoin<A>(Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        return LeftJoin(Table<A>(), alias, joinQuery);
    }

    public Query LeftJoin<A>(string table, Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        LeftJoin(
            $"{table} AS {Alias(alias)}",
            joinQuery
        );
        return this;
    }

    public Query RightJoin<A, J1, J2>(Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        return RightJoin(Table<A>(), alias, firstColumn, secondColumn, op);
    }

    public Query RightJoin<A, J1, J2>(string table, Expression<Func<A>> alias, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        RightJoin(
            $"{table} AS {Alias(alias)}",
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op: op
        );
        return this;
    }

    public Query RightJoin<A>(Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        return RightJoin(Table<A>(), alias, joinQuery);
    }

    public Query RightJoin<A>(string table, Expression<Func<A>> alias, Func<Join, Join> joinQuery)
    {
        RightJoin(
            $"{table} AS {Alias(alias)}",
            joinQuery
        );
        return this;
    }

    public Query CrossJoin<A>(Expression<Func<A>> alias)
    {
        CrossJoin(Alias(alias));
        return this;
    }

    public Query CrossJoin<A>(string table)
    {
        CrossJoin(table);
        return this;
    }

    public Join On<J1, J2>(this Join join, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        join.On(
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op);
        return join;
    }

    public Join OrOn<J1, J2>(this Join join, Expression<Func<J1>> firstColumn, Expression<Func<J2>> secondColumn, string op = "=")
    {
        join.OrOn(
            $"{AliasFromColumn(firstColumn)}.{Property(firstColumn)}",
            $"{AliasFromColumn(secondColumn)}.{Property(secondColumn)}",
            op);
        return join;
    }

    public Join JoinWith<A>(this Join join, Expression<Func<A>> alias)
    {
        join.JoinWith(Table<A>(), alias);
        return join;
    }

    public Join JoinWith<A>(this Join join, string table, Expression<Func<A>> alias)
    {
        join.JoinWith($"{table} AS {Alias(alias)}");
        return join;
    }

    #endregion Joins
}