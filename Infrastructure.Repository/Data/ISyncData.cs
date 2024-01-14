using Repository.Admin.Core;
using System.Threading.Tasks;

namespace Repository.Admin.Core;

/// <summary>
/// 同步数据接口
/// </summary>
public interface ISyncData
{
    Task SyncDataAsync(IFreeSql db);
}
