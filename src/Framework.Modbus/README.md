# YLFramework.Modbus

Modbus **主站（Master）** 实现。设计参照经典的 Java Modbus 库 [jamod](http://jamod.sourceforge.net/)（`net.wimpi.modbus`），
在 .NET 上以惯用风格复刻其全部 Master 侧能力。

## 特性

| 能力 | 说明 |
|------|------|
| 传输通道 | **Modbus TCP** / **Modbus UDP**（MBAP 报文头）、**Modbus RTU**（CRC16）、**Modbus ASCII**（LRC） |
| 功能码 | FC01 / 02 / 03 / 04 / 05 / 06 / 07 / 08 / 0B / 0C / 0F / 10 / 11 / 14 / 15 / 16 / 17 / 18 |
| 报文层 | 独立的请求 / 响应对象，PDU 可独立编解码，便于单元测试与报文抓包分析 |
| 事务层 | TCP / UDP / 串口三种事务，内置事务标识递增、重试与静默间隔 |
| 门面 API | `ModbusMasterFactory` + 定位器（`BaseLocator`）统一读写入口 |
| 异常 | 从站异常响应统一映射为 `SlaveException`，携带功能码与异常码 |

## 架构

```
Facade    ModbusMasterFactory / ModbusTcpMaster / ModbusUdpMaster / ModbusSerialMaster
          BaseLocator / ReadReference / WriteReference        ← 定位器读写
   ↑
Master    ModbusMaster                                        ← createXxxRequest + 便捷读写
   ↑
IO        IModbusTransport / ModbusIpTransport(TCP·UDP)
          ModbusRtuTransport / ModbusAsciiTransport            ← 组帧、校验、重试
          ModbusTcpTransaction / ModbusUdpTransaction / ModbusSerialTransaction
   ↑
Net       TcpConnection / UdpConnection / SerialConnection     ← 纯字节流
   ↑
Messages  ModbusRequest / ModbusResponse / ExceptionResponse   ← PDU 编解码（全部功能码）
Util      BitVector / ModbusUtil(CRC16·LRC·hex) / SerialParameters
```

## 快速开始

### Modbus TCP

```csharp
using Framework.Modbus.Facade;
using Framework.Modbus.ProcessImage;

using var master = ModbusMasterFactory.CreateTcpMaster("192.168.1.10", 502);
master.SetUnitId(1);
master.Connect();

// 读 16 个线圈
var coils = master.ReadCoils(0, 16);
Console.WriteLine(coils);

// 读 4 个保持寄存器并写入第一个
var registers = master.ReadMultipleRegisters(0, 4);
Console.WriteLine(string.Join(", ", registers.Select(r => r.Value)));
master.WriteRegister(0, 1234);
```

### Modbus RTU（串口）

```csharp
using Framework.Modbus.Facade;
using Framework.Modbus.Util;

var master = ModbusMasterFactory.CreateRtuMaster(
    portName: "/dev/ttyUSB0",
    baudRate: 9600,
    dataBits: 8,
    parity: System.IO.Ports.Parity.None,
    stopBits: System.IO.Ports.StopBits.One);

master.Timeout = 1000;
master.SetUnitId(1);
master.Connect();

var values = master.ReadInputRegisterValues(0, 8);   // FC04
```

### 定位器（jamod 风格）

```csharp
using Framework.Modbus.Facade;

var status = BaseLocator.CoilStatus(unitId: 1, reference: 0, count: 8);
var setpoint = BaseLocator.HoldingRegister(unitId: 1, reference: 10);

var bits = (BitVector)master.Read(status);       // 位区 → BitVector
master.Write(setpoint, 3000);                    // 寄存器区 → FC06
```

### 底层报文（可直接编解码）

```csharp
using Framework.Modbus.IO;
using Framework.Modbus.Messages;

// 单独抓取一次交互的线序字节
var transport = ModbusTcpTransport.Create("127.0.0.1", 502);
transport.MessageSent += (_, e) => Console.WriteLine($"→ {e.Hex}");
transport.MessageReceived += (_, e) => Console.WriteLine($"← {e.Hex}");

// PDU 往返
var request = new WriteMultipleRegistersRequest(0, new[] { 1, 2, 3 });
byte[] pdu = request.ToPdu();
var parsed = WriteMultipleRegistersRequest.FromPdu(pdu);
```

## 关键约定

- **地址一律使用 PDU 偏移（从 0 开始）**，与 jamod 的 `BaseLocator` 一致；
  习惯上的 40001 / 10001 等 5 位/6 位引用号需要自行减去基址。
- **位序遵循 Modbus 线序**：第一个线圈对应首字节的最低位（LSB）。`BitVector` 的索引 0 即第一个线圈。
- **寄存器为大端（高字节在前）**。
- **FC14 读文件记录的响应不携带起始记录号**，解析结果的记录号从 0 顺序编号。

## 单元测试

```bash
dotnet test test/Framework.Modbus.Tests/Framework.Modbus.Tests.csproj
```
