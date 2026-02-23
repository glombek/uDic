using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

public class AddItemCommand : Command<AddItemSettings>
{
    public override int Execute(CommandContext context, AddItemSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine($"Adding '{settings.Alias}' = '{settings.Value}'");

        var dictDir = DictionaryHelper.GetDictionaryDirectory(settings.ProjectPath, createIfMissing: false);

        var aliasMap = DictionaryHelper.LoadAliasMap(dictDir, cancellationToken);

        var (path, created) = DictionaryHelper.AddOrUpdateDictionaryItem(dictDir, aliasMap, settings.Alias, settings.Culture, settings.Value, cancellationToken, s => AnsiConsole.MarkupLine($"[green]{s}[/]"), overwriteEmpty: true);

        if (path is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to create or update dictionary item.[/]");
            return 1;
        }

        if (created)
        {
            AnsiConsole.MarkupLine($"[green]Created dictionary item at:[/] [blue]{path}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Updated dictionary item at:[/] [blue]{path}[/]");
        }

        AnsiConsole.MarkupLine("[blue]Add operation complete.[/]");

        return 0;
    }
}
