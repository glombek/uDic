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

        // Ensure parents exist for the new alias
        var newParent = DictionaryHelper.GetParent(settings.Alias);
        if (!string.IsNullOrEmpty(newParent))
        {
            DictionaryHelper.EnsureParents(settings.Alias, dictDir, aliasMap, cancellationToken, s => AnsiConsole.MarkupLine($"[green]{s}[/]"));
        }

        if (aliasMap.TryGetValue(settings.Alias, out var kv))
        {
            // Already exists, add this culture
            var root = kv.Doc.Root!;

            var translations = root.Element("Translations");
            if (translations != null)
            {
                // Get translation for specified culture

                var translation = translations.Elements("Translation")
                    .FirstOrDefault(t => t.Attribute("Language")?.Value == settings.Culture);

                if (translation == null)
                {
                    // Add new translation
                    translation = new XElement("Translation",
                        new XAttribute("Language", settings.Culture),
                        new XElement("Value", settings.Value ?? string.Empty));
                    translations.Add(translation);
                }

                translation.Value = settings.Value ?? string.Empty;
            }

            kv.Doc.Save(kv.Path);
        }
        else
        {

            var filePath = DictionaryHelper.CreateDictionaryItem(dictDir, aliasMap, settings.Alias, settings.Value is null ? null : (settings.Culture, settings.Value));

            if (filePath is null)
            {
                AnsiConsole.MarkupLine("[red]Failed to create dictionary item.[/]");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]Created dictionary item at:[/] [blue]{filePath}[/]");
        }

        AnsiConsole.MarkupLine("[blue]Add operation complete.[/]");

        return 0;
    }
}
