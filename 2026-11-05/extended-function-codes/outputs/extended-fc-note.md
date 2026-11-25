# 扩展功能码收口说明（Framework.Modbus.Abstractions）

日期：2026-11-05
范围：FC07 / 08 / 0B / 0C / 11 / 14 / 15 / 16 / 17 / 18。
不改发布链路、不改包版本、不新增第三方依赖、未提交 git、未打包。

## 一、结论

- 10 个扩展功能码已全部收口到契约层：PDU 布局、子功能码、字节计数、引用类型、长度校验都只有一份实现。
- 主站 `Messages.*` 七个文件完成迁移，只保留语义层（业务字段、单元号/事务号、请求-响应配对）；对外抛异常仍是 `ModbusIOException`（Inner 为 `Abstractions.ModbusSlaveException`）。
- 公开 API 零变更：类型名、命名空间、构造函数、属性、`ModbusMaster` / 从站入口签名均未改，因此本轮不需要新增 `[Obsolete]` 转发，也没有类型迁移清单。
- 构建 0 error（仅 Repository 既有 NU1510 警告 3 条）；`Framework.Modbus.Tests` 120 通过 / 0 失败（110 → 120，新增 10 个用例）。
- 从站（Middleware）对扩展功能码仍返回 IllegalFunction，行为与迁移前一致（原因见第六节）。

## 二、盘点表（迁移前）

| FC | 名称 | 主站实现位置 | 实现形态 | 从站实现 | 传输层形态 |
|----|------|--------------|----------|----------|------------|
| 07 | Read Exception Status | `Messages/Diagnostics.cs` | 就地拼字节 + `ModbusPduWriter`/`Reader` | 无（IllegalFunction） | 固定长 2 |
| 08 | Diagnostics（子功能码） | `Messages/Diagnostics.cs` | 就地拼字节，子功能码+16 位数据 | 无 | 固定长 5（主站限定 2 字节数据） |
| 0B | Get Comm Event Counter | `Messages/CommEvent.cs` | 就地拼字节 | 无 | 固定长 5 |
| 0C | Get Comm Event Log | `Messages/CommEvent.cs` | 就地拼字节 + 变长事件字节 | 无 | RTU 读 1 字节字节计数后再读 |
| 11 | Report Slave ID | `Messages/ReportSlaveId.cs` | 就地拼字节 + 变长数据 | 无 | RTU 读 1 字节字节计数后再读 |
| 14 | Read File Record | `Messages/FileRecordMessages.cs` | 就地拼字节 + 手工分段解析 | 无 | RTU 读 1 字节字节计数后再读 |
| 15 | Write File Record | `Messages/FileRecordMessages.cs` | 就地拼字节 + 手工分段解析（请求/响应回显共用） | 无 | RTU 读 1 字节字节计数后再读 |
| 16 | Mask Write Register | `Messages/MaskWriteRegister.cs` | 就地拼字节 | 无 | 固定长 7 |
| 17 | Read/Write Multiple Registers | `Messages/ReadWriteMultipleRegisters.cs` | 请求就地拼字节；响应已复用 `ReadRegistersResponse` | 无 | 响应按字节计数变长 |
| 18 | Read FIFO Queue | `Messages/ReadFifoQueue.cs` | 就地拼字节 + 变长 FIFO | 无 | RTU 先读 2 字节字节计数，再按值读数据 |

盘点结论：
- **没有主从两份实现**。Middleware 从站只实现 FC01/02/03/04/05/06/0F/10，扩展功能码在从站一律走默认分支抛 `IllegalFunction`。
- `IO/*Transport` 不参与 PDU 编解码，只按 `ExpectedResponsePduLength` 或字节计数做帧长度推断；不构成重复。
- 真正重复的是「主站消息对象自己拼字节/解字节」与「契约层已有基础能力」之间：功能码/异常码已在上一轮收口，但扩展 PDU 的布局、计数校验仍是主站独有的一份。

## 三、契约层新增类型

新增 8 个文件（`src/Framework.Modbus.Abstractions/`），共 23 个公开类型：

