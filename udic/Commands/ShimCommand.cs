using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

public class ShimCommand : Command<ShimSettings>
{
    private static readonly Regex FormatParser = new(@"{(?<name>[^:}]+)(?::(?<param>[^:}]+))*}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public override int Execute(CommandContext context, ShimSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine($"Shimming {settings.Culture} for '{settings.Name}' in format '{settings.Format}'");

        var dictDir = DictionaryHelper.GetDictionaryDirectory(settings.ProjectPath, createIfMissing: false);

        var aliasMap = DictionaryHelper.LoadAliasMap(dictDir, cancellationToken);

        var isGlob = DictionaryHelper.TryParseGlob(settings.Name, out var regex);

        foreach (var aliasMapping in aliasMap)
        {
            Match? m = null;
            if (isGlob && regex != null)
            {
                m = regex.Match(aliasMapping.Key);
                if (!m.Success) continue;
            }
            else
            {
                if (!string.Equals(aliasMapping.Key, settings.Name, StringComparison.OrdinalIgnoreCase)) continue;
            }

            var root = aliasMapping.Value.Doc.Root!;

            var translations = root.Element("Translations");
            if (translations != null)
            {
                var value = FormatParser.Replace(settings.Format, (match) =>
                {
                    var name = match.Groups["name"].Value;
                    var param = match.Groups["param"]?.Value;
                    switch (name)
                    {
                        case "culture":
                            return settings.Culture;
                        case "alias":
                            return aliasMapping.Key;
                        case "value":
                            var translation = translations.Elements("Translation")
                            .FirstOrDefault(t => t.Attribute("Language")?.Value == param);
                            return string.IsNullOrEmpty(translation?.Value) ? "[Empty]" : translation.Value;
                        default:
                            return $"{{{name}{(param is null ? "" : ":")}{param}}}";

                    }
                });

                // Get translation for specified culture
                var translation = translations.Elements("Translation")
                    .FirstOrDefault(t => t.Attribute("Language")?.Value == settings.Culture);

                if (translation == null)
                {
                    // Add new translation
                    translation = new XElement("Translation",
                        new XAttribute("Language", settings.Culture),
                        new XElement("Value", value ?? string.Empty));
                    translations.Add(translation);
                }

                translation.Value = value ?? string.Empty;

                aliasMapping.Value.Doc.Save(aliasMapping.Value.Path);

                AnsiConsole.MarkupLine($"[green]Shimmed dictionary item '{aliasMapping.Key.EscapeMarkup()}' = '{value.EscapeMarkup()}' at:[/] [blue]{aliasMapping.Value.Path.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.MarkupLine("[blue]Add operation complete.[/]");


        return 0;
    }
}