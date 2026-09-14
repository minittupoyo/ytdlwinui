namespace YtdlWinUI.Models;

public sealed record BrowserProfileOption(string Name, string Path, bool IsDefault)
{
    public string DisplayName => IsDefault ? $"{Name}（既定）" : Name;
}