| 文件 | 公开类型 | 说明 |
|------|----------|------|
| `ModbusDiagnosticsSubFunction.cs` | `ModbusDiagnosticsSubFunction` | FC08 的 15 个子功能码常量，成为唯一取值来源 |
| `Pdu/DiagnosticsPdu.cs` | `ReadExceptionStatusRequest/Response`、`DiagnosticsRequest/Response` | FC07/08；FC08 数据域为变长字节（主站仍保持 2 字节语义） |
| `Pdu/CommEventPdu.cs` | `GetCommEventCounterRequest/Response`、`GetCommEventLogRequest/Response` | FC0B/0C；FC0C 字节计数 = 6 + 事件字节 |
| `Pdu/ReportSlaveIdPdu.cs` | `ReportSlaveIdRequest/Response` | FC11；字节计数 + 变长数据 |
| `Pdu/FileRecordPdu.cs` | `FileRecordReference`、`FileRecordWrite`、`ReadFileRecordRequest/Response`、`WriteFileRecordRequest/Response` | FC14/15；引用类型 0x06、分段变长、FC15 响应回显 |
| `Pdu/MaskWriteRegisterPdu.cs` | `MaskWriteRegisterRequest/Response` | FC16；固定 7 字节 |
| `Pdu/ReadWriteMultipleRegistersPdu.cs` | `ReadWriteMultipleRegistersRequest/Response` | FC17；读数量 1..125、写数量 1..123、字节计数一致 |
| `Pdu/ReadFifoQueuePdu.cs` | `ReadFifoQueueRequest/Response` | FC18；FIFO 数量 0..31、字节计数一致 |

新增常量（`ModbusConstants`）：`FileRecordReferenceType = 0x06`、`MaxReadFileRecordSubRequests = 35`、`MaxFifoQueueCount = 31`。

设计取舍：
- 全部沿用已有核心 PDU 风格：Span 编码、`Length` 预声明、`Encode(Span<byte>)`、`ToByteArray()`；请求 `Decode(data)` 不含功能码，响应 `Decode(expectedFunctionCode, pdu)` 含功能码。
- FC08 保留「子功能码 + 变长数据」而非套用通用的「计数+数据」模板；`DataWord` 仅为主站 16 位语义提供便捷读取。
- FC14/15 保留协议原生的分段结构（引用类型、file/record、子响应长度），没有抽象成统一记录容器，避免丢失「响应不含文件号」「FC15 回显」等语义。
- FC17 响应因既有 `ReadRegistersResponse` 为 `sealed`，新类型直接继承 `ModbusResponse` 并复用 `ReadRegistersResponse.Decode`；未修改既有公开类型。

## 四、迁移与 API 兼容

主站修改（7 个消息文件 + 常量类）：

| 文件 | 迁移方法 | 契约来源 |
|------|----------|----------|
| `Messages/Diagnostics.cs` | `ToPdu` / `FromPdu` | `ReadExceptionStatusRequest/Response`、`DiagnosticsRequest/Response` |
| `Messages/CommEvent.cs` | `ToPdu` / `FromPdu` | `GetCommEventCounterRequest/Response`、`GetCommEventLogRequest/Response` |
| `Messages/ReportSlaveId.cs` | `ToPdu` / `FromPdu` | `ReportSlaveIdRequest/Response` |
| `Messages/FileRecordMessages.cs` | `ToPdu` / `FromPdu`、`BuildPdu` / `ParseFileRecords` 内部改走契约 | `ReadFileRecordRequest/Response`、`WriteFileRecordRequest/Response` |
| `Messages/MaskWriteRegister.cs` | `ToPdu` / `FromPdu` | `MaskWriteRegisterRequest/Response` |
| `Messages/ReadWriteMultipleRegisters.cs` | 请求 `ToPdu` / `FromPdu` | `ReadWriteMultipleRegistersRequest` |
| `Messages/ReadFifoQueue.cs` | `ToPdu` / `FromPdu` | `ReadFifoQueueRequest/Response` |
| `src/Framework.Modbus/ModbusConstants.cs` | `ModbusDiagnosticsSubFunction` 全部常量改为转发 | `Abstractions.ModbusDiagnosticsSubFunction` |

兼容处理：
- **公开签名零变更**：`ToPdu()` 仅从表达式体改为块体；所有属性、构造函数、`FunctionCode`、`PduLength`、`ExpectedResponsePduLength` 保持原值。
- **无需 `[Obsolete]` 转发**：没有删除、移动或重命名任何公开类型；`ModbusMaster` / 从站入口源码无需改动。
- **常量单源**：主站 `ModbusDiagnosticsSubFunction` 的 `const` 直接由契约层 `const` 初始化（编译期内联），取值与行为不变。
- **异常契约不变**：`FromPdu` 失败仍抛 `ModbusIOException`，`InnerException` 为 `Abstractions.ModbusSlaveException`；`ToPdu` 阶段若参数非法，契约构造器异常直接外露（与核心码迁移后的做法一致）。

行为收紧（非法帧识别加强，合法报文结果不变）：
1. FC14 请求校验引用类型必须为 0x06、字节计数必须是 7 的倍数、长度精确匹配、记录长度 >=1、子请求 <=35；旧实现跳过引用类型、允许尾随多余字节。
2. FC14 响应要求子响应长度 >=3 且为奇数、引用类型 0x06、消耗字节数恰好等于字节计数；旧实现允许空子响应和尾随字节。
3. FC15 请求/响应校验引用类型 0x06、每条记录值 >=1、字节计数精确匹配；旧实现跳过引用类型、允许 0 长度记录。
4. FC08 数据域要求 >=4 字节；FC18 校验字节计数偶性、FIFO 数量 <=31 且与字节计数一致；旧实现部分未校验。
5. 契约构造器对越界参数（如 FC17 读数量 0/126、FC18 32 个值）抛 `ModbusSlaveException.IllegalDataValue`。

