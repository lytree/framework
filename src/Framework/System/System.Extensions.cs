using Framework;
using Framework.System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace System;

/// <summary>
/// System.* 类型的扩展方法集合。单一 partial 文件，复制此文件即可携带所有扩展。
/// 注意：<see cref="DateTime"/> 相关扩展位于 <c>System.Extension</c>（单数），
/// 由 <c>System.Extension.cs</c> 维护，调用方通过扩展语法访问，与本类一致。
/// </summary>
public static partial class Extensions
{
    private static readonly MethodInfo CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<Type, object?> DefaultCache = new();

    #region IsXxx 类型判定

    /// <summary>
    /// 是否是基本数据类型
    /// </summary>
    public static bool IsPrimitive(this Type type)
    {
        if (type == typeof(string))
        {
            return true;
        }
        return type.IsValueType && type.IsPrimitive;
    }

    /// <summary>
    /// 判断类型是否是常见的简单类型
    /// </summary>
    public static bool IsSimpleType(this Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive || t.IsEnum || t == typeof(decimal) || t == typeof(string) || t == typeof(Guid) || t == typeof(TimeSpan) || t == typeof(Uri);
    }

    /// <summary>
    /// 是否是常见类型的数组形式
    /// </summary>
    public static bool IsSimpleArrayType(this Type type)
    {
        return type.IsArray && Type.GetType(type.FullName!.Trim('[', ']')).IsSimpleType();
    }

    /// <summary>
    /// 是否是常见类型的泛型形式
    /// </summary>
    public static bool IsSimpleListType(this Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsGenericType && type.GetGenericArguments().Length == 1 && type.GetGenericArguments().FirstOrDefault().IsSimpleType();
    }

    /// <summary>
    /// 是否是默认值
    /// </summary>
    public static bool IsDefaultValue(this object value)
    {
        if (value == default)
        {
            return true;
        }
        return value switch
        {
            byte s => s == 0,
            sbyte s => s == 0,
            short s => s == 0,
            char s => s == 0,
            bool s => s == false,
            ushort s => s == 0,
            int s => s == 0,
            uint s => s == 0,
            long s => s == 0,
            ulong s => s == 0,
            decimal s => s == 0,
            float s => s == 0,
            double s => s == 0,
            Enum s => Equals(s, Enum.GetValues(value.GetType()).GetValue(0)),
            DateTime s => s == DateTime.MinValue,
            DateTimeOffset s => s == DateTimeOffset.MinValue,
            Guid g => g == Guid.Empty,
            ValueType => DefaultCache.GetOrAdd(value.GetType(), t => Activator.CreateInstance(t))!.Equals(value),
            _ => false
        };
    }

