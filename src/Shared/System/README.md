# System — BCL 类型扩展

`System.Extensions`（复数）和 `System.Extension`（单数，DateTime专用）两个 static partial 类。

## 文件

| 文件 | 类型（全局命名空间） | 说明 |
|------|--------------|------|
| `NullObject.cs`           | `readonly record struct NullObject<T>`       | 可空值包装 |
| `System.Extensions.cs`     | `static partial class Extensions`            | 单一文件，包含 Array / Convert / DateTime / Exception / Object / String / Json / 转换 / 链式 / 字典化 等所有扩展方法；另含 `ArrayExtensions` |
| `System.Extension.cs`     | `static partial class Extension`（**单数**） | DateTime 专用：`ToMilliseconds` / `ToSeconds` / `ToMicroseconds` / `GetDayMinDate` 等 |
| `System.Enum.cs`          | `static class EnumExtension`       | `GetDescription` / `ToNameWithDescription` / `ToInt64` / `ToList` |

> 历史遗留：`Extension`（单数）与 `Extensions`（复数）是两个不同的类。
> 调用方通过扩展语法访问，对调用无感知。
> 原先 `Extension.In(...)` 与 `Helper.In(...)` 签名重复，全局命名空间下会二义，已统一保留 `Helper.In`（含 `RangeMode`）。

## 外部依赖

- 无（纯 BCL）
- `System.Enum.cs` 中的 `string.IsNull()` 来自 `System.Extensions.cs`（`Extensions.IsNull`）

## 复制最小子集

如果只需要 BCL 扩展（不要 Enum 扩展）：

```
NullObject.cs
System.Extensions.cs
System.Extension.cs
```

## 注意事项

- `System.Extensions.cs` 内含 `ArrayExtensions` 类（含 `Fill<T>`、`Resize`）
- `System.Extension.cs`（单数）依赖 `Helper.TimestampStart`（`Helpers/Helper.DateTime.cs`），若不复制 Helper，需自行修改或删除对应方法
