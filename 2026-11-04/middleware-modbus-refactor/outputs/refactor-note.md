# Middleware 与 Modbus 解耦重构说明

日期：2026-11-04
范围：仅消除 `Framework.AspNetCore.Middleware/Modbus` 内嵌从站实现与 `Framework.Modbus` 主站包之间的协议重复；
未改动 Logging / Mvvm / Charts / Repository / 发布链路，未改任何包版本，未提交 git、未打包推送。

## 一、结论

- 新增零依赖契约层包 **`YLFramework.Modbus.Abstractions`**（namespace `Framework.Modbus.Abstractions`），
  承载主从共用的功能码、异常码、协议常量、CRC16/LRC、Span 版 PDU 模型、ADU 帧编解码与协议异常。
- `Framework.Modbus`（主站）与 `Framework.AspNetCore.Middleware`（从站宿主）都只依赖该契约层，
  **不存在 `Middleware → Framework.Modbus` 的核心库引用**。
- Middleware 的 `Modbus/` 从 27 个 `.cs` / 2305 行降到 14 个 `.cs` / 1205 行（含 91 行兼容层），
  协议解析与帧编解码全部迁出，只保留从站服务、进程映像与 Kestrel 宿主。
- 公开 API 能兼容的都保留：`Middleware.Modbus` 下三个静态常量/校验类型保留为 `[Obsolete]` 转发类；
  其余被迁移类型无法用别名安全转发，列为破坏性变更并给出迁移方式（见第四节）。
- 验证：`dotnet build Framework.slnx` 0 error（仅 Framework.Repository 既有的 NU1510 警告）；
  `Framework.Modbus.Tests` 103 通过 / 0 失败（含新增 10 个契约层用例）。

## 二、核实到的重复点（重构前）

阅读两边实际代码后确认，重复不是“同名类型”，而是同一协议事实被维护了两份：

| 重复内容 | 主站 `Framework.Modbus` | 从站 `Middleware.Modbus` | 差异 |
|----------|------------------------|--------------------------|------|
| 功能码 | `ModbusFunctionCode`（jamod 命名：`ReadInputDiscretes` / `ReadMultipleRegisters` / `WriteCoil`） | 同名类（协议命名：`ReadDiscreteInputs` / `ReadHoldingRegisters` / `WriteSingleCoil`） | 值完全相同，命名不同 |
| 异常码 | `ModbusExceptionCode`（9 个） | 同名类（9 个） | 名称与值完全一致 |
| 协议上限 | `ModbusConstants.MaxPduSize/MaxReadCoils/...` | 分散在 `ReadRequests`/`WriteRequests` 的 `MaxCount`、`MbapFrameCodec` 的 7/253 | 值一致，无单一来源 |
| 校验 | `ModbusUtil.CalculateCrc16/CalculateLrc` | `ModbusCrc16.Compute` | 算法一致，实现两份 |
| PDU 模型 | `Framework.Modbus.Messages.*`（byte[]/IBufferWriter，主站侧） | `Middleware.Modbus.Pdu.*`（Span/SequenceReader，从站侧） | 同为功能码 + 数据域，但表示与 API 不共享 |
| 帧封装 | 主站 `IO/Modbus*Transport` 内各自处理 MBAP/RTU/ASCII | `Transport/MbapFrameCodec`、`RtuFrameCodec` | 概念重复，代码形态不同 |

主站的 `Messages` 模型是面向请求构造/事务重试的老设计；从站的 `Pdu` 模型是零拷贝 Span 设计。
本次**不重写主站消息层**（风险过大且超出范围），而是把从站已成熟的 Span PDU/帧编解码提升为公共契约，
主站先在常量与校验上收敛到同一来源；主站后续可逐步改用契约层 PDU（见第七节）。

## 三、方案选型

### 选中方案：独立契约层包

```
Framework.Modbus.Abstractions  ←  Framework.Modbus（主站）
                              ←  Framework.AspNetCore.Middleware（从站宿主）
```