    /// <summary>
    /// 判断是否为null，null或0长度都返回true
    /// </summary>
    public static bool IsNullOrEmpty<T>(this T value)
        where T : class
    {
        return value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            IEnumerable list => !list.GetEnumerator().MoveNext(),
            _ => false
        };
    }

    /// <summary>
    /// 转成非null
    /// </summary>
    public static T IfNull<T>(this T s, in T value)
    {
        return s ?? value;
    }

    #endregion

    #region Json

    /// <summary>
    /// 转换成json字符串
    /// </summary>
    public static string ToJsonString(this object obj, JsonSerializerOptions setting = null)
    {
        if (obj == null) return string.Empty;
        return JsonSerializer.Serialize(obj, setting);
    }

    /// <summary>
    /// json反序列化成对象
    /// </summary>
    public static T FromJson<T>(this string json, JsonSerializerOptions setting = null)
    {
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json, setting);
    }

    #endregion

    #region 类型转换

    /// <summary>
    /// 判断类型是否是数值类型
    /// </summary>
    public static bool IsNumeric(this Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or TypeCode.Double or TypeCode.Single => true,
            _ => false,
        };
    }

    /// <summary>
    /// 类型直转
    /// </summary>
    public static T ConvertTo<T>(this IConvertible value) where T : IConvertible
    {
        if (value != null)
        {
            var type = typeof(T);
            if (value.GetType() == type)
            {
                return (T)value;
            }
            if (type.IsNumeric())
            {
                return (T)value.ToType(type, new NumberFormatInfo());
            }
            if (value == DBNull.Value)
            {
                return default;
            }
            if (type.IsEnum)
            {
                return (T)Enum.Parse(type, value.ToString(CultureInfo.InvariantCulture));
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                return (T)(underlyingType!.IsEnum ? Enum.Parse(underlyingType, value.ToString(CultureInfo.CurrentCulture)) : Convert.ChangeType(value, underlyingType));
            }
            var converter = TypeDescriptor.GetConverter(value);
            if (converter.CanConvertTo(type))
            {
                return (T)converter.ConvertTo(value, type);
            }
            converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(value.GetType()))
            {
                return (T)converter.ConvertFrom(value);
            }
            return (T)Convert.ChangeType(value, type);
        }
        return (T)value;
    }

    /// <summary>
    /// 类型直转（带失败默认值）
    /// </summary>
    public static T TryConvertTo<T>(this IConvertible value, T defaultValue = default) where T : IConvertible
    {
        try { return value.ConvertTo<T>(); }
        catch (InvalidCastException) { return defaultValue; }
        catch (FormatException) { return defaultValue; }
    }

    /// <summary>
    /// 类型直转（out 形式）
    /// </summary>
    public static bool TryConvertTo<T>(this IConvertible value, out T result) where T : IConvertible
    {
        try { result = value.ConvertTo<T>(); return true; }
        catch (InvalidCastException) { result = default; return false; }
        catch (FormatException) { result = default; return false; }
    }

    /// <summary>
    /// 类型直转（Type 形式）
    /// </summary>
    public static bool TryConvertTo(this IConvertible value, Type type, out object result)
    {
        try { result = value.ConvertTo(type); return true; }
        catch (InvalidCastException) { result = default; return false; }
        catch (FormatException) { result = default; return false; }
    }

    /// <summary>
    /// 类型直转（Type 形式）
    /// </summary>
    public static object ConvertTo(this IConvertible value, Type type)
    {
        if (value == null) return default;
        if (value.GetType() == type) return value;
        if (value == DBNull.Value) return null;
        if (type.IsAssignableFrom(typeof(string))) return value.ToString();
        if (type.IsEnum) return Enum.Parse(type, value.ToString(CultureInfo.InvariantCulture));
        if (type.IsAssignableFrom(typeof(Guid))) return Guid.Parse(value.ToString());
        if (type.IsAssignableFrom(typeof(DateTime))) return DateTime.Parse(value.ToString());
        if (type.IsAssignableFrom(typeof(DateTimeOffset))) return DateTimeOffset.Parse(value.ToString());
        if (type.IsNumeric()) return value.ToType(type, new NumberFormatInfo());
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            return underlyingType!.IsEnum ? Enum.Parse(underlyingType, value.ToString(CultureInfo.CurrentCulture)) : Convert.ChangeType(value, underlyingType);
        }
        var converter = TypeDescriptor.GetConverter(value);
        if (converter.CanConvertTo(type)) return converter.ConvertTo(value, type);
        converter = TypeDescriptor.GetConverter(type);
        return converter.CanConvertFrom(value.GetType()) ? converter.ConvertFrom(value) : Convert.ChangeType(value, type);
    }

    /// <summary>
    /// 对象类型转换（泛型）
    /// </summary>
    public static T ChangeTypeTo<T>(this object @this)
    {
        return (T)@this.ChangeType(typeof(T));
    }

    /// <summary>
    /// 对象类型转换（Type）
    /// </summary>
    public static object ChangeType(this object @this, Type type)
    {
        var currType = Nullable.GetUnderlyingType(@this.GetType()) ?? @this.GetType();
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (@this == DBNull.Value)
        {
            if (!type.IsValueType) return null;
            throw new Exception("不能将null值转换为" + type.Name + "类型!");
        }
        if (currType == type) return @this;
        if (type.IsAssignableFrom(typeof(string))) return @this.ToString();
        if (type.IsEnum) return Enum.Parse(type, @this.ToString(), true);
        if (type.IsAssignableFrom(typeof(Guid))) return Guid.Parse(@this.ToString());
        if (!type.IsArray || !currType.IsArray) return Convert.ChangeType(@this, type);

        var length = ((Array)@this).Length;
        var targetType = Type.GetType(type.FullName.Trim('[', ']'));
        var array = Array.CreateInstance(targetType, length);
        for (int j = 0; j < length; j++)
        {
            var tmp = ((Array)@this).GetValue(j);
            array.SetValue(tmp.ChangeType(targetType), j);
        }
        return array;
    }

    #endregion

    #region Exception

    /// <summary>
    /// 获取异常的足迹（File / Method / LineNumber）
    /// </summary>
    public static string GetExceptionFootprints(this Exception exception)
    {
        StackTrace stackTrace = new(exception, true);
        StackFrame[] frames = stackTrace.GetFrames();
        if (frames is null) return string.Empty;

        var traceStringBuilder = new StringBuilder();
        for (var i = 0; i < frames.Length; i++)
        {
            StackFrame frame = frames[i];
            if (frame.GetFileLineNumber() < 1) continue;
            traceStringBuilder.AppendLine($"File: {frame.GetFileName()}");
            traceStringBuilder.AppendLine($"Method: {frame.GetMethod()?.Name}");
            traceStringBuilder.AppendLine($"LineNumber: {frame.GetFileLineNumber()}");
            if (i == frames.Length - 1) break;
            traceStringBuilder.AppendLine(" ---> ");
        }
        string stackTraceFootprints = traceStringBuilder.ToString();
        return string.IsNullOrWhiteSpace(stackTraceFootprints) ? "NO DETECTED FOOTPRINTS" : stackTraceFootprints;
    }

    #endregion

    #region 链式 / 字典化

    /// <summary>
    /// 链式操作
    /// </summary>
    public static T2 Next<T1, T2>(this T1 source, Func<T1, T2> action)
    {
        return action(source);
    }

    /// <summary>
    /// 将对象转换成字典
    /// </summary>
    public static Dictionary<string, object> ToDictionary(this object value)
    {
        var dictionary = new Dictionary<string, object>();
        if (value != null)
        {
            if (value is IDictionary dic)
            {
                foreach (DictionaryEntry e in dic)
                {
                    dictionary.Add(e.Key.ToString(), e.Value);
                }
                return dictionary;
            }
            foreach (var property in PropertyCache.GetOrAdd(value.GetType(), t => t.GetProperties()))
            {
                var obj = property.GetValue(value, null);
                dictionary.Add(property.Name, obj);
            }
        }
        return dictionary;
    }

    /// <summary>
    /// 将 JsonObject 转换成字典
    /// </summary>
    public static Dictionary<string, string> ToDictionary(this JsonObject value)
    {
        var dictionary = new Dictionary<string, string>();
        if (value != null)
        {
            using var enumerator = value.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var obj = enumerator.Current.Value ?? string.Empty;
                dictionary.Add(enumerator.Current.Key, obj + string.Empty);
            }
        }
        return dictionary;
    }

    /// <summary>
    /// 多个对象的属性值合并
    /// </summary>
    public static T Merge<T>(this T a, T b, params T[] others) where T : class
    {
        foreach (var item in new[] { b }.Concat(others))
        {
            var dic = item.ToDictionary();
            foreach (var p in dic.Where(p => a.GetProperty(p.Key).IsDefaultValue()))
            {
                a.SetProperty(p.Key, p.Value);
            }
        }
        return a;
    }

    /// <summary>
    /// 多个对象的属性值合并（集合形式）
    /// </summary>
    public static T Merge<T>(this IEnumerable<T> objects) where T : class
    {
        var list = objects as List<T> ?? [.. objects];
        switch (list.Count)
        {
            case 0: return null;
            case 1: return list[0];
        }
        foreach (var item in list.Skip(1))
        {
            var dic = item.ToDictionary();
            foreach (var p in dic.Where(p => list[0].GetProperty(p.Key).IsDefaultValue()))
            {
                list[0].SetProperty(p.Key, p.Value);
            }
        }
        return list[0];
    }

    #endregion

    #region String

    /// <summary>判断字符串是否为Null、空</summary>
    public static bool IsNull(this string str) => string.IsNullOrWhiteSpace(str);

    /// <summary>判断字符串是否不为Null、空</summary>
    public static bool NotNull(this string str) => !string.IsNullOrWhiteSpace(str);

    /// <summary>与字符串进行比较，忽略大小写</summary>
    public static bool EqualsIgnoreCase(this string str, string value) => str.Equals(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>首字母转小写</summary>
    public static string FirstCharToLower(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return string.Create(str.Length, str, (span, src) =>
        {
            src.CopyTo(span);
            span[0] = char.ToLowerInvariant(span[0]);
        });
    }

    /// <summary>首字母转大写</summary>
    public static string FirstCharToUpper(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return string.Create(str.Length, str, (span, src) =>
        {
            src.CopyTo(span);
            span[0] = char.ToUpperInvariant(span[0]);
        });
    }

    /// <summary>转为Base64（UTF-8）</summary>
    public static string ToBase64(this string str) => str.ToBase64(Encoding.UTF8);

    /// <summary>转为Base64（指定编码）</summary>
    public static string ToBase64(this string str, Encoding encoding)
    {
        if (str.IsNull()) return string.Empty;
        var bytes = encoding.GetBytes(str);
        return Helper.ToBase64(bytes);
    }

    /// <summary>反斜杠转正斜杠</summary>
    public static string ToPath(this string s)
    {
        if (s.IsNull()) return string.Empty;
        return s.Replace(@"\", "/");
    }

    /// <summary>截取固定长度</summary>
    public static string Limit(this string str, int length) => str.Length > length ? str[..length] : str;

    /// <summary>截取固定长度并加省略号</summary>
    public static string LimitWithEllipsis(this string str, int length) => str.Length > length ? str[..length] + "..." : str;

    /// <summary>字符串转指定类型数组</summary>
    public static T[] SplitToArray<T>(string str, char split)
    {
        if (string.IsNullOrEmpty(str)) return [];
        T[] arr = [.. str.Split([split.ToString()], StringSplitOptions.RemoveEmptyEntries).Cast<T>()];
        return arr;
    }

    #endregion
}

/// <summary>
/// 数组多维遍历工具 + Fill / Resize 扩展。
/// </summary>
public static partial class ArrayExtensions
{
    internal static void ForEach(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0) return;
        ArrayTraverse walker = new(array);
        do action(array, walker.Position);
        while (walker.Step());
    }

    internal class ArrayTraverse
    {
        public int[] Position;
        private readonly int[] _maxLengths;

        public ArrayTraverse(Array array)
        {
            _maxLengths = new int[array.Rank];
            for (int i = 0; i < array.Rank; ++i)
            {
                _maxLengths[i] = array.GetLength(i) - 1;
            }
            Position = new int[array.Rank];
        }

        public bool Step()
        {
            for (int i = 0; i < Position.Length; ++i)
            {
                if (Position[i] < _maxLengths[i])
                {
                    Position[i]++;
                    for (int j = 0; j < i; j++)
                    {
                        Position[j] = 0;
                    }
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>填充一维数组</summary>
    public static void Fill<T>(this T[] array, T value) => Fill((Array)array, value);
    /// <summary>填充二维数组</summary>
    public static void Fill<T>(this T[,] array, T value) => Fill((Array)array, value);
    /// <summary>填充三维数组</summary>
    public static void Fill<T>(this T[,,] array, T value) => Fill((Array)array, value);
    /// <summary>填充四维数组</summary>
    public static void Fill<T>(this T[,,,] array, T value) => Fill((Array)array, value);
    /// <summary>填充五维数组</summary>
    public static void Fill<T>(this T[,,,,] array, T value) => Fill((Array)array, value);

    /// <summary>调整 byte[] 长度</summary>
    public static byte[] Resize(this byte[] @this, int newSize)
    {
        Array.Resize(ref @this, newSize);
        return @this;
    }

    static void Fill<T>(Array array, T value)
    {
        var data = MemoryMarshal.CreateSpan(
            ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)),
            array.Length);
        data.Fill(value);
    }
}

