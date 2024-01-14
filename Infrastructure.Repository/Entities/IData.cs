namespace Infrastructure.Repository;

/// <summary>
/// 数据权限接口
/// </summary>
public interface IData
{
    /// <summary>
    /// 拥有者Id
    /// </summary>
    long? OwnerId { get; set; }

    /// <summary>
    /// 拥有者部门Id
    /// </summary>
    long? OwnerOrgId { get; set; }
}