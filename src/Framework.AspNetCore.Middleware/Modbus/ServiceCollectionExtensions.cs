using Microsoft.Extensions.DependencyInjection.Extensions;
using Middleware.Modbus;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// ServiceCollection扩展
	/// </summary>
	public static partial class ServiceCollectionExtensions
	{
		/// <summary>
		/// 添加Modbus从站及其默认进程映像（0~65535完整地址空间）
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configure">从站选项配置</param>
		/// <returns></returns>
		public static IServiceCollection AddModbusSlave(this IServiceCollection services, Action<ModbusSlaveOptions>? configure = null)
		{
			var options = new ModbusSlaveOptions();
			configure?.Invoke(options);

			services.TryAddSingleton(options);
			services.TryAddSingleton<IProcessImage>(_ => new SimpleProcessImage());
			AddModbusSlaveHosts(services);
			services.TryAddSingleton<ModbusSlave>();
			return services;
		}

		/// <summary>
		/// 添加Modbus从站，并指定进程映像实例
		/// </summary>
		/// <param name="services"></param>
		/// <param name="processImage">进程映像实例</param>
		/// <param name="configure">从站选项配置</param>
		/// <returns></returns>
		public static IServiceCollection AddModbusSlave(this IServiceCollection services, IProcessImage processImage, Action<ModbusSlaveOptions>? configure = null)
		{
			var options = new ModbusSlaveOptions();
			configure?.Invoke(options);

			services.TryAddSingleton(options);
			services.TryAddSingleton(processImage);
			AddModbusSlaveHosts(services);
			services.TryAddSingleton<ModbusSlave>();
			return services;
		}

		/// <summary>
		/// 注册扩展功能码宿主捆绑：通过 DI 中已注册的可选宿主装配，未注册的宿主使用安全兜底实现
		/// </summary>
		/// <param name="services"></param>
		private static void AddModbusSlaveHosts(IServiceCollection services)
		{
			services.TryAddSingleton(sp => new ModbusSlaveHosts(
				sp.GetService<IModbusSlaveExceptionStatusHost>(),
				sp.GetService<IModbusSlaveDiagnosticsHost>(),
				sp.GetService<IModbusSlaveCommunicationEventHost>(),
				sp.GetService<IModbusSlaveIdentityHost>(),
				sp.GetService<IModbusFileRecordStore>(),
				sp.GetService<IModbusFifoQueueStore>()));
		}
	}
}
