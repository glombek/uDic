using System.Text.RegularExpressions;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

public class CopyCommand : Command<CopySettings>
{
    public override int Execute(CommandContext context, CopySettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine($"Copying '{settings.CurrentName}' to '{settings.NewName}' (empty translations: {settings.Empty})");

        var dictDir = DictionaryHelper.GetDictionaryDirectory(settings.ProjectPath, createIfMissing: false);

        var aliasMap = DictionaryHelper.LoadAliasMap(dictDir, cancellationToken);

        var isGlob = DictionaryHelper.TryParseGlob(settings.CurrentName, out var regex);

        // Process in alphabetical order so parents are created before children
        var sortedKeys = aliasMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var originalAlias in sortedKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // originalAlias might represent an item that was copied earlier as a new alias; use current map
            if (!aliasMap.TryGetValue(originalAlias, out var kv)) continue;

            var alias = originalAlias;
            var (path, doc) = kv;

            Match? m = null;
            if (isGlob && regex != null)
            {
                m = regex.Match(alias);
                if (!m.Success) continue;
            }
            else
            {
                if (!string.Equals(alias, settings.CurrentName, StringComparison.OrdinalIgnoreCase)) continue;
            }

            // Determine suffix captured by wildcards
            string suffix = string.Empty;
            if (isGlob && m != null)
            {
                // Combine all capture groups into one suffix (if multiple wildcards, concatenate them)
                var groups = new List<string>();
                for (int gi = 1; gi < m.Groups.Count; gi++)
                {
                    groups.Add(m.Groups[gi].Value);
                }
                suffix = string.Concat(groups);
            }

            var newAlias = settings.NewName + suffix;

            if (aliasMap.ContainsKey(newAlias))
            {
                AnsiConsole.MarkupLine($"[yellow]Skipping copy: target alias '{newAlias}' already exists.[/]");
                continue;
            }

            // Ensure parents exist for the new alias
            var newParent = DictionaryHelper.GetParent(newAlias);
            if (!string.IsNullOrEmpty(newParent))
            {
                DictionaryHelper.EnsureParents(newAlias, dictDir, aliasMap, cancellationToken, s => AnsiConsole.MarkupLine($"[green]{s}[/]"));
            }

            // Clone the document for the new file
            var newDoc = XDocument.Parse(doc.ToString());
            var root = newDoc.Root!;
            root.SetAttributeValue("Alias", newAlias);

            // Update Info/Parent element
            var info = root.Element("Info");
            if (string.IsNullOrEmpty(newParent))
            {
                // Top level: make sure Info exists but is empty
                if (info == null)
                {
                    root.AddFirst(new XElement("Info"));
                }
                else
                {
                    info.RemoveNodes(); // remove any <Parent> child
                }
            }
            else
            {
                if (info == null)
                {
                    info = new XElement("Info");
                    root.AddFirst(info);
                }

                var parentElem = info.Element("Parent");
                if (parentElem == null)
                {
                    info.Add(new XElement("Parent", newParent));
                }
                else
                {
                    parentElem.Value = newParent;
                }
            }

            root.SetAttributeValue("Key", Guid.NewGuid());

            // Empty translations if specified
            if (settings.Empty)
            {
                var translations = root.Element("Translations");
                if (translations != null)
                {
                    foreach (var t in translations.Elements("Translation"))
                    {
                        t.Value = string.Empty;
                    }
                }
            }

            var newFileName = Path.Combine(dictDir, $"{Guid.NewGuid()}.config");
            newDoc.Save(newFileName);

            aliasMap[newAlias] = (newFileName, newDoc);
            AnsiConsole.MarkupLine($"[green]Copied '{alias}' -> '{newAlias}' (file: {Path.GetFileName(newFileName)})[/]");
        }

        AnsiConsole.MarkupLine("[blue]Copy operation complete.[/]");
        return 0;
    }
}
