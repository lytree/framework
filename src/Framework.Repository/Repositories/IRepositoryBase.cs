using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Framework.Repository.Repositories;

/// <summary>
/// 通用仓储接口。
/// 实现基于 linq2db <see cref="LinqToDB.IDataContext"/> 的 <see cref="LinqToDB.ITable{TEntity}"/>。
/// </summary>
public interface IRepositoryBase<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// linq2db 数据上下文，可由实现暴露以提供事务 / 批量插入等扩展能力。
    /// </summary>
    LinqToDB.IDataContext DataContext { get; }

    /// <summary>
    /// 获得 Dto
    /// </summary>
    Task<TDto> GetAsync<TDto>(TKey id);

    /// <summary>
    /// 根据条件获取 Dto
    /// </summary>
    Task<TDto> GetAsync<TDto>(Expression<Func<TEntity, bool>> exp);

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> exp);

    /// <summary>
    /// 软删除
    /// </summary>
    Task<bool> SoftDeleteAsync(TKey id);

    /// <summary>
    /// 批量软删除
    /// </summary>
    Task<bool> SoftDeleteAsync(TKey[] ids);

    /// <summary>
    /// 软删除
    /// </summary>
    Task<bool> SoftDeleteAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames);

    /// <summary>
    /// 递归删除：匹配行 + 全部子孙节点一起删除。
    /// 要求 <typeparamref name="TEntity"/> 拥有类型为 <typeparamref name="TKey"/> 的 <c>ParentId</c> 字段，
    /// 该字段参照自身的 <c>Id</c>。
    /// </summary>
    Task<bool> DeleteRecursiveAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames);

    /// <summary>
    /// 递归软删除：匹配行 + 全部子孙节点一起软删除。
    /// </summary>
    Task<bool> SoftDeleteRecursiveAsync(Expression<Func<TEntity, bool>> exp, params string[] disableGlobalFilterNames);
}