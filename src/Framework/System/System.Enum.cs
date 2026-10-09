using Framework.System;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Framework.System;

/// <summary>
/// <see cref="Enum"/> 的扩展方法集合。
/// 提供 <see cref="DescriptionAttribute"/> 的缓存读取、枚举项到 <c>Label/Value</c> 字典的批量导出等能力。
/// 内部使用 <see cref="ConcurrentDictionary{TKey, TValue}"/> 缓存反射结果以提升重复访问性能。
/// </summary>
public static class EnumExtension
{
    private static readonly ConcurrentDictionary<Enum, string> DescriptionCache = new();

    /// <summary>
    /// 获取枚举值上 <see cref="DescriptionAttribute"/> 标记的描述文本。
    /// 当未标记描述特性时，返回枚举项的字符串名称。
    /// 结果会按枚举值缓存以避免重复反射。
    /// </summary>
    /// <param name="item">当前枚举实例。</param>
    /// <returns>描述字符串；若没有 <see cref="DescriptionAttribute"/>，则返回枚举项名（例如 <c>"Foo"</c>）。</returns>
    public static string GetDescription(this Enum item)
        => DescriptionCache.GetOrAdd(item, v =>
        {
            string name = v.ToString();
            var desc = v.GetType().GetField(name)?.GetCustomAttribute<DescriptionAttribute>(false);
            return desc?.Description ?? name;
        });

    /// <summary>
    /// 将枚举值格式化为「名称(描述)」形式。当 <see cref="DescriptionAttribute"/> 不存在或描述为空时，仅返回名称部分。
    /// 注意：与 <see cref="GetDescription(Enum)"/> 不同，本方法不做缓存，每次调用都重新读取特性。
    /// </summary>
    /// <param name="item">当前枚举实例。</param>
    /// <returns>类似 <c>"Foo(描述)"</c> 或 <c>"Foo"</c> 的字符串。</returns>
    public static string ToNameWithDescription(this Enum item)
    {
        string name = item.ToString();
        var desc = item.GetType().GetField(name)?.GetCustomAttribute<DescriptionAttribute>(false);
        return $"{name}{(desc == null || desc.Description.IsNull() ? "" : $"({desc?.Description})")}";
    }

    /// <summary>
    /// 将枚举值转换为 <see cref="long"/>。底层使用 <see cref="Convert.ToInt64(object)"/>。
    /// </summary>
    /// <param name="item">当前枚举值。</param>
    /// <returns>对应的 <see cref="long"/> 数值。</returns>
    public static long ToInt64(this Enum item)
    {
        return Convert.ToInt64(item);
    }

    /// <summary>
    /// 将指定枚举类型的所有枚举项展开为 <c>Label/Value</c> 形式的字典列表。
    /// </summary>
    /// <param name="value">用于反射出枚举类型的任意枚举值。</param>
    /// <param name="ignoreNull">是否过滤掉名为 <c>"Null"</c> 的项（常用于带 <c>Null</c> 占位的枚举）。</param>
    /// <returns>包含 <c>Label</c>（描述）和 <c>Value</c>（枚举值）的字典列表；若 <paramref name="value"/> 不是枚举类型则返回 <c>null</c>。</returns>
    public static List<Dictionary<string, object>>? ToList(this Enum value, bool ignoreNull = false)
    {
        var enumType = value.GetType();

        if (!enumType.IsEnum)
            return null;

        return [.. Enum.GetValues(enumType).Cast<Enum>()
            .Where(m => !ignoreNull || !string.Equals(m.ToString(), "Null", StringComparison.Ordinal)).Select(x => new Dictionary<string, object>
            {
                ["Label"] = x.GetDescription(),
                ["Value"] = x
            })];
    }

    /// <summary>
    /// 将指定泛型 <typeparamref name="T"/> 对应枚举类型的所有枚举项展开为 <c>Label/Value</c> 形式的字典列表。
    /// </summary>
    /// <typeparam name="T">必须是 <see cref="Enum"/> 类型。</typeparam>
    /// <param name="ignoreNull">是否过滤掉名为 <c>"Null"</c> 的项。</param>
    /// <returns>字典列表；若 <typeparamref name="T"/> 不是枚举类型则返回 <c>null</c>。</returns>
    public static List<Dictionary<string, object>> ToList<T>(bool ignoreNull = false)
    {
        var enumType = typeof(T);

        if (!enumType.IsEnum)
            return null;

        return [.. Enum.GetValues(enumType).Cast<Enum>()
             .Where(m => !ignoreNull || !string.Equals(m.ToString(), "Null", StringComparison.Ordinal)).Select(x => new Dictionary<string, object>
             {
                 ["Label"] = x.GetDescription(),
                 ["Value"] = x
             })];
    }
}