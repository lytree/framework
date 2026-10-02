# Helpers — `Helper` 静态类

`static partial class Helper` 由本目录下 19 个分部文件组成。**复制 `Helper` 类必须复制所有分部文件**，否则编译失败。

## 文件清单

| 文件 | 提供的 API | 外部依赖 |
|------|----------|----------|
| `Helper.Assembly.cs`    | `GetAssemblyList`                                    | 无 |
| `Helper.Attribute.cs`   | `GetAttributeDisplayName` / `GetAttributeDisplay`    | 无 |
| `Helper.Compressor.cs`  | `Zip` / `Decompress` / `ZipStream`（压缩/解压）       | `SharpCompress` + `PooledMemoryStream` + `DisposableDictionary` |
| `Helper.Convert.cs`     | `ToBase32String` / `FromBase32String`（Base32/62 编解码） | 无 |
| `Helper.DateTime.cs`    | 时间戳、DateTimeRange、GetWeekOfYear 等              | 依赖 `System.Extension.ToMilliseconds` 等 |
| `Helper.DESEncrypt.cs`  | `DESEncrypt` / `DESDecrypt`（已过时）                | 无 |
| `Helper.Encoding.cs`    | `ToHex` / `ToBase64` / `StringToUnicode` 等          | 无 |
| `Helper.Entity.cs`      | `GetEntityPropertyNamesByAttribute` / `IsImplementInterface` | 无 |
| `Helper.File.cs`        | `CreateTempFile` / `ClearTempFiles`                  | 无 |
| `Helper.Hash.cs`        | `ComputeSha256Hash` / `ComputeSha384Hash` / `ComputeSha512Hash` | 无 |
| `Helper.Images.cs`      | `VerticalMergeImageByte` / `VerticalMergeImageStream`（PNG 纵向拼接） | `SkiaSharp` |
| `Helper.Interface.cs`   | `GetInterfacePropertyNames<T>`                       | 无 |
| `Helper.Json.cs`        | `JsonSerialize` / `JsonDeserialize`                  | 依赖 `System.Text.Json.Serialization/DateTimeJsonConverter.cs` |
| `Helper.MD5Encrypt.cs`  | `MD5Encrypt16/32/64` / `GetHash(Stream)`             | 无 |
| `Helper.SHAEncrypt.cs`  | `SHA1Hash`                                           | 无 |
| `Helper.SM4Encrypt.cs`  | `SM4Encrypt` / `SM4Decrypt`（国密 SM4）              | `BouncyCastle.Cryptography` |
| `Helper.String.cs`      | `GenerateRandom` / `GenerateRandomNumber` / `Format` | 无 |
| `Helper.Type.cs`        | `IsAnonymousType`                                    | 无 |
| `Helper.Validator.cs`   | 身份证 / 手机号 / 邮箱 / 电话 / 邮编等正则校验        | 无 |

## 跨分部依赖（必须一起复制）

| 文件 | 依赖的其它分部 |
|------|----------------|
| `Helper.MD5Encrypt.cs`  | `Helper.Encoding.cs`（用 `ToHex`/`ToBase64`） |
| `Helper.SHAEncrypt.cs`  | `Helper.Encoding.cs`（用 `ToHex`/`ToBase64`） |
| `Helper.DESEncrypt.cs`  | `Helper.Encoding.cs`（用 `ToHex`/`ToBase64`/`HexToBytes`） |
| `Helper.DateTime.cs`    | `System.Extension.cs`（用 `ToMilliseconds`） |
| `Helper.Json.cs`        | `System.Text.Json.Serialization/DateTimeJsonConverter.cs` |
| `Helper.Compressor.cs`  | `System.IO/PooledMemoryStream.cs`、`System.Collections.Generic/DisposableDictionary.cs` |

## 复制最小子集

若只需要加密工具（不要压缩、不要图片），至少复制：

```
Helper.Assembly.cs         （可选，其它分部通过反射也常用）
Helper.Encoding.cs          （必选，被 MD5/SHA/DES 依赖）
Helper.MD5Encrypt.cs
Helper.SHAEncrypt.cs
Helper.DESEncrypt.cs
Helper.Hash.cs
Helper.SM4Encrypt.cs       （可选，引入 BouncyCastle）
```

## NuGet 包

按需引入：

```xml
<PackageReference Include="BouncyCastle.Cryptography" Version="2.7.0" />   <!-- SM4 -->
<PackageReference Include="SharpCompress" Version="0.49.1" />              <!-- Compressor -->
<PackageReference Include="SkiaSharp" Version="4.151.1" />                 <!-- Images -->
<PackageReference Include="System.Text.Json" Version="10.0.11" />          <!-- Json（可选） -->
```
