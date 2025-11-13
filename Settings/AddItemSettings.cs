using Spectre.Console.Cli;

public class AddItemSettings : AddBaseSettings
{
    [CommandArgument(0, "<alias>")]
    public required string Alias { get; set; }

    [CommandArgument(1, "[value]")]
    public string? Value { get; set; }
}