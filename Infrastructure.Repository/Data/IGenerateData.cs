using Repository.Admin.Core;
using System.Threading.Tasks;

namespace Repository.Admin.Core;

/// <summary>
/// 生成数据接口
/// </summary>
public interface IGenerateData
{
    Task GenerateDataAsync(IFreeSql db);
}
