using System;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScottPlot.TickGenerators;
using Framework.Charts.TickGenerators;
using SkiaSharp;

namespace Framework.Charts;

public static partial class Plots
{
    #region Avalonia 加载字体

    public static string GetSafeFont()
    {
        var installed = SKFontManager.Default.GetFontFamilies();

        // 优先搜索 Linux 常用开源字体
        string[] linuxFonts = ["SimSun", "SimKai", "DejaVu Sans", "Liberation Sans", "Noto Sans", "FreeSans"];

        foreach (var font in linuxFonts)
        {
            if (installed.Contains(font)) return font;
        }

        // 如果都没有，返回第一个可用的字体
        return installed.Length > 0 ? installed[0] : "sans-serif";
    }
    #endregion

    /// <summary>
    /// 时序图
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    public static byte[] SequenceChartLine(List<(List<DateTime>, List<double>, string)> datas, int width = 2250, int height = 350)
    {
        Plot plt = new();
        plt.Font.Automatic();
        plt.Font.Set(GetSafeFont());
        plt.Axes.Margins(0.02, 0.02);
        plt.Axes.Bottom.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Bottom.TickGenerator = defaultTimeFormat;
        plt.Axes.Left.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Left.TickGenerator = defaultNumberFormat;
        foreach (var data in datas)
        {
            var scatter = plt.Add.SignalXY([.. data.Item1.Select(d => d.ToOADate())], [.. data.Item2]);
            scatter.LegendText = data.Item3;
            scatter.MarkerShape = MarkerShape.None;

        }
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }
    /// <summary>
    /// 趋势图
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    public static byte[] TrendChartLine(List<(List<double>, List<double>, string)> datas, int width = 2250, int height = 350)
    {
        Plot plt = new();
        plt.Font.Automatic();
        plt.Font.Set(GetSafeFont());
        plt.Axes.Margins(0.02, 0.02);
        plt.Axes.Bottom.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Bottom.TickGenerator = defaultTimeFormat;
        plt.Axes.Left.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Left.TickGenerator = defaultNumberFormat;
        foreach (var data in datas)
        {
            var scatter = plt.Add.SignalXY([.. data.Item1], [.. data.Item2]);
            scatter.LegendText = data.Item3;
            scatter.MarkerShape = MarkerShape.None;
        }
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }
    /// <summary>
    /// 频谱图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static byte[] SpectrumChart(List<double> x, List<double> y, int width = 2250, int height = 350)
    {
        Plot plt = new();
        plt.Font.Automatic();
        plt.Font.Set(GetSafeFont());
        plt.Axes.Left.Min = 0;
        plt.Axes.Left.Max = y.Max() * 1.1;
        plt.Axes.Margins(0.02, 0.02);
        plt.Axes.Bottom.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Bottom.TickGenerator = new FixedNumericManual(10, 0, x.Max() * 1.1);
        plt.Axes.Left.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Left.TickGenerator = defaultNumberFormat;

        var scatter = plt.Add.SignalXY([.. x], [.. y], color: new(System.Drawing.Color.FromArgb(61, 119, 255)));
        scatter.MarkerShape = MarkerShape.None;
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }

    /// <summary>
    /// 波形图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static byte[] WaveformChart(List<double> x, List<double> y, int width = 2250, int height = 350)
    {
        Plot plt = new();
        plt.Font.Automatic();
        plt.Font.Set(GetSafeFont());

        plt.Axes.Margins(0.02, 0.02);
        plt.Axes.Bottom.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Bottom.TickGenerator = new FixedNumericManual(10, 0, x.Max() * 1.1);
        plt.Axes.Left.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Left.TickGenerator = defaultNumberFormat;
        var scatter = plt.Add.SignalXY([.. x], [.. y], color: new(System.Drawing.Color.FromArgb(61, 119, 255)));
        scatter.MarkerShape = MarkerShape.None;
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }
    /// <summary>
    /// 波形图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static byte[] WaveformChart(List<double> x, List<double> y, string title, int width = 2250, int height = 350)
    {
        Plot plt = new();
        plt.Font.Automatic();
        plt.Font.Set(GetSafeFont());
        plt.Title(title, size: 20);
        plt.Axes.Margins(0.02, 0.02);
        plt.Axes.Bottom.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Bottom.TickGenerator = new FixedNumericManual(10, 0, x.Max() * 1.1);
        plt.Axes.Left.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Left.TickGenerator = defaultNumberFormat;


        var scatter = plt.Add.SignalXY([.. x], [.. y], color: new(System.Drawing.Color.FromArgb(61, 119, 255)));
        scatter.MarkerShape = MarkerShape.None;
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }
}
