using System;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace Framework.Plot;

public static partial class Plots
{
    public static readonly LabelStyle defaultLabelStyle = new()
    {
        FontName = "宋体",
        FontSize = 18,

    };
    public static readonly DateTimeAutomatic defaultTimeFormat = new()
    {
        LabelFormatter = (dt) => dt.ToString("yyyy-MM-dd")
    };
    public static readonly NumericAutomatic defaultNumberFormat = new()
    {
        LabelFormatter = (dt) => dt.ToString("F3")

    };
}
