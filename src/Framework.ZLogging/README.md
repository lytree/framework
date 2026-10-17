# YLFramework.ZLogging

基于 [Spectre.Console](https://spectreconsole.net/) 的彩色控制台日志 + 异步文件输出扩展。**不依赖 ZLogger**，
`ILoggerProvider` / `ILogger` 均为自研实现，仅依赖 `Microsoft.Extensions.Logging` 与 `Spectre.Console`。

## 功能概览

| 文件 | 类型 | 作用 |
|------|------|------|
| `SpectreLogOptions.cs` | `SpectreLogOptions` | 配置项：时间格式、级别颜色、队列策略、文件路径 / 滚动 / 编码 / 刷盘、专用格式注册 |
| `SpectreConsoleLoggerProvider.cs` | `SpectreConsoleLoggerProvider` / `SpectreLogProcessor` / `SpectreLogger` | `ILoggerProvider` + `ISupportExternalScope`；后台单消费者统一处理控制台与文件双输出 |
| `SpectreLogEntry.cs` | `SpectreLogEntry` / `SpectreMessageFormatter` | 调用线程上的结构化参数快照；消息模板解析与专用格式切分 |
| `FileLogWriter.cs` | `FileLogWriter` | 长连接 `FileStream` + 批量写 + 按体积滚动 + 断连重连 + IO 退避重试 |
| `SpectreObjectPrinter.cs` | `SpectreObjectPrinter` | 任意对象 → 表格 / 树 / JSON / 十六进制；循环引用与深度保护 |
| `SpectreLogFormats.cs` | 内置格式注册 | `{Value:table}`、`{Value:panel}`、`{Value:json}` 等指令的默认实现 |
| `SpectreLoggingExtensions.cs` | `SpectreLoggingExtensions` / `SpectreOutputExtensions` | 注册扩展 + `Write` / `WriteLine` / `Tree` / `Panel` / `Progress` 等控制台输出方法 |

## 快速上手

```csharp
// 1) 仅控制台
builder.Logging.AddSpectreConsole(options =>
{
    options.TimeFormat = "HH:mm:ss.fff";
    options.IncludeScopes = true;
});

// 2) 控制台 + 文件（异步批量写）
builder.Logging.AddSpectreConsoleAndFile("logs/app.log", options =>
{
    options.FileSizeLimitBytes = 64 * 1024 * 1024; // 单文件上限，超过后滚动
    options.MaxRollingFiles = 10;                  // 最多保留 10 个文件（含基文件）
    options.FileAppend = true;
});

// 3) 纯文件（CLI / 服务端后台日志）
builder.Logging.AddSpectreFile("logs/app-{Date}.log", options =>
{
    options.ConsoleOutput = false;
});
```

## 文件写入方案

| 关注点 | 做法 | 原因 |
|--------|------|------|
| 写入模式 | `FileOptions.Asynchronous \| SequentialScan` | 走异步 IO 路径并提示顺序访问，让 OS 合并写 |
| 共享模式 | `FileShare.ReadWrite \| Delete` | `tail -f` / VS 可实时读取；logrotate 可在写入期间搬走文件 |
| 刷盘 | 批末或定时器（默认 1s），不用 `AutoFlush` | 每条都 flush 会把吞吐拖垮一个数量级 |
| 批量 | 单批最多 100 条，一次 `WriteAsync` | 摊薄系统调用次数 |
| 滚动 | 超过 `FileSizeLimitBytes` 切到 `_1`、`_2`…，序号循环覆盖 | 磁盘占用有上界；基文件永不删除 |
| 断连 | 每 100 次写入检查文件是否还在，不在则重连 | 外部删除 / 移动后句柄会静默失效 |
| 瞬时错误 | `IOException` 线性退避重试 3 次 | 抗住 logrotate 搬文件时的短暂冲突 |

生命周期由 DI 容器托管：容器释放时调用 `DisposeAsync` 排空队列并关闭文件句柄。
**注意**：不要用 `builder.AddProvider(new SpectreConsoleLoggerProvider(...))` 传入外部实例——
`LoggerFactory` 不负责释放外部实例，日志文件句柄会一直占用到进程结束。

## 自定义指令（消息模板专用格式）

在消息模板里用 `{参数:指令}` 指定渲染方式：

```csharp
logger.LogInformation("订单 {Order:table}", order);        // 表格
logger.LogInformation("详情 {Order:panel}", order);       // 面板
logger.LogInformation("原始报文 {Raw:json}", payload);     // 缩进 JSON
logger.LogInformation("结构体 {Data:obj}", data);          // 对象转储
logger.LogInformation("内存 {Bytes:hex}", bytes);         // 十六进制
logger.LogInformation("异常 {Ex:exception}", ex);         // Spectre 异常视图
```

内置指令：`obj` `table` `dump` `dumptext` `array` `dictionary` `tuple` `datatable` `dataset`
`enum` `guid` `time` `type` `task` `lazy` `vertical` `horizontal` `json` `text` `markup`
`panel` `tree` `hex` `base64` `exception`

自定义自己的指令：

```csharp
options.RegisterFormat("money", (value, printer) => new Text($"￥{value:N2}"));
options.RegisterFormat("avatar", (value, printer) => LoadImage((string)value!));

logger.LogInformation("应付 {Amount:money}", 1234.5);   // ￥1,234.50
```

> ⚠️ **集合实参需显式装箱**：`logger.LogInformation("{Data:table}", array)` 会走
> `params object[]` 展开，把数组拆成多个占位符。要传整个集合请写
> `logger.LogInformation("{Data:table}", (object)array)`。

专用格式在控制台渲染为表格 / 面板，在日志文件中退化为等宽纯文本——信息不丢失，但控制台的
ANSI 颜色不会写入文件。

## 性能设计

- **调用线程只做快照**：结构化参数在 `ILogger.Log` 内同步物化为不可变条目，
  避免把 `TState` 引用泄漏到后台线程（`Log` 一返回调用方即可改写状态对象）。
- **后台单消费者**：消费队列固定 1 个读取者，控制台输出天然串行。
  这点很关键——`AnsiConsole` 本身并非线程安全，单消费者省掉了一层锁和一处死锁风险。
- **队列满不阻塞**：默认 `BoundedChannelFullMode.DropWrite`，丢弃新消息并立即返回，
  绝不阻塞业务线程。日志不允许丢失时可改为 `Wait` 让生产者等待。
- **日志失败不影响业务**：所有异常在内部捕获并交给 `InternalErrorLogger`，绝不冒泡打断业务代码。

## 配置项速查

| 分类 | 属性 | 默认值 |
|------|------|--------|
| 通用 | `TimeFormat` | `yyyy-MM-dd HH:mm:ss` |
| | `LogLevelColors` | 各级别默认配色（`FrozenDictionary`） |
| | `IncludeScopes` | `false` |
| | `TimeProvider` | `TimeProvider.System` |
| | `InternalErrorLogger` | `null` |
| 队列 | `QueueCapacity` | `12000` |
| | `QueueFullMode` | `DropWrite` |
| | `BatchSize` | `100` |
| 控制台 | `ConsoleOutput` | `true` |
| | `ConsoleRenderWidth` | `0`（跟随终端） |
| 文件 | `FilePath` | `null`（支持 `{Date}` 占位） |
| | `FileAppend` | `true` |
| | `FileEncoding` | 无 BOM 的 UTF-8 |
| | `FileSizeLimitBytes` | `0`（不滚动） |
| | `MaxRollingFiles` | `0`（不限制） |
| | `FlushInterval` | 1 秒 |
| | `FileRenderWidth` | `0`（不折行） |
| | `ShutdownTimeout` | 5 秒 |

## 版本

`2.0.0-alpha.1`，版本由本项目 csproj 的 `<Version>` 独立声明。
