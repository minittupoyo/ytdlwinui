using YtdlWinUI.Services;
using YtdlWinUI.Models;

namespace YtdlWinUI.Tests;

public sealed class YtDlpServiceTests
{
    [Fact]
    public void BuildArguments_RequestsJsonEscapedProgressTitle()
    {
        var settings = new AppSettings { OutputPath = Path.GetTempPath() };

        IReadOnlyList<string> arguments = YtDlpService.BuildArguments(settings, "https://example.com/video");

        int templateIndex = Array.IndexOf(arguments.ToArray(), "--progress-template");
        Assert.True(templateIndex >= 0);
        Assert.Equal("download:[DOWNLOADING]\t%(progress._percent)s\t%(info.title)j",
            arguments[templateIndex + 1]);
    }

    [Fact]
    public void ParseOutputLine_DecodesJsonEscapedJapaneseTitle()
    {
        const string line = "[DOWNLOADING]\t42.5\t\"\\u65e5\\u672c\\u8a9e\\u30bf\\u30a4\\u30c8\\u30eb\"";

        DownloadProgress? progress = YtDlpService.ParseOutputLine(line);

        Assert.NotNull(progress);
        Assert.Equal(42.5, progress.Percent);
        Assert.Equal("日本語タイトル をダウンロード中…", progress.Status);
    }

    [Fact]
    public void ParseOutputLine_PreservesTitleWhenJsonIsMalformed()
    {
        const string line = "[DOWNLOADING]\t10\t日本語タイトル";

        DownloadProgress? progress = YtDlpService.ParseOutputLine(line);

        Assert.NotNull(progress);
        Assert.Equal("日本語タイトル をダウンロード中…", progress.Status);
    }
}
