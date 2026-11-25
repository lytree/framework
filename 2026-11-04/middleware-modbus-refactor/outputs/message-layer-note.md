# 主站消息层收敛说明（Framework.Modbus → Framework.Modbus.Abstractions）

日期：2026-11-04
范围：仅收敛 `src/Framework.Modbus/Messages/` 的 PDU 编解码到 `Framework.Modbus.Abstractions`；
不改 Logging / Mvvm / Charts / Repository / Middleware 从站实现，不改包版本，不新增第三方依赖，未提交 git、未打包。

## 一、结论

- 核心功能码 **FC01 / 02 / 03 / 04 / 05 / 06 / 0F / 10** 的请求与响应 PDU 编码、解析已全部委托契约层，
  `Messages.*` 只保留请求/响应语义与业务字段，不再手写字节布局、不再自行判断功能码与字节计数。
- 异常响应编码统一走 `Abstractions.ModbusExceptionResponse`；功能码/异常码/CRC 已在上轮收敛，本轮无新增重复来源。
- **公开 API 零变更**：`Messages.*` 全部类型、命名空间、构造函数与成员签名保持原样，因此本轮不需要新增 `[Obsolete]` 转发；
  仅 3 处解析行为被有意收紧（见第五节）。
- `ModbusMaster` / `ModbusTcpMaster` / `ModbusSerialMaster` 等对外入口签名未变。
- 验证：`dotnet build Framework.slnx` 0 error（仅 `Framework.Repository` 既有 NU1510 警告，3 条同源）；
  `Framework.Modbus.Tests` 110 通过 / 0 失败（103 → 110，新增 7 个用例）。

## 二、盘点结论（收敛前）

`Messages/` 共 13 个语义文件 + 本轮新增 1 个桥接文件。逐个核对后的分类如下：

| 类型 | 职责 | 与契约层重复点 | 性质 | 处置 |
|------|------|----------------|------|------|
| `ModbusMessage` / `ModbusRequest` / `ModbusResponse` | 承载 `UnitId` / `TransactionId`，声明 `ToPdu` / `WritePdu` / `ExpectedResponsePduLength` | 契约层 PDU 不含单元号与事务号，无重复 | 语义基类（行为：序列化入口、异常标记） | 保留 |
| `ReadBitsRequest`（FC01/02 公共基类） | `Reference` + `BitCount`，构造时校验地址与 1..2000 | 与 `ReadCoilsRequest` / `ReadDiscreteInputsRequest` 的字段布局重复 | 半数据半行为（参数校验） | 保留语义，编码委托 |
| `ReadCoilsRequest` / `ReadInputDiscretesRequest` | FC01/FC02 请求 | 编码与解析重复 | 纯数据 | 编码/解析委托 |
| `ReadBitsResponse` + `ReadCoilsResponse` / `ReadInputDiscretesResponse` | 以 `BitVector` 承载位数据 | 字节布局 + 字节计数一致性校验重复 | 纯数据 + 解码校验 | 编码/解析委托 |
| `ReadRegistersRequest`（FC03/04 公共基类） | `Reference` + `WordCount`，构造校验 1..125 | 与契约同构 | 半数据半行为 | 保留语义，编码委托 |
| `ReadMultipleRegistersRequest` / `ReadInputRegistersRequest` | FC03/FC04 请求 | 编码与解析重复 | 纯数据 | 编码/解析委托 |
| `ReadRegistersResponse` + FC03/04 响应 | `int[] Values` + `Register[] Registers` | 字节计数偶数校验、大端读取重复 | 纯数据 + 解码校验 | 编码/解析委托 |
| `WriteCoilRequest` / `WriteCoilResponse` | FC05，`CoilState` / `CoilValue`（0xFF00 / 0x0000） | 线圈值常量与布局重复 | 纯数据 | 编码/解析委托 |
| `WriteRegisterRequest` / `WriteRegisterResponse` | FC06，`Register` / `Value` | 布局重复 | 纯数据 | 编码/解析委托 |
| `WriteMultipleCoilsRequest` / `WriteMultipleCoilsResponse` | FC0F，`BitVector` + `BitCount` | 位打包、字节计数校验重复 | 纯数据 + 解码校验 | 编码/解析委托 |
| `WriteMultipleRegistersRequest` / `WriteMultipleRegistersResponse` | FC10，`Register[]` + `WordCount` | 字节计数校验、大端写入重复 | 纯数据 + 解码校验 | 编码/解析委托 |
| `ExceptionResponse` | 原功能码 + 异常码，`FunctionCode = 原码 \| 0x80` | 异常码拼接与布局重复 | 纯数据 + 异常语义 | 编码委托 |
| `ModbusResponseFactory` | 按功能码分发响应类型，优先识别异常标记 | 无编解码重复（只做语义路由） | 行为（请求-响应配对） | 保留 |
| `ModbusPduWriter` / `ModbusPduReader` | 大端写/读缓冲 | 与契约层 `BinaryPrimitives` 用法同源但 API 不同 | 通用工具（扩展功能码仍用） | 保留 |
| `Diagnostics` / `CommEvent` / `ReportSlaveId` / `FileRecordMessages` / `MaskWriteRegister` / `ReadWriteMultipleRegisters` / `ReadFifoQueue` | FC07/08、0B/0C、11、14/15、16(0x16)、17、18 | **契约层无对应 PDU 类型，不存在第二份实现** | 各自语义 + 就地编解码 | 保留 |

