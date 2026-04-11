using Spectre.Console;
using Spectre.Console.Rendering;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static LiveDisplay Live(IRenderable target)
    {
        return Spectre.Console.AnsiConsole.Live(target);
    }
}