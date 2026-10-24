# YLFramework.DynamicApi

基于 Roslyn 增量源生成器的 Minimal API 框架：类标 `[DynamicApi]` 即可在**编译期**生成端点注册代码，运行时不扫描程序集、不依赖反射，可在 Native AOT 下工作。

> 3.0 起不再提供 MVC 约定式实现（`AddDynamicApi()` / `UseDynamicApi()` 已移除），详见文末“与旧版差异”。

## 特性

- **编译期生成**：`ControllerFeatureProvider`、`IApplicationModelConvention` 全部移除，无运行时类型扫描
- **零反射**：路由拼接为纯字符串运算，参数绑定、JSON 读写均使用编译期类型信息
- **AOT 友好**：生成 `RequestDelegate` 端点，不依赖 `RequestDelegateGenerator` 对第三方生成代码的拦截
- **统一包装**：默认把返回值包成 `ResponseResul<T>`，可在运行时关闭或按方法禁用
- **跨程序集聚合**：类库各自生成入口，Web 项目通过 `DynamicApiModules` 汇总
- **编译期诊断**：路由冲突、无法绑定的参数等在编译期暴露

## 快速开始

```csharp
using Framework.DynamicApi;
using Framework.DynamicApi.Attributes;

[DynamicApi]
public class UserService : IDynamicApi
{
    public Task<UserDto> GetUserAsync(long id) => ...;

    public Task<long> CreateUserAsync(CreateUserRequest req) => ...;
}
```

```csharp
using Framework.DynamicApi.Generated.Web;   // 命名空间 = Generated.{当前程序集名净化}

builder.Services.AddScoped<UserService>();   // 服务必须注册到 DI

var app = builder.Build();
app.MapDynamicApis();                        // 或 app.MapDynamicApis(new DynamicApiOptions { ... })

app.Run();
```

生成的路由（默认 KebabCase、前缀 `api`）：

| 方法 | 路由 |
|---|---|
| `GetUserAsync(long id)` | `GET /api/user/get-user?id=1` |
| `CreateUserAsync(CreateUserRequest req)` | `POST /api/user/create-user`（JSON body） |

路由模板固定为 `{DefaultApiPrefix}/{Area}/{controller}/{action}`，控制器名剥离 `Service`/`AppService` 等后缀，Action 名剥离 `Async`。

两个边界约定：

- **重载方法**：Action 名只由方法名决定，同名重载会解析出相同路由并触发 `YLDAPI004`；生成器仍按各自签名生成端点，但请求会命中歧义，请改用不同方法名。
- **继承方法**：只扫描服务类自身声明的公开实例方法，基类中的方法不会生成端点。

## 运行时配置（`DynamicApiOptions`）

| 选项 | 说明 |
|---|---|
| `DefaultApiPrefix` | 公共前缀，默认 `api` |
| `DefaultAreaName` | `[DynamicApi]` 未指定 `Area` 时使用的区域段 |
| `RemoveControllerPostfixes` / `RemoveActionPostfixes` | 名称后缀剥离规则 |
| `NamingConvention` | `KebabCase` / `SnakeCase` / `ExtensionCase` / `PascalCase` / `CamelCase` / `Custom` |
| `GetRestFulControllerName` / `GetRestFulActionName` | `Custom` 约定下的名称格式化委托 |
| `FormatResult` | 是否包装响应，默认 `true` |

HTTP 动词、参数绑定方式、返回形态均在编译期确定，不受运行时代码影响。

## 参数绑定

| 参数 | 生成方式 |
|---|---|
| `int/long/...`、`Guid`、`DateTime`、`DateTimeOffset`、`TimeSpan`、`decimal`、`string`、可空值类型 | 查询串（`[FromQuery]` 或默认）；`[FromRoute]` 从路由值读取 |
| 复杂类型 + `POST/PUT/PATCH` | JSON 请求体（`[FromBody]` 或默认） |
| `[FromServices]` 标注 | `HttpContext.RequestServices.GetRequiredService<T>()` |
| `HttpContext` / `HttpRequest` / `HttpResponse` / `ClaimsPrincipal` / `CancellationToken` | 直接传入 |
| `Task<T>` / `ValueTask<T>` / `T` | 取负载后写入响应 |
| `Task` / `void` | 写 200 空响应 |
| `IResult` | 直接 `ExecuteAsync` |

缺失或格式非法的查询参数返回 `400`。

**不支持**（编译期 `YLDAPI006` 并跳过端点）：`IFormFile`、`IFormFileCollection`、`Stream`、`[FromForm]`、`[AsParameters]`、返回 `IActionResult`、GET 类请求上的复杂类型参数（`YLDAPI003`）。

## 特性

| 特性 | 作用 |
|---|---|
| `[DynamicApi(Area = "...", GroupNames = new[] { "..." })]` | 标记服务类；Area 参与路由，GroupNames 写入 `WithTags` |
| `[NonDynamicApi]` | 类型/方法级排除 |
| `[NonDynamicMethod]` | 方法级排除 |
| `[NonFormatResult]` | 方法级关闭响应包装 |

### API 分组示例

