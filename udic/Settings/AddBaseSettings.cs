using Spectre.Console.Cli;

public class AddBaseSettings : BaseSettings
{

    [CommandOption("-c|--culture")]
    public string Culture { get; set; } = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
}