using System;
using System.Globalization;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace Framework.Charts;

public static partial class Plots
{
    private static readonly Lazy<LabelStyle> DefaultLabelStyleLazy = new(() => new LabelStyle
    {
        FontName = GetSafeFont(),
        FontSize = 18,
    });

    /// <summary>
    /// 默认刻度标签样式（懒初始化，避免静态字段初始化顺序依赖 <see cref="SafeFont"/>）。
    /// </summary>
    private static LabelStyle DefaultLabelStyle => DefaultLabelStyleLazy.Value;

    /// <summary>
    /// 默认时间轴刻度生成器。使用 <see cref="CultureInfo.InvariantCulture"/> 避免跨区域格式差异。
    /// </summary>
    private static readonly DateTimeAutomatic DefaultTimeFormat = new()
    {
        LabelFormatter = (dt) => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// 默认数值轴刻度生成器。
    /// </summary>
    private static readonly NumericAutomatic DefaultNumberFormat = new();
}
