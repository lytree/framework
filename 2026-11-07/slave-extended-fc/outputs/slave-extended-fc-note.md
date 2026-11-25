# Middleware Modbus 从站扩展功能码说明（FC07 / 08 / 0B / 0C / 11 / 14 / 15 / 16 / 17 / 18）

日期：2026-11-07
范围：仅 `src/Framework.AspNetCore.Middleware` 的 Modbus 从站侧能力扩展；复用 `Framework.Modbus.Abstractions` 已建好的 PDU。
未触碰：主站 `Framework.Modbus` 行为、`IO/*Transport` 组帧、其他包（Logging / Mvvm / Charts 等）。

## 1. 从站语义结论表

| 功能码 | 是否实现 | 实现程度 | 依赖状态 | 未提供状态时的异常 | 判断依据 |
|--------|----------|----------|----------|--------------------|----------|
| FC07 读异常状态 | 是 | 完整：返回 8 位异常状态字 | `IModbusSlaveExceptionStatusHost` | `IllegalFunction (0x01)` | 异常状态是设备自定义量，无法从进程映像推断；旧行为为非法功能，保持兜底语义 |
| FC08 诊断 | 部分 | 子功能 0x0000 回显可用；0x0002 / 0x000A / 0x000B~0x0012 / 0x0014 经宿主 | `IModbusSlaveDiagnosticsHost` | 计数器类子功能未配置时 `IllegalFunction` | 0x0000 不依赖状态；计数器与诊断寄存器必须由设备维护 |
| FC08 诊断（0x0001 / 0x0003 / 0x0004） | 否 | 不实现 | 需要传输层控制（重启通信、改 ASCII 分隔符、只听模式） | `IllegalFunction` | 属于串行链路 IO 控制，明确不在本轮范围内 |
| FC0B 取通信事件计数器 | 是 | 完整：返回状态字 + 事件计数 | `IModbusSlaveCommunicationEventHost` | `IllegalFunction` | 事件计数语义由设备/协议实现决定，从站不自行伪造 |
| FC0C 取通信事件记录 | 是 | 完整：状态字 + 事件计数 + 报文计数 + 事件字节 | `IModbusSlaveCommunicationEventHost` | `IllegalFunction`；事件字节超长时 `IllegalDataValue` | 事件缓冲维护策略属于宿主职责；响应 PDU 超过 253 字节按非法数据值拒绝 |
| FC11 报告从站标识 | 是 | 完整：返回宿主提供的标识数据 | `IModbusSlaveIdentityHost` | `IllegalFunction`；数据超长时 `IllegalDataValue` | 标识内容（厂商/型号/运行指示）属于设备信息，不硬编码 |
| FC14 读文件记录 | 是 | 完整：按「文件号 + 记录号 + 长度」读取 | `IModbusFileRecordStore.Read` | 未配置宿主 `IllegalFunction`；记录不存在 `IllegalDataAddress` | 文件记录是独立地址空间，不能映射到进程映像 |
| FC15 写文件记录 | 是 | 完整：写入并回显请求 | `IModbusFileRecordStore.Write` | 未配置宿主 `IllegalFunction`；写入被拒 `IllegalDataAddress` | 同上；地址是否存在由宿主判断 |
| FC16 屏蔽写寄存器 | 是 | 完整：按规范公式 `(当前 AND 与掩码) OR (或掩码 AND NOT 与掩码)` | 无（复用 `IProcessImage`） | 地址越界 `IllegalDataAddress` | 本质是对保持寄存器的读改写，必须走既有进程映像抽象 |
| FC17 读写多个寄存器 | 是 | 完整：先执行写、再执行读 | 无（复用 `IProcessImage`） | 读/写地址越界 `IllegalDataAddress` | 规范要求写先于读；同样复用进程映像 |
| FC18 读 FIFO 队列 | 是 | 完整：返回队列前 31 个寄存器 | `IModbusFifoQueueStore.Read` | 未配置宿主 `IllegalFunction`；地址不存在 `IllegalDataAddress`；数量超 31 `IllegalDataValue` | FIFO 指针地址与队列存储由设备定义，不映射到进程映像 |

> 说明：契约层 `ModbusRequest.Create` 已对 10 个扩展功能码做 PDU 解析与分派，不再一律抛 `IllegalFunction`；「未提供宿主」时的异常由从站按上表返回，从而保持扩展前的旧语义。

## 2. 新增宿主接口（可选扩展点）

全部位于 `src/Framework.AspNetCore.Middleware/Modbus/Hosting/ModbusSlaveHosts.cs`：

