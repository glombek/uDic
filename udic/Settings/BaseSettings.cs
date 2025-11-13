using Spectre.Console.Cli;

public class BaseSettings : CommandSettings
{
    [CommandOption("-p|--project")]
    public string ProjectPath { get; set; } = "./";
}
