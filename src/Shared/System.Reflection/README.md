# System.Reflection — 反射扩展

## 文件

| 文件 | 类型 |
|------|------|
| `System.MethodInfo.cs`    | `MethodInfo` 扩展：`HasAttribute<T>` / `GetAttribute<T>` / `IsAsync` / `GetReturnType` |
| `System.Reflection.cs`    | 大批量反射辅助方法（属性读写、`Merge`、`CopyProperties` 等） |

## 外部依赖

- 无

## 复制最小子集

整套复制：

```
System.MethodInfo.cs
System.Reflection.cs
```

`System.Reflection.cs` 部分方法依赖 `System.Extensions` 的 `IsDefaultValue` / `ToDictionary` / `SetProperty` 等。
