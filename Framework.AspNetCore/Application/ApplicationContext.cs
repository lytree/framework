using Microsoft.AspNetCore.Http.Features;

namespace Framework.AspNetCore.Application
{
	/// <summary>
	/// 表示应用程序请求上下文
	/// </summary>
	public abstract class ApplicationContext
	{
		/// <summary>
		/// 获取特征集合
		/// </summary>
		public IFeatureCollection Features { get; }

		/// <summary>
		/// 应用程序请求上下文
		/// </summary>
		/// <param name="features"></param>
		public ApplicationContext(IFeatureCollection features)
		{
			Features = new FeatureCollection(features);
		}
	}
}
