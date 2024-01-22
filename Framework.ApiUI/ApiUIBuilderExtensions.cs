using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/* 项目“Framework.ApiUI (net8.0)”的未合并的更改
在此之前:
using Microsoft.AspNetCore.Hosting;
在此之后:
using Microsoft.AspNetCore.Hosting;
using Infrastructure;
using Infrastructure.ApiUI;
using Framework.ApiUI;
*/
using Microsoft.AspNetCore.Hosting;

/* 项目“Infrastructure.ApiUI (net8.0)”的未合并的更改
在此之前:
#if NETSTANDARD2_0
在此之后:
using ZhonTai;
using ZhonTai.ApiUI;
using Infrastructure.ApiUI;
#if NETSTANDARD2_0
*/
#if NETSTANDARD2_0
using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
#endif

namespace Framework.ApiUI
{
    public static class ApiUIBuilderExtensions
    {
        /// <summary>
        /// Register the SwaggerUI middleware with provided options
        /// </summary>
        public static IApplicationBuilder UseSwaggerUI(this IApplicationBuilder app, ApiUIOptions options)
        {
            return app.UseMiddleware<ApiUIMiddleware>(options);
        }

        /// <summary>
        /// Register the SwaggerUI middleware with optional setup action for DI-injected options
        /// </summary>
        public static IApplicationBuilder UseApiUI(
            this IApplicationBuilder app,
            Action<ApiUIOptions> setupAction = null)
        {
            ApiUIOptions options;
            using (var scope = app.ApplicationServices.CreateScope())
            {
                options = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ApiUIOptions>>().Value;
                setupAction?.Invoke(options);
            }

            // To simplify the common case, use a default that will work with the SwaggerMiddleware defaults
            if (options.ConfigObject.Urls == null)
            {
                var hostingEnv = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                options.ConfigObject.Urls = new[] { new UrlDescriptor { Name = $"{hostingEnv.ApplicationName} v1", Url = "v1/swagger.json" } };
            }

            return app.UseSwaggerUI(options);
        }
    }
}