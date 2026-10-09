using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Framework.SlideCaptcha.Generator;
using Framework.SlideCaptcha.Resources;
using Framework.SlideCaptcha.Resources.Handler;
using Framework.SlideCaptcha.Resources.Provider;
using Framework.SlideCaptcha.Storage;
using Framework.SlideCaptcha.Validator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha
{
	/// <summary>
	/// 滑块验证码的 <see cref="IServiceCollection"/> 扩展入口。集中注册验证码生成、校验、资源管理、存储等组件。
	/// </summary>
	public static class CaptchaServiceCollectionExtensions
	{
		/// <summary>
		/// 注册滑块验证码所需的全部默认服务，并返回一个 <see cref="CaptchaBuilder"/> 以便继续链式配置。
		/// </summary>
		/// <remarks>
		/// 默认注册：
		/// <list type="bullet">
		///   <item><description><see cref="IResourceProvider"/>：<see cref="OptionsResourceProvider"/>（来自配置的背景/模板）+ <see cref="EmbeddedResourceProvider"/>（内嵌模板）</description></item>
		///   <item><description><see cref="IResourceHandlerManager"/>：<see cref="CachedResourceHandlerManager"/></description></item>
		///   <item><description><see cref="IResourceManager"/>：<see cref="DefaultResourceManager"/></description></item>
		///   <item><description><see cref="IResourceHandler"/>：<see cref="FileResourceHandler"/> + <see cref="EmbeddedResourceHandler"/></description></item>
		///   <item><description><see cref="ICaptchaImageGenerator"/>：<see cref="DefaultCaptchaImageGenerator"/>（Scoped）</description></item>
		///   <item><description><see cref="ICaptcha"/>：<see cref="DefaultCaptcha"/>（Scoped）</description></item>
		///   <item><description><see cref="IStorage"/>：<see cref="DefaultStorage"/>（Scoped）</description></item>
		///   <item><description><see cref="IValidator"/>：<see cref="SimpleValidator"/>（Scoped）</description></item>
		/// </list>
		/// </remarks>
		/// <param name="services">要注册到的服务集合。</param>
		/// <param name="configuration">从 <c>SlideCaptcha</c> 节读取 <see cref="CaptchaOptions"/> 的配置源；传 <c>null</c> 时仅依赖 <paramref name="optionsAction"/>。</param>
		/// <param name="optionsAction">在配置绑定之后执行的二次配置委托，可用于覆盖默认值；传 <c>null</c> 表示不附加额外配置。</param>
		/// <returns>封装了同一 <see cref="IServiceCollection"/> 的 <see cref="CaptchaBuilder"/>，用于继续链式注册。</returns>
		public static CaptchaBuilder AddSlideCaptcha(this IServiceCollection services, IConfiguration configuration, Action<CaptchaOptions> optionsAction = default)
		{
            _ = services.Configure<CaptchaOptions>(configuration?.GetSection("SlideCaptcha"));
			if (optionsAction != null) services.PostConfigure(optionsAction);

			var builder = new CaptchaBuilder(services);
			services.AddSingleton<IResourceProvider, OptionsResourceProvider>();
			services.AddSingleton<IResourceProvider, EmbeddedResourceProvider>();
			services.AddSingleton<IResourceHandlerManager, CachedResourceHandlerManager>();
			services.AddSingleton<IResourceManager, DefaultResourceManager>();
			services.AddSingleton<IResourceHandler, FileResourceHandler>();
			services.AddSingleton<IResourceHandler, EmbeddedResourceHandler>();
			services.AddScoped<ICaptchaImageGenerator, DefaultCaptchaImageGenerator>();
			services.AddScoped<ICaptcha, DefaultCaptcha>();
			services.AddScoped<IStorage, DefaultStorage>();
			services.AddScoped<IValidator, SimpleValidator>();
			return builder;
		}
	}
}
