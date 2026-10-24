# YLFramework.SlideCaptcha

滑块验证码后端库：图片生成、轨迹校验、存储、资源管理全部可插拔。前端可与任意滑块组件配合（PC、移动端 H5 通用）。

- 包名：`YLFramework.SlideCaptcha`
- 命名空间：`Framework.SlideCaptcha`
- 目标框架：`net10.0`
- 主要依赖：`SkiaSharp` 4.x（自带 Windows/macOS 原生库；Linux 服务端由包内 `SkiaSharp.NativeAssets.Linux.NoDependencies` 提供，无需 fontconfig）、`Microsoft.Extensions.*` 10.x

## 安装

```bash
dotnet add package YLFramework.SlideCaptcha
```

> 需要自行提供 `IDistributedCache` 实现。示例使用内存缓存；生产环境建议换成 Redis。

## 功能概览

### 验证码主体

| 类型 | 作用 |
|------|------|
| `ICaptcha` / `DefaultCaptcha` | 主入口：生成（`Generate`）和校验（`Validate`）验证码 |
| `CaptchaData` | 生成结果（背景图 Base64、缺口图 Base64、token、有效期） |
| `CaptchaValidateData` | 校验入参（轨迹 `SlideTrack` + token） |
| `CaptchaBuilder` | 链式构造验证码数据 |
| `ValidateResult` | 校验结果封装（成功 / 失败 / 过期 + 原因） |
| `CaptchaOptions` | 选项：模板数、有效期、滑动容差、存储键前缀等 |

### 图片生成

| 类型 | 作用 |
|------|------|
| `ICaptchaImageGenerator` / `DefaultCaptchaImageGenerator` | 把背景图 + 模板图合成出"带缺口的背景图"和"滑块图" |
| `CaptchaImageData` | 输出图像数据 |

### 资源管理

| 类型 | 作用 |
|------|------|
| `IResourceManager` / `DefaultResourceManager` | 提供背景图列表与模板图列表 |
| `IResourceProvider` + `EmbeddedResourceProvider` / `OptionsResourceProvider` | 资源来源：嵌入资源 或 `IOptions<>` 配置 |
| `IResourceHandler` + `FileResourceHandler` / `EmbeddedResourceHandler` | 资源加载：文件 或 嵌入资源 |
| `IResourceHandlerManager` + `CachedResourceHandlerManager` | 资源缓存：减少重复 IO |
| `Resource` / `TemplatePair` | 资源数据模型（背景图 / 模板对） |

### 存储

| 类型 | 作用 |
|------|------|
| `IStorage` / `DefaultStorage` | 验证码状态存储。`DefaultStorage` 默认使用 `IDistributedCache`（Redis / Memory） |
| `StorageKeyPrefix` | 键前缀（注意：历史拼写为 `StoreageKeyPrefix`，向后兼容） |

### 校验

| 类型 | 作用 |
|------|------|
| `IValidator` | 校验接口 |
| `BaseValidator` | 基类：通用轨迹与时间戳校验 |
| `BasicValidator` | 基础校验（轨迹长度、时间跨度） |
| `SimpleValidator` | 简化校验（按 X 轴位移 + 时间阈值） |
| `SlideTrack` | 轨迹数据模型（点序列、时间戳） |

### 异常

| 类型 | 触发场景 |
|------|---------|
| `SlideCaptchaException` | 验证码相关基础异常 |
| `SlideCaptchaTimeoutException` | 验证码已过期或被消费 |

## 快速上手

### 1. 注册服务

```csharp
using Framework.SlideCaptcha;

builder.Services.AddDistributedMemoryCache(); // 生产环境替换为 Redis 等

builder.Services.AddSlideCaptcha(builder.Configuration, options =>
{
    options.ExpirySeconds = 60;
    options.Tolerant = 0.02f;
    options.StorageKeyPrefix = "slide-captcha:";
});
```

`AddSlideCaptcha` 会注册以下服务（`ICaptcha`、`IStorage`、`IValidator`、`ICaptchaImageGenerator` 为 `Scoped`，其余为 `Singleton`）：

