# System.Collections.Generic — 集合扩展

## 文件

| 文件 | 提供的类型 |
|------|-----------|
| `Tree.cs` / `ITree.cs` / `TreeExtensions.cs` | 树形结构：`Tree<T>` / `ITree` / `TreeExtensions` |
| `NullableDictionary.cs` | `NullableDictionary<TKey, TValue>` |
| `DisposableDictionary.cs` | `DisposableDictionary<TKey, TValue>`（IDisposable） |
| `System.Collection.cs` / `System.Dictionary.cs` / `System.Enumerable.cs` | `partial class Extensions`（集合扩展） |
| `System.List.cs` | `Framework.System.Collections.Generic.Extensions.ToTree` / `Clone<T>` |

## 外部依赖

- 无（纯 BCL）
- `System.List.cs` 依赖 `Helper.JsonSerialize` / `Helper.JsonDeserialize`（`Helpers/Helper.Json.cs`）

## 复制最小子集

如果只需要 `Tree<T>` 系列：

```
Tree.cs
ITree.cs
TreeExtensions.cs
```

如果需要完整集合扩展：

```
Tree.cs
ITree.cs
TreeExtensions.cs
NullableDictionary.cs
DisposableDictionary.cs
System.Collection.cs
System.Dictionary.cs
System.Enumerable.cs
System.List.cs                # 依赖 Helper.JsonSerialize / Helper.JsonDeserialize
```
