using YtdlWinUI.Models;

namespace YtdlWinUI.Services;

public sealed class BrowserProfileService
{
    private static readonly IReadOnlyDictionary<string, string[]> RelativeIniPaths =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Firefox"] = [Path.Combine("Mozilla", "Firefox", "profiles.ini")],
            ["Floorp"] = [Path.Combine("Floorp", "profiles.ini")],
            ["Zen"] = [Path.Combine("zen", "profiles.ini"), Path.Combine("Zen", "profiles.ini")]
        };

    public IReadOnlyList<BrowserProfileOption> FindProfiles(string browser)
    {
        if (!RelativeIniPaths.TryGetValue(browser, out string[]? candidates)) return [];
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        foreach (string relativePath in candidates)
        {
            string iniPath = Path.Combine(appData, relativePath);
            try
            {
                if (File.Exists(iniPath)) return ParseProfilesIni(iniPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return [];
    }

    internal static IReadOnlyList<BrowserProfileOption> ParseProfilesIni(string iniPath)
    {
        var profiles = new List<BrowserProfileOption>();
        string? section = null;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void AddCurrentProfile()
        {
            if (section is null || !section.StartsWith("Profile", StringComparison.OrdinalIgnoreCase) ||
                !values.TryGetValue("Path", out string? configuredPath)) return;
            string path = configuredPath.Replace('/', Path.DirectorySeparatorChar);
            if (!values.TryGetValue("IsRelative", out string? relative) || relative != "0")
                path = Path.Combine(Path.GetDirectoryName(iniPath)!, path);
            path = Path.GetFullPath(path);
            if (!Directory.Exists(path)) return;
            string name = values.GetValueOrDefault("Name", Path.GetFileName(path));
            profiles.Add(new BrowserProfileOption(name, path, values.GetValueOrDefault("Default") == "1"));
        }

        foreach (string rawLine in File.ReadLines(iniPath))
        {
            string line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                AddCurrentProfile();
                section = line[1..^1];
                values.Clear();
                continue;
            }
            int separator = line.IndexOf('=');
            if (separator > 0) values[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }
        AddCurrentProfile();
        return profiles.OrderByDescending(profile => profile.IsDefault).ThenBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
}
