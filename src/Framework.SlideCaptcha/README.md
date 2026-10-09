# YLFramework.SlideCaptcha

滑块验证码后端库：图片生成、轨迹校验、存储、资源管理全部可插拔。前端可与任意滑块组件配合（PC、移动端 H5 通用）。

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
| `IStorage<T>` / `DefaultStorage<T>` | 验证码状态存储。`DefaultStorage` 默认使用 `IDistributedCache`（Redis / Memory） |
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

```csharp
// 1) 注册
builder.Services.AddSlideCaptcha(options =>
{
    options.ExpirySeconds = 60;
    options.Tolerant = 5;
});

// 2) 生成
public async Task<CaptchaData> GetCaptchaAsync([FromServices] ICaptcha captcha)
    => await captcha.GenerateAsync();

// 3) 校验
public async Task<ValidateResult> CheckAsync([FromServices] ICaptcha captcha,
                                             [FromBody] CaptchaValidateData data)
    => await captcha.ValidateAsync(data);
```

## 模板

仓库内置 5 套背景图 + 滑块模板（`templates/1..5/`），通过 `EmbeddedResourceProvider` 直接读取。也可以通过 `OptionsResourceProvider` 从 `appsettings.json` / 远程 URL 加载自定义素材。

## 行为兼容性说明

- 滑块轨迹（`SlideTrack`）默认不做"机器学习"判定，仅做基础时间/位移阈值校验
- 高安全场景可在 `BasicValidator` 之上扩展：检测加速度突变、Y 轴抖动、轨迹曲率等
- 校验通过后默认在 `finally` 块中主动 `Remove` 存储条目，避免重复使用

## 版本

`1.0.x`，遵循 GitVersion。
