# System.Buffers — 缓冲池与 BufferWriter

纯 BCL 实现的高性能缓冲池工具。

## 文件

| 文件 | 类型 |
|------|------|
| `IArrayOwner.cs`             | `IArrayOwner<T>`（IDisposable） |
| `ArrayOwner.cs`              | `ArrayOwner<T>` 实现 |
| `IWrittenBufferWriter.cs`    | `IWrittenBufferWriter<T>`（IBufferWriter<T> + 已写入数据访问） |
| `ArrayPoolBufferWriter.cs`   | `ArrayPoolBufferWriter<T>` 实现 |
| `BufferReader.cs`            | `BufferReader<T>` 读端 |
| `PooledByteBuf.cs`           | `PooledByteBuf`（Reentrant 锁缓冲） |
| `System.ArrayPool.cs`        | `ArrayPool<T>.RentArrayOwner<T>(int length)` 扩展 |
| `System.BufferWriter.cs`     | `IBufferWriter<T>` 扩展方法 |

## 外部依赖

- 无

## 复制最小子集

整套使用（8 个文件）：
```
IArrayOwner.cs
ArrayOwner.cs
IWrittenBufferWriter.cs
ArrayPoolBufferWriter.cs
BufferReader.cs
PooledByteBuf.cs
System.ArrayPool.cs
System.BufferWriter.cs
```

如果只需要 `PooledMemoryStream`（在 `System.IO`），可不复制本目录。
