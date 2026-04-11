using Spectre.Console;

namespace Framework.Cli;

public static partial class LAnsiConsole
{
    public static T Prompt<T>(IPrompt<T> prompt)
    {
        return Spectre.Console.AnsiConsole.Prompt(prompt);
    }

    public static T Ask<T>(string prompt)
    {
        return Spectre.Console.AnsiConsole.Ask<T>(prompt);
    }

    public static T Ask<T>(string prompt, T defaultValue)
    {
        return Spectre.Console.AnsiConsole.Ask(prompt, defaultValue);
    }

    public static bool Confirm(string prompt, bool defaultValue = true)
    {
        return Spectre.Console.AnsiConsole.Confirm(prompt, defaultValue);
    }
}