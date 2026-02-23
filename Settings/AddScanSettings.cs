using Spectre.Console.Cli;

public class AddScanSettings: AddBaseSettings
{
    [CommandOption("-f|--path")]
    public string? Path { get; set; }
}