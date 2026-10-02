using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Framework.Repository.Entities;
using Framework.System.Collections.Generic;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Mapping;
using Mapster;

namespace Framework.Repository.Data;

public abstract class AbstractSyncData
{
    /// <summary>
    /// 检查实体属性是否为自增长
    /// </summary>
    private static bool CheckIdentity<T>() where T : class
    {
        var isIdentity = false;
        var properties = typeof(T).GetProperties();
        foreach (var property in properties)
        {
            if (property.GetCustomAttributes(typeof(ColumnAttribute), false).FirstOrDefault() is ColumnAttribute columnAttribute && columnAttribute.IsIdentity)
            {
                isIdentity = true;
                break;
            }
        }

        return isIdentity;
    }

    /// <summary>
    /// 获得表名
    /// </summary>
    protected static string? GetTableName<T>() where T : class, new()
    {
        var table = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
        if (table is null) return null;
        return table.Name;
    }

    protected static bool IsSyncData(string? tableName, string[]? syncDataIncludeTables, string[]? syncDataExcludeTables)
    {
        var isSyncData = true;

        var hasDataIncludeTables = syncDataIncludeTables?.Length > 0;
        if (hasDataIncludeTables && !syncDataIncludeTables.Contains(tableName))
        {
            isSyncData = false;
        }

        var hasSyncDataExcludeTables = syncDataExcludeTables?.Length > 0;
        if (hasSyncDataExcludeTables && syncDataExcludeTables.Contains(tableName))
        {
            isSyncData = false;
        }

        return isSyncData;
    }

    /// <summary>
    /// 初始化数据表数据。
    /// </summary>
    /// <param name="db">linq2db 数据上下文。</param>
    /// <param name="dataList">待写入的数据列表。</param>
    /// <param name="sysUpdateData">若为 <c>true</c>，对已存在的记录执行覆盖式更新；否则仅插入新记录。</param>
    protected virtual async Task InitDataAsync<T>(
        IDataContext db,
        T[] dataList,
        bool sysUpdateData
    ) where T : class, new()
    {
        var tableName = GetTableName<T>();
        if (tableName is null) return;

        try
        {
            if (!(dataList?.Length > 0))
            {
                Console.WriteLine($"table: {tableName} import data []");
                return;
            }

            var table = db.GetTable<T>();

            if (sysUpdateData)
            {
                var keyProp = typeof(T).GetProperties()
                    .First(p => p.GetCustomAttributes(typeof(LinqToDB.Mapping.PrimaryKeyAttribute), false).Any());

                var ids = dataList.Select(d => keyProp.GetValue(d)).OfType<object>().ToArray();
                var existing = ids.Length > 0
                    ? await table.Where(BuildInSelector<T>(keyProp, ids!)).ToListAsync()
                    : new List<T>();

                var existingIds = new HashSet<object?>(existing.Select(e => keyProp.GetValue(e)));
                var toInsert = dataList.Where(d => !existingIds.Contains(keyProp.GetValue(d))).ToArray();
                var toUpdate = dataList.Where(d => existingIds.Contains(keyProp.GetValue(d))).ToArray();

                foreach (var item in toInsert) await db.InsertAsync(item);
                foreach (var item in toUpdate) await db.UpdateAsync(item);
            }
            else
            {
                foreach (var item in dataList) await db.InsertAsync(item);
            }

            Console.WriteLine($"table: {tableName} sync data succeed");
        }
        catch (Exception ex)
        {
            var msg = $"table: {tableName} sync data failed.\n{ex.Message}";
            Console.WriteLine(msg);
            throw new Exception(msg, ex);
        }
    }

    private static Expression<Func<T, bool>> BuildInSelector<T>(PropertyInfo keyProp, object[] ids)
    {
        var param = Expression.Parameter(typeof(T), "e");
        var member = Expression.Property(param, keyProp);
        var body = ids.Select(id => (Expression)Expression.Equal(member, Expression.Convert(Expression.Constant(id), member.Type)))
            .Aggregate((Expression)Expression.Constant(false), (acc, e) => Expression.OrElse(acc, e));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    protected virtual T[] GetData<T>(bool isTenant = false, string path = "InitData/Admin") where T : class, new()
    {
        var table = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
        var fileName = $"{table?.Name}{(isTenant ? ".tenant" : "")}.json";
        var filePath = Path.Combine(AppContext.BaseDirectory, $"{path}/{fileName}").ToPath();
        if (!File.Exists(filePath))
        {
            var msg = $"数据文件{filePath}不存在";
            Console.WriteLine(msg);
            throw new Exception(msg);
        }
        var jsonData = FileHelper.ReadFile(filePath);
        var data = Helper.JsonDeserialize<T[]>(jsonData);
        if (data is null) throw new Exception($"无法反序列化数据文件 {filePath}");
        return data;
    }

    /// <summary>
    /// 同步实体数据。
    /// 跨实体的事务由调用方负责（例如使用 <c>db.RunInTransaction(...)</c>）。
    /// </summary>
    protected virtual async Task SyncEntityAsync<T>(
        IDataContext db,
        string[]? syncDataIncludeTables,
        string[]? syncDataExcludeTables,
        string readPath,
        bool isTenantParam = false,
        bool processChilds = false,
        bool sysUpdateData = false)
        where T : Entity<long>, new()
    {
        if (processChilds && !typeof(T).IsAssignableTo(typeof(IChilds<T>)))
        {
            throw new InvalidOperationException("processChilds is true but T does not implement IChilds<T>");
        }

        var tableName = GetTableName<T>();
        try
        {
            if (!IsSyncData(tableName, syncDataIncludeTables, syncDataExcludeTables))
            {
                return;
            }

            var isTenant = isTenantParam && typeof(T).IsAssignableTo(typeof(EntityTenant));
            var table = db.GetTable<T>();

            var dataList = GetData<T>(isTenant, readPath);

            if (!(dataList?.Length > 0))
            {
                Console.WriteLine($"table: {tableName} import data []");
                return;
            }

            if (processChilds)
            {
                dataList = dataList.ToList().ToPlainList((a) => ((IChilds<T>)a).Childs).ToArray();
            }

            var dataIds = dataList.Select(e => e.Id).ToList();
            var dbDataList = await table.Where(a => dataIds.Contains(a.Id)).ToListAsync();

            var dbDataIds = dbDataList.Select(a => a.Id).ToList();
            var insertDataList = dataList.Where(a => !dbDataIds.Contains(a.Id)).ToArray();
            foreach (var item in insertDataList)
            {
                await db.InsertAsync(item);
            }

            if (sysUpdateData && dbDataList?.Count > 0)
            {
                foreach (var dbData in dbDataList)
                {
                    var data = dataList.FirstOrDefault(a => a.Id == dbData.Id);
                    if (data == null) continue;
                    data.Adapt(dbData);
                }

                foreach (var item in dbDataList)
                {
                    await db.UpdateAsync(item);
                }
            }

            Console.WriteLine($"table: {tableName} sync data succeed");
        }
        catch (Exception ex)
        {
            var msg = $"table: {tableName} sync data failed.\n{ex.Message}";
            Console.WriteLine(msg);
            throw new Exception(msg, ex);
        }
    }
}