| 服务 | 默认实现 |
|------|----------|
| `IResourceProvider` | `OptionsResourceProvider` + `EmbeddedResourceProvider` |
| `IResourceHandler` | `FileResourceHandler` + `EmbeddedResourceHandler` |
| `IResourceHandlerManager` | `CachedResourceHandlerManager` |
| `IResourceManager` | `DefaultResourceManager` |
| `ICaptchaImageGenerator` | `DefaultCaptchaImageGenerator` |
| `IStorage` | `DefaultStorage` |
| `IValidator` | `SimpleValidator` |
| `ICaptcha` | `DefaultCaptcha` |

### 2. appsettings.json（可选）

```json
{
  "SlideCaptcha": {
    "ExpirySeconds": 60,
    "StorageKeyPrefix": "slide-captcha:",
    "Tolerant": 0.02,
    "Track": {
      "MinSlidingMilliseconds": 300,
      "MinTrackCount": 10,
      "MaxTrackCountPerWidth": 5,
      "StartPointTolerance": 10,
      "MaxJumpX": 50,
      "MaxJumpY": 50,
      "MaxOutOfRangeTrackCount": 200
    },
    "Backgrounds": [
      { "Type": "file", "Data": "/data/captcha/bg-1.jpg" },
      { "Type": "file", "Data": "/data/captcha/bg-2.jpg" }
    ],
    "Templates": [
      {
        "Slider": { "Type": "file", "Data": "/data/captcha/1.slider.png" },
        "Hole":   { "Type": "file", "Data": "/data/captcha/1.hole.png" }
      }
    ]
  }
}
```

配置节固定为 `SlideCaptcha`；`AddSlideCaptcha` 中的委托会在配置绑定之后执行，可用于覆盖配置值。

### 3. 生成验证码

```csharp
using Framework.SlideCaptcha;

app.MapPost("/api/captcha", (ICaptcha captcha) => Results.Ok(captcha.Generate()));
```

响应结构：

```json
{
  "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
  "backgroundImage": "iVBORw0KGgo...",
  "sliderImage": "iVBORw0KGgo..."
}
```

两张图均为 PNG 的 Base64 字符串（不带 `data:image/png;base64,` 前缀），前端拼接前缀后直接作为 `<img src>` 使用。

### 4. 校验

```csharp
using Framework.SlideCaptcha;
using Framework.SlideCaptcha.Validator;

public sealed record CaptchaValidateRequest(string Id, SlideTrack SlideTrack);

app.MapPost("/api/captcha/validate", (ICaptcha captcha, CaptchaValidateRequest request) =>
{
    var result = captcha.Validate(request.Id, request.SlideTrack);

    return result.Result == ValidateResult.ValidateResultType.Success
        ? Results.Ok(result)
        : Results.BadRequest(result);
});
```

`ValidateResult` 的三种状态：

| 枚举值 | 数值 | Message | 含义 |
|--------|------|---------|------|
| `Success` | 0 | 成功 | 位置与轨迹校验通过 |
| `ValidateFail` | 1 | 验证失败 | 轨迹非法 / 位置超出容差 |
| `Timeout` | 2 | 验证超时 | captchaId 不存在、已过期或已被消费 |

> 验证码是一次性的：无论成功、失败还是抛异常，`Validate` 都会在 `finally` 中删除存储条目，重放同一 `id` 只会得到 `Timeout`。

## 前端接入协议

前端需要按以下顺序采集并回传轨迹：

1. 用 `backgroundImage` 渲染背景，用 `sliderImage` 渲染滑块；
2. 用户拖动时按时间采样点位，记录相对起点（滑块初始位置）的偏移量；
3. 提交校验时回传渲染尺寸、起止时间与轨迹点。

```json
{
  "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
  "slideTrack": {
    "backgroundImageWidth": 320,
    "backgroundImageHeight": 160,
    "sliderImageWidth": 60,
    "sliderImageHeight": 160,
    "startTime": "2026-10-24T08:00:00Z",
    "endTime": "2026-10-24T08:00:01.200Z",
    "tracks": [
      { "x": 0, "y": 0, "t": 0 },
      { "x": 3, "y": 1, "t": 16 },
      { "x": 12, "y": 2, "t": 48 },
      { "x": 86, "y": 0, "t": 420 }
    ]
  }
}
```

