using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using ScottPlot;
using ScottPlot.TickGenerators;
using Framework.Charts.TickGenerators;
using SkiaSharp;

namespace Framework.Charts;

public static partial class Plots
{
    #region 字体与默认样式

    private static readonly FrozenSet<string> PreferredFonts = new[]
    {
        "SimSun", "SimKai", "DejaVu Sans", "Liberation Sans", "Noto Sans", "FreeSans"
    }.ToFrozenSet();

    private static readonly string SafeFont = ResolveSafeFont();

    private static readonly ScottPlot.Color DefaultColor = new(System.Drawing.Color.FromArgb(61, 119, 255));

    public static string GetSafeFont() => SafeFont;

    private static string ResolveSafeFont()
    {
        var installed = SKFontManager.Default.GetFontFamilies();

        foreach (var font in PreferredFonts)
        {
            if (installed.Contains(font)) return font;
        }

        return installed.Length > 0 ? installed[0] : "sans-serif";
    }

    /// <summary>
    /// 创建一个应用了默认字体、坐标轴边距与刻度样式的 <see cref="Plot"/> 实例。
    /// 调用方负责释放返回实例（建议使用 using 语句）。
    /// </summary>
    /// <param name="title">可选标题。为 null 或空则不设置标题。</param>
    private static Plot CreateDefaultPlot(string? title = null)
    {
        var plt = new Plot();
        plt.Font.Automatic();
        plt.Font.Set(SafeFont);
        if (!string.IsNullOrEmpty(title))
        {
            plt.Title(title, size: 20);
        }
        plt.Axes.Margins(0.02, 0.02);
        plt.Axes.Bottom.TickLabelStyle = DefaultLabelStyle;
        plt.Axes.Left.TickLabelStyle = DefaultLabelStyle;
        plt.Axes.Left.TickGenerator = DefaultNumberFormat;
        return plt;
    }
    #endregion

    /// <summary>
    /// 时序图
    /// </summary>
    /// <param name="datas"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static byte[] SequenceChartLine(List<(List<DateTime> Dates, List<double> Values, string Label)> datas, int width = 2250, int height = 350)
    {
        ArgumentNullException.ThrowIfNull(datas);

        using var plt = CreateDefaultPlot();
        plt.Axes.Bottom.TickGenerator = DefaultTimeFormat;

        foreach (var data in datas)
        {
            var xs = data.Dates.Select(d => d.ToOADate()).ToArray();
            var scatter = plt.Add.SignalXY(xs, [.. data.Values]);
            scatter.LegendText = data.Label;
            scatter.MarkerShape = MarkerShape.None;
        }
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }

    /// <summary>
    /// 趋势图
    /// </summary>
    /// <param name="datas"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static byte[] TrendChartLine(List<(List<double> X, List<double> Y, string Label)> datas, int width = 2250, int height = 350)
    {
        ArgumentNullException.ThrowIfNull(datas);

        using var plt = CreateDefaultPlot();
        plt.Axes.Bottom.TickGenerator = DefaultTimeFormat;

        foreach (var data in datas)
        {
            var scatter = plt.Add.SignalXY([.. data.X], [.. data.Y]);
            scatter.LegendText = data.Label;
            scatter.MarkerShape = MarkerShape.None;
        }
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }

    /// <summary>
    /// 频谱图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static byte[] SpectrumChart(List<double> x, List<double> y, int width = 2250, int height = 350)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        using var plt = CreateDefaultPlot();
        plt.Axes.Left.Min = 0;
        plt.Axes.Left.Max = y.Max() * 1.1;
        plt.Axes.Bottom.TickGenerator = new FixedNumericManual(10, 0, x.Max() * 1.1);

        var scatter = plt.Add.SignalXY([.. x], [.. y], color: DefaultColor);
        scatter.MarkerShape = MarkerShape.None;
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }

    /// <summary>
    /// 波形图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static byte[] WaveformChart(List<double> x, List<double> y, int width = 2250, int height = 350)
        => WaveformChart(x, y, title: null, width, height);

    /// <summary>
    /// 波形图（带标题）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="title"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static byte[] WaveformChart(List<double> x, List<double> y, string? title, int width = 2250, int height = 350)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        using var plt = CreateDefaultPlot(title);
        plt.Axes.Bottom.TickGenerator = new FixedNumericManual(10, 0, x.Max() * 1.1);

        var scatter = plt.Add.SignalXY([.. x], [.. y], color: DefaultColor);
        scatter.MarkerShape = MarkerShape.None;
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }
}
