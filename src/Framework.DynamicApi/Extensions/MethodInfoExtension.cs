using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Framework.DynamicApi.Extensions;

/// <summary>
/// 针对 <see cref="MethodInfo"/> 的动态 API 反射辅助扩展方法集合。
/// 主要用于在生成动态 API 控制器时识别方法的异步性以及解析真实返回负载类型。
/// </summary>
public static class MethodInfoExtension
{
	/// <summary>
	/// 判断方法是否为异步方法。
	/// 仅识别返回 <see cref="Task"/> 或 <see cref="Task{TResult}"/> 的方法，对 <c>ValueTask</c>/<c>ValueTask&lt;T&gt;</c> 等不视作异步。
	/// </summary>
	/// <param name="method">待判断的方法元数据。</param>
	/// <returns>方法签名匹配异步模式返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsAsync(this MethodInfo method)
	{
		return method.ReturnType == typeof(Task)
			|| method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
	}

	// 异步方法解包为 Task<T> 的 T，否则返回原返回类型；空 Task 时回退为 typeof(void) 用于结果包装判断
	internal static Type GetReturnType(this MethodInfo method)
	{
		var isAsync = method.IsAsync();
		var returnType = method.ReturnType;
		return isAsync ? returnType.GenericTypeArguments.FirstOrDefault() ?? typeof(void) : returnType;
	}
}