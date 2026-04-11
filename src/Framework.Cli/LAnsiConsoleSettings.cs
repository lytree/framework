using System.IO;

namespace Framework.Cli;

public sealed class LAnsiConsoleSettings
{
    public Spectre.Console.AnsiSupport Ansi { get; set; } = Spectre.Console.AnsiSupport.Detect;
    public Spectre.Console.ColorSystemSupport ColorSystem { get; set; } = Spectre.Console.ColorSystemSupport.Detect;
    public TextWriter? Out { get; set; }
    public string? FilePath { get; set; }
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
}