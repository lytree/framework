using System;

namespace Infrastructure.DynamicApi.Response;

public static class FormatResultContext
{
	internal static Type FormatResultType = typeof(ResponseResult<>);
}