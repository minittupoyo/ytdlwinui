using System.Diagnostics;
using System.Globalization;
using System.Text;
using YtdlWinUI.Models;

namespace YtdlWinUI.Services;

public sealed record DownloadProgress(double? Percent, string Status, string? LogLine = null);

public sealed class YtDlpService
{
    public string? FindExecutable(string name)
    {
        string fileName = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : $"{name}.exe";
        string managed = Path.Combine(ToolPaths.ManagedToolsDirectory, fileName);
        if (File.Exists(managed)) return managed;
        string bundled = Path.Combine(AppContext.BaseDirectory, "tools", fileName);
        if (File.Exists(bundled)) return bundled;
        foreach (string folder in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                string candidate = Path.Combine(folder.Trim(), fileName);
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    public IReadOnlyList<string> MissingDependencies()
    {
        string[] required = ["yt-dlp", "ffmpeg", "ffprobe", "deno"];
        return required.Where(tool => FindExecutable(tool) is null).ToArray();
    }

    public static IReadOnlyList<string> BuildArguments(AppSettings settings, string url)
    {
        var args = new List<string>
        {
            "--newline", "--color", "no_color", "-o",
            string.IsNullOrWhiteSpace(settings.FilenameTemplate) ? "%(title)s.%(ext)s" : settings.FilenameTemplate,
            "-P", Path.GetFullPath(settings.OutputPath), "--progress-template",
            "download:[DOWNLOADING]\t%(progress._percent)s\t%(info.title)s", "--encoding", "utf-8"
        };
        if (settings.Format is "mp4" or "mkv")
        {
            var heights = new Dictionary<string, string> { ["4k"] = "2160", ["2k"] = "1440", ["1080p"] = "1080", ["720p"] = "720" };
            string selector = "bestvideo+bestaudio/best";
            if (heights.TryGetValue(settings.Quality.ToLowerInvariant(), out string? height))
                selector = $"bestvideo[height<={height}]+bestaudio/best[height<={height}]";
            args.AddRange(["-f", selector, "--merge-output-format", settings.Format]);
        }
        else
        {
            args.AddRange(["-x", "--audio-format", settings.Format, "--audio-quality", settings.Quality is "自動" or "auto" ? "0" : settings.Quality.ToLowerInvariant()]);
        }
        args.Add(settings.PlaylistMode || settings.AlbumMode ? "--yes-playlist" : "--no-playlist");
        if (settings.EmbedThumbnail || settings.CropThumbnail || settings.AlbumMode) args.Add("--embed-thumbnail");
        if (settings.CropThumbnail || settings.AlbumMode)
            args.AddRange(["--convert-thumbnails", "jpg", "--postprocessor-args", "ThumbnailsConvertor+ffmpeg_o:-vf crop=\"'min(iw,ih)':'min(iw,ih)':'(iw-ow)/2':'(ih-oh)/2'\""]);
        if (settings.AlbumMode)
            args.AddRange(["--embed-metadata", "--parse-metadata", "%(album|playlist_title)s:%(meta_album)s", "--parse-metadata", "%(playlist_index)02d:%(meta_track)s", "--parse-metadata", "%(uploader|)s:%(meta_artist)s"]);
        if (!string.IsNullOrWhiteSpace(settings.CookieProfilePath))
            args.AddRange(["--cookies-from-browser", $"firefox:{settings.CookieProfilePath}"]);
        args.Add(url.Trim());
        return args;
    }

    public async Task<int> DownloadAsync(AppSettings settings, string url, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        string executable = FindExecutable("yt-dlp") ?? throw new FileNotFoundException("yt-dlp が見つかりません。");
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        // yt-dlp is a Python application. When stdout is redirected on Japanese
        // Windows, Python can otherwise choose the active ANSI code page while
        // StreamReader expects UTF-8, which corrupts non-ASCII video titles.
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment.TryGetValue("PATH", out string? existingPath);
        startInfo.Environment["PATH"] = string.Join(Path.PathSeparator,
            ToolPaths.ManagedToolsDirectory, existingPath ?? "");
        foreach (string argument in BuildArguments(settings, url)) startInfo.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        });
        Task stdout = ReadLinesAsync(process.StandardOutput, progress, cancellationToken);
        Task stderr = ReadLinesAsync(process.StandardError, progress, cancellationToken);
        await Task.WhenAll(stdout, stderr, process.WaitForExitAsync(cancellationToken));
        return process.ExitCode;
    }

    private static async Task ReadLinesAsync(StreamReader reader, IProgress<DownloadProgress> progress, CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("[DOWNLOADING]\t", StringComparison.Ordinal))
            {
                string[] parts = line.Split('\t', 3);
                if (parts.Length == 3 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double percent))
                    progress.Report(new DownloadProgress(percent, $"{parts[2]} をダウンロード中…"));
            }
            else if (!string.IsNullOrWhiteSpace(line)) progress.Report(new DownloadProgress(null, "処理しています…", line));
        }
    }
}
