using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Routing;

public interface IBaseRouter
{
    void AddModuleRoutes(IEndpointRouteBuilder app);
    void AddModuleIoC(IServiceCollection services);
}
