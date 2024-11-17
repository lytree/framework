using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Middleware.FlowAnalyze
{
	sealed class FlowAnalyzer : IFlowAnalyzer
	{
		private const int INTERVAL_SECONDS = 5;
		private readonly FlowQueues readQueues = new(INTERVAL_SECONDS);
		private readonly FlowQueues writeQueues = new(INTERVAL_SECONDS);

		/// <summary>
		/// 收到数据
		/// </summary>
		/// <param name="flowType"></param>
		/// <param name="length"></param>
		public void OnFlow(FlowType flowType, int length)
		{
			if (flowType == FlowType.Read)
			{
				readQueues.OnFlow(length);
			}
			else
			{
				writeQueues.OnFlow(length);
			}
		}

		/// <summary>
		/// 获取流量分析
		/// </summary>
		/// <returns></returns>
		public FlowStatistics GetFlowStatistics()
		{
			return new FlowStatistics
			{
				TotalRead = readQueues.TotalBytes,
				TotalWrite = writeQueues.TotalBytes,
				ReadRate = readQueues.GetRate(),
				WriteRate = writeQueues.GetRate()
			};
		}

		private class FlowQueues
		{
			private int cleaning = 0;
			private long totalBytes = 0L;
			private record QueueItem(long Ticks, int Length);
			private readonly ConcurrentQueue<QueueItem> queues = new();

			private readonly int intervalSeconds;

			public long TotalBytes => totalBytes;

			public FlowQueues(int intervalSeconds)
			{
				this.intervalSeconds = intervalSeconds;
			}

			public void OnFlow(int length)
			{
				Interlocked.Add(ref totalBytes, length);
				CleanInvalidRecords();
				queues.Enqueue(new QueueItem(Environment.TickCount64, length));
			}

			public double GetRate()
			{
				CleanInvalidRecords();
				return (double)queues.Sum(item => item.Length) / intervalSeconds;
			}

			/// <summary>
			/// 清除无效记录
			/// </summary>
			/// <returns></returns>
			private bool CleanInvalidRecords()
			{
				if (Interlocked.CompareExchange(ref cleaning, 1, 0) != 0)
				{
					return false;
				}

				var ticks = Environment.TickCount64;
				while (queues.TryPeek(out var item))
				{
					if (ticks - item.Ticks < intervalSeconds * 1000)
					{
						break;
					}
					else
					{
						queues.TryDequeue(out _);
					}
				}

				Interlocked.Exchange(ref cleaning, 0);
				return true;
			}
		}
	}
}
