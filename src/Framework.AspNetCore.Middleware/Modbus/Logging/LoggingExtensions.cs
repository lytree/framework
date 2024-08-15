using AspNetCore.Middleware.Modbus.Extensions;

namespace AspNetCore.Middleware.Modbus.Logging;

public static class LoggingExtensions
{

	#region Frame logging

	private static void LogFrame<T>(this ILogger<T> logger, string validPrefix, string invalidPrefix, byte[] frame)
	{

		if (logger.IsEnabled(LogLevel.Trace))
		{
			string prefix = frame.DoesCrcMatch() ? validPrefix : invalidPrefix;

			logger.LogTrace($"{prefix}: {string.Join(" ", frame.Select(static b => b.ToString("X2")))}");
		}

	}

	internal static void LogFrameTx<T>(this ILogger<T> logger, byte[] frame)
	{
		logger.LogFrame("TX", "tx", frame);
	}

	internal static void LogFrameRx<T>(this ILogger<T> logger, byte[] frame)
	{
		logger.LogFrame("RX", "rx", frame);
	}

	internal static void LogFrameIgnoreRx<T>(this ILogger<T> logger, byte[] frame)
	{
		logger.LogFrame("IR", "ir", frame);
	}

	#endregion
}