**为什么不是把从站整体搬进 `Framework.Modbus`**：
从站的宿主依赖 Kestrel 的 `ConnectionHandler` / `ListenOptions`，搬进主站包会给
`YLFramework.Modbus` 增加 `Microsoft.AspNetCore.App` FrameworkReference。主站常用于控制台、
工控网关、串口采集等无 ASP.NET 场景，这会让纯主站用户被动背上 ASP.NET 依赖，包体积与依赖面都变差。
若真要单包，需要再造一个 `Framework.Modbus.AspNetCore` 宿主包，等于多拆一层，收益不及当前方案。

**为什么不用 `src/Shared` 内联**：
Shared 约定是“不声明 namespace、顶层类型 internal”。契约层需要作为**公开 API**供两个包及下游使用
（如 `IProcessImage`、`IModbusFrameCodec`、PDU 类型），internal 无法满足；若改为 public 内联，
两个程序集会导出同名全限定类型，下游引用双包必触发 `CS0433`。因此协议契约不适合走 Shared，
Shared 只适合包内私有实现细节。

**与“核心库之间不互相 ProjectReference”的关系**：
该约定的目的是禁止实现型核心库相互耦合（如 Middleware 直接引用 Modbus 主站）。
本方案新增的是**零外部依赖、无实现的纯契约包**，主站与从站都不引用对方的实现，
符合本任务给出的“抽出薄契约层”方向；这是一处需要明确记录的例外。

## 四、前后结构对比

### 重构前

```
src/Framework.Modbus/                    主站实现（39 .cs / 5274 行）
src/Framework.AspNetCore.Middleware/
└── Modbus/                              27 .cs / 2305 行
    ├── ModbusFunctionCode.cs            功能码（与主站重复）
    ├── ModbusExceptionCode.cs           异常码（与主站重复）
    ├── ModbusCrc16.cs                   CRC16（与主站重复）
    ├── ModbusSlaveException.cs          协议异常
    ├── ModbusFrameException.cs          帧异常
    ├── Pdu/                             6 文件：PDU 模型与请求/响应
    ├── Transport/                       3 文件：MBAP / RTU 帧编解码
    ├── ModbusSlave*.cs                  从站核心 + 进程内直通
    ├── ProcessImage/                    进程映像
    ├── *ConnectionHandler.cs            Kestrel 宿主
    └── *Extensions.cs                   DI / ListenOptions
```

### 重构后

```
src/Framework.Modbus.Abstractions/       新契约层（19 .cs / 1302 行，零 NuGet 依赖）
├── ModbusConstants.cs                   协议常量 / 尺寸 / 数量上限（唯一来源）
├── ModbusFunctionCode.cs                功能码（含主站/从站两套命名别名）
├── ModbusExceptionCode.cs               异常码
├── ModbusCrc16.cs / ModbusLrc.cs        CRC16 / LRC
├── ModbusFrameException.cs              ADU 帧异常
├── ModbusSlaveException.cs              协议异常（从站解析需要，故随 PDU 下沉）
├── Pdu/                                 PDU 模型与请求/响应编解码（6 文件）
└── Transport/                           IModbusFrameCodec / Mbap / Rtu（3 文件）

src/Framework.Modbus/                    主站；常量与校验转发到契约层，公开 API 不变
src/Framework.AspNetCore.Middleware/
└── Modbus/                              14 .cs / 1205 行
    ├── ModbusLegacyAliases.cs           仅兼容层（Obsolete 转发）
    ├── ModbusSlave*.cs                  从站核心 + 进程内直通
    ├── ProcessImage/                    进程映像
    ├── *ConnectionHandler.cs            Kestrel 宿主
    └── *Extensions.cs                   DI / ListenOptions
```

## 五、文件移动表

