
using Framework.SlideCaptcha.Resources;
using Framework.SlideCaptcha.Resources.Handler;
using Framework.SlideCaptcha.Resources.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

public static class CaptchaServiceCollectionExtensions
{
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
