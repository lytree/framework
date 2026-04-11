using System;
using ZLogger;

namespace Framework.ZLogging;

public class ZLoggerSpectreConsoleOptions : SpectreConsoleLogProcessorOptions
{
    public bool EnableAnsi { get; set; } = true;
    public bool UseTime { get; set; } = true;
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public string TraceColor { get; set; } = "grey";
    public string DebugColor { get; set; } = "grey";
    public string InformationColor { get; set; } = "green";
    public string WarningColor { get; set; } = "yellow";
    public string ErrorColor { get; set; } = "red";
    public string CriticalColor { get; set; } = "red bold";
}