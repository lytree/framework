# YLFramework — .NET 工具类与基础设施套件

`YLFramework` 是一个使用 Cake Frosting 编排的 .NET 10 monorepo，提供 10 个互相独立、可单独引用的 NuGet 包，覆盖从 BCL 扩展、加密、压缩、JSON、HTTP/SOCKS 代理、ASP.NET Core 中间件、动态 API、MVVM、仓库层到滑块验证码、图表与日志的常见基础设施能力。

## 包清单

| 包 | 用途 | 主要外部依赖 |
|----|------|-------------|
| [`YLFramework`](src/Framework/README.md) | BCL 扩展、集合、缓冲、IO、加密、压缩、JSON、HTTP/SOCKS 代理、MIME 等核心工具 | 可选 `BouncyCastle.Cryptography` / `SharpCompress` / `SkiaSharp` |
| [`YLFramework.AspNetCore`](src/Framework.AspNetCore/README.md) | ASP.NET Core 基础集成：连接管道扩展、统一 API 响应、异常中间件 | ASP.NET Core 10 |
| [`YLFramework.AspNetCore.Middleware`](src/Framework.AspNetCore.Middleware/README.md) | 一组可直接 `Use` 的 Kestrel 连接中间件：流量统计、XOR 混淆、HTTP 代理、TLS 检测、Telnet、回显 | ASP.NET Core 10 |
| [`YLFramework.Repository`](src/Framework.Repository/README.md) | 基于 linq2db 的仓库模式 + 实体基类、雪花 ID、事务传播、属性注入 | linq2db |
| [`YLFramework.DynamicApi`](src/Framework.DynamicApi/README.md) | 把任意服务类自动暴露为 Web API（约定路由 + 结果包装） | ASP.NET Core MVC |
| [`YLFramework.Mvvm`](src/Framework.Mvvm/README.md) | 基于 `CommunityToolkit.Mvvm` 的窗口状态消息 | CommunityToolkit.Mvvm |
| [`YLFramework.SlideCaptcha`](src/Framework.SlideCaptcha/README.md) | 滑块验证码：图片生成、轨迹校验、存储、资源管理 | `SkiaSharp`（可选） |
| [`YLFramework.Charts`](src/Framework.Charts/README.md) | ScottPlot 风格的图表生成（折线 / 柱状 / 饼图等） | — |
| [`YLFramework.ZLogging`](src/Framework.ZLogging/README.md) | 基于 ZLogger + Spectre.Console 的彩色控制台日志与文件输出扩展 | ZLogger / Spectre.Console |
| [`YLFramework.Logging`](src/Framework.Logging/README.md) | 基于 NLog 的 ASP.NET Core 通用日志接入 | NLog / `NLog.Web.AspNetCore` |

## 仓库结构

```
.
├── src/                 # 10 个生产包
├── test/                # 2 个测试工程（TUnit + MTP）
├── build/Build.cs       # Cake Frosting 编排
├── Directory.Packages.props  # 集中式 NuGet 版本（CPM）
├── Directory.Build.props     # 全仓库默认开关
├── Framework.slnx       # 解决方案
├── GitVersion.yml       # GitVersion 语义版本
└── global.json          # .NET SDK 锁定
```

## 构建与运行

```bash
# 编译全部
dotnet build Framework.slnx

# 打包（输出到 ./nugets）
dotnet run build/Build.cs

# 运行所有测试（TUnit / MTP）
dotnet test

# 仅构建单个项目
dotnet pack src/Framework/Framework.csproj
```

默认任务链：`Clean → Restore → Compile → Pack`。命中 `V*.*.*` 标签推送时追加 `Push` 任务并发布到 NuGet。

## 版本控制

- 使用 [GitVersion](https://gitversion.net/) 按 `git tag`（前缀 `V`）自动生成 SemVer
- `Directory.Packages.props` 启用 **Central Package Management**：所有 NuGet 依赖版本集中在该文件，`.csproj` 中 `<PackageReference>` 不写版本
- 升级依赖只需修改 `Directory.Packages.props` 一处即可全仓库生效

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
- 各包发布包名前缀统一为 `YLFramework.`，包版本由 GitVersion 决定
- 新建模块优先以 partial class / 单文件分部方式聚合，避免新增顶层命名空间碎片
