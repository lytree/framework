using System.Threading.Tasks;

namespace Framework.Repository.Data;

/// <summary>
/// 生成数据接口
/// </summary>
public interface IGenerateData
{
    Task GenerateDataAsync(IFreeSql db);
}