| 原位置（`src/Framework.AspNetCore.Middleware/Modbus/`） | 新位置（`src/Framework.Modbus.Abstractions/`） | 处理 |
|------|------|------|
| `ModbusFunctionCode.cs` | `ModbusFunctionCode.cs` | 移入；补齐主站命名别名与全部功能码 |
| `ModbusExceptionCode.cs` | `ModbusExceptionCode.cs` | 移入，值不变 |
| `ModbusCrc16.cs` | `ModbusCrc16.cs` | 移入，逻辑不变 |
| — | `ModbusLrc.cs` | 新增（主站 `ModbusUtil` 原有 LRC 算法收敛到此） |
| — | `ModbusConstants.cs` | 新增（收敛主站 `ModbusConstants` + 从站分散上限） |
| `ModbusFrameException.cs` | `ModbusFrameException.cs` | 移入 |
| `ModbusSlaveException.cs` | `ModbusSlaveException.cs` | 移入（PDU 解析抛出，必须随契约层） |
| `Pdu/ModbusPdu.cs` | `Pdu/ModbusPdu.cs` | 移入 |
| `Pdu/ModbusRequest.cs` | `Pdu/ModbusRequest.cs` | 移入（上限改为引用 `ModbusConstants`） |
| `Pdu/ModbusResponse.cs` | `Pdu/ModbusResponse.cs` | 移入 |
| `Pdu/ReadRequests.cs` | `Pdu/ReadRequests.cs` | 移入（`MaxCount` 改为引用 `ModbusConstants`） |
| `Pdu/WriteRequests.cs` | `Pdu/WriteRequests.cs` | 移入（`MaxCount` 改为引用 `ModbusConstants`） |
| `Pdu/Responses.cs` | `Pdu/Responses.cs` | 移入 |
| `Transport/IModbusFrameCodec.cs` | `Transport/IModbusFrameCodec.cs` | 移入 |
| `Transport/MbapFrameCodec.cs` | `Transport/MbapFrameCodec.cs` | 移入（头长/PDU 上限引用 `ModbusConstants`） |
| `Transport/RtuFrameCodec.cs` | `Transport/RtuFrameCodec.cs` | 移入（CRC 引用 `ModbusCrc16`） |
| — | `README.md` | 新增包说明 |
| （删除后） | `ModbusLegacyAliases.cs`（留在 Middleware） | 三个旧静态类型的 Obsolete 转发类 |

其余 Middleware 文件（`ModbusSlave`、`ModbusSlave.DirectAccess`、`ModbusSlaveOptions`、`ModbusDataArea`、
`ModbusValue`、`ModbusValueChangedEventArgs`、`ProcessImage/*`、三个连接处理器、DI/ListenOptions 扩展）
原地保留，只把 `using` 指向契约层。

## 六、API 兼容处理

### 保留兼容（源码级不变，仅出现 Obsolete 提示）

| 类型（`Middleware.Modbus`） | 兼容方式 |
|------|------|
| `ModbusFunctionCode` | `[Obsolete]` 静态类，全部常量 `const` 初始化自 `Framework.Modbus.Abstractions.ModbusFunctionCode` |
| `ModbusExceptionCode` | `[Obsolete]` 静态类，常量转发 |
| `ModbusCrc16` | `[Obsolete]` 静态类，`Compute` / `Length` 转发 |

由于转发的是 `const`，原有代码无需修改即可编译，值仍来自唯一来源；删除兼容层是后续可选的清理动作。

### 破坏性变更（无法安全做源码别名）

以下类型的**命名空间**从 `Middleware.Modbus` 改为 `Framework.Modbus.Abstractions`。不能保留旧名的原因：
PDU/编解码类型若在 Middleware 侧再定义一次转发类，会与真实类型在继承、sealed、返回类型上不一致，
产生“catch 不到 / 类型不匹配”的隐性错误，比直接改名更危险。

- `ModbusPdu`、`ModbusRequest`、`ModbusResponse`
- `ReadCoilsRequest`、`ReadDiscreteInputsRequest`、`ReadHoldingRegistersRequest`、`ReadInputRegistersRequest`
- `WriteSingleCoilRequest`、`WriteSingleRegisterRequest`、`WriteMultipleCoilsRequest`、`WriteMultipleRegistersRequest`
- `ReadBitsResponse`、`ReadRegistersResponse`、`WriteSingleResponse`、`WriteMultipleResponse`、`ModbusExceptionResponse`
- `IModbusFrameCodec`、`MbapFrameCodec`、`RtuFrameCodec`
- `ModbusFrameException`、`ModbusSlaveException`

