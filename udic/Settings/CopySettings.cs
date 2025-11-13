using Spectre.Console.Cli;

public class CopySettings : BaseSettings
{
    [CommandArgument(0, "<currentName>")]
    public required string CurrentName { get; set; }

    [CommandArgument(1, "<newName>")]
    public required string NewName { get; set; }

    [CommandOption("--empty")]
    public bool Empty { get; set; } = false;
}
