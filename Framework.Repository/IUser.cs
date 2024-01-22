using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Repository;


/// <summary>
/// 用户信息接口
/// </summary>
public interface IUser
{
    /// <summary>
    /// 用户Id
    /// </summary>
    long Id { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// 姓名
    /// </summary>
    string Name { get; }
}