结论：真正“同一协议事实存在两份”的只有核心 8 个功能码的 PDU 布局与解码校验；扩展功能码没有重复，强行上收只会把主站语义塞进契约层，收益为负。

## 三、收敛方案

边界划分：

- **契约层（Abstractions）负责“字节层”**：功能码、字段偏移、长度/字节计数校验、异常码、Span 编解码。
- **消息层（Messages）负责“语义层”**：单元号、事务号、`BitVector` / `Register` / `SimpleRegister` 业务类型、
  请求-响应配对、期望响应长度、对外异常类型的翻译。
- 帧组装与 CRC 不在消息层：消息对象只产出 PDU，MBAP/RTU/ASCII 组帧仍在 `IO/*Transport`。

实现要点：

1. 新增内部桥接 `Messages/PduBridge.cs`（`internal`，不进公开 API）：
   - `Write(IBufferWriter<byte>, ModbusPdu)`：`GetSpan` → `Encode` → `Advance`，保留原有“PDU 直接写进复用缓冲”的零中间数组路径。
   - `DecodeAs<T>(Func<T>, context)`：把契约层 `ModbusSlaveException` 转成主站对外的 `ModbusIOException`（`InnerException` 保留原异常），调用方捕获类型不变。
2. 请求编码按 `FunctionCode` 分发到对应契约 PDU。`ReadBitsRequest` 的合法子类只有 FC01/FC02，
   `ReadRegistersRequest` 只有 FC03/FC04，分发是完备映射，不会产生错误功能码。
3. 响应解析统一走契约层 `Decode`。为让主从共用同一份解码逻辑，在 `Pdu/Responses.cs` 为
   `ReadBitsResponse` / `ReadRegistersResponse` / `WriteSingleResponse` / `WriteMultipleResponse`
   补充了 4 个静态 `Decode(byte expectedFunctionCode, ReadOnlySpan<byte> pdu)`；
   主站 `FromPdu` 与从站解析都调用它们。
4. `ExceptionResponse.ToPdu()` 改由 `Abstractions.ModbusExceptionResponse` 生成 `FC|0x80` 编码。

## 四、类型对照表

| 消息类型（`Framework.Modbus.Messages`） | 收敛后的编码来源 | 收敛后的解析来源 |
|------------------------------------------|------------------|------------------|
| `ReadCoilsRequest` | `Abstractions.ReadCoilsRequest` | `Abstractions.ReadCoilsRequest.Decode` |
| `ReadInputDiscretesRequest` | `Abstractions.ReadDiscreteInputsRequest` | `Abstractions.ReadDiscreteInputsRequest.Decode` |
| `ReadCoilsResponse` / `ReadInputDiscretesResponse` | `Abstractions.ReadBitsResponse` | `Abstractions.ReadBitsResponse.Decode` |
| `ReadMultipleRegistersRequest` | `Abstractions.ReadHoldingRegistersRequest` | `Abstractions.ReadHoldingRegistersRequest.Decode` |
| `ReadInputRegistersRequest` | `Abstractions.ReadInputRegistersRequest` | `Abstractions.ReadInputRegistersRequest.Decode` |
| `ReadMultipleRegistersResponse` / `ReadInputRegistersResponse` | `Abstractions.ReadRegistersResponse` | `Abstractions.ReadRegistersResponse.Decode` |
| `WriteCoilRequest` | `Abstractions.WriteSingleCoilRequest` | `Abstractions.WriteSingleCoilRequest.Decode` |
| `WriteCoilResponse` | `Abstractions.WriteSingleResponse` | `Abstractions.WriteSingleResponse.Decode` |
| `WriteRegisterRequest` | `Abstractions.WriteSingleRegisterRequest` | `Abstractions.WriteSingleRegisterRequest.Decode` |
| `WriteRegisterResponse` | `Abstractions.WriteSingleResponse` | `Abstractions.WriteSingleResponse.Decode` |
| `WriteMultipleCoilsRequest` | `Abstractions.WriteMultipleCoilsRequest` | `Abstractions.WriteMultipleCoilsRequest.Decode` |
| `WriteMultipleCoilsResponse` | `Abstractions.WriteMultipleResponse` | `Abstractions.WriteMultipleResponse.Decode` |
| `WriteMultipleRegistersRequest` | `Abstractions.WriteMultipleRegistersRequest` | `Abstractions.WriteMultipleRegistersRequest.Decode` |
| `WriteMultipleRegistersResponse` | `Abstractions.WriteMultipleResponse` | `Abstractions.WriteMultipleResponse.Decode` |
| `ExceptionResponse` | `Abstractions.ModbusExceptionResponse` | 既有 `ExceptionResponse.FromPdu`（仅拆异常码，无布局重复） |
| `ModbusPduWriter` / `ModbusPduReader` | 保留（扩展功能码与主站内部仍使用） | 保留 |

