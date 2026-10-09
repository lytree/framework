using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Routing;

/// <summary>
/// 模块路由与依赖注册的契约。
/// <para>业务模块实现该接口以声明其路由注册与 IoC 注册逻辑，
/// 由 <see cref="RouterExtensions.AddIoC"/> 和 <see cref="RouterExtensions.AddRouters"/>
/// 在框架启动阶段自动发现并调用。</para>
/// </summary>
public interface IBaseRouter
{
    /// <summary>
    /// 将本模块的终结点（路由、Hub、gRPC 服务等）注册到 <paramref name="app"/>。
    /// </summary>
    /// <param name="app">ASP.NET Core 的终结点路由构建器，承载所有模块共用的路由表。</param>
    void AddModuleRoutes(IEndpointRouteBuilder app);
    /// <summary>
    /// 将本模块需要的服务注册到 <see cref="IServiceCollection"/>（如仓储、Service、Singleton 等）。
    /// </summary>
    /// <param name="app">ASP.NET Core 的依赖注入容器集合。</param>
    void AddModuleIoC(IServiceCollection services);
}
