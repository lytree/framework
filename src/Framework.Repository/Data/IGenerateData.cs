using LinqToDB;
using System.Threading.Tasks;

namespace Framework.Repository.Data;

/// <summary>
/// 生成数据接口。
/// linq2db 适配版本：接收 <see cref="IDataContext"/> 作为数据访问入口。
/// </summary>
public interface IGenerateData
{
    /// <summary>
    /// 根据 <see cref="IDbConfig"/> 生成初始数据。
    /// </summary>
    Task GenerateDataAsync(IDataContext db, IDbConfig dbConfig);
}