| 接口 | 用途 | 关键成员 |
|------|------|----------|
| `IModbusSlaveExceptionStatusHost` | FC07 | `byte GetExceptionStatus()` |
| `IModbusSlaveDiagnosticsHost` | FC08 | `GetDiagnosticRegister()`、`GetCounter(ushort)`、`ClearCounters()`、`ClearOverrunCounter()` |
| `IModbusSlaveCommunicationEventHost` | FC0B / FC0C | `GetStatus()`、`GetEventCount()`、`GetMessageCount()`、`GetEvents()` |
| `IModbusSlaveIdentityHost` | FC11 | `GetSlaveIdData()` |
| `IModbusFileRecordStore` | FC14 / FC15 | `Read(file, record, length)`、`Write(file, start, values)` |
| `IModbusFifoQueueStore` | FC18 | `Read(address)` |

`ModbusSlaveHosts` 是这六个宿主的捆绑：
- 构造函数每个参数都可为 `null`，为 `null` 时使用内置兜底实现；
- `ModbusSlaveHosts.Default` 表示「全部未实现」；
- 兜底实现在被调用时抛 `ModbusSlaveException.IllegalFunction(...)`，不会导致从站崩溃。

## 3. 实现与兜底策略

1. **契约层**：`src/Framework.Modbus.Abstractions/Pdu/ModbusRequest.cs` 增加 10 个扩展功能码的 `Create` 分支，直接复用已有的响应/请求 PDU，未修改任何 PDU 语义或校验。
2. **从站分派**：`ModbusSlave.Dispatch` 增加 10 个 `case`，实现集中在 `ModbusSlave.Extended.cs`。
3. **宿主注入**：
   - 保留原三参构造函数 `(options, processImage, logger)`，转发到 `ModbusSlaveHosts.Default`，公开 API 源码兼容；
   - 新增四参构造函数 `(options, processImage, logger, hosts)`；
   - `ServiceCollectionExtensions` 新增 `AddModbusSlaveHosts`，从 DI 中解析已注册的可选宿主；未注册的宿主使用兜底实现。
4. **进程映像访问**：FC16 / FC17 只用 `IProcessImage` 的 `ReadHoldingRegister` / `WriteHoldingRegister`，并复用 `RaiseValueChanged`，未另起读写路径。
5. **异常映射**：
   - 契约层请求格式错误 → `IllegalDataValue`；
   - 地址 / 记录不存在 → `IllegalDataAddress`；
   - 宿主未实现或子功能不支持 → `IllegalFunction`；
   - 其他内部异常仍按现有 `Execute` 逻辑映射为 `SlaveDeviceFailure`。

## 4. 测试结果

- 构建：`dotnet build Framework.slnx` 成功，0 error；仅 NU1510（Repository 既有告警）与 Telnet 既有 XML 文档告警。
- 测试：仅运行 `test/Framework.Modbus.Tests`，**146 通过 / 0 失败 / 0 跳过**。
- 新增用例：`test/Framework.Modbus.Tests/SlaveExtendedFunctionCodeTests.cs` 共 **26** 个用例：
  - FC07 / 0B / 0C / 11 / 18：各 2~3 例（正常 + 无宿主非法功能，部分含地址异常）；
  - FC08：4 例（回显、计数器、不支持子功能、截断请求）；
  - FC14 / 15：各 3 例（正常、地址异常、无宿主）；
  - FC16 / 17：各 2 例（正常、地址越界）；
  - 另有 1 例验证契约层 `ModbusRequest.Create` 对 10 个扩展码的分派与回显。
- 测试工程新增 `Framework.AspNetCore.Middleware` 的 `ProjectReference`，宿主使用内存假实现（`FakeSlaveHosts`）。

## 5. 明确不实现的项及原因

| 项 | 原因 |
|----|------|
| FC08 子功能 0x0001 重启通信 | 需要重置串口/链路状态，超出从站 PDU 层能力，且本轮禁止触碰 `IO/*Transport` |
| FC08 子功能 0x0003 修改 ASCII 输入分隔符 | 影响 ASCII 帧编解码，属于传输层配置 |
| FC08 子功能 0x0004 强制只听模式 | 需要关闭发送通道，属于传输层控制；协议也不要求响应 |
| FC14 / FC15 映射到进程映像 | 文件记录是独立地址空间，映射会扭曲协议语义 |
| FC18 映射到进程映像 | FIFO 指针地址与队列生命周期由设备定义，不适合绑定到寄存器区 |
| FC0B / FC0C 从站内置事件缓冲 | 事件定义与缓冲策略属于设备/产品语义，本轮只提供宿主扩展点 |

## 6. 需要产品/协议拍板的点

以下问题未自行决定，保持默认 `IllegalFunction`，待确认后再实现内置默认行为：

1. FC0B / FC0C 是否需要从站**内置**维护一份通信事件缓冲（默认事件集合、容量、是否随重启清空）；当前必须由宿主注入。
2. FC08 计数器和诊断寄存器是否需要从站内置默认实现（例如按收发报文自动计数），以及各计数器口径。
3. FC07 是否允许无宿主时默认返回 `0x00`（无异常），FC11 是否允许默认返回一个内置标识（例如 `unitId + 运行指示`）。
4. FC18 读取是否需要「读即弹出」（本实现把是否移除交给宿主；规范表述存在不同实现惯例）。
