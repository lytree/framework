using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Framework.Repository.Entities;
using LinqToDB;
using LinqToDB.Async;
using Mapster;

namespace Framework.Repository.Repositories;

public class RepositoryBase<TEntity, TKey> : IRepositoryBase<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// linq2db 数据上下文，负责 SQL 连接与单元作用域。
    /// </summary>
    public IDataContext DataContext { get; }

    public RepositoryBase(IDataContext db)
    {
        DataContext = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// 当前实体对应的表。
    /// </summary>
    protected ITable<TEntity> Table => DataContext.GetTable<TEntity>();

    public virtual async Task<TDto> GetAsync<TDto>(TKey id)
    {
        var entity = await Table.FirstOrDefaultAsync(BuildKeySelector().Apply(id));
        return entity is null ? default! : entity.Adapt<TDto>();
    }

    public virtual async Task<TDto> GetAsync<TDto>(Expression<Func<TEntity, bool>> exp)
    {
        var entity = await Table.FirstOrDefaultAsync(exp);
        return entity is null ? default! : entity.Adapt<TDto>();
    }

    public virtual Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> exp)
    {
        return Table.FirstOrDefaultAsync(exp);
    }

    public virtual async Task<bool> SoftDeleteAsync(TKey id)
    {
        await Table.Where(BuildKeySelector().Apply(id))
            .AsUpdatable()
            .Set(e => ((IDelete)e).IsDeleted, true)
            .UpdateAsync();
        return true;
    }

    public virtual async Task<bool> SoftDeleteAsync(TKey[] ids)
    {
        if (ids is null || ids.Length == 0) return true;
        await Table.Where(BuildKeySelector().WithValues(ids.Cast<object>()))
            .AsUpdatable()
            .Set(e => ((IDelete)e).IsDeleted, true)
            .UpdateAsync();
        return true;
    }

    public virtual async Task<bool> SoftDeleteAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames)
    {
        // linq2db 仅支持统一禁用所有过滤器 (IgnoreFilters)；此处保留 disableGlobalFilterNames
        // 参数仅为 API 兼容性，调用方通过 .IgnoreFilters() 一并禁用。
        _ = disableGlobalFilterNames;
        var query = Table.Where(exp).IgnoreFilters();
        await query.AsUpdatable().Set(e => ((IDelete)e).IsDeleted, true).UpdateAsync();
        return true;
    }

    public virtual async Task<bool> DeleteRecursiveAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames)
    {
        _ = disableGlobalFilterNames;
        var query = Table.Where(exp).IgnoreFilters();

        var parentProp = ResolveParentIdProperty();
        if (parentProp is null)
        {
            throw new InvalidOperationException(
                $"{typeof(TEntity).FullName} 必须拥有 ParentId 属性才能执行递归删除。");
        }

        var ids = await CollectDescendantIds(query, parentProp);
        if (ids.Count == 0) return true;

        await Table.Where(BuildKeySelector().WithValues(ids.Cast<object>())).DeleteAsync();
        return true;
    }

    public virtual async Task<bool> SoftDeleteRecursiveAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames)
    {
        _ = disableGlobalFilterNames;
        var query = Table.Where(exp).IgnoreFilters();

        var parentProp = ResolveParentIdProperty();
        if (parentProp is null)
        {
            throw new InvalidOperationException(
                $"{typeof(TEntity).FullName} 必须拥有 ParentId 属性才能执行递归软删除。");
        }

        var ids = await CollectDescendantIds(query, parentProp);
        if (ids.Count == 0) return true;

        await Table.Where(BuildKeySelector().WithValues(ids.Cast<object>()))
            .AsUpdatable()
            .Set(e => ((IDelete)e).IsDeleted, true)
            .UpdateAsync();
        return true;
    }

    /// <summary>
    /// 通过 BFS / 多次查询的方式收集满足 <paramref name="seed"/> 的全部子孙主键。
    /// 适用于任何支持标准 SQL 的数据库（避免依赖 CTE 方言）。
    /// </summary>
    private async Task<List<object>> CollectDescendantIds(IQueryable<TEntity> seed, PropertyInfo parentProp)
    {
        var initial = await seed.SelectKeyValues().ToListAsync();
        var all = new HashSet<object>(initial);
        var currentLayer = initial;
        while (currentLayer.Count > 0)
        {
            var nextLayer = await Table
                .Where(BuildParentIdSelector(parentProp, currentLayer))
                .SelectKeyValues()
                .ToListAsync();
            var fresh = nextLayer.Where(id => !all.Contains(id)).ToList();
            foreach (var id in fresh) all.Add(id);
            currentLayer = fresh;
        }
        return all.ToList();
    }

    private static KeySelector BuildKeySelector() => new();

    private static Expression<Func<TEntity, bool>> BuildParentIdSelector(PropertyInfo parentProp, IList<object> ids)
    {
        var param = Expression.Parameter(typeof(TEntity), "e");
        var member = Expression.Property(param, parentProp);
        var body = ids
            .Select(id => (Expression)Expression.Equal(member, Expression.Convert(Expression.Constant(id), member.Type)))
            .Aggregate((Expression)Expression.Constant(false), (acc, e) => Expression.OrElse(acc, e));
        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    private static PropertyInfo? ResolveParentIdProperty() =>
        typeof(TEntity).GetProperty("ParentId", BindingFlags.Public | BindingFlags.Instance);

    /// <summary>
    /// 承载 "Id == value" 与 "Id IN (...)" 谓词的内部构造器。
    /// </summary>
    private sealed class KeySelector
    {
        private readonly ParameterExpression _param;
        private readonly MemberExpression _member;

        public KeySelector()
        {
            var keyProp = typeof(TEntity).GetProperties()
                .First(p => p.GetCustomAttributes(typeof(LinqToDB.Mapping.PrimaryKeyAttribute), false).Any());
            _param = Expression.Parameter(typeof(TEntity), "e");
            _member = Expression.Property(_param, keyProp);
        }

        public Expression<Func<TEntity, bool>> Apply(TKey id)
        {
            var body = Expression.Equal(_member, Expression.Convert(Expression.Constant(id), _member.Type));
            return Expression.Lambda<Func<TEntity, bool>>(body, _param);
        }

        public Expression<Func<TEntity, bool>> WithValues(IEnumerable<object> ids)
        {
            var values = ids.ToArray();
            Expression body = Expression.Constant(false);
            if (values.Length > 0)
            {
                body = values
                    .Select(v => (Expression)Expression.Equal(_member, Expression.Convert(Expression.Constant(v), _member.Type)))
                    .Aggregate((Expression)Expression.Constant(false), (acc, e) => Expression.OrElse(acc, e));
            }
            return Expression.Lambda<Func<TEntity, bool>>(body, _param);
        }
    }
}

internal static class KeySelectorQueryable
{
    /// <summary>
    /// 在 <see cref="IQueryable{T}"/> 上投影出主键列（装箱为 <see cref="object"/>）。
    /// </summary>
    public static IQueryable<object> SelectKeyValues<TEntity>(this IQueryable<TEntity> source)
        where TEntity : class
    {
        var keyProp = typeof(TEntity).GetProperties()
            .First(p => p.GetCustomAttributes(typeof(LinqToDB.Mapping.PrimaryKeyAttribute), false).Any());
        var param = Expression.Parameter(typeof(TEntity), "e");
        var member = Expression.Property(param, keyProp);
        var lambda = Expression.Lambda<Func<TEntity, object>>(Expression.Convert(member, typeof(object)), param);
        return source.Select(lambda);
    }
}

public class RepositoryBase<TEntity> : RepositoryBase<TEntity, long>, IRepositoryBase<TEntity, long> where TEntity : class
{
    public RepositoryBase(IDataContext db) : base(db) { }
}