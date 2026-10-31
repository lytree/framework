# YLFramework.Logging

基于 [Serilog](https://serilog.net/) 的日志接入包，提供两层价值：

1. **一行接入**：控制台 + 滚动文件 + 异步写入 + CLEF JSON，默认配置即可用于生产。
2. **一套结构化日志约定**：`LogProperties` 统一字段名、`LogTemplates` 统一消息模板、强类型方法消除参数错位。

结构化日志的价值不在于「输出好看的文本」，而在于**字段名稳定**——
只有 `UserId` 永远是 `UserId`，日志平台才能按它聚合、告警、画图。

---

## 快速上手

### 1. 代码配置（最简）

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFrameworkSerilog(options =>
{
    options.Application = "OrderService";
    options.FileEnabled = true;              // logs/OrderService-20261031.log
});

// 之后照常注入 ILogger<T>
```

### 2. 配置文件驱动（推荐用于多环境）

复制包内 `appsettings.serilog.json` 的 `Serilog` 节到 `appsettings.json`，然后：

```csharp
builder.Logging.AddFrameworkSerilog(builder.Configuration, options =>
{
    options.Application = "OrderService";    // 公共字段仍由代码提供
});
```

> **配置优先级**：只要配置里存在 `Serilog` 节，输出目标（`WriteTo`）就完全以配置文件为准，
> `FrameworkSerilogOptions` 里的 `Console*` / `File*` / `Async*` 属性不再生效。
> 这样避免代码和配置文件各配一份，导致每条日志打印两次。

### 3. 挂载自定义 Sink

需要 Seq、OpenTelemetry 等内置目标之外的 Sink 时，自行拼装管道：

```csharp
var logger = new LoggerConfiguration()
    .UseFrameworkDefaults(new FrameworkSerilogOptions
    {
        Application = "OrderService",
        ConsoleEnabled = false,
        FileEnabled = false
    })
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
```

---

## 结构化日志模板

### 为什么需要约定

Serilog 的消息模板把 `{Name}` 变成日志事件上的**属性名**。下面三种写法编译都能过，后果却完全不同：

```csharp
logger.LogInformation("用户 {UserId} 下单", userId);      // ✅ 属性名 UserId
logger.LogInformation("用户 {user_id} 下单", userId);     // ❌ 属性名 user_id，查不到 UserId
logger.LogInformation("用户 {UserId}{OrderId} 下单", userId);  // ❌ 占位符 2 个、实参 1 个，参数错位
```

第三种尤其危险：日志看起来正常，但 `UserId` 上挂的是 `null`、`OrderId` 上挂的是用户 ID。
本包用三层手段把它挡在编译期与 CI 里。

### 第一层：字段名常量 `LogProperties`

所有占位符名都必须取自此处的常量，禁止手写字符串。

| 分类 | 字段 |
|------|------|
| 公共上下文（Enricher 自动注入） | `Application` `ApplicationVersion` `EnvironmentName` `MachineName` `TraceId` `SpanId` |
| 请求 / 响应 | `Method` `Path` `StatusCode` `Elapsed` `ClientIp` `RequestId` |
| 业务 / 身份 | `Operation` `UserId` `TenantId` `EntityType` `EntityId` `ErrorCode` `RetryCount` `MaxRetry` |
| 数据访问 | `Command` `Table` `Rows` `Database` |
| 缓存 / 消息 / 任务 | `CacheKey` `JobName` `QueueName` `MessageId` `Target` |

`Application` / `EnvironmentName` / `MachineName` / `TraceId` 等由 Enricher 自动附加到**每一条**日志，
业务代码不需要重复传参。`TraceId` 取自 `Activity.Current`，与 OpenTelemetry、HttpClient 诊断天然对齐。

### 第二层：消息模板常量 `LogTemplates`

| 分类 | 模板常量 |
|------|---------|
| 请求 | `RequestStarted` `RequestCompleted` `RequestFailed` `RequestSlow` `RequestAborted` |
| 数据访问 | `DatabaseCommandExecuting` `DatabaseCommandCompleted` `DatabaseCommandFailed` `DatabaseSlowCommand` `TransactionRolledBack` |
| 缓存 | `CacheHit` `CacheMiss` `CacheWritten` `CacheEvicted` |
| 外部调用 | `OutboundCallStarted` `OutboundCallCompleted` `OutboundCallFailed` `OutboundCallRetrying` |
| 后台任务 / 消息 | `JobStarted` `JobCompleted` `JobFailed` `MessageConsumed` `MessageHandled` |
| 认证授权 | `AuthenticationSucceeded` `AuthenticationFailed` `AuthorizationDenied` |
| 业务操作 | `OperationStarted` `OperationCompleted` `OperationFailed` `EntityChanged` `ValidationFailed` |
| 生命周期 | `ApplicationStarting` `ApplicationStopping` |

用法：

```csharp
logger.LogInformation(LogTemplates.RequestCompleted, method, path, statusCode, elapsed);
```

编写新模板时必须遵守：

- 模板是 `const string`，**绝不能**用字符串插值——插值会立即求值，结构化字段全部丢失。
- 占位符只能取自 `LogProperties`，且大小写必须一致（`UserId` ≠ `UserID`）。
- 占位符数量必须与实参数量严格一致。
- 浮点测量值带 `:0.####`，避免日志里出现 `12.300000000000001`。

这些规则由 `test/Framework.Logging.Tests` 中的反射测试在 CI 上强制校验。

### 第三层：强类型方法（推荐）

高频场景直接用扩展方法，参数数量与顺序由编译器保证：

```csharp
logger.LogRequestCompleted("GET", "/api/orders", 200, 12.5);
logger.LogRequestFailed("POST", "/api/orders", 500, 33.1, exception);
logger.LogDatabaseSlowCommand(sql, 1200);
logger.LogOutboundCallRetrying("https://pay.api", retryCount: 2, maxRetry: 3);
```

| 方法 | 级别 |
|------|------|
| `LogRequestCompleted` | 5xx → Error，其余 → Information |
| `LogRequestFailed` | 5xx → Error，4xx → Warning |
| `LogRequestSlow` / `LogDatabaseSlowCommand` / `LogOutboundCallFailed` / `LogOutboundCallRetrying` | Warning |
| `LogJobFailed` / `LogOperationFailed` | Error |
| `LogCacheHit` / `LogCacheMiss` / `LogDatabaseCommandCompleted` / `LogOutboundCallCompleted` | Debug |

### 作用域：业务维度自动带上

```csharp
using (logger.BeginOperationScope("CreateOrder", userId: currentUser.Id, tenantId: tenant))
{
    logger.LogInformation("校验库存");
    logger.LogInformation("写入订单");
    // 两条日志都自动带上 Operation / UserId / TenantId
}
```

---

## 输出目标

| 目标 | 说明 |
|------|------|
| 控制台（交互式） | `[HH:mm:ss LVL] SourceContext: 消息`，带颜色主题 |
| 控制台（重定向） | 自动补全时间戳、应用名、TraceId，并关闭 ANSI 颜色 |
| 文件（纯文本） | `时间戳 [LVL] 应用 [TraceId] SourceContext 消息`，按天 / 按体积滚动 |
| 文件（CLEF JSON） | 一行一条 JSON，Seq / Loki / Elasticsearch 可直接按字段检索 |

`JsonFileOutput = true` 时输出 CLEF：

```json
{"@t":"2026-10-31T00:05:01.62Z","@mt":"订单 {OrderId} 已创建","@l":"Warning","OrderId":12345,
 "SourceContext":"Order.Service","Application":"OrderService","TraceId":"40afb8c7...","EnvironmentName":"Production"}
```

> CLEF 会省略默认级别 `Information` 的 `@l` 字段以压缩体积，排查级别字段时请用 Warning 以上的日志验证。

---

## 配置项参考

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `Application` | 入口程序集名 | 注入到每条日志 |
| `ApplicationVersion` | 入口程序集版本 | 注入到每条日志 |
| `EnvironmentName` | `DOTNET_ENVIRONMENT` → `ASPNETCORE_ENVIRONMENT` → `Production` | 运行环境 |
| `MinimumLevel` | `Information` | 全局最低级别 |
| `LevelOverrides` | `Microsoft` = Warning | 按命名空间压制噪音 |
| `ConsoleEnabled` | `true` | 控制台输出 |
| `FileEnabled` | `false` | 文件输出 |
| `FilePath` | `logs/{Application}-.log` | `-` 由滚动周期替换；相对路径基于应用目录 |
| `JsonFileOutput` | `false` | `true` 输出 CLEF JSON |
| `RollingInterval` | `Day` | 滚动周期 |
| `FileSizeLimitBytes` | 50 MB | 单文件上限 |
| `RetainedFileCountLimit` | 31 | 保留文件数 |
| `AsyncWrite` | `true` | 异步写入，业务线程不等待 IO |
| `BlockWhenQueueFull` | `false` | 队列满时丢弃新日志保吞吐；`true` 则阻塞保日志完整性 |

---

## 从 NLog 迁移

`2.x` 及以前基于 NLog，`3.0.0` 起改为 Serilog，属于破坏性变更：

| NLog 用法 | Serilog 等价写法 |
|-----------|-----------------|
| `builder.Logging.AddNLogWeb()` | `builder.Logging.AddFrameworkSerilog(builder.Configuration)` |
| `NLog.config` | `appsettings.json` 的 `Serilog` 节（模板见包内 `appsettings.serilog.json`） |
| `${message}` | `{Message}` / `{Message:lj}` |
| `${event-properties:item=UserId}` | `{UserId}` |
| `${callsite}` | `{SourceContext}` |
| `${shortdate}` 滚动 | `rollingInterval: "Day"` + 文件名里的 `-` |

原来 README 中的 `AddFrameworkNLog()` 从未实现过，迁移时请勿沿用。

---

## 与 YLFramework.ZLogging 的区别

| 维度 | YLFramework.Logging | YLFramework.ZLogging |
|------|--------------------|----------------------|
| 后端 | Serilog | 自研 `ILoggerProvider` + Spectre.Console |
| 结构化落盘 | ✅ CLEF JSON，可按字段检索 | ❌ 纯文本 |
| 富终端渲染 | ❌ | ✅ 表格 / 面板 / 六边形转储 |
| 适用场景 | 后端服务、需要日志平台采集 | CLI、需要对象可视化转储 |

两者互不依赖，可按进程选择。

## 依赖

- `Serilog` / `Serilog.Extensions.Logging`
- `Serilog.Settings.Configuration`
- `Serilog.Sinks.Console` / `Serilog.Sinks.File` / `Serilog.Sinks.Async`
- `Serilog.Formatting.Compact`

## 版本

`3.0.0-alpha.1`，版本由本项目 csproj 的 `<Version>` 独立声明。
