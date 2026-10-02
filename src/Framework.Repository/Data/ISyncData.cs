using LinqToDB;
using System.Threading.Tasks;

namespace Framework.Repository.Data;

/// <summary>
/// 同步数据接口。
/// linq2db 适配版本：接收 <see cref="IDataContext"/> 作为数据访问入口。
/// </summary>
public interface ISyncData
{
    /// <summary>
    /// 根据 <see cref="IDbConfig"/> 与内置策略同步实体数据。
    /// </summary>
    Task SyncDataAsync(IDataContext db, IDbConfig dbConfig);
}