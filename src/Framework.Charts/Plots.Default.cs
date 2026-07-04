using System;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace Framework.Charts;

public static partial class Plots
{
    private static readonly Lazy<LabelStyle> defaultLabelStyleLazy = new(() => new LabelStyle
    {
        FontName = GetSafeFont(),
        FontSize = 18,
    });
    public static LabelStyle defaultLabelStyle => defaultLabelStyleLazy.Value;
    public static readonly DateTimeAutomatic defaultTimeFormat = new()
    {
        LabelFormatter = (dt) => dt.ToString("yyyy-MM-dd")
    };
    public static readonly NumericAutomatic defaultNumberFormat = new()
    {

    };
}
