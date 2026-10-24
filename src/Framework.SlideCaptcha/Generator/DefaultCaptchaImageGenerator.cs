using Framework.SlideCaptcha.Exceptions;
using Framework.SlideCaptcha.Resources;
using SkiaSharp;

namespace Framework.SlideCaptcha.Generator;

public class DefaultCaptchaImageGenerator : ICaptchaImageGenerator
{
	private readonly IResourceManager _resourceManager;

	public DefaultCaptchaImageGenerator(IResourceManager resourceManager)
	{
		_resourceManager = resourceManager;
	}

	/// <summary>
	/// 计算凹槽轮廓
	/// 原理： 一行一行扫描，每行不透明像素连接形成数个小矩形（<see cref="SKRect"/>）。
	///       多个矩形合成一个 <see cref="SKPath"/>，作为裁剪凹槽的形状。
	/// </summary>
	/// <param name="holeTemplateImage">缺口模板图，形状由不透明区域决定。</param>
	/// <returns>由矩形组成的凹槽轮廓路径。</returns>
	private static SKPath CalcHoleShape(SKBitmap holeTemplateImage)
	{
		using var pathBuilder = new SKPathBuilder();

		for (int y = 0; y < holeTemplateImage.Height; y++)
		{
			// 不透明区段的起始列，-1 表示当前行尚未进入不透明区段。
			// 用 -1 而非 0 作为哨兵，避免「从第 0 列开始的不透明区段」被误判为未开始而整段丢失。
			int start = -1;

			for (int x = 0; x < holeTemplateImage.Width; x++)
			{
				if (holeTemplateImage.GetPixel(x, y).Alpha != 0)
				{
					start = start < 0 ? x : start;
				}
				else if (start >= 0)
				{
					pathBuilder.AddRect(SKRect.Create(start, y, x - start, 1), SKPathDirection.Clockwise);
					start = -1;
				}
			}

			// 行尾收尾：若不透明区段一直延伸到行末，需要补上这一段
			if (start >= 0)
			{
				pathBuilder.AddRect(SKRect.Create(start, y, holeTemplateImage.Width - start, 1), SKPathDirection.Clockwise);
			}
		}

		return pathBuilder.Snapshot();
	}

	/// <summary>
	/// 解码图片字节；解码失败时抛出 <see cref="SlideCaptchaException"/>。
	/// </summary>
	private static SKBitmap LoadBitmap(byte[] data, string name)
	{
		var bitmap = SKBitmap.Decode(data);
		if (bitmap == null)
		{
			throw new SlideCaptchaException($"{name}解码失败，请确认是有效的图片格式(PNG/JPEG 等)");
		}

		return bitmap;
	}

	/// <summary>
	/// 创建指定尺寸的全透明位图。
	/// </summary>
	private static SKBitmap CreateTransparentBitmap(int width, int height)
	{
		var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(SKColors.Transparent);
		return bitmap;
	}

	/// <summary>
	/// 把位图编码为 PNG 并返回 Base64 字符串。
	/// </summary>
	private static string ToPngBase64(SKBitmap bitmap)
	{
		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		return Convert.ToBase64String(data.ToArray());
	}

	public CaptchaImageData Generate()
	{
		var background = _resourceManager.RandomBackground();
		(var slider, var hole) = _resourceManager.RandomTemplate();

		using var backgroundImage = LoadBitmap(background, "背景图");
		using var sliderTemplateImage = LoadBitmap(slider, "滑块模板");
		using var holeTemplateImage = LoadBitmap(hole, "缺口模板");

		// 随机定位需要留出边距，先校验模板相对背景图是否放得下，
		// 否则 Random.Next(min, max) 会因 min >= max 抛 ArgumentOutOfRangeException。
		const int MarginX = 5;
		const int MarginY = 5;

		int maxX = backgroundImage.Width - holeTemplateImage.Width - MarginX;
		int maxY = backgroundImage.Height - holeTemplateImage.Height - MarginY;

		if (holeTemplateImage.Width + MarginX >= backgroundImage.Width ||
			holeTemplateImage.Height + MarginY >= backgroundImage.Height)
		{
			throw new SlideCaptchaException(
				$"模板尺寸({holeTemplateImage.Width}x{holeTemplateImage.Height})过大，无法放入背景图({backgroundImage.Width}x{backgroundImage.Height})");
		}

		// 凹槽位置
		int randomX = Random.Shared.Next(holeTemplateImage.Width + MarginX, maxX);
		int randomY = Random.Shared.Next(MarginY, maxY);

		// 根据透明度计算凹槽图轮廓形状(形状由不透明区域形成)
		using var holeShape = CalcHoleShape(holeTemplateImage);

		// 生成凹槽抠图：把背景图按凹槽形状裁剪下来
		using var holeMattingImage = CreateTransparentBitmap(sliderTemplateImage.Width, sliderTemplateImage.Height);
		var sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);

		using (var canvas = new SKCanvas(holeMattingImage))
		{
			canvas.ClipPath(holeShape, SKClipOperation.Intersect, antialias: true);
			canvas.DrawBitmap(backgroundImage, -randomX, -randomY, sampling);
		}

		// 叠加拖块模板
		using (var canvas = new SKCanvas(holeMattingImage))
		{
			canvas.DrawBitmap(sliderTemplateImage, 0, 0, sampling);
		}

		// 绘制拖块条(高度与背景图一致)
		using var sliderBarImage = CreateTransparentBitmap(sliderTemplateImage.Width, backgroundImage.Height);
		using (var canvas = new SKCanvas(sliderBarImage))
		{
			canvas.DrawBitmap(holeMattingImage, 0, randomY, sampling);
		}

		// 生成背景(在凹槽位置绘制缺口模板)
		using (var canvas = new SKCanvas(backgroundImage))
		{
			canvas.DrawBitmap(holeTemplateImage, randomX, randomY, sampling);
		}

		return new CaptchaImageData
		{
			X = randomX,
			Y = randomY,
			BackgroundImageWidth = backgroundImage.Width,
			BackgroundImageHeight = backgroundImage.Height,
			BackgroundImageBase64 = ToPngBase64(backgroundImage),
			// 滑块图实际是 sliderBarImage，其高度为背景图高度
			SliderImageWidth = sliderBarImage.Width,
			SliderImageHeight = sliderBarImage.Height,
			SliderImageBase64 = ToPngBase64(sliderBarImage)
		};
	}
}
