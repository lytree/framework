using Framework.SlideCaptcha.Exceptions;
using Framework.SlideCaptcha.Resources;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
	/// 原理： 一行一行扫描，每行不透明小方块连接形成数个小长方形（RectangularPolygon）。
	///       多个RectangularPolygon形成ComplexPolygon，ComplexPolygon则代表图形的轮廓
	/// </summary>
	/// <param name="holeTemplateImage"></param>
	/// <returns></returns>
	private static ComplexPolygon CalcHoleShape(Image<Rgba32> holeTemplateImage)
	{
		var pathList = new List<IPath>();
		holeTemplateImage.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < holeTemplateImage.Height; y++)
			{
				var rowSpan = accessor.GetRowSpan(y);

				// 不透明区段的起始列，-1 表示当前行尚未进入不透明区段。
				// 原实现用 0 作为哨兵，导致「从第 0 列开始的不透明区段」被误判为未开始而整段丢失。
				int start = -1;

				for (int x = 0; x < rowSpan.Length; x++)
				{
					if (rowSpan[x].A != 0)
					{
						start = start < 0 ? x : start;
					}
					else if (start >= 0)
					{
						pathList.Add(new RectangularPolygon(start, y, x - start, 1));
						start = -1;
					}
				}

				// 行尾收尾：若不透明区段一直延伸到行末，原实现不会补上这一段
				if (start >= 0)
				{
					pathList.Add(new RectangularPolygon(start, y, rowSpan.Length - start, 1));
				}
			}
		});

		return new ComplexPolygon(new PathCollection(pathList));
	}

	public CaptchaImageData Generate()
	{
		var background = _resourceManager.RandomBackground();
		(var silder, var hole) = _resourceManager.RandomTemplate();

		using var backgroundImage = Image.Load<Rgba32>(background);
		using var sliderTemplateImage = Image.Load<Rgba32>(silder);
		using var holeTemplateImage = Image.Load<Rgba32>(hole);

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
		var holeShape = CalcHoleShape(holeTemplateImage);
		// 生成凹槽抠图
		using var holeMattingImage = new Image<Rgba32>(sliderTemplateImage.Width, sliderTemplateImage.Height);
		using var sliderBarImage = new Image<Rgba32>(sliderTemplateImage.Width, backgroundImage.Height);

		holeMattingImage.Mutate(x =>
		{
			x.Clip(holeShape, p => p.DrawImage(backgroundImage, new Point(-randomX, -randomY), 1));
		});
		// 叠加拖块模板
		holeMattingImage.Mutate(x => x.DrawImage(sliderTemplateImage, new Point(0, 0), 1));
		// 绘制拖块条
		sliderBarImage.Mutate(x => x.DrawImage(holeMattingImage, new Point(0, randomY), 1));

		// 生成背景
		backgroundImage.Mutate(x => x.DrawImage(holeTemplateImage, new Point(randomX, randomY), 1));

		return new CaptchaImageData
		{
			X = randomX,
			Y = randomY,
			BackgroundImageWidth = backgroundImage.Width,
			BackgroundImageHeight = backgroundImage.Height,
			BackgroundImageBase64 = backgroundImage.ToBase64String(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance),
			// 滑块图实际是 sliderBarImage，其高度为背景图高度（原实现误报为 holeMattingImage 的高度，
			// 前端按该高度布局会与真实图片不一致）
			SliderImageWidth = sliderBarImage.Width,
			SliderImageHeight = sliderBarImage.Height,
			SliderImageBase64 = sliderBarImage.ToBase64String(SixLabors.ImageSharp.Formats.Png.PngFormat.Instance)
		};
	}
}