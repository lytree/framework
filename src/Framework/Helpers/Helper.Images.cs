using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats;

namespace Framework.Helpers;

public static partial class Helper
{
	/// <summary>
	/// 竖向合并png 图片
	/// </summary>
	/// <param name="bitmaps">多长图片对应的字节数组</param>
	/// <returns></returns>
	public static string VerticalMergeImageByte(params byte[][] bitmaps)
	{
		var images = bitmaps.ToList().Select(image => Image.Load(new MemoryStream(image))).ToList();
		var height = images.Sum(image => image.Height);
		var width = images.Max(image => image.Width);
		using (var mergeImage = new Image<Rgba32>(width, height))
		{
			int y = 0;//y坐标
			foreach (var image in images)
			{
				mergeImage.Mutate(o => o.DrawImage(image, new Point(0, y), 1));
				y += image.Height;
			}
			return mergeImage.ToBase64String(PngFormat.Instance);
		}
	}
	public static string VerticalMergeImageStream(params Stream[] bitmaps)
	{
		var images = bitmaps.ToList().Select(Image.Load).ToList();
		var height = images.Sum(image => image.Height);
		var width = images.Max(image => image.Width);
		using (var mergeImage = new Image<Rgba32>(width, height))
		{
			int y = 0;//y坐标
			foreach (var image in images)
			{
				mergeImage.Mutate(o => o.DrawImage(image, new Point(0, y), 1));
				y += image.Height;
			}
			return mergeImage.ToBase64String(PngFormat.Instance);
		}
	}
}
