using Framework.Repository.Repositories;
using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Repository.Extensions;

public static class Linq2DbDbContextExtensions
{
    /// <summary>
    /// 返回 linq2db 驱动的默认仓库。
    /// </summary>
    public static IRepositoryBase<TEntity, TKey> GetRepositoryBase<TEntity, TKey>(this IDataContext that)
        where TEntity : class
    {
        return new RepositoryBase<TEntity, TKey>(that);
    }

    /// <summary>
    /// 返回主键为 long 的默认仓库。
    /// </summary>
    public static IRepositoryBase<TEntity, long> GetRepositoryBase<TEntity>(this IDataContext that) where TEntity : class
    {
        return new RepositoryBase<TEntity, long>(that);
    }
}