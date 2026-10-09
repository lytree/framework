using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework;

/// <summary>
/// 属性反射相关的辅助方法集，依赖 <see cref="TypeDescriptor"/> 反射基础设施，
/// 用于从类型 <typeparamref name="T"/> 的属性元数据中提取 <see cref="DisplayNameAttribute"/>、
/// <see cref="DisplayAttribute"/> 等声明性元数据，便于在 UI 绑定、表头生成、验证提示等场景复用。
/// </summary>
public static partial class Helper
{
    /// <summary>
    /// 通过 <see cref="TypeDescriptor"/> 反射获取类型 <typeparamref name="T"/> 上指定属性的 <see cref="DisplayNameAttribute.DisplayName"/> 值。
    /// </summary>
    /// <typeparam name="T">目标类型，必须具备由 <see cref="TypeDescriptor"/> 公开的属性元数据。</typeparam>
    /// <param name="propertyDisplayName">属性名。必须与 <see cref="TypeDescriptor.GetProperties(Type)"/> 返回的属性名精确匹配。</param>
    /// <returns>该属性上 <see cref="DisplayNameAttribute"/> 的 <see cref="DisplayNameAttribute.DisplayName"/> 文本。</returns>
    /// <exception cref="ArgumentNullException"><typeparamref name="T"/> 上不存在名为 <paramref name="propertyDisplayName"/> 的属性，或对应属性未标注 <see cref="DisplayNameAttribute"/> 时可能引发空引用异常（具体取决于运行时路径）。</exception>
    public static string GetAttributeDisplayName<T>(string propertyDisplayName)
    {
        return (TypeDescriptor.GetProperties(typeof(T))[propertyDisplayName].Attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute).DisplayName;
    }

    /// <summary>
    /// 通过 <see cref="TypeDescriptor"/> 反射获取类型 <typeparamref name="T"/> 上指定属性的 <see cref="DisplayAttribute.Name"/> 值。
    /// </summary>
    /// <typeparam name="T">目标类型，必须具备由 <see cref="TypeDescriptor"/> 公开的属性元数据。</typeparam>
    /// <param name="propertyDisplayName">属性名。必须与 <see cref="TypeDescriptor.GetProperties(Type)"/> 返回的属性名精确匹配。</param>
    /// <returns>该属性上 <see cref="DisplayAttribute"/> 的 <see cref="DisplayAttribute.Name"/> 文本。</returns>
    /// <exception cref="ArgumentNullException"><typeparamref name="T"/> 上不存在名为 <paramref name="propertyDisplayName"/> 的属性，或对应属性未标注 <see cref="DisplayAttribute"/> 时可能引发空引用异常（具体取决于运行时路径）。</exception>
    public static string GetAttributeDisplay<T>(string propertyDisplayName)
    {
        return (TypeDescriptor.GetProperties(typeof(T))[propertyDisplayName].Attributes[typeof(DisplayAttribute)] as DisplayAttribute).Name;
    }
}
