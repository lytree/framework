using System;
using SqlKata;

namespace Framework.SqlKata;

public static class SqlKataExtensions
{
    private static IDictionary<Type, IDictionary<string, string>> ColumnsCache = new Dictionary<Type, IDictionary<string, string>>();

    public static void AddFluentSqlKata(this Query query) { }
}
