// netstandard2.1 不包含 IsExternalInit，record / init 需要该编译器特性才能使用。
namespace System.Runtime.CompilerServices
{
	internal static class IsExternalInit
	{
	}
}
