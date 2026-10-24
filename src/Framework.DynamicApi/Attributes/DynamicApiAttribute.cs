using System;

namespace Framework.DynamicApi.Attributes;

[Serializable]
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class DynamicApiAttribute : Attribute
{
	/// <summary>
	/// 对应 MVC Areas 的区域名称，会作为路由中的 region 段参与拼接。
	/// </summary>
	public string Area { get; set; }

	/// <summary>
	/// 端点分组名称列表，生成代码会写入 <c>WithTags</c>。
	/// </summary>
	public string[] GroupNames { get; set; }
}