字段说明：

| 字段 | 说明 |
|------|------|
| `backgroundImageWidth/Height` | **渲染后**的背景图尺寸（可能经过 CSS 缩放），用于计算滑动百分比 |
| `sliderImageWidth/Height` | **渲染后**的滑块图尺寸 |
| `startTime/endTime` | 本次滑动的起止时间（`DateTime`，建议统一用 UTC） |
| `tracks[].x` | 相对拖动起点的横向位移（像素），不是页面绝对坐标 |
| `tracks[].y` | 相对拖动起点的纵向位移（像素） |
| `tracks[].t` | 相对开始时间的毫秒数 |

滑动百分比按 `最后一个轨迹点.x / backgroundImageWidth` 计算，因此务必回传渲染宽度而不是原图宽度。

最小前端采集示例：

```js
let startTime, tracks = [];

function onPointerDown(e) {
  startTime = Date.now();
  tracks = [{ x: 0, y: 0, t: 0 }];
}

function onPointerMove(e, startX, startY) {
  tracks.push({
    x: Math.round(e.clientX - startX),
    y: Math.round(e.clientY - startY),
    t: Date.now() - startTime
  });
}

function buildPayload(id, bg, slider) {
  return {
    id,
    slideTrack: {
      backgroundImageWidth: bg.clientWidth,
      backgroundImageHeight: bg.clientHeight,
      sliderImageWidth: slider.clientWidth,
      sliderImageHeight: slider.clientHeight,
      startTime: new Date(startTime).toISOString(),
      endTime: new Date().toISOString(),
      tracks
    }
  };
}
```

## 配置项

`CaptchaOptions`：

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `ExpirySeconds` | `60` | 验证码有效期（秒），写入存储的绝对过期时间 |
| `StorageKeyPrefix` | `"slide-captcha"` | 存储键前缀，最终键为 `前缀 + captchaId` |
| `StoreageKeyPrefix` | — | 历史拼写，`[Obsolete]`，内部转发到 `StorageKeyPrefix` |
| `Tolerant` | `0.02` | 位置容差（按背景图宽度比例），`0.02` 表示 2% |
| `Track` | 见下表 | 行为轨迹校验阈值，仅 `BasicValidator` 生效 |
| `InterferenceCount` | `0` | 预留项，`[Obsolete]`，当前未实现，设置无效 |
| `Backgrounds` | 空 | 背景图资源列表，支持 `file` / `embedded` 类型 |
| `Templates` | 空 | 模板对列表（`Slider` + `Hole`） |

`CaptchaTrackOptions`（`options.Track`）：

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `MinSlidingMilliseconds` | `300` | 滑动最短时长，低于该值判定为机器 |
| `MinTrackCount` | `10` | 轨迹点最少数量 |
| `MaxTrackCountPerWidth` | `5` | 每像素背景宽度允许的最大轨迹点数 |
| `StartPointTolerance` | `10` | 起点允许偏移（像素） |
| `MaxJumpX` | `50` | 相邻点横向最大跳变（取绝对值） |
| `MaxJumpY` | `50` | 相邻点纵向正向最大跳变 |
| `MaxOutOfRangeTrackCount` | `200` | 允许 `x >= 背景宽度` 的轨迹点数量上限 |

## 校验策略

默认注册 `SimpleValidator`，只校验缺口位置是否落在 `Tolerant` 范围内。

需要更严格的行为检测时，替换为 `BasicValidator`：

```csharp
builder.Services
    .AddSlideCaptcha(builder.Configuration)
    .ReplaceValidator<BasicValidator>();
```

`BasicValidator` 在位置校验之外还会检查：

1. 滑动总时长 ≥ `MinSlidingMilliseconds`；
2. 轨迹点数量在 `[MinTrackCount, 背景宽度 × MaxTrackCountPerWidth]` 区间内；
3. 起点 `x/y` 绝对值 ≤ `StartPointTolerance`；
4. 所有点的 `y` 完全相同 → 机器行为，拒绝；
5. `x >= 背景宽度` 的点过多（> `MaxOutOfRangeTrackCount`）→ 拒绝；
6. 相邻点横向跳变绝对值 > `MaxJumpX` 或纵向正向跳变 > `MaxJumpY` → 拒绝。

