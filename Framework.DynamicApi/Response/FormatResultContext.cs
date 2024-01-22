using System;

namespace Framework.DynamicApi.Response;

public static class FormatResultContext
{
    internal static Type FormatResultType = typeof(ResponseResult<>);
}