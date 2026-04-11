# AnsiConsole 文件输出扩展设计

## 概述

为 Framework.Cli 项目扩展 Spectre.Console 的 AnsiConsole，添加同时输出到文件和终端的能力。

## 功能需求

1. **双写输出**：写入时同时输出到 console 和文件
2. **前后缀**：支持自定义前缀和后缀，默认为空
3. **同步写入**：每次写入后同步刷新到文件
4. **错误处理**：写入失败时抛出异常

## API 设计

```csharp
public class AnsiConsoleWriter : IAnsiConsole
{
    public static AnsiConsoleWriter Create(IAnsiConsole console, string filePath, string? prefix = null, string? suffix = null);
    
    // 实现 IAnsiConsole 所有接口方法，透传到原 console + 文件
    // 每个写入方法支持 writeToFile 参数控制是否写入文件
}
```

```csharp
var writer = AnsiConsoleWriter.Create(AnsiConsole.Console, "output.log", prefix: "[", suffix: "]");

writer.WriteLine("Hello"); // 仅输出到 console
writer.WriteLine("Hello", writeToFile: true); // 同时输出到 console 和文件
```

## 实现细节

- **接口**：实现 `IAnsiConsole` 接口，透传所有成员到原始 console
- **文件写入**：每次写入时创建新的 FileStream，写入后关闭（保证数据落盘）
- **线程安全**：使用 `lock` 保护文件写入操作
- **异常**：文件写入失败时抛出 `IOException`

## 验收标准

1. `Create` 方法返回的对象可以正常使用所有 IAnsiConsole 方法
2. 写入内容同时出现在 console 和指定文件
3. 前后缀正确添加到文件输出
4. 文件写入失败时抛出异常
5. 单元测试覆盖核心功能