阈值可通过 `appsettings.json` 的 `SlideCaptcha:Track` 节实时调整（`BasicValidator` 使用 `IOptionsMonitor`，配置热更新生效）。

自定义策略只需继承 `BaseValidator` 并重写 `ValidateCore`：

```csharp
public sealed class MyValidator : BaseValidator
{
    public override bool ValidateCore(SlideTrack slideTrack, CaptchaValidateData data)
    {
        // 自定义加速度、曲率、Y 轴抖动等判定
        return true;
    }
}

builder.Services.AddSlideCaptcha(builder.Configuration).ReplaceValidator<MyValidator>();
```

## 资源管理

### 内置模板

包内嵌 5 套滑块 + 缺口模板（`templates/1..5`），由 `EmbeddedResourceProvider` 自动扫描加载。默认即可使用，无需配置。

内置 `EmbeddedResourceHandler` 只读取 **`YLFramework.SlideCaptcha` 自身程序集** 的内嵌资源；若要把资源放进业务程序集，请使用 `file` 类型或自定义 `IResourceHandler`。

### 使用本地文件作为背景

```csharp
builder.Services.AddSlideCaptcha(builder.Configuration, options =>
{
    options.Backgrounds =
    [
        new Resource(FileResourceHandler.TYPE, "D:/captcha/bg-1.jpg"),
        new Resource(FileResourceHandler.TYPE, "D:/captcha/bg-2.jpg")
    ];
});
```

### 使用自定义模板

```csharp
options.Templates =
[
    TemplatePair.Create(
        new Resource(FileResourceHandler.TYPE, "D:/captcha/1.slider.png"),
        new Resource(FileResourceHandler.TYPE, "D:/captcha/1.hole.png"))
];
```

自定义模板会与内置模板合并，共同参与随机选择；调用 `DisableDefaultTemplates()` 可移除内置模板：

```csharp
builder.Services
    .AddSlideCaptcha(builder.Configuration, options => options.Templates = [/* ... */])
    .DisableDefaultTemplates();
```

模板约定：`slider.png` 为滑块（不透明区域即滑块形状），`hole.png` 为缺口遮罩。模板尺寸必须小于背景图，否则生成时抛出 `SlideCaptchaException`。

### 扩展资源来源

```csharp
// 追加资源提供者（例如从数据库/对象存储读取）
builder.Services
    .AddSlideCaptcha(builder.Configuration)
    .AddResourceProvider<DatabaseResourceProvider>()
    .AddResourceHandler<HttpResourceHandler>();
```

`Resource.Type` 用于选择处理器，处理器按注册顺序匹配第一个 `CanHandle` 返回 `true` 的实现。`CachedResourceHandlerManager` 会按 `Resource` 实例缓存字节，因此自定义 Provider 若希望命中缓存，应复用同一实例返回资源。

## 图片生成（SkiaSharp）

默认的 `DefaultCaptchaImageGenerator` 基于 **SkiaSharp** 完成位图合成，不依赖任何字体或文字渲染能力。

### 合成流程

1. 从 `IResourceManager` 随机取一张背景图和一组 `(slider, hole)` 模板；
2. 在背景图内随机选定缺口位置 `(X, Y)`，四周各留 5px 边距（X 方向起点还需再加一个模板宽度，见下表区间）；
3. 按 `hole` 模板的 **alpha 通道**逐行扫描轮廓（不透明像素即形状），生成 `SKPath`；
4. 用该形状裁剪背景图，得到缺口位置的画面，再叠加 `slider` 模板，形成滑块内容；
5. 把滑块内容绘制到「背景图同高」的透明条上，得到返回给前端的 `SliderImage`；
6. 把 `hole` 模板绘制回背景原位，得到带缺口的 `BackgroundImage`；
7. 两张图统一编码为 PNG 并转 Base64。

### 输入输出约定

