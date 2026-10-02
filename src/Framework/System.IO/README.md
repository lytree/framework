# System.IO — Stream 扩展

## 文件

| 文件 | 类型 |
|------|------|
| `DelegatingStream.cs`      | `DelegatingStream`（流包装基类） |
| `PooledMemoryStream.cs`    | `PooledMemoryStream`（基于 ArrayPool 的 MemoryStream） |
| `System.Stream.cs`         | `Stream` 扩展方法（`Fill<T>`/`Resize` 在 `System.Extensions.cs` 的 `ArrayExtensions` 中） |

## 外部依赖

- 无
- `PooledMemoryStream` 引用 `System.Buffers/ArrayOwner<T>`（来自 `System.Buffers/` 目录）

## 复制最小子集

如果只需要 `PooledMemoryStream`，复制：

```
PooledMemoryStream.cs                # 强依赖 System.Buffers/ArrayOwner
```

如果需要 `DelegatingStream`：

```
DelegatingStream.cs
PooledMemoryStream.cs
```
