using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

public class TreeCommand : Command<BaseSettings>
{
    public override int Execute(CommandContext context, BaseSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine("Enforcing parent relationships for all aliases...");

        var dictDir = DictionaryHelper.GetDictionaryDirectory(settings.ProjectPath, createIfMissing: false);

        // Load current map
        var aliasMap = DictionaryHelper.LoadAliasMap(dictDir, cancellationToken);

        // Ensure parents for every alias (this will create missing parent files and update aliasMap)
        var sortedKeys = aliasMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var alias in sortedKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DictionaryHelper.EnsureParents(alias, dictDir, aliasMap, cancellationToken, s => AnsiConsole.MarkupLine($"[green]{s}[/]"));
        }

        // Now enforce Info/Parent values for every file based on its alias
        var modified = 0;
        var allKeys = aliasMap.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var alias in allKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!aliasMap.TryGetValue(alias, out var kv)) continue;
            var (path, doc) = kv;
            var root = doc.Root!;
            var desiredParent = DictionaryHelper.GetParent(alias);

            var info = root.Element("Info");
            if (string.IsNullOrEmpty(desiredParent))
            {
                if (info == null)
                {
                    root.AddFirst(new XElement("Info"));
                    doc.Save(path);
                    modified++;
                    AnsiConsole.MarkupLine($"[green]Added empty <Info/> for top-level alias '{alias}' ({Path.GetFileName(path)})[/]");
                }
                else
                {
                    // remove Parent child if present
                    var parentElem = info.Element("Parent");
                    if (parentElem != null)
                    {
                        parentElem.Remove();
                        doc.Save(path);
                        modified++;
                        AnsiConsole.MarkupLine($"[green]Removed incorrect <Parent> for top-level alias '{alias}' ({Path.GetFileName(path)})[/]");
                    }
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
                    info.Add(new XElement("Parent", desiredParent));
                    doc.Save(path);
                    modified++;
                    AnsiConsole.MarkupLine($"[green]Set <Parent>='{desiredParent}' for '{alias}' ({Path.GetFileName(path)})[/]");
                }
                else if (!string.Equals(parentElem.Value, desiredParent, StringComparison.OrdinalIgnoreCase))
                {
                    parentElem.Value = desiredParent;
                    doc.Save(path);
                    modified++;
                    AnsiConsole.MarkupLine($"[green]Updated <Parent> to '{desiredParent}' for '{alias}' ({Path.GetFileName(path)})[/]");
                }
            }
        }

        AnsiConsole.MarkupLine($"[blue]Tree enforcement complete. Files modified: {modified}[/]");
        return 0;
    }
}