| 项目 | 约定 |
|------|------|
| 背景图格式 | 任意 SkiaSharp 可解码的位图（PNG / JPEG / WebP 等），建议与模板尺寸比例接近 |
| 模板格式 | PNG，必须保留透明通道；`hole` 的 alpha 决定缺口形状 |
| 输出格式 | 固定 PNG 的 Base64 字符串（不含 `data:` 前缀） |
| `BackgroundImageWidth/Height` | 等于背景图原始像素尺寸 |
| `SliderImageWidth/Height` | 宽度等于 `slider` 模板宽度，高度等于背景图高度 |
| 随机位置范围 | `X ∈ [holeWidth + 5, bgWidth - holeWidth - 5)`、`Y ∈ [5, bgHeight - holeHeight - 5)` |
| 尺寸校验 | 上述两个区间为空（`min > max`）时抛 `SlideCaptchaException`，等价于 `2 × holeWidth + 10 > bgWidth` 或 `holeHeight + 10 > bgHeight` |

> 尺寸校验必须比较区间端点本身，而不能只判断「模板 + 5px 边距是否小于背景」：例如背景宽 100、模板宽 60 时 `min = 65`、`max = 35`，仅凭后者无法发现区间为空，`Random.Next` 会抛 `ArgumentOutOfRangeException`。模板解码失败同样抛 `SlideCaptchaException`。

`CaptchaImageData.Check()` 还会校验 `X`、`Y`、各尺寸均大于 0 且 Base64 非空，因此自定义生成器返回的坐标不能为 0。

### 原生库与部署

| 平台 | 原生库来源 |
|------|-----------|
| Windows / macOS | `SkiaSharp` 包自带（`NativeAssets.Win32` / `macOS`） |
| Linux (glibc / musl) | 本包已引入 `SkiaSharp.NativeAssets.Linux.NoDependencies`，无需安装 fontconfig |

注意事项：

- 本包只做位图合成，不需要字体渲染，因此选择 `NoDependencies` 变体即可；
- 自包含发布 / 单文件发布 / 自定义容器镜像时，确认 `libSkiaSharp` 已被复制到输出目录（`NativeAssets` 包会按 RID 自动处理）；
- 若运行时报 `DllNotFoundException: libSkiaSharp`，说明目标 RID 缺少原生资产，需显式添加对应平台的 `SkiaSharp.NativeAssets.*` 包。

### 性能与并发

- `ICaptchaImageGenerator` 为 `Scoped`，每次生成会解码三张位图（背景 + 滑块 + 缺口）并编码两张 PNG，属于 CPU 密集操作；
- `IResourceManager` 与 `IResourceHandlerManager` 为 `Singleton`，资源字节会被缓存，重复生成不会重复读文件；
- 高并发场景建议缩小背景/模板尺寸（生成耗时与像素量正相关），并横向扩容实例。

### 自定义图片生成器

需要更换合成算法（例如增加干扰块、旋转缺口、叠加噪点）时，实现 `ICaptchaImageGenerator` 并替换默认注册：

```csharp
using Framework.SlideCaptcha.Exceptions;
using Framework.SlideCaptcha.Generator;
using Framework.SlideCaptcha.Resources;
using SkiaSharp;

public sealed class MyCaptchaImageGenerator(IResourceManager resourceManager) : ICaptchaImageGenerator
{
    public CaptchaImageData Generate()
    {
        var backgroundBytes = resourceManager.RandomBackground();
        var (sliderBytes, holeBytes) = resourceManager.RandomTemplate();

        using var background = SKBitmap.Decode(backgroundBytes);
        using var sliderTemplate = SKBitmap.Decode(sliderBytes);
        using var holeTemplate = SKBitmap.Decode(holeBytes);

        // 缺口位置留出 5px 边距，坐标必须大于 0
        const int margin = 5;
        var maxX = background.Width - holeTemplate.Width - margin;
        if (maxX <= holeTemplate.Width + margin)
        {
            throw new SlideCaptchaException("模板尺寸过大");
        }

        var x = Random.Shared.Next(holeTemplate.Width + margin, maxX);
        var y = 60;

        // 滑块条与背景图同高，滑块模板绘制在缺口对应的纵坐标上
        using var sliderBar = new SKBitmap(
            new SKImageInfo(sliderTemplate.Width, background.Height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(sliderBar))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(sliderTemplate, 0, y);
        }

        // ... 自定义合成：按 holeTemplate 的 alpha 轮廓裁剪背景、叠加干扰块/噪点等 ...

        return new CaptchaImageData
        {
            X = x,
            Y = y,
            BackgroundImageWidth = background.Width,
            BackgroundImageHeight = background.Height,
            BackgroundImageBase64 = ToPngBase64(background),
            SliderImageWidth = sliderBar.Width,
            SliderImageHeight = sliderBar.Height,
            SliderImageBase64 = ToPngBase64(sliderBar)
        };
    }

    private static string ToPngBase64(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(data.ToArray());
    }
}
```

