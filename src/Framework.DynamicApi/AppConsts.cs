using Framework.DynamicApi.Enums;
using Framework.DynamicApi.Response;
using System;
using System.Collections.Generic;
using System.Reflection;


/// <summary>
/// 动态 API 模块的全局静态配置容器。
/// 用于集中维护 HTTP 动词映射、区域/路由前缀、命名约定以及结果包装等可被全局读取或覆盖的选项。
/// </summary>
public static class AppConsts
{
    /// <summary>
    /// 获取或设置默认的 HTTP 动词（当无法从方法名推断时使用）。
    /// </summary>
    public static string DefaultHttpVerb { get; set; }

    /// <summary>
    /// 获取或设置默认的区域名称（Areas）。
    /// </summary>
    public static string DefaultAreaName { get; set; } 

    /// <summary>
    /// 获取或设置动态 API 的默认路由前缀（例如 <c>api</c>）。
    /// </summary>
    public static string DefaultApiPreFix { get; set; }

    /// <summary>
    /// 获取或设置控制器名称识别时需要移除的常见后缀集合（例如 <c>AppService</c>、<c>Service</c>）。
    /// </summary>
    public static List<string> ControllerPostfixes { get; set; }

    /// <summary>
    /// 获取或设置 Action 名称识别时需要移除的常见后缀集合（例如 <c>Async</c>）。
    /// </summary>
    public static List<string> ActionPostfixes { get; set; }

    /// <summary>
    /// 获取或设置在进行 FromBody 绑定时需要忽略的类型集合（这些类型不会按 JSON 反序列化绑定）。
    /// </summary>
    public static List<Type> FormBodyBindingIgnoredTypes { get; set; }

    /// <summary>
    /// 获取或设置方法名前缀到 HTTP 动词的映射字典，用于根据方法名推断 RESTful 动作的 HTTP 动词。
    /// 初始化时已内置常见动词映射（add/create → POST、get/find → GET、update/put → PUT、delete → DELETE 等）。
    /// </summary>
    public static Dictionary<string,string> HttpVerbs { get; set; }

    /// <summary>
    /// 获取或设置动态 API 控制器与 Action 的命名约定。
    /// 默认为 <see cref="NamingConventionEnum.KebabCase"/>（短横线命名）。
    /// </summary>
    public static NamingConventionEnum NamingConvention { get; set; } = NamingConventionEnum.KebabCase;

    /// <summary>
    /// 获取或设置用于从服务名生成 RESTful 控制器名的委托。
    /// </summary>
    public static Func<string, string> GetRestFulControllerName { get; set; }

    /// <summary>
    /// 获取或设置用于从方法名生成 RESTful Action 名的委托。
    /// </summary>
    public static Func<string, string> GetRestFulActionName { get; set; }

    /// <summary>
    /// 获取或设置按程序集分组的动态 API 配置字典，每个程序集可单独指定 ApiPrefix 等选项。
    /// </summary>
    public static Dictionary<Assembly, AssemblyDynamicApiOptions> AssemblyDynamicApiOptions { get; set; }

    /// <summary>
    /// 获取或设置是否将动态 API 的返回值包装为统一的响应结果格式。默认为 <c>true</c>。
    /// </summary>
    public static bool FormatResult { get; set; } = true;

    /// <summary>
    /// 获取或设置结果包装所使用的开放泛型类型（例如 <c>ResponseResult<T></c>）。
    /// 默认为 <see cref="FormatResultContext.FormatResultType"/>。
    /// </summary>
    public static Type FormatResultType { get; set; } = FormatResultContext.FormatResultType;

    static AppConsts()
    {
        HttpVerbs=new Dictionary<string, string>()
        {
            ["add"] = "POST",
            ["create"] = "POST",
            ["insert"] = "POST",
            ["submit"] = "POST",
            ["post"] = "POST",

            ["get"] = "GET",
            ["find"] = "GET",
            ["fetch"] = "GET",
            ["query"] = "GET",

            ["update"] = "PUT",
            ["change"] = "PUT",
            ["put"] = "PUT",
            ["batch"] = "PUT",
            ["patch"] = "PATCH",

            ["delete"] = "DELETE",
            ["soft"] = "DELETE",
            ["remove"] = "DELETE",
            ["clear"] = "DELETE",
        };
    }
}