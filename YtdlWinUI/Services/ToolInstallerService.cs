using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace YtdlWinUI.Services;

public sealed record ToolInstallProgress(string Tool, string Status, double? Percent);

public sealed class ToolInstallerService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task InstallMissingAsync(
        IReadOnlyCollection<string> missingTools,
        IProgress<ToolInstallProgress> progress,
        CancellationToken cancellationToken)
    {
        string architecture = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException("自動インストールはx64とARM64に対応しています。")
        };

        string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"ytdlgui-tools-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        Directory.CreateDirectory(ToolPaths.ManagedToolsDirectory);
        try
        {
            if (missingTools.Contains("yt-dlp"))
                await InstallYtDlpAsync(architecture, temporaryDirectory, progress, cancellationToken);
            if (missingTools.Contains("deno"))
                await InstallDenoAsync(architecture, temporaryDirectory, progress, cancellationToken);
            if (missingTools.Contains("ffmpeg") || missingTools.Contains("ffprobe"))
                await InstallFfmpegAsync(architecture, temporaryDirectory, progress, cancellationToken);
        }
        finally
        {
            try { Directory.Delete(temporaryDirectory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static async Task InstallYtDlpAsync(string architecture, string temporaryDirectory,
        IProgress<ToolInstallProgress> progress, CancellationToken cancellationToken)
    {
        string asset = architecture == "arm64" ? "yt-dlp_arm64.exe" : "yt-dlp.exe";
        string source = $"https://github.com/yt-dlp/yt-dlp/releases/latest/download/{asset}";
        string checksums = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS";
        string downloaded = Path.Combine(temporaryDirectory, asset);
        await DownloadAndVerifyAsync("yt-dlp", source, checksums, asset, downloaded, progress, cancellationToken);
        InstallFile(downloaded, Path.Combine(ToolPaths.ManagedToolsDirectory, "yt-dlp.exe"));
    }

    private static async Task InstallDenoAsync(string architecture, string temporaryDirectory,
        IProgress<ToolInstallProgress> progress, CancellationToken cancellationToken)
    {
        string target = architecture == "arm64" ? "aarch64" : "x86_64";
        string asset = $"deno-{target}-pc-windows-msvc.zip";
        string source = $"https://github.com/denoland/deno/releases/latest/download/{asset}";
        string checksums = $"{source}.sha256sum";
        string archive = Path.Combine(temporaryDirectory, asset);
        await DownloadAndVerifyAsync("Deno", source, checksums, asset, archive, progress, cancellationToken);
        string extracted = ExtractExecutable(archive, "deno.exe", temporaryDirectory);
        InstallFile(extracted, Path.Combine(ToolPaths.ManagedToolsDirectory, "deno.exe"));
    }

    private static async Task InstallFfmpegAsync(string architecture, string temporaryDirectory,
        IProgress<ToolInstallProgress> progress, CancellationToken cancellationToken)
    {
        string platform = architecture == "arm64" ? "winarm64" : "win64";
        string asset = $"ffmpeg-master-latest-{platform}-gpl.zip";
        string source = $"https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/{asset}";
        string checksums = "https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/checksums.sha256";
        string archive = Path.Combine(temporaryDirectory, asset);
        await DownloadAndVerifyAsync("FFmpeg", source, checksums, asset, archive, progress, cancellationToken);
        InstallFile(ExtractExecutable(archive, "ffmpeg.exe", temporaryDirectory),
            Path.Combine(ToolPaths.ManagedToolsDirectory, "ffmpeg.exe"));
        InstallFile(ExtractExecutable(archive, "ffprobe.exe", temporaryDirectory),
            Path.Combine(ToolPaths.ManagedToolsDirectory, "ffprobe.exe"));
    }

    internal static async Task DownloadAndVerifyAsync(string tool, string source, string checksums,
        string assetName, string destination, IProgress<ToolInstallProgress> progress,
        CancellationToken cancellationToken)
    {
        ValidateDownloadUri(source);
        ValidateDownloadUri(checksums);
        progress.Report(new ToolInstallProgress(tool, $"{tool} のチェックサムを取得しています…", null));
        string checksumDocument = await GetChecksumDocumentAsync(checksums, cancellationToken);
        string expectedHash = ParseSha256(checksumDocument, assetName);

        progress.Report(new ToolInstallProgress(tool, $"{tool} をダウンロードしています…", 0));
        using HttpResponseMessage response = await HttpClient.GetAsync(source,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        ValidateResolvedUri(response.RequestMessage?.RequestUri);
        long? totalBytes = response.Content.Headers.ContentLength;
        {
            await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 128 * 1024, FileOptions.Asynchronous);
            byte[] buffer = new byte[128 * 1024];
            long received = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;
                double? percent = totalBytes > 0 ? received * 100.0 / totalBytes.Value : null;
                progress.Report(new ToolInstallProgress(tool, $"{tool} をダウンロードしています…", percent));
            }
            await output.FlushAsync(cancellationToken);
        }

        progress.Report(new ToolInstallProgress(tool, $"{tool} を検証しています…", null));
        string actualHash = await ComputeSha256Async(destination, cancellationToken);
        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"{tool} のSHA-256チェックサムが一致しません。ファイルは配置されませんでした。");
    }

    internal static string ParseSha256(string document, string assetName)
    {
        string? labeledHash = null;
        bool labeledPathMatches = false;
        foreach (string line in document.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("Hash", StringComparison.OrdinalIgnoreCase))
            {
                int separator = trimmed.IndexOf(':');
                if (separator >= 0)
                {
                    string candidate = trimmed[(separator + 1)..].Trim();
                    if (candidate.Length == 64 && candidate.All(Uri.IsHexDigit)) labeledHash = candidate;
                }
            }
            else if (trimmed.StartsWith("Path", StringComparison.OrdinalIgnoreCase))
            {
                int separator = trimmed.IndexOf(':');
                labeledPathMatches = separator >= 0 &&
                    trimmed[(separator + 1)..].Trim().EndsWith(assetName, StringComparison.OrdinalIgnoreCase);
            }
            if (!trimmed.EndsWith(assetName, StringComparison.OrdinalIgnoreCase)) continue;
            string hash = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)[0];
            if (hash.Length == 64 && hash.All(Uri.IsHexDigit)) return hash;
        }
        if (labeledHash is not null && labeledPathMatches) return labeledHash;
        throw new InvalidDataException($"{assetName} のSHA-256チェックサムが見つかりません。");
    }

    private static async Task<string> GetChecksumDocumentAsync(string source, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await HttpClient.GetAsync(source, cancellationToken);
        response.EnsureSuccessStatusCode();
        ValidateResolvedUri(response.RequestMessage?.RequestUri);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static string ExtractExecutable(string archivePath, string fileName, string temporaryDirectory)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        ZipArchiveEntry entry = archive.Entries.FirstOrDefault(candidate =>
            string.Equals(Path.GetFileName(candidate.FullName), fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"アーカイブに {fileName} がありません。");
        string destination = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():N}-{fileName}");
        entry.ExtractToFile(destination);
        return destination;
    }

    private static void InstallFile(string source, string destination)
    {
        string pending = destination + ".new";
        try
        {
            File.Copy(source, pending, overwrite: true);
            File.Move(pending, destination, overwrite: true);
        }
        finally
        {
            try { File.Delete(pending); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void ValidateDownloadUri(string value)
    {
        var uri = new Uri(value);
        if (uri.Scheme != Uri.UriSchemeHttps || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("許可されていないダウンロード元です。");
    }

    private static void ValidateResolvedUri(Uri? uri)
    {
        bool trustedHost = uri is not null && uri.Scheme == Uri.UriSchemeHttps &&
            (string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase));
        if (!trustedHost) throw new InvalidOperationException("ダウンロードが許可されていないホストへ転送されました。");
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Ytdlgui-WinUI/1.0");
        return client;
    }
}