注册方式（在 `AddSlideCaptcha` 之后追加，后注册的实现会覆盖默认实现）：

```csharp
builder.Services.AddSlideCaptcha(builder.Configuration);
builder.Services.AddScoped<ICaptchaImageGenerator, MyCaptchaImageGenerator>();
```

## 自定义存储

```csharp
// 自定义存储（内存 / 数据库 / 其它缓存）
builder.Services.AddSlideCaptcha(builder.Configuration);
builder.Services.AddScoped<IStorage, MyStorage>();
```

`IStorage` 只需实现 `Set<T>` / `Get<T>` / `Remove`，过期淘汰由实现负责。默认实现 `DefaultStorage` 使用 `IDistributedCache` 并 JSON 序列化。

自定义图片生成见上一节「[自定义图片生成器](#自定义图片生成器)」。

## 错误与常见问题

| 现象 / 异常 | 原因与处理 |
|-------------|-----------|
| `SlideCaptchaException: 背景图不能为空` | 未配置 `Backgrounds`，且内置 Provider 不提供背景图 |
| `SlideCaptchaException: 模板不能为空` | 自定义模板为空且调用了 `DisableDefaultTemplates()` |
| `SlideCaptchaException: 没有可用的资源提供者!` | `Resource.Type` 不是已注册处理器支持的值（`file` / `embedded`） |
| `SlideCaptchaException: 背景图解码失败` | 资源字节不是 SkiaSharp 可解码的图片（损坏、非图片，或扩展名与实际内容不符） |
| `SlideCaptchaException: 模板尺寸(...)过大` | 缺口模板连同 5px 边距放不进背景图（`2 × 模板宽 + 10 > 背景宽` 或 `模板高 + 10 > 背景高`），换更大的背景图或更小的模板 |
| `DllNotFoundException: libSkiaSharp` | 目标平台缺少 SkiaSharp 原生库，按「[原生库与部署](#原生库与部署)」补充对应 `SkiaSharp.NativeAssets.*` |
| 校验总是返回 `Timeout` | `captchaId` 错误、已过期、已被消费，或生成的实例与校验实例使用不同存储 |
| 校验总是返回 `ValidateFail` | 前端回传的 `backgroundImageWidth` 与渲染尺寸不一致，或轨迹 `x` 使用了页面绝对坐标 |
| 本地/多实例环境校验失败 | 使用内存缓存时多实例不共享，需换成 Redis 等分布式缓存 |
| 非法轨迹导致 500 | `DefaultCaptcha.Validate` 已捕获 `SlideCaptchaException` 并返回 `ValidateFail` |

## 行为兼容性说明

- 滑块轨迹（`SlideTrack`）默认不做"机器学习"判定，仅做位置 + 基础时间/位移阈值校验
- 高安全场景可在 `BasicValidator` 之上扩展：检测加速度突变、Y 轴抖动、轨迹曲率等
- 校验通过后默认在 `finally` 块中主动 `Remove` 存储条目，避免重复使用
- 缺口模板无法放入背景图时统一抛 `SlideCaptchaException`（2.0.0-alpha.1 之前会从 `Random.Next` 冒泡 `ArgumentOutOfRangeException`）

## 版本

`2.0.0-alpha.1`，版本由本项目 csproj 的 `<Version>` 独立声明。
