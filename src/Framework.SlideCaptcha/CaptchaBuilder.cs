using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha
{
	/// <summary>
	/// 滑块验证码的服务注册构建器。封装 <see cref="IServiceCollection"/>，用于链式注册资源提供者、处理器、校验器等。
	/// </summary>
	public class CaptchaBuilder
	{
		/// <summary>
		/// 底层服务集合。扩展方法通过它完成依赖注入注册。
		/// </summary>
		public IServiceCollection Services { get; set; }

		/// <summary>
		/// 使用给定的服务集合构造构建器。
		/// </summary>
		/// <param name="serviceCollection">用于注册验证码相关服务的 <see cref="IServiceCollection"/> 实例。</param>
		public CaptchaBuilder(IServiceCollection serviceCollection)
		{
			Services = serviceCollection;
		}
	}
}
