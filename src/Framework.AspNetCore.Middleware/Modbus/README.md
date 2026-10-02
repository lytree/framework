# Modbus 从站中间件

仿照 Java 的 [jamod](http://jamod.sourceforge.net/) 分层（Transport / PDU / ProcessImage）实现，
以本仓库既有的 Kestrel 原始 TCP 中间件风格落地。

当前范围：**仅从站（Slave）侧**，且**仅 TCP 形态**的两种承载方式。

| 传输承载 | 帧格式 | 入口 | 状态 |
|---------|-------|------|------|
| Modbus/TCP | MBAP 头（7 字节） | `listen.UseModbusTcp()` | 已实现 |
| RTU over TCP | 从站地址 + PDU + CRC16 | `listen.UseModbusRtuOverTcp()` | 已实现 |
| Modbus/UDP | MBAP 头 | — | 预留（见「扩展接缝」） |
| 串口 RTU | 地址 + PDU + CRC16 | — | 预留 |
| 串口 ASCII | `:` + HEX + LRC + CRLF | — | 预留 |

## 目录结构

```
Modbus/
├── ModbusFunctionCode.cs      功能码常量（FC01~FC16）
├── ModbusExceptionCode.cs     异常码常量
├── ModbusCrc16.cs             RTU 用的 CRC-16（多项式 0xA001）
├── ModbusSlaveException.cs    协议异常，携带异常码
├── ModbusFrameException.cs    ADU 帧结构错误（CRC 错误 / MBAP 头非法）
├── ModbusSlaveOptions.cs      从站地址、广播抑制
├── ModbusSlave.cs             从站核心：请求 PDU → 进程映像 → 响应 PDU
├── ModbusSlave.DirectAccess.cs 进程内直通读写（不经总线）+ 值变更事件
├── ModbusDataArea.cs          数据区枚举 / 写入来源枚举
├── ModbusValue.cs             位或寄存器的值（结构体，无装箱）
├── ModbusValueChangedEventArgs.cs 值变更事件参数
├── Pdu/                       与传输无关的协议数据单元
│   ├── ModbusPdu.cs           PDU 抽象（功能码 / 长度 / 编码）
│   ├── ModbusRequest.cs       按功能码分派的工厂 + 解析辅助
│   ├── ModbusResponse.cs      响应抽象
│   ├── ReadRequests.cs        FC01/02/03/04 读请求
│   ├── WriteRequests.cs       FC05/06/15/16 写请求
│   └── Responses.cs           读/写响应 + 异常响应
├── ProcessImage/              进程映像（从站内部数据模型）
│   ├── IProcessImage.cs       四类数据区接口
│   └── SimpleProcessImage.cs  定长数组实现，线程安全
├── Transport/                 ADU 帧编解码
│   ├── IModbusFrameCodec.cs   帧编解码接口
│   ├── MbapFrameCodec.cs      Modbus/TCP
│   └── RtuFrameCodec.cs       RTU over TCP
├── ModbusConnectionHandler.cs 连接处理器基类（读帧-处理-写帧）
├── ModbusTcpConnectionHandler.cs
├── ModbusRtuOverTcpConnectionHandler.cs
├── ListenOptionsExtensions.cs     UseModbusTcp / UseModbusRtuOverTcp
└── ServiceCollectionExtensions.cs AddModbusSlave
```

## 用法

```csharp
builder.Services.AddModbusSlave(options => options.UnitId = 1);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    // 标准 Modbus/TCP 端口
    kestrel.ListenAnyIP(502, listen => listen.UseModbusTcp());
    // 或者串口服务器常用的 RTU over TCP
    kestrel.ListenAnyIP(5020, listen => listen.UseModbusRtuOverTcp());
});
```

启动后从站应答功能码 01/02/03/04/05/06/15/16，数据取自 `IProcessImage`。

### 应用侧直接赋值（不经总线）

数据供给方在进程内时（采集程序、上位机、数据库轮询、仿真器），不需要走 TCP，
直接把值写进从站映像，总线上的客户端即可读到：

```csharp
public class MyService(ModbusSlave slave)
{
    public void Refresh(ReadOnlySpan<byte> rawFromDevice)
    {
        // 对应 FC04 读输入寄存器：大端字节流一次写一段
        slave.SetInputRegisters(0, rawFromDevice);
        slave.SetInputRegister(64, 0x1234);                 // 单点
        slave.SetDiscreteInputs(0, new bool[] { true, false }); // 对应 FC02
        slave.SetCoils(0, new bool[] { true, true });       // 对应 FC01（总线可读可写）
        slave.Clear(ModbusDataArea.Coil, 0, 8);             // 批量清零

        var v = slave.GetHoldingRegister(0);                // 也能直接读回
    }
}
```

- 四个数据区都有成对的 `Get*` / `Set*`；寄存器区额外提供「大端字节流」重载，
  便于把设备返回的原始字节直接灌入映像。
- 跨数据区的统一入口是 `SetValue(ModbusDataArea, address, value)` / `GetValue(...)`。
- 越界抛 `ArgumentOutOfRangeException`——本地代码用异常更利于定位问题；
  总线路径仍按规范翻译成 `IllegalDataAddress(0x02)`，两条路径互不影响。
- 想完全掌控数据来源时，也可以继续实现 `IProcessImage` 并用
  `AddModbusSlave(myProcessImage, options => ...)` 注册；`ModbusSlave` 的 Set/Get
  相当于在映像之上补了边界校验与变更通知。

### 感知写入（值变更事件）

总线客户端下发的写入（FC05/06/15/16）与进程内赋值都会触发同一个事件：

```csharp
slave.ValueChanged += (sender, e) =>
{
    if (e.Source == ModbusWriteSource.Bus)   // 客户端下发的命令
    {
        Console.WriteLine($"{e.Area}[{e.Address}] {e.OldValue} -> {e.NewValue}");
    }
};
```

- `Source` 区分 `Bus`（总线请求）与 `Internal`（进程内赋值）。
- 仅在值真正变化时触发；写入相同值不产生通知。
- 订阅者抛出的异常会被记录并吞掉，不会影响从站与总线的正常工作。

### 不依赖任何传输的入口

`slave.Execute(unitId, pdu)` 只吃请求 PDU、吐响应 PDU，与承载无关：
单元测试、进程内仿真，以及将来 UDP / 串口的收发循环都直接调它即可。

## 设计要点

- **分层**：`IModbusFrameCodec` 只负责「字节流 ↔ PDU」，`ModbusSlave` 只负责
  「请求 PDU ↔ 响应 PDU」，两者互不感知；新增一种承载只需实现一个编解码器。
- **零拷贝解析**：帧解析基于 `SequenceReader<byte>`，PDU 以 `ReadOnlySequence<byte>` 暴露；
  单内存段时直接使用 `FirstSpan`，跨段时才回退为数组拷贝。从站内部一律在
  `PipeReader.AdvanceTo` 之前完成消费，避免缓冲区被写入方复用时读到脏数据。
- **长度推断代替时序**：RTU 依赖 3.5 字符时间的静默间隔分帧，在 TCP 上无法复现，
  因此 `RtuFrameCodec` 按功能码推断帧长（FC01~06 固定 8 字节，FC15/16 由字节数域推出）。
- **异常一律转为协议响应**：地址越界回 `IllegalDataAddress(0x02)`，数量/数值非法回
  `IllegalDataValue(0x03)`，未知功能码回 `IllegalFunction(0x01)`，未预期异常回
  `SlaveDeviceFailure(0x04)`；从站不会因为一个坏请求而断开连接。
- **广播**：单元标识 0 视为广播，执行写入但不响应（`SuppressBroadcastResponse`，默认为 true）。
- **双写入路径**：总线写入与进程内直通写入共用同一套映像和变更事件，差异只在错误表达
  （协议异常码 vs `ArgumentOutOfRangeException`）与事件来源标记（`Bus` / `Internal`）。

## 扩展接缝

UDP 与串口（RTU / ASCII）尚未实现，但协议核心已经完全复用：

- **Modbus/UDP**：`MbapFrameCodec` 可直接复用，只需一个基于 `UdpClient` 的
  `IHostedService` 收发数据报（UDP 天然保留报文边界，一个数据报即一帧）。
- **串口 RTU**：`RtuFrameCodec` 可直接复用，配一个 `SerialPort` + 读循环的 `IHostedService`；
  串口上可真正按静默间隔分帧，因此可以把未知功能码的分帧策略换成定时切帧。
- **串口 ASCII**：新增一个 `AsciiFrameCodec`（`:` + HEX + LRC + CRLF）即可，
  PDU 层与从站核心无需改动。LRC 算法与 `src/Framework/Proxy/Lrc.cs` 同构。

## 已知限制

- 仅实现从站侧；主站（请求构造、事务重试、连接池）未实现，但请求类型均已实现
  `Encode`，可直接用于主站发送。
- `RtuFrameCodec` 遇到未知功能码时按最短帧（5 字节 PDU）处理，客户端需要自行重新同步。
- MBAP 头非法或 RTU CRC 校验失败时，该帧会被消费并记录警告，不再应答（符合规范）。
- 备注：`src/Framework/Proxy/Crc16.cs` 与 `Lrc.cs` 为 `internal` 且当前无任何引用，
  其 `HashCore` 循环上界写成 `i < count`（应为 `i < offset + count`），偏移非 0 时会算错。
  本目录因此自带了一份正确的 `ModbusCrc16`，未依赖上述实现。
