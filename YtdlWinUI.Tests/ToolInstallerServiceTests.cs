using System.Security.Cryptography;
using YtdlWinUI.Services;

namespace YtdlWinUI.Tests;

public sealed class ToolInstallerServiceTests
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    public static TheoryData<string, string> ChecksumSources => new()
    {
        {
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS",
            "yt-dlp.exe"
        },
        {
            "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip.sha256sum",
            "deno-x86_64-pc-windows-msvc.zip"
        },
        {
            "https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/checksums.sha256",
            "ffmpeg-master-latest-win64-gpl.zip"
        }
    };

    [Fact]
    public void ParseSha256_ParsesDenoPowerShellFormat()
    {
        const string expected = "15E5300B0BA3C3695A7621D90160A746EC9E710228CEE639AFA9D580F6E3CD11";
        const string document = """

            Algorithm : SHA256
            Hash      : 15E5300B0BA3C3695A7621D90160A746EC9E710228CEE639AFA9D580F6E3CD11
            Path      : C:\a\deno\deno\target\release\deno-x86_64-pc-windows-msvc.zip

            """;

        Assert.Equal(expected, ToolInstallerService.ParseSha256(
            document, "deno-x86_64-pc-windows-msvc.zip"));
    }

    [Fact]
    public void ParseSha256_RejectsHashForDifferentDenoAsset()
    {
        const string document = """
            Algorithm : SHA256
            Hash      : 15E5300B0BA3C3695A7621D90160A746EC9E710228CEE639AFA9D580F6E3CD11
            Path      : C:\a\deno\deno\target\release\deno-aarch64-pc-windows-msvc.zip
            """;

        Assert.Throws<InvalidDataException>(() => ToolInstallerService.ParseSha256(
            document, "deno-x86_64-pc-windows-msvc.zip"));
    }

    [Theory]
    [MemberData(nameof(ChecksumSources))]
    [Trait("Category", "Integration")]
    public async Task PublishedChecksum_CanBeDownloadedAndParsed(string checksumUrl, string assetName)
    {
        string document = await HttpClient.GetStringAsync(checksumUrl);

        string hash = ToolInstallerService.ParseSha256(document, assetName);

        Assert.Equal(64, hash.Length);
        Assert.All(hash, character => Assert.True(Uri.IsHexDigit(character)));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DenoAsset_DownloadMatchesChecksum_AndFileIsUnlockedAfterVerification()
    {
        const string asset = "deno-x86_64-pc-windows-msvc.zip";
        const string source = "https://github.com/denoland/deno/releases/latest/download/" + asset;
        const string checksums = source + ".sha256sum";
        string directory = Path.Combine(Path.GetTempPath(), $"ytdlgui-test-{Guid.NewGuid():N}");
        string destination = Path.Combine(directory, asset);
        Directory.CreateDirectory(directory);

        try
        {
            await ToolInstallerService.DownloadAndVerifyAsync("Deno", source, checksums, asset,
                destination, new Progress<ToolInstallProgress>(), CancellationToken.None);

            Assert.True(new FileInfo(destination).Length > 0);
            await using FileStream exclusive = new(destination, FileMode.Open, FileAccess.Read, FileShare.None);
            byte[] hash = await SHA256.HashDataAsync(exclusive, CancellationToken.None);
            Assert.Equal(32, hash.Length);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
