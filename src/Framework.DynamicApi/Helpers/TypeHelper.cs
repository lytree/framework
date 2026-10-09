using System;
using System.Reflection;

namespace Framework.DynamicApi.Helpers;

/// <summary>
/// 动态 API 内部的类型判定工具集。
/// 用于在动态生成 API 时判断对象是否为 Func、类型是否属于"可视为简单值类型"的扩展集合。
/// </summary>
internal static class TypeHelper
{
	/// <summary>
	/// 判断给定对象是否为开放泛型委托 <see cref="Func{TResult}"/> 的实例。
	/// </summary>
	/// <param name="obj">待检测对象。</param>
	/// <returns>对象是 <c>Func&lt;&gt;</c> 开放类型实例则返回 <c>true</c>；对象为 <c>null</c> 或非泛型则返回 <c>false</c>。</returns>
	public static bool IsFunc(object obj)
	{
		if (obj == null)
		{
			return false;
		}

		var type = obj.GetType();
		if (!type.GetTypeInfo().IsGenericType)
		{
			return false;
		}

		return type.GetGenericTypeDefinition() == typeof(Func<>);
	}

	/// <summary>
	/// 判断给定对象的运行时类型是否正好是 <see cref="Func{TReturn}"/>（指定具体类型参数）。
	/// </summary>
	/// <typeparam name="TReturn">Func 的返回值类型。</typeparam>
	/// <param name="obj">待检测对象。</param>
	/// <returns>对象为 <c>Func&lt;TReturn&gt;</c> 类型实例返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsFunc<TReturn>(object obj)
	{
		return obj != null && obj.GetType() == typeof(Func<TReturn>);
	}

	/// <summary>
	/// 判断类型是否属于"扩展的简单类型"集合，包括基本数值类型以及常用的不可变引用类型（<see cref="string"/>、<see cref="decimal"/>、<see cref="DateTime"/>、<see cref="DateTimeOffset"/>、<see cref="TimeSpan"/>、<see cref="Guid"/>）。
	/// 若 <paramref name="type"/> 为 <see cref="Nullable{T}"/>，会解包后再判断。
	/// </summary>
	/// <param name="type">待判断的 .NET 类型。</param>
	/// <param name="includeEnums">是否将枚举类型视为简单类型；默认为 <c>false</c>。</param>
	/// <returns>属于扩展简单类型集合则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	public static bool IsPrimitiveExtendedIncludingNullable(Type type, bool includeEnums = false)
	{
		if (IsPrimitiveExtended(type, includeEnums))
		{
			return true;
		}

		if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			return IsPrimitiveExtended(type.GenericTypeArguments[0], includeEnums);
		}

		return false;
	}

	// 不处理 Nullable<T>，仅判断原始类型；被 IsPrimitiveExtendedIncludingNullable 包裹用于递归解包
	private static bool IsPrimitiveExtended(Type type, bool includeEnums)
	{
		if (type.GetTypeInfo().IsPrimitive)
		{
			return true;
		}

		if (includeEnums && type.GetTypeInfo().IsEnum)
		{
			return true;
		}

		return type == typeof(string) ||
			   type == typeof(decimal) ||
			   type == typeof(DateTime) ||
			   type == typeof(DateTimeOffset) ||
			   type == typeof(TimeSpan) ||
			   type == typeof(Guid);
	}
}
