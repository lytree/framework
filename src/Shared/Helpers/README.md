# Helpers — `Helper` 静态类

`static partial class Helper` 由本目录下 20 个分部文件组成。**复制 `Helper` 类必须复制所有分部文件**，否则编译失败。

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
| `Helper.Pinyin.cs`      | `GetPinyin` / `GetPinyinInitials` / `GetPinyinFirstLetter` / `IsChinese`（汉字拼音，内嵌 2 万字拼音表） | 无 |
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

## 汉字拼音（`Helper.Pinyin.cs`）

单文件自包含：拼音表以 Deflate + Base64 内嵌在源码里，**无外部依赖、无资源文件**，首次调用时才解压（约 20 ms 冷启动，之后走静态缓存）。

```csharp
Helper.GetPinyin("中国2024");                        // "zhōng guó 2024"
Helper.GetPinyin("中国", PinyinFormat.ToneNumber);   // "zhong1 guo2"
Helper.GetPinyin("张三", PinyinFormat.Plain, "");    // "zhangsan"（可直接当排序 / 检索键）
Helper.GetPinyinInitials("中华人民共和国");          // "ZHRMGHG"
Helper.GetPinyinFirstLetter('中');                   // 'Z'
Helper.TryGetPinyin('中', out var pinyin);           // true / "zhōng"
Helper.IsChinese("汉字");                            // true
```

- 覆盖 **U+4E00–U+9FFF**（CJK 统一汉字基本区）20992 字，其中 20924 字有读音；扩展 A 区等冷僻字返回原字符。
- 数据来源 [mozillazg/pinyin-data](https://github.com/mozillazg/pinyin-data)（Unihan `kMandarin` 派生，Unicode License）。
- 每字 1 个 `ushort`：高 9 位音节序号（1 基，0 表示无数据）+ 低 3 位声调（1–4，0 表示轻声）；数组小端序打包 → Deflate → Base64。
- **只保留主读音**：多音字按最常用读音输出（「重」→ zhòng、「长」→ zhǎng），不做词组消歧，需要时请在调用侧覆盖。
- 重新生成表数据：`tools/gen-pinyin-table.py`（脚本头注释说明了数据文件与用法）。

## NuGet 包

按需引入：

```xml
<PackageReference Include="BouncyCastle.Cryptography" Version="2.7.0" />   <!-- SM4 -->
<PackageReference Include="SharpCompress" Version="0.49.1" />              <!-- Compressor -->
<PackageReference Include="SkiaSharp" Version="4.151.1" />                 <!-- Images -->
<PackageReference Include="System.Text.Json" Version="10.0.11" />          <!-- Json（可选） -->
```
