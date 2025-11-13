using Spectre.Console.Cli;

public class ShimSettings : BaseSettings
{

    [CommandArgument(0, "<format>")]
    public required string Format { get; set; }

    [CommandOption("-n|--name")]
    public string Name { get; set; } = "*";

    [CommandOption("-c|--culture")]
    public string Culture { get; set; } = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
}
