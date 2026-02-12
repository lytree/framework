using System;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace Framework.Charts;

public static partial class Plots
{
    public static readonly LabelStyle defaultLabelStyle = new()
    {
        FontName = GetSafeFont(),
        FontSize = 18,

    };
    public static readonly DateTimeAutomatic defaultTimeFormat = new()
    {
        LabelFormatter = (dt) => dt.ToString("yyyy-MM-dd")
    };
    public static readonly NumericAutomatic defaultNumberFormat = new()
    {

    };
}
