using System;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScottPlot.TickGenerators;
using Framework.Charts.TickGenerators;

namespace Framework.Charts;

public static partial class Plots
{
    /// <summary>
    /// 时序图
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    public static byte[] SequenceChartLine(List<(List<DateTime>, List<double>, string)> datas, int width = 2250, int height = 350)
    {
        Plot plt = new();
        plt.Font.Automatic();
        plt.Font.Set("SimSun");
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
        plt.Font.Set("SimSun"); plt.Axes.Margins(0.02, 0.02);
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
        plt.Font.Set("SimSun");
        plt.Axes.Left.Min = 0;
        plt.Axes.Left.Max = y.Max() * 1.1; plt.Axes.Margins(0.02, 0.02);
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
        Plot plt = new(); plt.Font.Automatic(); plt.Font.Set("SimSun");

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

        plt.Font.Set("SimSun");
        plt.Title(title, size: 20); plt.Axes.Margins(0.02, 0.02);
        plt.Axes.Bottom.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Bottom.TickGenerator = new FixedNumericManual(10, 0, x.Max() * 1.1);
        plt.Axes.Left.TickLabelStyle = defaultLabelStyle;
        plt.Axes.Left.TickGenerator = defaultNumberFormat;


        var scatter = plt.Add.SignalXY([.. x], [.. y], color: new(System.Drawing.Color.FromArgb(61, 119, 255)));
        scatter.MarkerShape = MarkerShape.None;
        return plt.GetImageBytes(width, height, ImageFormat.Png);
    }
}
