# Shared 死代码与 CPM 死依赖清理说明

日期：2026-11-07
范围：`src/Shared` 引用关系核实与死代码判定、`Directory.Packages.props` 死依赖核实。
未改其他包功能代码、未改命名空间、未新增依赖、未改 README/AGENTS、未提交 git、未打包推送。

## 一、Shared 引用关系核实方法

不使用文件名相似度，改为脚本解析各 csproj 的 `<Compile Include="..\Shared\...">` 实际条目：

- 显式条目按完整路径 `Test-Path` 解析；
- 通配条目（`System.Buffers\*.cs` 等）按目录 + 过滤模式展开成实际文件；
- 覆盖 `src/**`、`test/**` 的全部 csproj，并排除 `bin/obj`。

结果：`src/Shared` 共 **73** 个 `.cs`，被至少一个项目引用 **38** 个，**35** 个零引用；删除 1 个后为 72 / 38 / 34。

### 1.1 各包引入明细（按实际 csproj 条目）

| 项目 | 引入的 Shared 文件 |
|------|--------------------|
| `Framework.AspNetCore` | `Helpers/Helper.Json.cs`；`System.Text.Json.Serialization/DateTimeJsonConverter.cs`、`DateTimeOffsetJsonConverter.cs`（3） |
| `Framework.AspNetCore.Middleware` | `System.Buffers/*`（8）、`System.IO/*`（4）、`System.IO.Pipelines/DelegatingDuplexPipe.cs`（1）、`System.Threading/*`（2），共 15 |
| `Framework.Modbus` | `System.Buffers/PooledByteBuf.cs`（1） |
| `Framework.Repository` | `Helpers/{Entity,Interface,Json,Encoding,DateTime}`（5）、`System/*`（4）、`System.Reflection/*`（2）、`System.Collections.Generic/*`（9）、`System.Convert/*`（1）、`System.Text.Json.Serialization/*`（2），共 23 |
| `test/Framework.Tests` | `System.Buffers/{ArrayOwner,IArrayOwner,System.ArrayPool,PooledByteBuf}.cs`、`System.IO/PooledMemoryStream.cs`（5） |

## 二、零引用文件判定

### 2.1 删除（确认死代码）：1 个

| 文件 | 判定依据 |
|------|----------|
| `src/Shared/Helpers/Helper.cs` | 文件内容仅一行注释「此文件已删除。Helper 由其它 partial 文件聚合，不再需要空壳入口。」；不在任何 `<Compile Include>`；`MODULES.md` 的 Helper 分部类清单也不含它；无 `#if` / `Link` 别名 / 测试引用 |

删除安全性：该文件无任何编译入口（零引用 + 全仓库无 `Compile` 命中 + 内容为纯注释），删除后构建通过（见第五节）。

### 2.2 保留（预留/未接线）：34 个

判定依据：`src/Shared/MODULES.md` 与各目录 `README.md` 明确把 Shared 定义为「**可独立复制的源码模板库**」，这些文件都属于有文档的模块，属于「预留/未接线（刻意保留供复制）」而非历史残留。

| 分组 | 文件（数量） | 文档依据 |
|------|--------------|----------|
| Core | `UnsupportedException.cs`、`Utils/Stopwatcher.cs`（2） | `MODULES.md` Core 行；`Utils/README.md` |
| Helper 分部类（未被当前包挑选） | `Helper.{Assembly,Attribute,Compressor,Convert,DESEncrypt,File,Hash,Images,MD5Encrypt,SHAEncrypt,SM4Encrypt,String,Type,Validator}.cs`（14） | `MODULES.md`「Helper 分部类说明」「Cryptography / Imaging / Compression」 |
| Mime | `Mime/ContentType.cs`（1） | `MODULES.md` Mime 行 |
| Net | `Net/Buffer.cs`（1） | `MODULES.md` Net 行 |
| Proxy | `Proxy/{ArrayBuilder,ArrayUtils,Crc16,Crc32,Lrc,UrlParser}.cs` + `Proxy/Proxy/*`（9）（15） | `Proxy/README.md` 文件表与「复制最小子集」逐项列出 |
| Xml | `System.Xml/XmlExtensions.cs`（1） | `MODULES.md` Xml 行 |

补充说明：`Helper.{SM4Encrypt,Compressor,Images}.cs` 分别依赖 BouncyCastle / SharpCompress / SkiaSharp，但因为**没有任何包编译它们**，目前不产生实际依赖（对应第三节的 CPM 判定）。是否要整体删除这些「当前无人使用、仅供复制」的模块属于产品/文档层面的取舍，本轮按「不自行拍板删除有文档的模块」保留，待后续决定。

## 三、重复编译（跨包同一文件）分析

`src` 侧的跨包重复共 4 个（测试工程另有一份重复，属测试夹具用途，不影响包）：

