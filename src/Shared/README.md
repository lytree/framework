# Shared — 共享源码目录

通用 .NET 工具类集合 —— 字符串、集合、IO、缓冲、反射、并发、JSON、加密、压缩、图片、HTTP / SOCKS 代理等。

**这里不是独立项目，也不产出程序集。** 各库在 csproj 中用 `<Compile Include>` 直接引入所需文件编译：

```xml
<ItemGroup>
  <Compile Include="..\Shared\Helpers\Helper.Json.cs" Link="Shared\Helpers\Helper.Json.cs" />
  <Compile Include="..\Shared\System.Collections.Generic\*.cs" Link="Shared\System.Collections.Generic\%(Filename)%(Extension)" />
</ItemGroup>
```

**命名空间规则：Shared 里的文件一律不声明 namespace**，类型直接落在全局命名空间（`Helper`、`Extensions`、`ContentType`、`Stopwatcher`…）。因此：

- 引入后**不需要** `using`，直接调用即可（`Helper.JsonSerialize(...)`、`"a".ToPath()`）
- 为免同名冲突，扩展类按文件区分为 `Extensions` / `ListExtensions` / `CollectionExtensions` / `StreamExtensions` / `ReflectionExtensions` 等
- `Extension.In(...)`（DateTime 区间判断）与 `RangeMode` 已并入 `Helper.In`，不再保留重复副本

**可见性规则：所有顶层类型一律 `internal`。** 同一份文件会被多个包各自内联编译，若保持 `public`，下游同时引用这些包时会出现 `CS0433`（类型存在于多个程序集）。改为 `internal` 后每个包都有一份私有副本，互不冲突，也不出现在包的公开 API 中。

> 例外处理：若某个包确实需要在公开签名中暴露这些类型，应把该类型在**该包内**单独复制一份并改回 `public`，而不是放开 Shared 源文件。

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

共享源码随引用它的包一起发布，自身不再单独设版本。
