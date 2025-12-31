using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using FreeSql.DataAnnotations;
using System.Text.Json;
using Framework.Repository;
using System.Data.Common;
using Framework.Repository.Entities;
using Framework.System.Collections.Generic;
using FreeSql;
using Mapster;

namespace Framework.Repository.Data;

public abstract class AbstractSyncData
{
    /// <summary>
    /// 检查实体属性是否为自增长
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected static string GetTableName<T>() where T : class, new()
    {
        var table = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
        return table.Name;
    }

    protected static bool IsSyncData(string tableName, string[]? syncDataIncludeTables, string[]? syncDataExcludeTables)
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
    /// 初始化数据表数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <param name="tran"></param>
    /// <param name="dataList"></param>
    /// <returns></returns>
    protected virtual async Task InitDataAsync<T>(
        IFreeSql db,
        DbTransaction tran,
        T[] dataList,
        bool sysUpdateData
    ) where T : class, new()
    {
        var table = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
        var tableName = table.Name;

        try
        {
            if (!(dataList?.Length > 0))
            {
                Console.WriteLine($"table: {tableName} import data []");
                return;
            }

            var insertOrUpdate = db.InsertOrUpdate<T>();
            if (tran != null)
            {
                insertOrUpdate = insertOrUpdate.WithTransaction(tran);
            }
            if (!sysUpdateData)
            {
                insertOrUpdate.IfExistsDoNothing();
            }
            await insertOrUpdate.SetSource(dataList).ExecuteAffrowsAsync();

            Console.WriteLine($"table: {tableName} sync data succeed");
        }
        catch (Exception ex)
        {
            var msg = $"table: {tableName} sync data failed.\n{ex.Message}";
            Console.WriteLine(msg);
            throw new Exception(msg);
        }
    }

    protected virtual T[] GetData<T>(bool isTenant = false, string path = "InitData/Admin")
    {
        var table = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
        var fileName = $"{table.Name}{(isTenant ? ".tenant" : "")}.json";
        var filePath = Path.Combine(AppContext.BaseDirectory, $"{path}/{fileName}").ToPath();
        if (!File.Exists(filePath))
        {
            var msg = $"数据文件{filePath}不存在";
            Console.WriteLine(msg);
            throw new Exception(msg);
        }
        var jsonData = FileHelper.ReadFile(filePath);
        var data = Helper.JsonDeserialize<T[]>(jsonData);

        return data;
    }
    /// <summary>
    /// 同步实体数据
    /// </summary>
    /// <param name="db"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="dbConfig">模块数据库配置</param>
    /// <param name="appConfig">应用配置</param>
    /// <param name="readPath">读取数据路径 InitData/xxx </param>
    /// <param name="processChilds">处理子级列表</param>
    /// <returns></returns>
    protected virtual async Task SyncEntityAsync<T>(IFreeSql db,
        IRepositoryUnitOfWork unitOfWork, string[]? syncDataIncludeTables, string[]? syncDataExcludeTables,
        string readPath, bool isTenantParam = false,
        bool processChilds = false,bool sysUpdateData = false)
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
            var rep = db.GetRepository<T>();
            rep.UnitOfWork = unitOfWork;

            //数据列表
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

            //查询
            var dataIds = dataList.Select(e => e.Id).ToList();
            var dbDataList = await rep.Where(a => dataIds.Contains(a.Id)).ToListAsync();

            //新增
            var dbDataIds = dbDataList.Select(a => a.Id).ToList();
            var insertDataList = dataList.Where(a => !dbDataIds.Contains(a.Id));
            if (insertDataList.Any())
            {
                await rep.InsertAsync(insertDataList);
            }

            //修改
            if (sysUpdateData && dbDataList?.Count > 0)
            {
                foreach (var dbData in dbDataList)
                {
                    var data = dataList.Where(a => a.Id == dbData.Id).First();
                    data.Adapt(dbData);
                }

                await rep.UpdateAsync(dbDataList);
            }

            Console.WriteLine($"table: {tableName} sync data succeed");
        }
        catch (Exception ex)
        {
            var msg = $"table: {tableName} sync data failed.\n{ex.Message}";
            Console.WriteLine(msg);
            throw new Exception(msg);
        }
    }
}
