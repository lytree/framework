using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.DynamicApi.Helpers;

/// <summary>
/// 针对 <see cref="IServiceCollection"/> 的动态 API 内部辅助扩展方法集合。
/// 用于查询服务是否已注册、获取已注册的单例实例，以及根据 <see cref="IServiceProviderFactory{TContainerBuilder}"/> 构建容器。
/// </summary>
internal static class ServiceCollectionExtensions
{
	/// <summary>
	/// 泛型便捷重载，判断指定类型的服务是否已被注册到容器。
	/// </summary>
	/// <typeparam name="T">要检查的服务类型。</typeparam>
	/// <param name="services">服务集合。</param>
	/// <returns>已注册返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsAdded<T>(this IServiceCollection services)
	{
		return services.IsAdded(typeof(T));
	}

	/// <summary>
	/// 判断给定运行时类型的服务是否已被注册到容器。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="type">要检查的服务类型。</param>
	/// <returns>已注册返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsAdded(this IServiceCollection services, Type type)
	{
		return services.Any(d => d.ServiceType == type);
	}

	/// <summary>
	/// 获取已注册的泛型类型 <typeparamref name="T"/> 单例实例；若不存在则返回 <c>null</c>。
	/// 仅匹配 <see cref="ServiceDescriptor.ImplementationInstance"/>，忽略工厂与类型实现。
	/// </summary>
	/// <typeparam name="T">要获取的单例类型。</typeparam>
	/// <param name="services">服务集合。</param>
	/// <returns>已注册的单例实例或 <c>null</c>。</returns>
	public static T GetSingletonInstanceOrNull<T>(this IServiceCollection services)
	{
		return (T)services
			.FirstOrDefault(d => d.ServiceType == typeof(T))
			?.ImplementationInstance;
	}

	/// <summary>
	/// 获取已注册的泛型类型 <typeparamref name="T"/> 单例实例；若不存在则抛出异常。
	/// </summary>
	/// <typeparam name="T">要获取的单例类型。</typeparam>
	/// <param name="services">服务集合。</param>
	/// <returns>已注册的单例实例。</returns>
	/// <exception cref="InvalidOperationException">当指定服务未以 <see cref="ServiceDescriptor.ImplementationInstance"/> 方式注册时抛出。</exception>
	public static T GetSingletonInstance<T>(this IServiceCollection services)
	{
		var service = services.GetSingletonInstanceOrNull<T>();
		if (service == null)
		{
			throw new InvalidOperationException("Could not find singleton service: " + typeof(T).AssemblyQualifiedName);
		}

		return service;
	}

	/// <summary>
	/// 自动扫描已注册到容器中的 <see cref="IServiceProviderFactory{TContainerBuilder}"/> 实例并据此构建 <see cref="IServiceProvider"/>。
	/// 未找到任何工厂时回退到 <see cref="ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(IServiceCollection)"/>。
	/// 通过反射调用对应的泛型版本 <see cref="BuildServiceProviderFromFactory{TContainerBuilder}(IServiceCollection, Action{TContainerBuilder})"/>。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <returns>构建完成的 <see cref="IServiceProvider"/> 实例。</returns>
	public static IServiceProvider BuildServiceProviderFromFactory(this IServiceCollection services)
	{
		foreach (var service in services)
		{
			var factoryInterface = service.ImplementationInstance?.GetType()
				.GetTypeInfo()
				.GetInterfaces()
				.FirstOrDefault(i => i.GetTypeInfo().IsGenericType &&
									 i.GetGenericTypeDefinition() == typeof(IServiceProviderFactory<>));

			if (factoryInterface == null)
			{
				continue;
			}

			var containerBuilderType = factoryInterface.GenericTypeArguments[0];
			return (IServiceProvider)typeof(ServiceCollectionExtensions)
				.GetTypeInfo()
				.GetMethods()
				.Single(m => m.Name == nameof(BuildServiceProviderFromFactory) && m.IsGenericMethod)
				.MakeGenericMethod(containerBuilderType)
				.Invoke(null, new object[] { services, null });
		}

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 使用注册到容器中的 <see cref="IServiceProviderFactory{TContainerBuilder}"/> 构建 <see cref="IServiceProvider"/>。
	/// </summary>
	/// <typeparam name="TContainerBuilder">容器构造器类型（例如 Autofac 的 <c>ContainerBuilder</c>）。</typeparam>
	/// <param name="services">服务集合。</param>
	/// <param name="builderAction">可选的容器构造回调，用于在创建 <see cref="IServiceProvider"/> 之前进一步配置容器构造器。</param>
	/// <returns>构建完成的 <see cref="IServiceProvider"/> 实例。</returns>
	/// <exception cref="Exception">当未找到对应泛型工厂时抛出。</exception>
	public static IServiceProvider BuildServiceProviderFromFactory<TContainerBuilder>(this IServiceCollection services, Action<TContainerBuilder> builderAction = null)
	{

		var serviceProviderFactory = services.GetSingletonInstanceOrNull<IServiceProviderFactory<TContainerBuilder>>();
		if (serviceProviderFactory == null)
		{
			throw new Exception($"Could not find {typeof(IServiceProviderFactory<TContainerBuilder>).FullName} in {services}.");
		}

		var builder = serviceProviderFactory.CreateBuilder(services);
		builderAction?.Invoke(builder);
		return serviceProviderFactory.CreateServiceProvider(builder);
	}
}