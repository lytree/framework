using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Middleware.HttpProxy;
using AspNetCore.Middleware.Modbus;

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
		public static IServiceCollection AddModbusTcpSlave(this IServiceCollection services, Func<IServiceProvider, ModbusFactory> modbudFactory)
		{
			services.TryAddSingleton(modbudFactory);
			services.TryAddSingleton<ModbusTcpConnectionHandler>();
			return services;
		}
		public static IServiceCollection AddModbusTcpSlave(this IServiceCollection services, Func<ModbusFactory> modbudFactory)
		{
			services.TryAddSingleton(modbudFactory);
			services.TryAddSingleton<ModbusTcpConnectionHandler>();
			return services;
		}
		public static IServiceCollection AddModbusRtuOverTcpSlave(this IServiceCollection services, Func<IServiceProvider, ModbusFactory> modbudFactory)
		{
			services.TryAddSingleton(modbudFactory);
			services.TryAddSingleton<ModbusRtuOverTcpConnectionHandler>();
			return services;
		}
		public static IServiceCollection AddModbusRtuOverTcpSlave(this IServiceCollection services, Func<ModbusFactory> modbudFactory)
		{
			services.TryAddSingleton(modbudFactory);
			services.TryAddSingleton<ModbusRtuOverTcpConnectionHandler>();
			return services;
		}
	}
}
