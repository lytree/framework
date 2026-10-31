# YLFramework — .NET 工具类与基础设施套件

`YLFramework` 是一个使用 Cake Frosting 编排的 .NET 10 monorepo，提供 9 个互相独立、可单独引用的 NuGet 包，覆盖从 BCL 扩展、加密、压缩、JSON、HTTP/SOCKS 代理、ASP.NET Core 中间件、动态 API、MVVM、仓库层到滑块验证码、图表与日志的常见基础设施能力。

通用工具代码不再以独立 `Framework` 程序集的形式发布，而是放在 [`src/Shared`](src/Shared/README.md)，由需要的包以 `<Compile Include>` 方式**直接引入源码编译**——每个包自包含，互不依赖程序集。

## 包清单

| 包 | 用途 | 主要外部依赖 |
|----|------|-------------|
| [`YLFramework.AspNetCore`](src/Framework.AspNetCore/README.md) | ASP.NET Core 基础集成：连接管道扩展、统一 API 响应、异常中间件 | ASP.NET Core 10 |
| [`YLFramework.AspNetCore.Middleware`](src/Framework.AspNetCore.Middleware/README.md) | 一组可直接 `Use` 的 Kestrel 连接中间件：流量统计、XOR 混淆、HTTP 代理、TLS 检测、Telnet、回显 | ASP.NET Core 10 |
| [`YLFramework.Repository`](src/Framework.Repository/README.md) | 基于 linq2db 的仓库模式 + 实体基类、雪花 ID、事务传播、属性注入 | linq2db |
| [`YLFramework.DynamicApi`](src/Framework.DynamicApi/README.md) | 编译期生成 Minimal API 端点（约定路由 + 结果包装，零反射 / Native AOT） | ASP.NET Core Minimal API + Roslyn 源生成器 |
| [`YLFramework.Mvvm`](src/Framework.Mvvm/README.md) | 基于 `CommunityToolkit.Mvvm` 的窗口状态消息 | CommunityToolkit.Mvvm |
| [`YLFramework.SlideCaptcha`](src/Framework.SlideCaptcha/README.md) | 滑块验证码：图片生成、轨迹校验、存储、资源管理 | `SkiaSharp` |
| [`YLFramework.Charts`](src/Framework.Charts/README.md) | ScottPlot 风格的图表生成（折线 / 柱状 / 饼图等） | — |
| [`YLFramework.ZLogging`](src/Framework.ZLogging/README.md) | 自研 ILoggerProvider 的彩色控制台日志 + 异步文件写入（滚动 / 重连 / 自定义指令） | Spectre.Console |
| [`YLFramework.Logging`](src/Framework.Logging/README.md) | 基于 Serilog 的日志接入：控制台 / 滚动文件 / CLEF JSON + 结构化日志模板与字段约定 | Serilog |

## 仓库结构

```
.
├── src/                 # 10 个生产包 + Shared 共享源码
│   └── Shared/          # 共享源码，被各包 <Compile Include> 引入编译
├── test/                # 3 个测试工程（TUnit + MTP）
├── build/Build.cs       # Cake Frosting 编排（+ build/Directory.Packages.props 关闭该目录 CPM）
├── Directory.Packages.props  # 集中式 NuGet 依赖版本（CPM）
├── Directory.Build.props     # 全仓库默认开关
├── Framework.slnx       # 解决方案
└── global.json          # .NET SDK 锁定
```

## 构建与运行

```bash
# 编译全部
dotnet build Framework.slnx

# 打包全部包（输出到 ./nugets）
dotnet run build/Build.cs

# 只打包某一个包（按 PackageId 或项目名精确匹配）
dotnet run build/Build.cs -t Pack --Package=YLFramework.Repository
dotnet run build/Build.cs -t Pack --Package=Framework.Repository

# 运行所有测试（TUnit / MTP）
dotnet test

# 仅构建单个项目
dotnet pack src/Framework.Repository/Framework.Repository.csproj
```

默认任务链：`Clean → Restore → Compile → Pack`。指定 `-Package` 时只编译/打包该包，不再全量构建。

## 发版

支持两种标签，推送后由 CI 自动打包并推送 NuGet：

| 标签形式 | 含义 |
|---|---|
| `YLFramework.Repository/v2.0.0-alpha.1` | **单包发布**：只发布该包（推荐，配合各包独立版本） |
| `V2.0.0-alpha.1` | **全量发布**：`src` 下所有包一起发 |

单包发布时，标签里的版本必须与该 csproj 的 `<Version>` 完全一致，否则构建阶段直接报错，防止标签与源码版本漂移。

## 版本控制

- **依赖版本集中**：`Directory.Packages.props` 启用 **Central Package Management**，所有 NuGet 依赖版本集中在该文件，`.csproj` 中 `<PackageReference>` 不写版本；升级依赖只改这一处
- **包版本各自独立**：每个包的 `<Version>` 在自己的 `.csproj` 中声明，不再由 `src/Directory.Build.props` 统一管理；`Pack` 默认按各自版本打包
- 需要临时统一覆盖时：`dotnet run build/Build.cs -Version 1.2.3`
- **不使用 GitVersion**：版本号完全由各 csproj 的 `<Version>` 决定，构建与发布不依赖任何 Git 版本推算工具

## 命名空间约定

- 各包命名空间统一保留 `Framework.*` 前缀（`Framework.Repository.*`、`Framework.AspNetCore.*`、`Framework.DynamicApi.*`、`Framework.SlideCaptcha.*`、`Framework.ZLogging.*`、`Framework.Charts.*`、`Framework.Mvvm.*`）
- **唯一例外是 `src/Shared`**：其中的文件不声明 `namespace`，类型落在全局命名空间，被引入后无需 `using` 直接调用
- 全局命名空间下的扩展类按文件区分名字（`Extensions` / `ListExtensions` / `CollectionExtensions` / `StreamExtensions` / `ReflectionExtensions` 等），避免合并冲突
- `src/Shared` 中的所有类型一律 `internal`：同一份源码会被多个包各自内联编译，保持 `public` 会让下游同时引用多包时触发 `CS0433`

## 目标框架与语言

- 全部包仅针对 `net10.0`
- 启用 `Nullable` / `ImplicitUsings` / `GenerateDocumentationFile`
- 通过 `<NoWarn>` 在 `src/Directory.Build.props` 集中抑制常见可空引用警告

## 测试

- 测试项目使用 [TUnit](https://github.com/thomhurst/TUnit) + [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform)
- `global.json` 中 `test.runner=Microsoft.Testing.Platform` 强制使用 MTP
- 在 .NET 10 SDK 及更高版本上，VSTest 模式的 `dotnet test` 不再支持 MTP 项目，必须保留该 opt-in

## 贡献提示

- 修改公共 API 时同时更新对应模块的 README
- 各包发布包名前缀统一为 `YLFramework.`，包版本由各包 csproj 的 `<Version>` 决定
- 新建模块优先以 partial class / 单文件分部方式聚合，避免新增顶层命名空间碎片
- 需要复用 `src/Shared` 里的工具代码时，在目标 csproj 用 `<Compile Include>` 引入文件，不要新建中心化程序集
