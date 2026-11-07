# YLFramework.Modbus.Abstractions

Modbus **协议契约层**：主站（`YLFramework.Modbus`）与从站（`YLFramework.AspNetCore.Middleware` 的 Modbus 子模块）
共同依赖的唯一协议定义来源，避免功能码 / 异常码 / 校验 / PDU / 帧编解码出现两份实现。

## 内容

| 目录 / 类型 | 职责 |
|-------------|------|
| `ModbusConstants` | 协议默认参数、尺寸上限、各功能码的单次数量上限 |
| `ModbusFunctionCode` / `ModbusExceptionCode` | 功能码与异常码常量（含主站 / 从站两套命名的别名） |
| `ModbusCrc16` / `ModbusLrc` | RTU 的 CRC-16 与 ASCII 的 LRC 校验 |
| `ModbusPdu` / `ModbusRequest` / `ModbusResponse` 及 `Pdu/*` | 与传输无关的 Span 版 PDU 模型与请求/响应编解码 |
| `Pdu/*` 覆盖的功能码 | FC01/02/03/04/05/06/0F/10 核心码，以及 FC07/08/0B/0C/11/14/15/16/17/18 扩展码 |
| `ModbusDiagnosticsSubFunction` | FC08 Diagnostics 的子功能码常量 |
| `IModbusFrameCodec` / `MbapFrameCodec` / `RtuFrameCodec` | ADU 帧编解码（Modbus/TCP 与 RTU over TCP） |
| `ModbusSlaveException` / `ModbusFrameException` | 协议异常与帧结构异常 |

## 依赖

零外部 NuGet 依赖，仅使用 BCL（`System.Buffers`）。

## 版本

`2.0.0-alpha.1`，版本由本项目 csproj 的 `<Version>` 独立声明。
