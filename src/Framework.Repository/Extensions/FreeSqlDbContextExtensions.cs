using Framework.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Repository.Extensions;

public static class FreeSqlDbContextExtensions
{
    /// <summary>
    /// 返回默认仓库类
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="that"></param>
    /// <returns></returns>
    public static IRepositoryBase<TEntity, TKey> GetRepositoryBase<TEntity, TKey>(this IFreeSql that) where TEntity : class
    {
        return new RepositoryBase<TEntity, TKey>(that);
    }

    /// <summary>
    /// 返回默认仓库类，适用联合主键的仓储类
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="that"></param>
    /// <returns></returns>
    public static IRepositoryBase<TEntity, long> GetRepositoryBase<TEntity>(this IFreeSql that) where TEntity : class
    {
        return new RepositoryBase<TEntity, long>(that);
    }

}
