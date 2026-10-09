# YLFramework.Repository

基于 [linq2db](https://linq2db.github.io/) 的轻量仓库层，附带实体基类、特性注入、雪花 ID、事务传播、统一异常包装等通用数据访问能力。

## 功能概览

### 实体与基类

| 文件 | 类型 | 作用 |
|------|------|------|
| `Entities/IEntity.cs` / `EntityBase.cs` | 实体接口与基类 | 提供主键 `Id` 与创建/更新/删除审计字段的契约 |
| `Entities/EntityAdd.cs` / `IEntityAdd.cs` | 创建审计 | 注入 `CreateTime` / `CreateUserId` 等 |
| `Entities/EntityUpdate.cs` / `IEntityUpdate.cs` | 更新审计 | 注入 `UpdateTime` / `UpdateUserId` 等 |
| `Entities/EntityDelete.cs` / `IDelete.cs` | 软删除 | 实体实现 `IDelete` 即可启用"软删除 + 全局过滤" |
| `Entities/EntityVersion.cs` / `IVersion.cs` | 乐观锁 | 实体实现 `IVersion` 启用 `RowVersion` 字段并发检查 |
| `Entities/EntityTenant.cs` / `ITenant.cs` | 多租户 | 实体实现 `ITenant` 注入租户字段，配合拦截器自动按租户过滤 |
| `Entities/IChilds.cs` | 父子关系 | 标记一个实体为"可拥有子项"的聚合根 |

### 仓库与扩展

| 文件 | 类型 | 作用 |
|------|------|------|
| `Repositories/IRepositoryBase.cs` | 仓储接口 | 统一 CRUD / 批量 / 分页 / 事务 API |
| `Repositories/RepositoryBase.cs` | 仓储基类 | linq2db `DataConnection` 之上封装，开放可继承钩子 |
| `Extensions/Linq2DbDbContextExtensions.cs` | DataConnection 扩展 | 软删除过滤、租户过滤、审计字段自动写入 |
| `Extensions/Linq2DbExtensions.cs` | IQueryable 扩展 | `ToPageList` / `ToTree` 等业务常用方法 |
| `FileHelper.cs` | 工具 | JSON 持久化辅助（用于种子数据 / 缓存导入导出） |

### 数据生成与同步

| 文件 | 类型 | 作用 |
|------|------|------|
| `Data/IGenerateData.cs` | 生成器接口 | 配合 `AbstractGenerateData` 提供代码生成钩子 |
| `Data/AbstractGenerateData.cs` | 生成器基类 | 保存生成结果到 JSON / 文件 |
| `Data/ISyncData.cs` / `AbstractSyncData.cs` | 同步器 | 在不同数据源之间同步基础数据 |
| `Data/IDbConfig.cs` | 数据库配置 | 描述一个数据库连接 + 类型映射 |
| `Data/Propagation.cs` | 事务传播枚举 | `Required` / `RequiresNew` / `Nested` / `Suppressed` |

### 特性（Attributes）

| 特性 | 作用 |
|------|------|
| `ApiGroupAttribute` | 在动态 API 暴露时归到指定 `Group`，影响 Swagger 文档 |
| `NoOprationLogAttribute` | 标注的方法不会记录操作日志 |
| `NonRegisterIOCAttribute` | 标注的类不参与自动依赖注入扫描 |
| `NotGenAttribute` | 标注的实体不参与代码生成 |
| `OrderGuidAttribute` | 字段使用有序 GUID 提升索引性能 |
| `ServerTimeAttribute` | 字段值由数据库 `GETDATE()` 写入 |
| `SingleInstanceAttribute` | 仓库/服务在 DI 容器中注册为单例 |
| `SnowflakeAttribute` | 字段由雪花算法生成 |
| `TransactionAttribute` | 标注的方法启用事务，并指定传播行为 |

## 快速上手

```csharp
// 1) 注册
services.AddLinq2DbContext<MyDb>(opts => opts.UseSqlServer(connStr));
services.AddScoped<IRepositoryBase, MyRepository>();

// 2) 注入并使用
public class UserService(IRepositoryBase repo)
{
    public Task<User?> GetAsync(long id) => repo.Get<User>(id);
}
```

## 版本

`1.0.x`，遵循 GitVersion。
