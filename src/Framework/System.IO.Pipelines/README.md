# System.IO.Pipelines — Pipe 适配

## 文件

| 文件 | 类型 |
|------|------|
| `DelegatingDuplexPipe.cs` | `DelegatingDuplexPipe`（IDuplexPipe 包装） |

## 外部依赖

- **`System.IO.Pipelines`** NuGet 包（≥ 8.0）
- 依赖 `System.IO/DelegatingStream.cs` 与 `System.Threading/TaskExtensions.cs`

## csproj 引用

```xml
<PackageReference Include="System.IO.Pipelines" Version="10.0.11" />
```

## 复制最小子集

```
DelegatingDuplexPipe.cs        # 需先复制 System.IO/DelegatingStream.cs + System.Threading/TaskExtensions.cs
```

`System.IO/DuplexPipeStream.cs`（同样依赖 Pipelines）也在本目录下；如使用请一并复制。
