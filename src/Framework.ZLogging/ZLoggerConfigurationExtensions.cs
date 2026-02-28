using ZLogger;
using ZLogger.Formatters;
using ZLogger.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public static class ZLoggerConfigurationExtensions
{
    public static void AddZLogger(
        this ILoggingBuilder builder, string FormatterType = "PlainText")
    {


        builder.AddZLoggerConsole(options =>
    {
        options.UsePlainTextFormatter(formatter =>
                    {
                        // 时间(暗灰) [级别(彩色)] [类名(青色)] 消息(白色)
                        formatter.SetPrefixFormatter(
                            $"{0}{1:local-longdate} {2}[{3:short}]{4} [{5}]{6} ",
                            (in MessageTemplate template, in LogInfo info) =>
                                template.Format(DimGray, info.Timestamp, GetLogLevelColor(info.LogLevel), info.LogLevel, Cyan, info.Category, White));

                        formatter.SetSuffixFormatter($"{0}", (in MessageTemplate template, in LogInfo info) =>
                            template.Format(Reset));
                    });
    });
        // Add to output the file that rotates at constant intervals.
        builder.AddZLoggerRollingFile(options =>
       {
           // File name determined by parameters to be rotated
           options.FilePathSelector = (timestamp, sequenceNumber) => $"logs/{timestamp.ToLocalTime():yyyy-MM-dd}_{sequenceNumber:000}.log";

           // The period of time for which you want to rotate files at time intervals.
           options.RollingInterval = RollingInterval.Day;

        
           // Limit of size if you want to rotate by file size. (KB)
           options.RollingSizeKB = 1024;

           options.UsePlainTextFormatter(formatter =>
                    {
                        // 时间(暗灰) [级别(彩色)] [类名(青色)] 消息(白色)
                        formatter.SetPrefixFormatter(
                            $"{0}{1:local-longdate} {2}[{3:short}]{4} [{5}]{6} ",
                            (in MessageTemplate template, in LogInfo info) =>
                                template.Format(DimGray, info.Timestamp, GetLogLevelColor(info.LogLevel), info.LogLevel, Cyan, info.Category, White));

                        formatter.SetSuffixFormatter($"{0}", (in MessageTemplate template, in LogInfo info) =>
                            template.Format(Reset));
                    });
       });
    }

    // ANSI 颜色常量
    private const string Reset = "\u001b[0m";
    private const string DimGray = "\u001b[90m";   // 暗灰色 - 时间戳
    private const string White = "\u001b[97m";    // 亮白色 - 消息内容
    private const string Cyan = "\u001b[36m";     // 青色 - 类名

    /// <summary>
    /// 根据日志级别获取 ANSI 颜色代码（VSCode 风格）
    /// </summary>
    private static string GetLogLevelColor(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "\u001b[90m",           // 暗灰色
        LogLevel.Debug => "\u001b[34m",           // 蓝色
        LogLevel.Information => "\u001b[32m",    // 绿色
        LogLevel.Warning => "\u001b[33m",         // 黄色
        LogLevel.Error => "\u001b[91m",           // 亮红色
        LogLevel.Critical => "\u001b[95;1m",     // 亮紫色+粗体
        _ => Reset
    };
}