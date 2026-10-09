# YLFramework.DynamicApi

把任意服务类（无需继承 `Controller`、无需添加 `[Route]` / `[HttpGet]`）自动暴露为 ASP.NET Core MVC 控制器，并按约定生成路由与结果包装。

## 适用场景

- 一组业务服务类想以最少的样板代码暴露为 HTTP API
- 已有应用服务层，不想为每个服务额外写"薄控制器"
- 想要按命名约定（如 `GetXxx` / `CreateXxx`）自动映射 HTTP Method

## 核心组件

| 类型 | 作用 |
|------|------|
| `DynamicApiServiceExtensions.AddDynamicApi()` | 启用动态 API 注册 |
| `DynamicApiOptions` | 全局配置：路由前缀、命名约定 (`NamingConventionEnum`)、HttpMethod 推断规则等 |
| `AssemblyDynamicApiOptions` | 按程序集覆盖：白名单 / 黑名单 / 路由前缀 |
| `DynamicApiControllerFeatureProvider` | 替换默认控制器发现，让框架识别动态 API 类 |
| `DynamicApiConvention` | 按方法名 / 返回类型自动应用 `[HttpGet]` 等 |
| `ApiGroupConvention` | 按 `ApiGroup` 分组 |
| `IActionRouteFactory` / `DefaultActionRouteFactory` | 路由生成器，可自定义 |
| `IDynamicApi` | 标记接口：实现该接口的类会被动态注册 |
| `ISelectController` | 选择器：决定哪些服务/方法被暴露 |
| `IDynamicApiControllerFeatureProvider` | 扩展点 |

### 特性

| 特性 | 作用 |
|------|------|
| `DynamicApiAttribute` | 类级：显式声明为动态 API（可指定 Name / Group） |
| `NonDynamicApiAttribute` | 类级：禁止某个类被暴露 |
| `NonDynamicMethodAttribute` | 方法级：禁止某个方法被暴露 |
| `ApiGroupAttribute` | 类/方法级：归到指定分组（影响 Swagger 文档） |
| `FormatResultAttribute` | 方法级：自定义结果包装类型 |
| `NonFormatResultAttribute` | 方法级：禁用结果包装 |
| `OrderAttribute` | 方法级：调整路由展示顺序 |

### 命名约定

`AppConsts` + `NamingConventionEnum` 决定：

- `ControllerPostfixes`：哪些后缀被认为是"控制器"（如 `Service` / `Application` / `App`），命中后会被去除再生成路由
- `ActionPostfixes`：方法后缀到 HTTP Method 的映射
- 默认命名约定：方法名前缀 `Get` → `GET`，`Create` → `POST`，`Update` → `PUT`，`Delete` → `DELETE`

## 快速上手

```csharp
// 业务服务（无需继承 Controller）
[DynamicApi]
public class UserService
{
    public Task<UserDto> GetUserAsync(long id) => /* ... */;
    public Task<long> CreateUserAsync(CreateUserRequest req) => /* ... */;
}

// 注册
builder.Services.AddDynamicApi(options =>
{
    options.AddAssemblyTypes(typeof(UserService).Assembly);
});
```

框架会自动暴露：

- `GET  /api/user/{id}`
- `POST /api/user`

## 结果包装

默认开启：方法返回值会被 `ApiResponse<T>` 包装。如需直接返回原对象：

```csharp
[NonFormatResult]
public string Raw() => "hello";
```

## 自定义命名约定

实现 `IDynamicApi` + 自定义 `DynamicApiOptions.NamingConventionType` 即可覆盖默认行为。

## 版本

`1.0.x`，遵循 GitVersion。