## 五、构建与测试结果

构建：

```
dotnet build Framework.slnx
→ Build succeeded，0 Error，3 Warning
  3 条均为 Framework.Repository 的 NU1510（System.Text.Json 显式引用），既有问题，与本轮无关
```

测试（仅 `test/Framework.Modbus.Tests`）：

```
dotnet test test/Framework.Modbus.Tests/Framework.Modbus.Tests.csproj --no-build
→ total 120 / succeeded 120 / failed 0 / skipped 0
```

新增用例：**10 个 `[Test]` 方法**（110 → 120），位于 `test/Framework.Modbus.Tests/ExtendedPduTests.cs`，按功能码分组，覆盖全部 20 个新增请求/响应 PDU 类：

| 测试方法 | 覆盖类 | 非法/边界样例 |
|----------|--------|----------------|
| `Fc07_ReadExceptionStatus_RoundTrips` | FC07 请求/响应 | 请求携带数据；响应功能码不匹配；响应截断 |
| `Fc08_Diagnostics_RoundTrips` | FC08 请求/响应 | 请求数据域 <4；响应 PDU <5 |
| `Fc0B_GetCommEventCounter_RoundTrips` | FC0B 请求/响应 | 请求携带数据；响应 4 字节截断 |
| `Fc0C_GetCommEventLog_RoundTrips` | FC0C 请求/响应 | byteCount=5 <6；声明长度大于实际 |
| `Fc11_ReportSlaveId_RoundTrips` | FC11 请求/响应 | 请求携带数据；声明的字节数大于实际 |
| `Fc14_ReadFileRecord_RoundTrips` | FC14 请求/响应 | 引用类型 0x05；字节计数非 7 倍数；响应引用类型非法 |
| `Fc15_WriteFileRecord_RoundTrips` | FC15 请求/响应 | 引用类型 0x05；响应功能码不匹配 |
| `Fc16_MaskWriteRegister_RoundTrips` | FC16 请求/响应 | 数据域 5 字节/7 字节；响应截断 |
| `Fc17_ReadWriteMultipleRegisters_RoundTrips` | FC17 请求/响应 | 读数量 0 / 126；字节计数与写数量不符；响应功能码不匹配 |
| `Fc18_ReadFifoQueue_RoundTrips` | FC18 请求/响应 | 请求数据域长度错误；byteCount 与 fifoCount 不符；32 个值超上限 |

期望字节序列按 Modbus 规范逐字节断言（例如 FC08 `08 00 00 12 34`、FC14 `14 07 06 00 01 00 02 00 03`、FC18 `18 00 06 00 02 01 B8 12 84`）。

## 六、未接线与剩余项

1. **没有「只定义常量、未实现」的功能码**：本轮的 10 个功能码在迁移前都已由主站 `ModbusMaster` 提供完整入口（`ReadExceptionStatus`、`Diagnostics`、`GetCommEventCounter`、`GetCommEventLog`、`ReportSlaveId`、`ReadFileRecord`、`WriteFileRecord`、`MaskWriteRegister`、`ReadWriteMultipleRegisters`、`ReadFifoQueue`），因此全部已接线，不存在仅补 PDU 的悬空类型。
2. **从站未接线（有意保留）**：Middleware 从站仍只支持核心 8 个功能码。扩展 PDU 类型已经可用，但未加入 `ModbusRequest.Create` 分发，也未在 `ModbusSlave` 增加分支——这样从站对扩展功能码仍按规范返回 `IllegalFunction`，不会因「先解析再拒绝」而把非法数据误报成 `IllegalDataValue`。若后续从站要支持（尤其串行专用码），再接 `ModbusRequest.Create` 与从站处理分支。
3. **串行链路专用码的传输依赖**：FC07/08/0B/0C/11/14/15/16/17/18 多用于串行链路；主站已有 RTU/ASCII 传输（`ModbusSerialMaster`），因此不存在「无链路可用」的阻塞。RTU 变长响应的长度推断仍在 `IO/ModbusRtuTransport.ReadResponseTail` 中（按 `ExpectedResponsePduLength` 或字节计数），本轮未迁移到 `IModbusFrameCodec`，属独立的传输层任务。
4. **`ModbusPduWriter` / `ModbusPduReader` 仍保留**：它们是无契约层时期的公开 API，且主站 `ToPdu` 仍用其池化缓冲；本轮未删除，避免破坏二进制/源码兼容。
