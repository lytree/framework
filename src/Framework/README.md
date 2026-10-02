# Framework 核心 (`YLFramework`)

通用 .NET 工具类集合 —— 字符串、集合、IO、缓冲、反射、并发、JSON、加密、压缩、图片、HTTP / SOCKS 代理等。

## 拆分文档

- [MODULES.md](./MODULES.md)：模块总览、依赖关系、复制作业流程
- 各子目录 `README.md`：单个模块的依赖清单与使用说明

## 快速复制指引

| 我想要… | 复制这些模块 |
|---------|-------------|
| 字符串 / 集合 / 反射 / Threading 扩展 | `System/`、`System.Collections.Generic/`、`System.Reflection/`、`System.Threading/` |
| JSON 序列化 | `System.Text.Json.Serialization/` + `Helpers/Helper.Json.cs` |
| MD5 / SHA / DES 加密 | `Helpers/Helper.Hash.cs` + `Helpers/Helper.{MD5,SHA,DES}Encrypt.cs` |
| SM4 国密 | 上述 + `Helpers/Helper.SM4Encrypt.cs` + `BouncyCastle.Cryptography` 包 |
| 图片合并 (PNG) | `Helpers/Helper.Images.cs` + `SkiaSharp` 包 |
| 压缩 / 解压 (Zip/Rar/7z) | `Helpers/Helper.Compressor.cs` + `System.IO/PooledMemoryStream.cs` + `System.Collections.Generic/DisposableDictionary.cs` + `SharpCompress` 包 |
| SOCKS5 / HTTP 代理 | `Proxy/*` 整目录 |
| 身份证 / 手机号 / 邮箱校验 | `Helpers/Helper.Validator.cs` |

详细依赖见各模块 README。

## 版本

`1.0.x`，遵循 GitVersion。
