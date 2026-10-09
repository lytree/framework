using System;
using System.Reflection;
using Framework.DynamicApi.Attributes;
using Framework.DynamicApi.Helpers;

namespace Framework.DynamicApi;

/// <summary>
/// 控制器筛选策略接口。
/// 在 MVC 控制器发现阶段被 <see cref="DynamicApiControllerFeatureProvider"/> 调用，用于决定某个 .NET 类型是否应作为动态 API 控制器暴露。
/// 可替换默认实现以自定义筛选规则（例如加入命名空间白名单、只扫描特定基类的子类等）。
/// </summary>
public interface ISelectController
{
	/// <summary>
	/// 判断给定类型是否应作为动态 API 控制器被注册。
	/// </summary>
	/// <param name="type">候选类型的运行时 <see cref="Type"/>。</param>
	/// <returns>若应作为动态 API 控制器返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	bool IsController(Type type);
}

internal class DefaultSelectController : ISelectController
{
	/// <summary>
	/// 默认的控制器筛选实现。
	/// 要求类型必须实现 <see cref="IDynamicApi"/>、是公共非抽象非泛型类，并标注了 <see cref="DynamicApiAttribute"/>，且未被 <see cref="NonDynamicApiAttribute"/> 排除。
	/// </summary>
	/// <param name="type">候选类型。</param>
	/// <returns>满足上述全部条件时返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public bool IsController(Type type)
	{
		var typeInfo = type.GetTypeInfo();

		if (!typeof(IDynamicApi).IsAssignableFrom(type) ||
			!typeInfo.IsPublic || typeInfo.IsAbstract || typeInfo.IsGenericType)
		{
			return false;
		}


		var attr = ReflectionHelper.GetSingleAttributeOrDefaultByFullSearch<DynamicApiAttribute>(typeInfo);

		if (attr == null)
		{
			return false;
		}

		if (ReflectionHelper.GetSingleAttributeOrDefaultByFullSearch<NonDynamicApiAttribute>(typeInfo) != null)
		{
			return false;
		}

		return true;
	}
}