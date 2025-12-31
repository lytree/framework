using System;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScottPlot.TickGenerators;

namespace Framework.Plot;

public static partial class Plots
{
    /// <summary>
    /// 时序图
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    public static byte[] SequenceChartLine(List<(List<DateTime>, List<double>, string)> datas)
    {
        ScottPlot.Plot plt = new();
        foreach (var data in datas)
        {
            var scatter = plt.Add.SignalXY(data.Item1.Select(d => d.ToOADate()).ToArray(), data.Item2.ToArray());
            scatter.LegendText = data.Item3;
            scatter.MarkerShape = MarkerShape.None;
            scatter.Axes.XAxis.TickLabelStyle = defaultLabelStyle;
            scatter.Axes.XAxis.TickGenerator = defaultTimeFormat;
            scatter.Axes.YAxis.TickLabelStyle = defaultLabelStyle;
            scatter.Axes.YAxis.TickGenerator = defaultNumberFormat;
        }
        return plt.GetImageBytes(1500, 600, ImageFormat.Png);
    }
    /// <summary>
    /// 趋势图
    /// </summary>
    /// <param name="datas"></param>
    /// <returns></returns>
    public static byte[] TrendChartLine(List<(List<double>, List<double>, string)> datas)
    {
        ScottPlot.Plot plt = new();
        foreach (var data in datas)
        {
            var scatter = plt.Add.SignalXY(data.Item1.ToArray(), data.Item2.ToArray());
            scatter.LegendText = data.Item3;
            scatter.MarkerShape = MarkerShape.None;
            scatter.Axes.XAxis.TickLabelStyle = defaultLabelStyle;
            scatter.Axes.XAxis.TickGenerator = defaultNumberFormat;
            scatter.Axes.YAxis.TickLabelStyle = defaultLabelStyle;
            scatter.Axes.YAxis.TickGenerator = defaultNumberFormat;
        }
        return plt.GetImageBytes(1500, 600, ImageFormat.Png);
    }
    /// <summary>
    /// 趋势图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static byte[] SpectrumChart(List<double> x, List<double> y)
    {
        ScottPlot.Plot plt = new();
        plt.Axes.Left.Min = 0;
        plt.Axes.Left.Max = y.Max() * 1.1;

        var scatter = plt.Add.SignalXY(x.ToArray(), y.ToArray(), color: new(System.Drawing.Color.FromArgb(61, 119, 255)));
        scatter.MarkerShape = MarkerShape.None;
        scatter.Axes.XAxis.TickLabelStyle = defaultLabelStyle;
        scatter.Axes.XAxis.TickGenerator = defaultTimeFormat;
        scatter.Axes.YAxis.TickLabelStyle = defaultLabelStyle;
        scatter.Axes.YAxis.TickGenerator = defaultNumberFormat;
        return plt.GetImageBytes(2250, 350, ImageFormat.Png);
    }

    /// <summary>
    /// 波形图
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static byte[] WaveformChart(List<double> x, List<double> y)
    {
        ScottPlot.Plot plt = new();

        // 固定 Y 轴最小值为0
        plt.Axes.Left.Min = y.Min() * 1.1;

        // 可选：最大值自动计算或手动设置
        plt.Axes.Left.Max = y.Max() * 1.1;

        var scatter = plt.Add.SignalXY(x.ToArray(), y.ToArray(), color: new(System.Drawing.Color.FromArgb(61, 119, 255)));
        scatter.MarkerShape = MarkerShape.None;
        scatter.Axes.XAxis.TickLabelStyle = defaultLabelStyle;
        scatter.Axes.XAxis.TickGenerator = defaultTimeFormat;
        scatter.Axes.YAxis.TickLabelStyle = defaultLabelStyle;
        scatter.Axes.YAxis.TickGenerator = defaultNumberFormat;
        return plt.GetImageBytes(2250, 350, ImageFormat.Png);
    }
}