| 文件 | 编译进 | 是否必要 | 结论 / 风险 |
|------|--------|----------|-------------|
| `Helpers/Helper.Json.cs` | AspNetCore、Repository | **必要** | AspNetCore 的 `ApiExceptionMiddleware` 调用 `Helper.JsonSerialize`；Repository 的 `AbstractSyncData` 调用 `Helper.JsonDeserialize`。两包都真实使用，无法合并到单一使用方（包之间不互相引用共享程序集） |
| `System.Text.Json.Serialization/DateTimeJsonConverter.cs` | AspNetCore、Repository | **必要** | `Helper.Json.cs` 静态构造中注册 `new DateTimeJsonConverter()`；凡编译 `Helper.Json.cs` 的包都必须同时带该转换器 |
| `System.Text.Json.Serialization/DateTimeOffsetJsonConverter.cs` | AspNetCore、Repository | **必要** | 同上（`Helper.Json.cs` 同时注册两个转换器） |
| `System.Buffers/PooledByteBuf.cs` | Middleware、Modbus | Modbus **必要**；Middleware 为通配附带 | Modbus 传输层（`ModbusTransport`/`ModbusIpTransport`/`ModbusAsciiTransport`/`ModbusMessage`）大量使用；Middleware 无直接引用，仅因 `<Compile Include="..\Shared\System.Buffers\*.cs">` 被带入 |

保留理由与风险：

- 重复是当前共享机制（文件级内联、包间不引用公共程序集）的必然结果；类型均为 `internal`，同一进程同时加载两个包不会 CS0433。
- 如需消除 Middleware 侧的 `PooledByteBuf` 重复，只能加 `<Compile Remove>` 单文件排除，会让 csproj 与 Shared 文件名强耦合、收益仅减小编译量，本轮不做。
- 长期风险（既有）：若把 Shared 顶层类型改为 `public` 以满足公开签名，会立即在同时引用两个包的下游触发 CS0433；`MODULES.md` 只有文字约定，没有 CI 校验。

## 四、可见性问题

- 对 `src/Shared/**/*.cs` 检索行首 `public` 顶层类型声明：**无匹配**，顶层类型全部 `internal`，符合约定。
- 存在 `public` 成员嵌在 `internal` 外层类型中（如 `internal partial class PooledByteBuf` 的公开构造/方法、`internal static partial class Helper` 内嵌的 `DateTimeRange` 等），对外不可见，不构成可见性问题，本轮未改。

## 五、CPM 死依赖核实

`Directory.Packages.props` 原有 **29** 条 `PackageVersion`；逐条与仓库内全部 `<PackageReference>`（src / test 的 csproj，含 CPM 覆盖写法）比对，并额外排查源码引用与隐式引用（`Directory.Build.props` 只做 `Compile Include`，无隐式 `PackageReference`；无条件 `PackageReference`）。

| 依赖 | 结论 | 处理 |
|------|------|------|
| `System.IO.Pipelines` 10.0.13 | 无任何 `PackageReference`；Middleware 的 `using System.IO.Pipelines` 由共享框架提供（`System.Text.Json` 等基础类型已由 SDK 引用，`DelegatingDuplexPipe.cs` 现有编译不依赖该 NuGet 包） | **删除声明** |
| `SharpCompress` 0.50.4 | 无任何 `PackageReference`；仅出现在未被任何包编译的 `Shared/Helpers/Helper.Compressor.cs` | **删除声明** |
| `ZLogger` 2.5.10 | 无任何 `PackageReference`；`src`/`test` 源码 0 处引用（`Framework.ZLogging` 只用 `Microsoft.Extensions.Logging` + Spectre.Console） | **删除声明** |
| `BouncyCastle.Cryptography` 2.7.0 | 有 3 处 `PackageReference`（`Framework.AspNetCore`、`Framework.AspNetCore.Middleware`、`Framework.Repository`），但**编译代码 0 处使用**（唯一使用点 `Shared/Helpers/Helper.SM4Encrypt.cs` 未被任何包编译） | **已按方案 A 删除**（见第八节） |
| `SkiaSharp` / `SkiaSharp.NativeAssets.Linux.NoDependencies` | `Framework.SlideCaptcha` 直接引用，代码使用 | 保留 |
| `System.Text.Json` | `Framework.Repository` 有 `PackageReference`（既有 NU1510 警告） | 保留 |
| 其余 23 条 | 均有对应 `PackageReference` | 保留 |

**BouncyCastle 当时的判断**：它并非「声明未用」，而是「已被 3 个包的 `PackageReference` 引用、但代码未使用」。要删掉 CPM 声明，必须同时删除这三个 `PackageReference`，会改变已发布包的依赖图，属于影响下游的发布决定，因此先列出两个方案供拍板。

**用户已选方案 A**：删除 3 处 `PackageReference` + CPM 声明，执行与核实记录见第八节。

## 六、验证结果

