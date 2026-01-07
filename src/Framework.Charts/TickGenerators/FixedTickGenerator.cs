using System;
using ScottPlot;
using ScottPlot.TickGenerators;

namespace Framework.Charts.TickGenerators;

public class FixedNumericManual : NumericManual
{
    /// <summary>
    /// 构造函数：固定刻度数量
    /// </summary>
    /// <param name="tickCount">刻度总数（最少2个）</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="integerOnly">是否只显示整数刻度</param>
    /// <param name="decimalPlaces">小数位数（非整数刻度时使用）</param>
    public FixedNumericManual(int tickCount, double min, double max, bool integerOnly = false, int decimalPlaces = 2)
    {
        tickCount = Math.Max(2, tickCount);

        if (min > max) (min, max) = (max, min);

        double spacing = (max - min) / (tickCount - 1);

        if (integerOnly)
        {
            min = Math.Floor(min);
            max = Math.Ceiling(max);
            spacing = Math.Max(1, Math.Ceiling((max - min) / (tickCount - 1)));
        }

        for (int i = 0; i < tickCount; i++)
        {
            double value = min + i * spacing;
            string label = integerOnly ? ((int)value).ToString() : value.ToString($"F{decimalPlaces}");
            AddMajor(value, label);
        }
    }
}