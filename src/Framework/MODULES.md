# Framework 核心模块拆分说明

本目录是 `YLFramework` 包的核心代码（`Framework` 项目），按职责拆分为以下可独立复制的模块。

## 拆分原则

1. **单一职责**：每个文件夹/模块围绕一个职责域组织
2. **依赖最小化**：模块内文件尽量自包含；跨模块依赖通过 README 显式声明
3. **可直接复制**：除特别声明外，每个模块内的 `.cs` 文件均可直接复制到目标项目使用
4. **公开 API 不变**：拆分不修改任何命名空间、类名或方法签名

## 模块总览

| 模块 | 路径 | 外部 NuGet 依赖 | 可独立复制 |
|------|------|-----------------|-----------|
| **Core**              | `UnsupportedException.cs`、`System/NullObject.cs`、`Utils/Stopwatcher.cs` | 无 | ✅ |
| **Extensions**        | `System/System.Extensions.cs` + `System/System.Extension.cs` | 无 | ✅（需配套 System.IO / System.Collections.Generic） |
| **Collections**       | `System.Collections.Generic/*` | 无 | ✅ |
| **Buffers**           | `System.Buffers/*` | 无 | ✅ |
| **Reflection**        | `System.Reflection/*` | 无 | ✅ |
| **Threading**         | `System.Threading/*` | 无 | ✅ |
| **Xml**               | `System.Xml/*` | 无 | ✅ |
| **IO**                | `System.IO/*` | 无 | ✅ |
| **Pipelines**         | `System.IO/*` + `System.IO.Pipelines/*` | `System.IO.Pipelines` | ✅ |
| **Net**               | `Net/*` | 无 | ✅ |
| **Mime**              | `Mime/*` | 无 | ✅ |
| **Helper**            | `Helpers/Helper.*.cs`（partial class） | 部分文件需要 BCL 之外 | ⚠️ 见各 Helper README |
| **Cryptography**      | `Helpers/Helper.{MD5,SHA,DES,SM4}Encrypt.cs` + `Helper.Hash.cs` | `BouncyCastle.Cryptography`（仅 SM4） | 部分 ✅ |
| **Json**              | `Helpers/Helper.Json.cs` + `System.Text.Json.Serialization/*` | 无（BCL） | ✅ |
| **Imaging**           | `Helpers/Helper.Images.cs` | `SkiaSharp` | ✅ |
| **Compression**       | `Helpers/Helper.Compressor.cs` | `SharpCompress` | ⚠️ 强依赖 `PooledMemoryStream` + `DisposableDictionary` |
| **Proxy**             | `Proxy/*` | 无 | ✅ |

## Helper 分部类说明

`static partial class Helper` 由约 18 个 `Helpers/Helper.*.cs` 分部文件构成。

**使用 Helper 类时必须把所有分部文件一起复制**，否则编译失败。各分部文件按职责分类如下：

| 分部文件 | 类别 | 外部依赖 |
|----------|------|----------|
| `Helper.Assembly.cs`    | 程序集 | 无 |
| `Helper.Attribute.cs`   | 特性   | 无 |
| `Helper.Compressor.cs`  | 压缩   | `SharpCompress` + `System.IO/PooledMemoryStream` |
| `Helper.Convert.cs`     | 进制转换（Base32 / Base62） | 无 |
| `Helper.DateTime.cs`    | 时间 / 时间段 | 无（依赖 `System.Extension.ToMilliseconds` 等） |
| `Helper.DESEncrypt.cs`  | DES 加密（已过时） | 无 |
| `Helper.Encoding.cs`    | Hex / Base64 / Unicode | 无 |
| `Helper.Entity.cs`      | 实体类反射 | 无 |
| `Helper.File.cs`        | 临时文件 | 无 |
| `Helper.Hash.cs`        | SHA-256/384/512 | 无 |
| `Helper.Images.cs`      | 图片合并（PNG） | `SkiaSharp` |
| `Helper.Interface.cs`   | 接口反射 | 无 |
| `Helper.Json.cs`        | JSON 序列化 | 依赖 `System.Text.Json.Serialization/DateTimeJsonConverter.cs` |
| `Helper.MD5Encrypt.cs`  | MD5 加密 | 无 |
| `Helper.SHAEncrypt.cs`  | SHA1 加密 | 无 |
| `Helper.SM4Encrypt.cs`  | SM4 加密 | `BouncyCastle.Cryptography` |
| `Helper.String.cs`      | 字符串工具 | 无 |
| `Helper.Type.cs`        | 匿名类型判断 | 无 |
| `Helper.Validator.cs`   | 身份证 / 手机号 / 邮箱校验 | 无 |

> **复制建议**：先只复制所需的 Helper 分部文件 + `System.Extensions.cs`、`System.Extension.cs`，再视情况加入其它模块。

## 模块依赖关系

```
                    ┌──────────────┐
                    │     Core     │  (零依赖)
                    └──────────────┘
                           ▲
       ┌───────────────────┼───────────────────┐
       │                   │                   │
┌─────────────┐    ┌───────────────┐    ┌─────────────────┐
│ Collections │    │   Extensions  │    │     Buffers     │
└─────────────┘    └───────────────┘    └─────────────────┘
                           ▲
       ┌───────────────────┼───────────────────┐
       │                   │                   │
┌─────────────┐    ┌───────────────┐    ┌─────────────────┐
│   Helper    │───▶│      Json     │    │  Pipelines / IO │
└─────────────┘    └───────────────┘    └─────────────────┘
       │                                       ▲
       ├────────────▶ Cryptography              │
       ├────────────▶ Compression ──────────────┘
       ├────────────▶ Imaging
       └────────────▶ (无外部依赖类)
```

## 复制作业流程

要把某个模块复制到目标项目：

1. 复制该模块文件夹下的所有 `.cs` 文件
2. 如果模块依赖其它模块（见 README），同时复制那些模块
3. 在目标项目 `.csproj` 中添加对应 NuGet 包
4. 确保目标项目启用了 `<Nullable>enable</Nullable>` 与 `<ImplicitUsings>enable</ImplicitUsings>`（与本项目一致）

各模块的详细依赖见各自目录下的 `README.md`。
