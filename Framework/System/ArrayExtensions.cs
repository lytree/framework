using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Framework.System;

public static partial class ArrayExtensions
{
	public static void Fill<T>(this T[,] array, T value) => Fill((Array)array, value);
	public static void Fill<T>(this T[,,] array, T value) => Fill((Array)array, value);
	public static void Fill<T>(this T[,,,] array, T value) => Fill((Array)array, value);
	public static void Fill<T>(this T[,,,,] array, T value) => Fill((Array)array, value);

	static void Fill<T>(Array array, T value)
	{
		var data = MemoryMarshal.CreateSpan(
			ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)),
			array.Length);

		data.Fill(value);
	}
}