分组只支持类级声明（旧版的 `[ApiGroup]` 已移除），通过 `[DynamicApi]` 的 `GroupNames` 指定：

```csharp
[DynamicApi(Area = "admin", GroupNames = new[] { "用户管理", "内部接口" })]
public class UserService : IDynamicApi
{
    public Task<UserDto> GetUserAsync(long id) => ...;

    public Task<long> CreateUserAsync(CreateUserRequest req) => ...;
}
```

生成结果：

```csharp
// 路由：Area 参与拼接（未指定时回退 options.DefaultAreaName）
//   GET  /api/admin/user/get-user?id=1
//   POST /api/admin/user/create-user

// Tag：控制器名 + GroupNames 全部写入 WithTags
endpoint0.WithTags(controllerName, "用户管理", "内部接口");
endpoint0.WithName("Web.UserService.GetUserAsync");
```

分组只影响 OpenAPI/Swagger 的 tag；同一服务类下的所有端点共享同一组 tag，无法按方法单独分组。

端点名默认是 `{类型全名}.{方法名}`，用于 `WithName` / 链接生成。同一服务类内存在重载方法时，会附加参数类型签名（如 `Web.UserService.GetAsync(long)`）以保证唯一，否则 ASP.NET Core 会因端点名重复在启动阶段抛异常。

## 跨程序集（类库）

类库中同样引用 `YLFramework.DynamicApi`（生成器随包提供），会生成 `Framework.DynamicApi.Generated.{程序集名净化}.DynamicApiEndpoints`。Web 项目声明模块：

```xml
<PropertyGroup>
  <DynamicApiModules>MyLib;MyApp.Core</DynamicApiModules>
</PropertyGroup>
```

自己的 `MapDynamicApis()` 会自动调用这些模块入口。程序集名中的 `.` 净化为 `_`（`MyApp.Core` → `MyApp_Core`）。模块入口缺失时报 `YLDAPI005`。

> 注意：分析器不随 `ProjectReference` 传递，每个含 `[DynamicApi]` 类的项目都必须自行引用该 NuGet 包（或在本仓库内以 `OutputItemType="Analyzer"` 引用生成器项目）。

## Native AOT

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

响应写入与请求体读取使用 `JsonSerializerOptions.GetTypeInfo`，因此需要注册源生成的 JSON 上下文：

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default));

[JsonSerializable(typeof(ResponseResul<UserDto>))]   // 每个被包装的负载
[JsonSerializable(typeof(ResponseResul<long>))]
[JsonSerializable(typeof(CreateUserRequest))]        // 每个请求体类型
internal partial class AppJsonContext : JsonSerializerContext { }
```

JIT 下不注册也能工作（回退反射解析）；AOT 下缺少 `JsonTypeInfo` 会在写响应/读请求体时失败。

## 诊断

| ID | 级别 | 含义 |
|---|---|---|
| `YLDAPI001` | Warning | 类型/方法带有 MVC 路由特性，已跳过 |
| `YLDAPI003` | Warning | GET 类请求上的复杂类型参数无法绑定，端点已跳过 |
| `YLDAPI004` | Warning | 同程序集内控制器名 + 方法名 + 动词重复，路由可能冲突 |
| `YLDAPI005` | Warning | `DynamicApiModules` 声明的模块入口不存在 |
| `YLDAPI006` | Warning | 参数或返回类型不支持生成式绑定，端点已跳过 |
| `YLDAPI007` | Error | 多个服务类净化后得到同一生成类型名（如 `A.B.Svc` 与 `A_B.Svc`），后者端点被跳过 |

生成器目标框架为 `netstandard2.1`：可在 `dotnet build` / VS Code / Rider 下加载；Visual Studio 2022 与桌面 `msbuild.exe`（.NET Framework 编译器宿主）无法加载该分析器。

## 与旧版（≤2.x，MVC 约定式）的差异

| 旧版 | 3.0 |
|---|---|
| `AddDynamicApi()` / `UseDynamicApi()` | 移除，改为 `app.MapDynamicApis()` |
| `ISelectController` / `IActionRouteFactory` / `DynamicApiConvention` / `ApiGroupConvention` | 移除（编译期完成） |
| `AssemblyDynamicApiOptions` / `DefaultHttpVerb` / `FormBodyBindingIgnoredTypes` | 移除 |
| `FormatResultAttribute` / `FormatResultType`（自定义包装类型） | 移除，统一包装为 `ResponseResul<T>`（可用 `[NonFormatResult]` 关闭） |
| `ApiGroupAttribute` / `OrderAttribute` | 移除，分组使用 `[DynamicApi(GroupNames = ...)]` |
| 运行时反射扫描服务类 | 编译期符号扫描 |
| 复杂参数 GET 静默绑定失败 | 编译期 `YLDAPI003` |
| 方法返回值仅写 Swagger 元数据 | 真实写入包装后的响应 |
| 服务未注册时由 `ActivatorUtilities` 创建 | 必须注册到 DI |

## 版本

`3.0.0-alpha.1`，版本由本项目 csproj 的 `<Version>` 独立声明。
