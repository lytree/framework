using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace System;
/// <summary>
/// 数组扩展类
/// </summary>
public static partial class Extensions
{
	/// <summary>
	/// 填充一维数组
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="array">数组</param>
	/// <param name="value">默认值</param>
	public static void Fill<T>(this T[,] array, T value) => Fill((Array)array, value);
	/// <summary>
	/// 填充二维数组
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="array">数组</param>
	/// <param name="value">默认值</param>
	public static void Fill<T>(this T[,,] array, T value) => Fill((Array)array, value);
	/// <summary>
	/// 填充三维数组
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="array">数组</param>
	/// <param name="value">默认值</param>
	public static void Fill<T>(this T[,,,] array, T value) => Fill((Array)array, value);
	/// <summary>
	/// 填充四维数组
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="array">数组</param>
	/// <param name="value">默认值</param>
	public static void Fill<T>(this T[,,,,] array, T value) => Fill((Array)array, value);


	/// <summary>
	///     A byte[] extension method that resizes the byte[].
	/// </summary>
	/// <param name="this">The @this to act on.</param>
	/// <param name="newSize">Size of the new.</param>
	/// <returns>A byte[].</returns>
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
