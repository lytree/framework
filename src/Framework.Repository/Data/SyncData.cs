using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using FreeSql.DataAnnotations;
using System.Text.Json;
using Framework.Helper;
using Framework.Repository;
using System.Data.Common;

namespace Framework.Repository.Data;

public abstract class SyncData
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

	protected virtual T[] GetData<T>(string path = "InitData/Admin")
	{
		var table = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
		var fileName = $"{table.Name}.json";
		var filePath = Path.Combine(AppContext.BaseDirectory, $"{path}/{fileName}").Replace(@"\", "/");
		if (!File.Exists(filePath))
		{
			var msg = $"数据文件{filePath}不存在";
			Console.WriteLine(msg);
			throw new Exception(msg);
		}
		var jsonData = FileHelper.ReadFile(filePath);
		var data = JsonHelper.Deserialize<T[]>(jsonData);

		return data;
	}
}
