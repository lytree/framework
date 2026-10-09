using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha.Resources
{
	/// <summary>
	/// 验证码资源的统一描述：<see cref="Type"/> 标识资源种类（对应某个 <see cref="Handler.IResourceHandler"/>），
	/// <see cref="Data"/> 为具体位置（内嵌资源名或文件路径），<see cref="Extras"/> 用于携带附加信息（如背景图标签、来源等）。
	/// </summary>
	public class Resource
	{
		/// <summary>
		/// 初始化 <see cref="Resource"/> 的新实例，所有字段均未设置。
		/// 适用于通过对象初始化器逐步赋值 <see cref="Data"/>、<see cref="Type"/>、<see cref="Extras"/> 的场景。
		/// </summary>
		public Resource()
		{

		}

		/// <summary>
		/// 使用指定的资源类型和数据 <paramref name="data"/> 初始化 <see cref="Resource"/> 的新实例。
		/// </summary>
		/// <param name="type">资源类型标识，对应 <see cref="Handler.IResourceHandler.CanHandle"/> 的判定值，例如 <c>"embedded"</c>、<c>"file"</c>。</param>
		/// <param name="data">资源定位信息：对于 <c>embedded</c> 类型为完整的内嵌资源名（含命名空间前缀），对于 <c>file</c> 类型为绝对/相对文件路径。</param>
		public Resource(string type, string data)
		{
			Data = data;
			Type = type;
		}

		/// <summary>
		/// 资源定位数据。其含义由 <see cref="Type"/> 决定（内嵌资源名或文件路径）。
		/// </summary>
		public string Data { get; set; }

		/// <summary>
		/// 资源类型标识，用于选择对应的 <see cref="Handler.IResourceHandler"/>。
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// 附加元数据字典（例如背景图的描述标签、模板来源等业务自定义信息）。
		/// </summary>
		public Dictionary<string, object> Extras { get; set; }
	}
}