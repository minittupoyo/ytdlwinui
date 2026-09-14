namespace YtdlWinUI.Services;

public static class ToolPaths
{
    public static string ManagedToolsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ytdlgui", "bin");
}