连带签名变化：`ModbusSlave.Execute(...)` 的返回类型现在是
`Framework.Modbus.Abstractions.ModbusResponse?`。

迁移方式：

1. 在引用这些类型的文件加 `using Framework.Modbus.Abstractions;`；
2. 删除 `using Middleware.Modbus;` 中已不存在的类型引用（服务类型仍在 `Middleware.Modbus`）；
3. 捕获异常时改为 `catch (Framework.Modbus.Abstractions.ModbusFrameException)` / `ModbusSlaveException`；
4. 自定义帧编解码器实现改为契约层的 `IModbusFrameCodec` 接口。

### 主站侧 API

`Framework.Modbus` 的公开类型与签名**未变**（`ModbusConstants`、`ModbusFunctionCode`、`ModbusExceptionCode`、
`ModbusUtil` 等），只是内部取值/实现改为委托契约层；行为与返回值已用测试对照校验。

## 七、依赖与包体积

| 包 | 变更 | 依赖影响 |
|----|------|----------|
| `YLFramework.Modbus.Abstractions`（新） | 19 .cs / 1302 行，`net10.0`，`<Version>2.0.0-alpha.1</Version>` | 零 NuGet 依赖，仅 BCL（`System.Buffers`），无 ASP.NET |
| `YLFramework.Modbus` | 新增 1 个 ProjectReference | 无新增 NuGet；不引入 ASP.NET |
| `YLFramework.AspNetCore.Middleware` | 新增 1 个 ProjectReference | 无新增 NuGet；从站协议实现外迁，包内代码减少约 1100 行 |

- 所有依赖版本仍集中在根 `Directory.Packages.props`（本次未新增任何 NuGet 包，故无需改该文件）。
- 各包 `<Version>` 保持原值；新包自带独立版本。
- `build/Build.cs` 按 `src/**/*.csproj` 自动发现包，新包无需改构建脚本；CI 单包/全量标签逻辑未动。

## 八、验证结果

| 验证项 | 命令 | 结果 |
|--------|------|------|
| 全仓构建 | `dotnet build Framework.slnx` | 成功，0 error；仅 Framework.Repository 既有的 NU1510 警告 |
| Modbus 测试 | `dotnet test test/Framework.Modbus.Tests/Framework.Modbus.Tests.csproj --no-build` | 103 通过 / 0 失败 / 0 跳过 |
| 新增契约测试 | `test/Framework.Modbus.Tests/SlaveProtocolContractTests.cs` | 10 个用例：常量单一来源、CRC/LRC 与主站一致、8 类请求往返、非法功能码/数量、响应编码、MBAP 往返、RTU 往返与 CRC 拒绝、RTU 按字节数推断帧长 |

未运行其它测试工程的理由：
- Middleware 目前没有测试工程（全仓 `test/` 下无对应项目），无法运行；
- `Framework.DynamicApi.Tests` 与 Modbus 无依赖关系，本次未改动其输入，故未运行。

## 九、遗留与后续建议

1. **主站消息层尚未改用 Span PDU**：`Framework.Modbus.Messages` 仍是独立模型，当前只共享常量与校验。
   后续可在不改公开门面的前提下，把消息层的字节编解码切换到契约层 PDU，彻底消除概念重复。
2. **同名类型共存风险**：`Framework.Modbus.Messages.ReadCoilsRequest` 与
   `Framework.Modbus.Abstractions.ReadCoilsRequest` 同名不同命名空间。若同一文件同时 `using` 两个命名空间，
   会出现 `CS0104` 二义性；建议使用别名或全限定名（已在契约层 README 说明）。
3. **从站是否进一步独立成包**：若希望 Middleware 彻底不出现 Modbus 字样，可再拆
   `Framework.Modbus.Slave`（协议服务）与 `Framework.Modbus.Slave.AspNetCore`（Kestrel 宿主）。
   本次未做，因为会增加两个包且对使用方是更大的迁移面。
