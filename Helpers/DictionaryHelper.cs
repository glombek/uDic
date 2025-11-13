using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

public static class DictionaryHelper
{
    public static string GetParent(string alias)
    {
        if (string.IsNullOrEmpty(alias)) return string.Empty;
        var idx = alias.LastIndexOf('.');
        return idx <= 0 ? string.Empty : alias.Substring(0, idx);
    }

    // Resolve the uSync Dictionary directory (matches any version folder, e.g. v8, v16)
    // If createIfMissing is true the returned path will be created if it does not exist.
    public static string GetDictionaryDirectory(string projectPath, bool createIfMissing = true)
    {
        var projectFull = Path.GetFullPath(string.IsNullOrWhiteSpace(projectPath) ? "./" : projectPath);
        var usyncDir = Path.Combine(projectFull, "uSync");

        // If uSync exists, look for v* folders and a Dictionary subfolder
        if (Directory.Exists(usyncDir))
        {
            // prefer the highest version folder (lexicographical descending is fine for v* names)
            var versionDirs = Directory.GetDirectories(usyncDir, "v*", SearchOption.TopDirectoryOnly)
                                       .OrderByDescending(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)
                                       .ToList();

            foreach (var vdir in versionDirs)
            {
                var dictPath = Path.Combine(vdir, "Dictionary");
                if (Directory.Exists(dictPath))
                {
                    return dictPath;
                }
            }

            // no existing Dictionary folder found — pick the highest v* folder and return its Dictionary path (create later if requested)
            if (versionDirs.Count > 0)
            {
                var chosen = Path.Combine(versionDirs.First(), "Dictionary");
                if (createIfMissing)
                    Directory.CreateDirectory(chosen);
                return chosen;
            }
        }

        // fallback to uSync\data\Dictionary 
        var fallback = Path.Combine(projectFull, "uSync", "data", "Dictionary");
        if (createIfMissing)
            Directory.CreateDirectory(fallback);
        return fallback;
    }

    public static Dictionary<string, (string Path, XDocument Doc)> LoadAliasMap(string dictDir, CancellationToken cancellationToken)
    {
        var aliasMap = new Dictionary<string, (string Path, XDocument Doc)>(StringComparer.OrdinalIgnoreCase);

        var xmlFiles = Directory.GetFiles(dictDir, "*.config", SearchOption.TopDirectoryOnly)
                        .Distinct()
                        .ToList();

        foreach (var file in xmlFiles)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var doc = XDocument.Load(file);
                var root = doc.Root;
                if (root == null) continue;
                if (root.Name != "Dictionary") continue;
                var aliasAttr = root.Attribute("Alias")?.Value;
                if (string.IsNullOrWhiteSpace(aliasAttr)) continue;
                aliasMap[aliasAttr] = (file, doc);
            }
            catch
            {
                // ignore invalid xml or load errors
            }
        }

        return aliasMap;
    }

    // Ensure parents for alias exist in aliasMap; creates files as necessary and updates aliasMap
    public static void EnsureParents(string alias, string dictDir, Dictionary<string, (string Path, XDocument Doc)> aliasMap, CancellationToken cancellationToken, Action<string>? log = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parts = alias.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parentAlias = string.Join('.', parts.Take(i + 1));

            var fileName = CreateDictionaryItem(dictDir, aliasMap, parentAlias);
            if (fileName is null)
            {
                continue;
            }
            log?.Invoke($"Created parent file for '{parentAlias}' -> {fileName}");
        }
    }

    public static string? CreateDictionaryItem(string dictDir, Dictionary<string, (string Path, XDocument Doc)> aliasMap, string alias, (string culture, string? value)? value = null)
    {
        if (aliasMap.ContainsKey(alias)) return null;

        var key = Guid.NewGuid().ToString();
        var fileName = Path.Combine(dictDir, $"{key}.config");
        var parentOfParent = GetParent(alias);

        XElement infoElem;
        if (string.IsNullOrEmpty(parentOfParent))
        {
            infoElem = new XElement("Info");
        }
        else
        {
            infoElem = new XElement("Info", new XElement("Parent", parentOfParent));
        }

        var dict = new XElement("Dictionary",
            new XAttribute("Key", key),
            new XAttribute("Alias", alias),
            new XAttribute("Level", alias.Split(".").Count() - 1),
            infoElem,
            new XElement("Translations",
            value is null ? null :
                new XElement("Translation",
                    new XAttribute("Language", value.Value.culture),
                    value.Value.value
                )
            )
        );

        var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), dict);
        newDoc.Save(fileName);

        aliasMap[alias] = (fileName, newDoc);
        return fileName;
    }

    public static bool TryParseGlob(string pattern, out Regex? regex)
    {
        var isGlob = pattern.Contains("*");
        regex = null;
        if (isGlob)
        {
            // Convert glob to regex, capture wildcard group(s) as (.*)
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", "(.*)") + "$";
            regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        }
        return isGlob;
    }


}