契约层新增 4 个 `Decode` 不改变已有类型签名（只新增静态方法），对已发布的主从包是纯增量。

## 五、API 兼容与行为变化

**兼容部分**

- 本轮没有删除、移动或改名任何公开类型，`Messages.*` 所有公开成员签名不变，因此无需 `[Obsolete]` 转发；
  与上一轮“类型迁移才需要转发”的口径一致。
- 对外异常契约不变：解析失败一律抛 `ModbusIOException`，`InnerException` 为 `Abstractions.ModbusSlaveException`。
- `ModbusMaster`、`ModbusTcpMaster`、`ModbusSerialMaster`、`IModbusTransport` 等入口签名未动。
- 依赖方向不变：`Framework.Modbus` → `Framework.Modbus.Abstractions`（上轮已建立），无新增核心库互引。

**行为收紧（3 处，均属非法帧识别加强）**

1. 请求 `FromPdu` 现在校验计数范围：FC01/02 为 1..2000、FC03/04 为 1..125、FC0F ≤1968、FC10 ≤123。
   越界抛 `ModbusIOException`（内层 `IllegalDataValue`）；旧实现不校验、会接受非法计数。
2. FC05 请求的线圈值只接受 `0xFF00` / `0x0000`。旧实现把任何非 `0xFF00` 的值一律视为 `false`。
3. FC0F/FC10 请求校验“字节数与数量一致”。旧实现只按声明的字节数读取，忽略两者不一致。

**响应解析加强**

- `WriteCoilResponse` / `WriteRegisterResponse` / `WriteMultipleCoilsResponse` / `WriteMultipleRegistersResponse`
  的 `FromPdu` 现在校验响应功能码与期望一致；旧实现只校验长度，不校验功能码。
  合法响应结果完全一致，仅非法帧从“静默接受”变为抛异常。

## 六、构建与测试结果

构建：

```
dotnet build Framework.slnx
→ 0 个错误，3 个警告（Framework.Repository 的 NU1510，既有问题，与本轮无关）
```

测试（仅 `test/Framework.Modbus.Tests`）：

```
dotnet test test/Framework.Modbus.Tests/Framework.Modbus.Tests.csproj --no-build
→ 总计 110 / 成功 110 / 失败 0 / 跳过 0
```

新增用例：**7 个**，位于 `test/Framework.Modbus.Tests/MessagePduConvergenceTests.cs`：

| 用例 | 覆盖点 |
|------|--------|
| `ReadRequests_EncodeAsContract` | FC01/02/03/04 请求编码与契约逐字节一致 + 往返字段 |
| `ReadResponses_EncodeAndRoundTripAsContract` | 位/寄存器响应编码一致 + 往返 |
| `WriteRequests_EncodeAndRoundTripAsContract` | FC05/06/0F/10 请求编码一致 + 往返 |
| `WriteResponses_EncodeAndRoundTripAsContract` | 单个/多个写入响应编码一致 + 往返 |
| `ExceptionResponse_UsesContractEncoding` | 异常响应 `83 02` 编码与工厂解析（异常响应） |
| `IllegalResponsePdu_ThrowsModbusIOExceptionWrappingSlaveException` | 截断/计数不符的非法帧（非法帧） |
| `ContractDecode_RejectsFunctionCodeMismatch` | 契约层解码拒绝功能码不匹配（非法帧） |

超时路径未被改动，沿用既有覆盖：
`MasterIntegrationTests.Transport_SurfacesTimeoutWhenAllRetriesFail`（主站重试耗尽）与
`SerialFramingTests.Rtu_ReportsTimeoutWhenNoResponse`（串口无响应），两者在本次 110 例中均通过。

## 七、未统一 / 剩余项

1. **扩展功能码仍由 `Messages` 就地编解码**：FC07/08（`Diagnostics`）、FC0B/0C（`CommEvent`）、
   FC11（`ReportSlaveId`）、FC14/15（`FileRecordMessages`）、FC16(0x16)（`MaskWriteRegister`）、
   FC17（`ReadWriteMultipleRegisters`）、FC18（`ReadFifoQueue`）。
   原因：契约层没有对应 PDU 类型，也不存在第二份实现，上收只会把主站业务语义塞进契约层。
   触发条件：若从站后续也要支持这些功能码，再统一提升到 `Abstractions`。
2. **传输层组帧未收敛**：`IO/ModbusTcpTransport` / `ModbusRtuTransport` / `ModbusAsciiTransport`
   仍自行处理 MBAP/RTU/ASCII 封装。它们依赖增量流式读取与 `WritePdu` 零分配快速路径，
   与 `IModbusFrameCodec` 存在概念重叠但形态不同；改造需重做主站读写循环，属独立任务。
3. `ModbusPduWriter` / `ModbusPduReader` 与契约层的大端读写存在能力重叠，
   但它们是公开 API 且被扩展功能码使用，本轮保留；若扩展功能码上收后可一并评估收敛。
