using System.Text.RegularExpressions;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

public class MoveCommand : Command<MoveSettings>
{
    public override int Execute(CommandContext context, MoveSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine($"Moving '{settings.CurrentName}' to '{settings.NewName}'");

        var dictDir = DictionaryHelper.GetDictionaryDirectory(settings.ProjectPath, createIfMissing: false);

        var aliasMap = DictionaryHelper.LoadAliasMap(dictDir, cancellationToken);

        var isGlob = DictionaryHelper.TryParseGlob(settings.CurrentName, out var regex);

        // Process matched files in alphabetical order so parents are handled before children
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Capture keys sorted alphabetically (case-insensitive)
        var sortedKeys = aliasMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var originalAlias in sortedKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The alias may have been removed/renamed already
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
            if (string.Equals(newAlias, alias, StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[grey]Alias '{alias}' unchanged (new alias same as old).[/]");
                continue;
            }

            // Ensure parents for newAlias exist
            var newParent = DictionaryHelper.GetParent(newAlias);
            if (!string.IsNullOrEmpty(newParent))
            {
                DictionaryHelper.EnsureParents(newAlias, dictDir, aliasMap, cancellationToken, s => AnsiConsole.MarkupLine($"[green]{s}[/]"));
            }

            // Update document: Alias attribute
            var root = doc.Root!;
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

            // Save document back to same file path
            doc.Save(path);
            AnsiConsole.MarkupLine($"[green]Moved '{alias}' -> '{newAlias}' (file: {Path.GetFileName(path)})[/]");

            // Update aliasMap key: remove old key and add new mapping
            aliasMap.Remove(alias);
            aliasMap[newAlias] = (path, doc);
            processed.Add(newAlias);
        }

        AnsiConsole.MarkupLine("[blue]Move operation complete.[/]");
        return 0;
    }
}