| 项 | 结果 |
|----|------|
| 引用关系复核脚本 | 73 → 72 个文件；38 被引用 / 34 零引用（全为有文档模块）；重复编译 4 个已逐个确认 |
| `dotnet restore Framework.slnx` | **成功**（退出码 0），仅既有 NU1510（`System.Text.Json`）警告 |
| `dotnet build Framework.slnx` | **成功**，0 error；18 个警告全部为既有警告（NU1510、CS8625、CS1574/CS1570 等），无新增 |
| 测试 | 未运行：删除的文件不参与任何编译，CPM 删除项无编译期消费者；无受影响的测试工程。`Framework.Tests` 引入的 5 个 Shared 文件未被改动 |
| git | 未提交；工作区改动为本轮 `Directory.Packages.props`、删除 `src/Shared/Helpers/Helper.cs`，以及上一轮遗留的 `build/Build.cs`、`public.yml`、两个 csproj |

## 七、风险提示

1. **BouncyCastle 已按方案 A 移除**（3 处 `PackageReference` + CPM 声明），见第八节。
2. **34 个零引用文件均为「有文档的复制模板」**，本轮按「不删除有文档模块」保留；若决定精简 `src/Shared`，需要连带修改 `MODULES.md` 与各目录 README（文档本轮按要求不动）。
3. **重复编译风险**：Shared 顶层类型一旦改为 `public`，同时在两个包之间会出现 CS0433；建议后续加 CI 校验（如编译期禁止 `src/Shared` 顶层 `public`）。
4. **`System.Text.Json` NU1510** 为既有警告（.NET 10 已内置），本轮未处理。
5. `test/Framework.Tests` 对 5 个 Shared 文件也有独立副本，属测试夹具的预期行为，不涉及包产物。

## 八、BouncyCastle 移除记录（方案 A）

### 删除位置

| 文件 | 删除内容 |
|------|----------|
| `src/Framework.AspNetCore/Framework.AspNetCore.csproj` | `<PackageReference Include="BouncyCastle.Cryptography" />` |
| `src/Framework.AspNetCore.Middleware/Framework.AspNetCore.Middleware.csproj` | `<PackageReference Include="BouncyCastle.Cryptography" />` |
| `src/Framework.Repository/Framework.Repository.csproj` | `<PackageReference Include="BouncyCastle.Cryptography" />` |
| `Directory.Packages.props` | `<PackageVersion Include="BouncyCastle.Cryptography" Version="2.7.0" />` |

只动了上述 4 个文件，未改任何源码；未提交 git，未打包推送。

### 核实方式与结论

- 全仓 `*.cs` 搜索 `BouncyCastle` / `Org.BouncyCastle`：唯一命中 `src/Shared/Helpers/Helper.SM4Encrypt.cs`（`using Org.BouncyCastle.Crypto.*`）。
- 全仓 `*.csproj` 搜索 `SM4` / `Helper.SM4`：**0 处**，说明该文件没有被任何包或测试工程 `<Compile Include>`，不参与编译。
- 三个包的其余源码（含各自内联的 Shared 文件）中无 `BouncyCastle` 类型引用，无 `#if` 条件编译分支；全仓也没有任何含 `BouncyCastle` 的字符串可被 `Type.GetType` / `Assembly.Load` 等反射加载。
- 结论：无编译期或运行期隐式依赖，删除安全。`Helper.SM4Encrypt.cs` 仍保留在 Shared 作为「可复制模板」，其 README 中关于自备 BouncyCastle 的说明属于模板文档，不影响本仓库构建。

### 构建与测试结果

| 项 | 结果 |
|----|------|
| `dotnet restore Framework.slnx` | 成功（退出码 0），仅既有 NU1510（`System.Text.Json`） |
| `dotnet build Framework.slnx` | 成功，**0 error**；警告均为既有警告（以 `System.Text.Json` 的 NU1510 为主），无新增 |
| `dotnet test test/Framework.Modbus.Tests`（唯一间接引用 Middleware → AspNetCore 的测试工程） | **146 / 146 通过**，0 失败 |

### 三个包残留的依赖核对（未自行删除）

| 包 | 依赖 | 使用情况 | 建议 |
|----|------|----------|------|
| `Framework.AspNetCore` | 无 `PackageReference`（仅 `FrameworkReference Microsoft.AspNetCore.App`） | — | — |
| `Framework.AspNetCore.Middleware` | `Yarp.ReverseProxy` | `HttpProxy/HttpProxyMiddleware.cs` 使用 `Yarp.ReverseProxy.Forwarder` | 保留 |
| `Framework.Repository` | `linq2db` | `Repositories/RepositoryBase.cs`、`Extensions/Linq2DbDbContextExtensions.cs` 等使用 | 保留 |
| `Framework.Repository` | `Mapster` | `Data/AbstractSyncData.cs`、`Repositories/RepositoryBase.cs` 使用 | 保留 |
| `Framework.Repository` | `System.Text.Json` | `Data/AbstractGenerateData.cs`、`Entities/*.cs` 使用；但 net10.0 已内置，显式引用触发 NU1510 | 非零使用，属冗余声明，待后续判断 |
| `Framework.Repository` | `Yitter.IdGenerator` | **编译代码 0 处使用**：全仓无 `Yitter` / `IdGenerator` 类型引用，`SnowflakeAttribute` 是本地特性且未被 `RepositoryBase` 消费 | **零使用依赖，待后续判断（本轮未删）** |
