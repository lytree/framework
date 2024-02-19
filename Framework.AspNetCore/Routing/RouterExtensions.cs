using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Routing;

public static class RouterExtensions
{
    static readonly List<IBaseRouter> moduleList = new();

    private static IEnumerable<IBaseRouter> GetModules()
    {

        var modules = typeof(IBaseRouter).Assembly
            .GetTypes()
            .Where(p => p.IsClass && p.IsAssignableTo(typeof(IBaseRouter)))
            .Select(Activator.CreateInstance)
            .Cast<IBaseRouter>();

        return modules;
    }

    public static void AddRouters(this IEndpointRouteBuilder builder)
    {

        foreach (var module in moduleList)
        {
            module.AddModuleRoutes(builder);
        }

    }

    public static void AddIoC(this IServiceCollection services)
    {

        foreach (var module in GetModules())
        {
            module.AddModuleIoC(services);
            moduleList.Add(module);
        }

    }



}
