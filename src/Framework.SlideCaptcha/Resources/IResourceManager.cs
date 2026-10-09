using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha.Resources
{
	/// <summary>
	/// 资源管理契约：在生成验证码时按需返回随机的背景图字节以及「滑块图 + 缺口图」二元组。
	/// 实现通常在构造时聚合若干 <see cref="Provider.IResourceProvider"/>，运行期通过 <see cref="Handler.IResourceHandlerManager"/> 解析字节。
	/// </summary>
	public interface IResourceManager
	{
		/// <summary>
		/// 随机返回一张背景图的字节内容。
		/// </summary>
		/// <returns>背景图原始字节。</returns>
		byte[] RandomBackground();

		/// <summary>
		/// 随机返回一个滑块/缺口模板对。
		/// </summary>
		/// <returns>二元组 <c>(slider, hole)</c>，第一个元素是滑块图字节，第二个元素是缺口图字节。</returns>
		(byte[], byte[]) RandomTemplate();
	}
}