# YLFramework.Plot

> 发布包名为 `YLFramework.Plot`，工程目录与项目文件名仍是 `Framework.Charts`。

基于 [ScottPlot](https://scottplot.net/) 风格的图表生成工具：把数据点序列一键渲染为 PNG 字节数组。提供"默认风格"与"业务风格"两套预设。

## 功能概览

| 文件 | 类型 | 作用 |
|------|------|------|
| `Plots.cs` | `Plots` 静态类 | 业务风格图表工厂方法（折线、柱状、饼图、面积图等） |
| `Plots.Default.cs` | `Plots.Default` 静态类 | 默认风格图表工厂方法 |
| `TickGenerators/FixedTickGenerator.cs` | 自定义刻度生成器 | 把 X 轴按固定步长打点，避免大数据量下标签重叠 |
| `simhei.ttf` / `simkai.ttf` / `simsun.ttc` | 字体文件 | 仓库内保留的中文字体文件；**不随 NuGet 包分发** |

## 快速上手

```csharp
// 折线图
var bytes = Plots.Line(
    title: "近 30 天访问量",
    xs: Enumerable.Range(0, 30).Select(i => (double)i).ToArray(),
    ys: visits,
    outputSize: new Size(1200, 600));

// 柱状图
var bytes = Plots.Bar("机型分布", labels, counts);

// 默认风格
var bytes = Plots.Default.Line(...);
```

返回值为 PNG 字节数组，可直接写入 HTTP 响应 / 文件 / OSS。

## 设计要点

- 全部图表为**预渲染**（无交互），适合"导出图片给前端展示"或"邮件报表"
- 通过 `TickGenerators.FixedTickGenerator` 在 X 轴样本很多时保持标签稀疏可读
- 字体从系统已安装字体中挑选（依次尝试 SimSun / SimKai / DejaVu Sans / Liberation Sans / Noto Sans / FreeSans，均未命中时回退到首个可用字体）；仓库内的 `simhei.ttf` / `simkai.ttf` / `simsun.ttc` 不会被自动加载，需要中文标签时请确保运行环境已安装对应字体

## 适用场景

- 服务端定时生成业务报表截图
- 与 `IHostedService` 结合做"每日业务概览邮件"
- 与 `Framework.SlideCaptcha` / `Framework.ZLogging` 配合做监控大屏

## 版本

`2.0.0-alpha.1`，版本由本项目 csproj 的 `<Version>` 独立声明；发布标签前缀使用 `PackageId`，即 `YLFramework.Plot/v*`。
