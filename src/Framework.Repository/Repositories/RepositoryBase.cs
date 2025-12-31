using Framework.Repository;
using FreeSql;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Framework.Repository.Repositories;

public class RepositoryBase<TEntity, TKey> : BaseRepository<TEntity, TKey>, IRepositoryBase<TEntity, TKey> where TEntity : class
{

    public RepositoryBase(IFreeSql fsql) : base(fsql) { }
    public RepositoryBase(IFreeSql fsql, UnitOfWorkManager uowManger) : base(uowManger?.Orm ?? fsql)
    {
        uowManger?.Binding(this);
    }

    public virtual Task<TDto> GetAsync<TDto>(TKey id)
    {
        return Select.WhereDynamic(id).ToOneAsync<TDto>();
    }

    public virtual Task<TDto> GetAsync<TDto>(Expression<Func<TEntity, bool>> exp)
    {
        return Select.Where(exp).ToOneAsync<TDto>();
    }

    public virtual Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> exp)
    {
        return Select.Where(exp).ToOneAsync();
    }

    public virtual async Task<bool> SoftDeleteAsync(TKey id)
    {
        await UpdateDiy
            .SetDto(new
            {
                IsDeleted = true,
            })
            .WhereDynamic(id)
            .ExecuteAffrowsAsync();

        return true;
    }

    public virtual async Task<bool> SoftDeleteAsync(TKey[] ids)
    {
        await UpdateDiy
            .SetDto(new
            {
                IsDeleted = true,
            })
            .WhereDynamic(ids)
            .ExecuteAffrowsAsync();

        return true;
    }

    public virtual async Task<bool> SoftDeleteAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames)
    {
        await UpdateDiy
            .SetDto(new
            {
                IsDeleted = true,
            })
            .Where(exp)
            .DisableGlobalFilter(disableGlobalFilterNames)
            .ExecuteAffrowsAsync();

        return true;
    }

    public virtual async Task<bool> DeleteRecursiveAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames)
    {
        await Select
        .Where(exp)
        .DisableGlobalFilter(disableGlobalFilterNames)
        .AsTreeCte()
        .ToDelete()
        .ExecuteAffrowsAsync();

        return true;
    }

    public virtual async Task<bool> SoftDeleteRecursiveAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames)
    {
        await Select
        .Where(exp)
        .DisableGlobalFilter(disableGlobalFilterNames)
        .AsTreeCte()
        .ToUpdate()
        .SetDto(new
        {
            IsDeleted = true,
        })
        .ExecuteAffrowsAsync();

        return true;
    }
}

public class RepositoryBase<TEntity> : RepositoryBase<TEntity, long>, IRepositoryBase<TEntity> where TEntity : class
{
    public RepositoryBase(UnitOfWorkManager uowm) : base(uowm.Orm)
    {
        uowm.Binding(this);
    }
}