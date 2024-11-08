using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Framework;

public static partial class Helper
{
    /// <summary>
    /// 判断对象是否是匿名类型。
    /// </summary>
    /// <param name="obj">对象。</param>
    /// <returns>是否匿名类型。</returns>
    /// <exception cref="ArgumentNullException">对象不能为空。</exception>
    public static bool IsAnonymousType(object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var type = obj.GetType();
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
            && type.IsGenericType && type.Name.Contains("AnonymousType")
            && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
            && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
    }
}
