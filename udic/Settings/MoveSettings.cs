using Spectre.Console.Cli;

public class MoveSettings : BaseSettings
{
    [CommandArgument(0, "<currentName>")]
    public required string CurrentName { get; set; }

    [CommandArgument(1, "<newName>")]
    public required string NewName { get; set; }
}
