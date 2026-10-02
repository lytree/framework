using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Middleware.Modbus
{
	/// <summary>
	/// Modbus 从站连接处理器基类：读取 ADU 帧、交给从站处理、写回响应帧
	/// </summary>
	public abstract class ModbusConnectionHandler : ConnectionHandler
	{
		private readonly ModbusSlave slave;
		private readonly ILogger logger;

		/// <summary>
		/// Modbus 从站连接处理器
		/// </summary>
		/// <param name="slave">Modbus 从站</param>
		/// <param name="logger">日志</param>
		protected ModbusConnectionHandler(ModbusSlave slave, ILogger logger)
		{
			this.slave = slave;
			this.logger = logger;
		}

		/// <summary>
		/// 创建本连接使用的帧编解码器
		/// </summary>
		/// <returns></returns>
		protected abstract IModbusFrameCodec CreateCodec();

		/// <inheritdoc/>
		public override async Task OnConnectedAsync(ConnectionContext context)
		{
			var codec = this.CreateCodec();
			var input = context.Transport.Input;
			var output = context.Transport.Output;
			var cancellation = context.ConnectionClosed;

			this.logger.LogDebug("{Protocol} 从站已接入连接 {ConnectionId}", codec.Name, context.ConnectionId);
			try
			{
				while (cancellation.IsCancellationRequested == false)
				{
					var result = await input.ReadAsync(cancellation);
					if (result.IsCanceled)
					{
						break;
					}

					if (this.ProcessBuffer(codec, result.Buffer, input, output))
					{
						await output.FlushAsync(cancellation);
					}

					if (result.IsCompleted)
					{
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				// 连接已关闭
			}

			this.logger.LogDebug("{Protocol} 从站连接已断开 {ConnectionId}", codec.Name, context.ConnectionId);
		}

		/// <summary>
		/// 解析缓冲区内的所有完整帧并写入响应（同一个读批次内完成，避免跨 await 持有 SequenceReader）
		/// </summary>
		/// <param name="codec">帧编解码器</param>
		/// <param name="buffer">本次读取到的数据</param>
		/// <param name="input">输入管道</param>
		/// <param name="output">输出管道</param>
		/// <returns>是否写入了响应，需要刷新输出</returns>
		private bool ProcessBuffer(IModbusFrameCodec codec, ReadOnlySequence<byte> buffer, PipeReader input, PipeWriter output)
		{
			var reader = new SequenceReader<byte>(buffer);
			var hasResponse = false;
			try
			{
				while (codec.TryReadFrame(ref reader, out var unitId, out var transactionId, out var pdu))
				{
					// pdu 指向缓冲区内部，本批次内不推进管道，直到 finally 统一 AdvanceTo
					var pduSpan = pdu.IsSingleSegment ? pdu.FirstSpan : pdu.ToArray();
					var response = this.slave.Execute(unitId, pduSpan);
					if (response is not null)
					{
						codec.WriteFrame(output, unitId, transactionId, response);
						hasResponse = true;
					}
				}
			}
			catch (ModbusFrameException ex)
			{
				this.logger.LogWarning("{Protocol} 丢弃一个非法帧：{Reason}", codec.Name, ex.Message);
			}
			finally
			{
				// 剩余数据标记为「已检查、未消费」，等待后续字节补齐
				input.AdvanceTo(buffer.GetPosition(reader.Consumed), buffer.End);
			}

			return hasResponse;
		}
	}
}
