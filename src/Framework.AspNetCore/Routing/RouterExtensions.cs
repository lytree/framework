using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Routing;

/// <summary>
/// 提供模块化路由与 IoC 注册的扩展方法。
/// <para>在 <c>Startup</c> 中通过 <see cref="AddIoC"/> 与 <see cref="AddRouters"/>
/// 自动发现所有实现 <see cref="IBaseRouter"/> 的模块，并按调用顺序执行其注册逻辑。</para>
/// </summary>
public static class RouterExtensions
{
    // 已注册 IoC 的模块缓存：避免在 AddRouters 时再次反射重建实例，
    // 并保证两个扩展方法操作的模块集合一致。
    static readonly List<IBaseRouter> moduleList = new();

    // 通过反射扫描 IBaseRouter 所在程序集，自动发现所有实现该接口的具体模块类。
    // 注意：模块类必须具有无参构造函数，否则 Activator.CreateInstance 将抛异常。
    private static IEnumerable<IBaseRouter> GetModules()
    {

        var modules = typeof(IBaseRouter).Assembly
            .GetTypes()
            .Where(p => p.IsClass && p.IsAssignableTo(typeof(IBaseRouter)))
            .Select(Activator.CreateInstance)
            .Cast<IBaseRouter>();

        return modules;
    }

    /// <summary>
    /// 将所有通过 <see cref="AddIoC"/> 注册过的模块的路由注册到 <paramref name="builder"/>。
    /// <para>调用前必须先在 DI 阶段执行 <see cref="AddIoC"/>，否则内部模块列表为空，将不会注册任何路由。</para>
    /// </summary>
    /// <param name="builder">ASP.NET Core 的终结点路由构建器。</param>
    public static void AddRouters(this IEndpointRouteBuilder builder)
    {

        foreach (var module in moduleList)
        {
            module.AddModuleRoutes(builder);
        }

    }

    /// <summary>
    /// 扫描所有实现 <see cref="IBaseRouter"/> 的模块并触发其 IoC 注册逻辑。
    /// <para>同时将发现的模块实例缓存到内部列表，供 <see cref="AddRouters"/> 使用。</para>
    /// </summary>
    /// <param name="services">ASP.NET Core 的依赖注入容器集合。</param>
    /// <exception cref="System.Reflection.TargetInvocationException">当某个模块类缺少无参构造函数时由反射触发。</exception>
    public static void AddIoC(this IServiceCollection services)
    {

        foreach (var module in GetModules())
        {
            module.AddModuleIoC(services);
            moduleList.Add(module);
        }

    }



}
