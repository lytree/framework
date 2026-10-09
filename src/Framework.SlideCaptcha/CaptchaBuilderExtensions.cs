using Microsoft.Extensions.DependencyInjection;
using Framework.SlideCaptcha.Resources.Handler;
using Framework.SlideCaptcha.Resources.Provider;
using Framework.SlideCaptcha.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Framework.SlideCaptcha
{
	/// <summary>
	/// <see cref="CaptchaBuilder"/> 的链式配置扩展，集中放置资源提供者、资源处理器、校验器的注册逻辑。
	/// </summary>
	public static class CaptchaBuilderExtensions
	{
		/// <summary>
		/// 注册一个自定义的 <see cref="IResourceProvider"/>（用于追加背景图或模板来源），单例生命周期。
		/// </summary>
		/// <typeparam name="TProvider">实现 <see cref="IResourceProvider"/> 的具体类型。</typeparam>
		/// <param name="builder">当前构建器。</param>
		/// <returns>同一个构建器实例，便于继续链式调用。</returns>
		public static CaptchaBuilder AddResourceProvider<TProvider>(this CaptchaBuilder builder) where TProvider : class, IResourceProvider
		{
			builder.Services.AddSingleton<IResourceProvider, TProvider>();
			return builder;
		}

		/// <summary>
		/// 注册一个自定义的 <see cref="IResourceHandler"/>（用于支持新的资源类型，例如 base64、网络 URL 等），单例生命周期。
		/// </summary>
		/// <typeparam name="THandler">实现 <see cref="IResourceHandler"/> 的具体类型。</typeparam>
		/// <param name="builder">当前构建器。</param>
		/// <returns>同一个构建器实例，便于继续链式调用。</returns>
		public static CaptchaBuilder AddResourceHandler<THandler>(this CaptchaBuilder builder) where THandler : class, IResourceHandler
		{
			builder.Services.AddSingleton<IResourceHandler, THandler>();
			return builder;
		}

		/// <summary>
		/// 用指定的校验器替换默认的 <see cref="SimpleValidator"/>，用于启用自定义的位置/行为校验策略。
		/// </summary>
		/// <typeparam name="TValidator">实现 <see cref="IValidator"/> 的具体类型。</typeparam>
		/// <param name="builder">当前构建器。</param>
		/// <returns>同一个构建器实例，便于继续链式调用。</returns>
		public static CaptchaBuilder ReplaceValidator<TValidator>(this CaptchaBuilder builder) where TValidator : class, IValidator
		{
			builder.Services.Replace<IValidator, TValidator>();
			return builder;
		}

		/// <summary>
		/// 移除 <see cref="EmbeddedResourceProvider"/> 的注册，从而禁用框架自带的内嵌模板。
		/// 通常用于希望完全由 <see cref="OptionsResourceProvider"/> 提供模板的场景。
		/// </summary>
		/// <param name="builder">当前构建器。</param>
		/// <returns>同一个构建器实例，便于继续链式调用。</returns>
		public static CaptchaBuilder DisableDefaultTemplates(this CaptchaBuilder builder)
		{
			var serviceDescriptor = builder.Services.FirstOrDefault(e => e.ImplementationType == typeof(EmbeddedResourceProvider));
			if (serviceDescriptor != null)
			{
				builder.Services.Remove(serviceDescriptor);
			}

			return builder;
		}
	}
}
