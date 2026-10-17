# System.Text.Json.Serialization — JSON 转换器

## 文件

| 文件 | 类型 |
|------|------|
| `DateTimeJsonConverter.cs`       | `JsonConverter<DateTime>`（格式 `yyyy-MM-dd HH:mm:ss`，可自定义） |
| `DateTimeOffsetJsonConverter.cs` | `JsonConverter<DateTimeOffset>`（同上） |

## 外部依赖

- BCL `System.Text.Json`（.NET 8+ 自带）

## 复制最小子集

单文件 / 双文件均可独立使用。`Helpers/Helper.Json.cs` 默认注册这两个转换器，如不复制 Helper 需自行注册：

```csharp
options.Converters.Add(new DateTimeJsonConverter());
options.Converters.Add(new DateTimeOffsetJsonConverter());
```
