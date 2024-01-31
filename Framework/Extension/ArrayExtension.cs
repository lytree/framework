using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Framework.Extension;

public static class ArrayExtension
{
	public static void Fill<T>(this T[,] array, T value) => Fill<T>((Array)array, value);
	public static void Fill<T>(this T[,,] array, T value) => Fill<T>((Array)array, value);
	public static void Fill<T>(this T[,,,] array, T value) => Fill<T>((Array)array, value);
	public static void Fill<T>(this T[,,,,] array, T value) => Fill<T>((Array)array, value);

	static void Fill<T>(Array array, T value)
	{
		var data = MemoryMarshal.CreateSpan(
			ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)),
			array.Length);

		data.Fill(value);
	}
}
