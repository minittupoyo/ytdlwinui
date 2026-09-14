namespace YtdlWinUI.Models;

public sealed class AppSettings
{
    public string OutputPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public string Format { get; set; } = "mp4";
    public string Quality { get; set; } = "自動";
    public string FilenameTemplate { get; set; } = "%(title)s.%(ext)s";
    public bool PlaylistMode { get; set; }
    public bool AlbumMode { get; set; }
    public bool EmbedThumbnail { get; set; }
    public bool CropThumbnail { get; set; }
    public string CookieBrowser { get; set; } = "使用しない";
    public string CookieProfileName { get; set; } = "";
    public string CookieProfilePath { get; set; } = "";
}
