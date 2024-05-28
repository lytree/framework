using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Middleware.HttpProxy;
using Net.Middleware.Modbus;

namespace Microsoft.Extensions.DependencyInjection
{
	/// <summary>
	/// ServiceCollection扩展
	/// </summary>
	public static partial class ServiceCollectionExtensions
	{
		/// <summary>
		/// 添加Modbus Slave 端
		/// </summary>
		/// <param name="services"></param> 
		/// <returns></returns>
		public static IServiceCollection AddModbusTcpSlave(this IServiceCollection services, Func<IServiceProvider, ModbusSlaveNetwork> modbusSlaveNetwork)
		{
			services.TryAddSingleton(modbusSlaveNetwork);
			services.TryAddSingleton<ModbusConnectionHandler>();
			return services;
		}
		public static IServiceCollection AddModbusTcpSlave(this IServiceCollection services, Func<ModbusSlaveNetwork> modbusSlaveNetwork)
		{
			services.TryAddSingleton(modbusSlaveNetwork);
			services.TryAddSingleton<ModbusConnectionHandler>();
			return services;
		}
	}
}
