using System.Threading.Tasks;

namespace Framework.Repository.Data;

/// <summary>
/// 同步数据接口
/// </summary>
public interface ISyncData
{
    Task SyncDataAsync(IFreeSql db);
}
