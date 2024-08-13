using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore;

namespace Framework.AspNetCore.Test.Middleware;

public class ModbusMiddlewareTest : IClassFixture<ModbusMiddlewareFixture>
{
	private readonly ModbusMiddlewareFixture _fixture;

	public ModbusMiddlewareTest(ModbusMiddlewareFixture factory)
	{
		_fixture = factory;
	}
	[Fact]
	public void Modbus()
	{
		byte slaveId = 1;
		int port = 502;
		IPAddress address = new IPAddress(new byte[] { 127, 0, 0, 1 });

		//// create and start the TCP slave
		//TcpListener slaveTcpListener = new TcpListener(address, port);
		//slaveTcpListener.Start();

		var factory = new ModbusFactory();
		//var network = factory.CreateSlaveNetwork(slaveTcpListener);

		//IModbusSlave slave = factory.CreateSlave(slaveId);

		//network.AddSlave(slave);

		//var listenTask = network.ListenAsync();

		// create the master
		TcpClient masterTcpClient = new TcpClient(address.ToString(), port);
		IModbusMaster master = factory.CreateMaster(masterTcpClient);

		ushort numInputs = 5;
		ushort startAddress = 100;

		// read five register values
		ushort[] inputs = master.ReadInputRegisters(0, startAddress, numInputs);

		for (int i = 0; i < numInputs; i++)
		{
			Console.WriteLine($"Register {startAddress + i}={inputs[i]}");
		}

		// clean up
		masterTcpClient.Close();
		//slaveTcpListener.Stop();

	}
}
public class ModbusMiddlewareFixture : IDisposable
{
	private IHost _host;
	public ModbusMiddlewareFixture()
	{
		var builder = WebApplication
			.CreateBuilder();
		builder.WebHost.ConfigureKestrel((context, serverOptions) =>
				{
					serverOptions.ListenAnyIP(502, (listenOptions) => { listenOptions.UseModbus(); });
				});

		_host = builder.Build();
		_host.Start();
	}

	public void Dispose()
	{
		_host?.Dispose();
	}
}