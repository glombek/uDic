using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

public class AddScanCommand : Command<AddScanSettings>
{
    public override int Execute(CommandContext context, AddScanSettings settings, CancellationToken cancellationToken)
    {
        // Resolve path to scan, defaulting to project directory, defaulting to current directory if project path is not provided. The path may resolve to a single cshtml file, or a directory to scan for cshtml files (recursively)
        string resolvedPath;
        var provided = settings.Path;

        if (string.IsNullOrWhiteSpace(provided))
        {
            resolvedPath = !string.IsNullOrWhiteSpace(settings.ProjectPath)
                ? Path.GetFullPath(settings.ProjectPath)
                : Directory.GetCurrentDirectory();
        }
        else
        {
            if (Path.IsPathRooted(provided))
            {
                resolvedPath = Path.GetFullPath(provided);
            }
            else
            {
                var baseDir = !string.IsNullOrWhiteSpace(settings.ProjectPath)
                    ? settings.ProjectPath
                    : Directory.GetCurrentDirectory();
                resolvedPath = Path.GetFullPath(Path.Combine(baseDir, provided));
            }
        }

        AnsiConsole.WriteLine($"Scanning for dictionary items in '{resolvedPath}'");

        Dictionary<string, string> matches = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Build list of cshtml files to scan
        var filesToScan = new List<string>();
        if (File.Exists(resolvedPath))
        {
            // single file
            if (string.Equals(Path.GetExtension(resolvedPath), ".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                filesToScan.Add(resolvedPath);
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Specified file is not a .cshtml file: {resolvedPath} - skipping.[/]");
            }
        }
        else if (Directory.Exists(resolvedPath))
        {
            filesToScan.AddRange(Directory.GetFiles(resolvedPath, "*.cshtml", SearchOption.AllDirectories));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Path does not exist: {resolvedPath}[/]");
            return 1;
        }

        // Regex to match @Umbraco.GetDictionaryValue("key", "value")  or single-arg form
        var pattern = new Regex(@"@Umbraco\.GetDictionaryValue\(\s*""(?<key>[^""]+)""\s*(?:,\s*""(?<value>[^""]*)"")?\s*\)", RegexOptions.Compiled);

        foreach (var file in filesToScan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text;
            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Failed to read '{file}': {ex.Message}[/]");
                continue;
            }

            var ms = pattern.Matches(text);
            foreach (Match m in ms)
            {
                if (!m.Success) continue;
                var key = m.Groups["key"]?.Value?.Trim();
                var value = m.Groups["value"]?.Success == true ? m.Groups["value"]?.Value : string.Empty;
                if (string.IsNullOrWhiteSpace(key)) continue;

                // If we've already seen this key, prefer a non-empty value
                if (matches.TryGetValue(key, out var existing))
                {
                    if (string.IsNullOrEmpty(existing) && !string.IsNullOrEmpty(value))
                    {
                        matches[key] = value ?? string.Empty;
                    }
                }
                else
                {
                    matches[key] = value ?? string.Empty;
                }
            }
        }

        var dictDir = DictionaryHelper.GetDictionaryDirectory(settings.ProjectPath, createIfMissing: false);

        var aliasMap = DictionaryHelper.LoadAliasMap(dictDir, cancellationToken);

        foreach (var match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AnsiConsole.WriteLine($"Adding '{match.Key}' = '{match.Value}'");

            // Ensure parents exist for the new alias
            var newParent = DictionaryHelper.GetParent(match.Key);
            if (!string.IsNullOrEmpty(newParent))
            {
                DictionaryHelper.EnsureParents(match.Key, dictDir, aliasMap, cancellationToken, s => AnsiConsole.MarkupLine($"[green]{s}[/]"));
            }

            if (aliasMap.TryGetValue(match.Key, out var kv))
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
                            new XElement("Value", match.Value ?? string.Empty));
                        translations.Add(translation);
                    }

                    if (!string.IsNullOrEmpty(match.Value))
                    {
                        translation.Value = match.Value ?? string.Empty;
                    }
                }

                kv.Doc.Save(kv.Path);
            }
            else
            {

                var filePath = DictionaryHelper.CreateDictionaryItem(dictDir, aliasMap, match.Key, match.Value is null ? null : (settings.Culture, match.Value));

                if (filePath is null)
                {
                    AnsiConsole.MarkupLine("[red]Failed to create dictionary item.[/]");
                    return 1;
                }

                AnsiConsole.MarkupLine($"[green]Created dictionary item at:[/] [blue]{filePath}[/]");
            }
        }

        AnsiConsole.MarkupLine("[blue]Add operation complete.[/]");

        return 0;
    }
}
