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
			services.TryAddSingleton<ModbusSlave>();
			return services;
		}
	}
}
