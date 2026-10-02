using SkiaSharp;

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
		var images = bitmaps.ToList().Select(image => SKBitmap.Decode(new MemoryStream(image))).ToList();
		try
		{
			var height = images.Sum(image => image.Height);
			var width = images.Max(image => image.Width);
			using (var mergeBitmap = new SKBitmap(width, height))
			using (var canvas = new SKCanvas(mergeBitmap))
			{
				int y = 0;//y坐标
				foreach (var image in images)
				{
					canvas.DrawBitmap(image, 0, y, SKSamplingOptions.Default);
					y += image.Height;
				}
				using (var pngImage = SKImage.FromBitmap(mergeBitmap))
				using (var pngData = pngImage.Encode(SKEncodedImageFormat.Png, 100))
				{
					return Convert.ToBase64String(pngData.ToArray());
				}
			}
		}
		finally
		{
			foreach (var image in images)
			{
				image.Dispose();
			}
		}
	}
	public static string VerticalMergeImageStream(params Stream[] bitmaps)
	{
		var images = bitmaps.ToList().Select(SKBitmap.Decode).ToList();
		try
		{
			var height = images.Sum(image => image.Height);
			var width = images.Max(image => image.Width);
			using (var mergeBitmap = new SKBitmap(width, height))
			using (var canvas = new SKCanvas(mergeBitmap))
			{
				int y = 0;//y坐标
				foreach (var image in images)
				{
					canvas.DrawBitmap(image, 0, y, SKSamplingOptions.Default);
					y += image.Height;
				}
				using (var pngImage = SKImage.FromBitmap(mergeBitmap))
				using (var pngData = pngImage.Encode(SKEncodedImageFormat.Png, 100))
				{
					return Convert.ToBase64String(pngData.ToArray());
				}
			}
		}
		finally
		{
			foreach (var image in images)
			{
				image.Dispose();
			}
		}
	}
}
