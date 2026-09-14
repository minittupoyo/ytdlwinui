using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtdlWinUI.Models;
using YtdlWinUI.Services;

namespace YtdlWinUI.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = new();
    private readonly YtDlpService _ytDlpService = new();
    private readonly BrowserProfileService _browserProfileService = new();
    private readonly ToolInstallerService _toolInstallerService = new();
    private CancellationTokenSource? _downloadCancellation;
    private CancellationTokenSource? _toolInstallCancellation;
    private CancellationTokenSource? _noticeCancellation;
    private bool _isInitializing = true;
    private IReadOnlyList<string> _missingTools = [];

    public IReadOnlyList<string> Formats { get; } = ["mp4", "mkv", "mp3", "aac", "flac"];
    public IReadOnlyList<string> CookieBrowsers { get; } = ["使用しない", "Firefox", "Floorp", "Zen"];
    public ObservableCollection<string> QualityOptions { get; } = ["自動", "4K", "2K", "1080p", "720p"];
    public ObservableCollection<BrowserProfileOption> CookieProfiles { get; } = [];

    [ObservableProperty] public partial string Url { get; set; } = "";
    [ObservableProperty] public partial string OutputPath { get; set; } = "";
    [ObservableProperty] public partial string SelectedFormat { get; set; } = "mp4";
    [ObservableProperty] public partial string SelectedQuality { get; set; } = "自動";
    [ObservableProperty] public partial string FilenameTemplate { get; set; } = "%(title)s.%(ext)s";
    [ObservableProperty] public partial string SelectedCookieBrowser { get; set; } = "使用しない";
    [ObservableProperty] public partial BrowserProfileOption? SelectedCookieProfile { get; set; }
    [ObservableProperty] public partial bool PlaylistMode { get; set; }
    [ObservableProperty] public partial bool AlbumMode { get; set; }
    [ObservableProperty] public partial bool EmbedThumbnail { get; set; }
    [ObservableProperty] public partial bool CropThumbnail { get; set; }
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial bool IsProgressIndeterminate { get; set; }
    [ObservableProperty] public partial double ProgressValue { get; set; }
    [ObservableProperty] public partial string Status { get; set; } = "準備完了";
    [ObservableProperty] public partial string LogText { get; set; } = "";
    [ObservableProperty] public partial string DependencyStatus { get; set; } = "確認しています…";
    [ObservableProperty] public partial string NoticeTitle { get; set; } = "";
    [ObservableProperty] public partial string NoticeMessage { get; set; } = "";
    [ObservableProperty] public partial bool IsNoticeOpen { get; set; }
    [ObservableProperty] public partial NoticeKind NoticeSeverity { get; set; }

    public bool IsAudioFormat => SelectedFormat is "mp3" or "aac" or "flac";
    public bool HasMissingTools => _missingTools.Count > 0;
    public bool HasCookieProfiles => CookieProfiles.Count > 0;
    public string CookieProfileHint => SelectedCookieBrowser == "使用しない"
        ? "ブラウザーのCookieを使用しません。"
        : HasCookieProfiles ? "選択したプロファイルのCookieをyt-dlpで使用します。" : "プロファイルが見つかりません。";

    public async Task InitializeAsync()
    {
        AppSettings settings = await _settingsService.LoadAsync();
        OutputPath = settings.OutputPath;
        SelectedFormat = Formats.Contains(settings.Format) ? settings.Format : "mp4";
        RefreshQualities(settings.Quality switch { "auto" => "自動", "4k" => "4K", "2k" => "2K", _ => settings.Quality });
        FilenameTemplate = settings.FilenameTemplate;
        PlaylistMode = settings.PlaylistMode;
        AlbumMode = IsAudioFormat && settings.AlbumMode;
        EmbedThumbnail = settings.EmbedThumbnail;
        CropThumbnail = settings.CropThumbnail;
        SelectedCookieBrowser = CookieBrowsers.Contains(settings.CookieBrowser) ? settings.CookieBrowser : "使用しない";
        RefreshCookieProfiles(settings.CookieProfilePath);
        _isInitializing = false;
        RefreshDependencies();
        if (HasMissingTools)
            ShowNotice("必要なツールが見つかりません", $"{string.Join("、", _missingTools)}。設定から自動インストールできます。", NoticeKind.Warning);
    }

    public void SetOutputPath(string path) { OutputPath = path; _ = SaveAsync(); }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(Url) || string.IsNullOrWhiteSpace(OutputPath))
        {
            ShowNotice("入力を確認してください", "URL と保存先を指定してください。", NoticeKind.Warning);
            return;
        }
        try { Directory.CreateDirectory(OutputPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowNotice("保存先を使用できません", ex.Message, NoticeKind.Error);
            return;
        }
        IsBusy = true;
        IsProgressIndeterminate = true;
        ProgressValue = 0;
        Status = "ダウンロードの準備をしています…";
        DownloadCommand.NotifyCanExecuteChanged();
        _downloadCancellation = new CancellationTokenSource();
        await SaveAsync();
        try
        {
            var progress = new Progress<DownloadProgress>(update =>
            {
                Status = update.Status;
                IsProgressIndeterminate = update.Percent is null;
                if (update.Percent is not null) ProgressValue = update.Percent.Value;
                if (update.LogLine is not null)
                {
                    AppendLog(update.LogLine);
                }
            });
            int exitCode = await _ytDlpService.DownloadAsync(ToSettings(), Url, progress, _downloadCancellation.Token);
            if (exitCode != 0) throw new InvalidOperationException($"yt-dlp が終了コード {exitCode} を返しました。");
            ProgressValue = 100;
            IsProgressIndeterminate = false;
            Status = "正常にダウンロードできました";
            ShowNotice("完了", Status, NoticeKind.Success, TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException)
        {
            Status = "キャンセルしました";
            ShowNotice("キャンセル", Status, NoticeKind.Informational, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            Status = "処理中にエラーが発生しました";
            AppendLog(ex.Message);
            ShowNotice("ダウンロードできませんでした", ex.Message, NoticeKind.Error);
        }
        finally
        {
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
            IsBusy = false;
            IsProgressIndeterminate = false;
            DownloadCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanDownload() => !IsBusy;
    [RelayCommand] private void Cancel()
    {
        _downloadCancellation?.Cancel();
        _toolInstallCancellation?.Cancel();
    }
    [RelayCommand] private void ClearLog() => LogText = "";

    [RelayCommand(CanExecute = nameof(CanInstallTools))]
    private async Task InstallToolsAsync()
    {
        if (!HasMissingTools) return;
        IsBusy = true;
        IsProgressIndeterminate = true;
        ProgressValue = 0;
        _toolInstallCancellation = new CancellationTokenSource();
        DownloadCommand.NotifyCanExecuteChanged();
        InstallToolsCommand.NotifyCanExecuteChanged();
        try
        {
            var progress = new Progress<ToolInstallProgress>(update =>
            {
                Status = update.Status;
                IsProgressIndeterminate = update.Percent is null;
                if (update.Percent is not null) ProgressValue = update.Percent.Value;
            });
            await _toolInstallerService.InstallMissingAsync(_missingTools, progress, _toolInstallCancellation.Token);
            RefreshDependencies();
            if (HasMissingTools)
                throw new InvalidOperationException($"未導入のツールがあります: {string.Join("、", _missingTools)}");
            Status = "必要なツールをインストールしました";
            ProgressValue = 100;
            ShowNotice("インストール完了", Status, NoticeKind.Success, TimeSpan.FromSeconds(5));
        }
        catch (OperationCanceledException)
        {
            Status = "ツールのインストールをキャンセルしました";
            ShowNotice("キャンセル", Status, NoticeKind.Informational, TimeSpan.FromSeconds(3));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidDataException or
                                   InvalidOperationException or PlatformNotSupportedException or UnauthorizedAccessException)
        {
            Status = "ツールをインストールできませんでした";
            AppendLog(ex.Message);
            ShowNotice("インストールエラー", ex.Message, NoticeKind.Error);
        }
        finally
        {
            _toolInstallCancellation?.Dispose();
            _toolInstallCancellation = null;
            IsBusy = false;
            IsProgressIndeterminate = false;
            DownloadCommand.NotifyCanExecuteChanged();
            InstallToolsCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanInstallTools() => HasMissingTools && !IsBusy;

    private void RefreshDependencies()
    {
        _missingTools = _ytDlpService.MissingDependencies();
        DependencyStatus = HasMissingTools
            ? $"不足: {string.Join("、", _missingTools)}"
            : $"すべて利用可能（{ToolPaths.ManagedToolsDirectory} または PATH）";
        OnPropertyChanged(nameof(HasMissingTools));
        InstallToolsCommand.NotifyCanExecuteChanged();
    }

    private void AppendLog(string line)
    {
        string[] lines = string.IsNullOrEmpty(LogText)
            ? [line]
            : string.Concat(LogText, Environment.NewLine, line)
                .Split(["\r\n", "\n"], StringSplitOptions.None);
        LogText = string.Join(Environment.NewLine, lines.TakeLast(500));
    }

    partial void OnSelectedFormatChanged(string value)
    {
        RefreshQualities("自動");
        OnPropertyChanged(nameof(IsAudioFormat));
        if (!IsAudioFormat) AlbumMode = false;
        UpdateFilenameTemplate();
        QueueSave();
    }
    partial void OnSelectedQualityChanged(string value) => QueueSave();
    partial void OnOutputPathChanged(string value) => QueueSave();
    partial void OnFilenameTemplateChanged(string value) => QueueSave();
    partial void OnEmbedThumbnailChanged(bool value) => QueueSave();
    partial void OnCropThumbnailChanged(bool value) => QueueSave();
    partial void OnSelectedCookieBrowserChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        RefreshCookieProfiles();
        QueueSave();
    }
    partial void OnSelectedCookieProfileChanged(BrowserProfileOption? value) => QueueSave();
    partial void OnPlaylistModeChanged(bool value) { UpdateFilenameTemplate(); QueueSave(); }
    partial void OnAlbumModeChanged(bool value) { if (value) PlaylistMode = true; UpdateFilenameTemplate(); QueueSave(); }

    private void RefreshQualities(string preferred)
    {
        QualityOptions.Clear();
        string[] values = IsAudioFormat ? SelectedFormat == "flac" ? ["自動"] : ["自動", "320k", "256k", "192k", "128k"] : ["自動", "4K", "2K", "1080p", "720p"];
        foreach (string value in values) QualityOptions.Add(value);
        SelectedQuality = values.Contains(preferred) ? preferred : "自動";
        // The current value can already be "自動" while ComboBox has not selected it yet.
        // Notify explicitly after rebuilding the collection so SelectedItem is reapplied.
        OnPropertyChanged(nameof(SelectedQuality));
    }

    private void RefreshCookieProfiles(string? preferredPath = null)
    {
        CookieProfiles.Clear();
        foreach (BrowserProfileOption profile in _browserProfileService.FindProfiles(SelectedCookieBrowser))
            CookieProfiles.Add(profile);
        SelectedCookieProfile = CookieProfiles.FirstOrDefault(profile =>
            !string.IsNullOrWhiteSpace(preferredPath) && string.Equals(profile.Path, preferredPath, StringComparison.OrdinalIgnoreCase))
            ?? CookieProfiles.FirstOrDefault();
        OnPropertyChanged(nameof(HasCookieProfiles));
        OnPropertyChanged(nameof(CookieProfileHint));
        OnPropertyChanged(nameof(SelectedCookieProfile));
    }

    private void UpdateFilenameTemplate() => FilenameTemplate = AlbumMode
        ? "%(album|playlist_title)s/%(playlist_index)02d - %(title)s.%(ext)s"
        : PlaylistMode ? "%(playlist_title)s/%(playlist_index)02d - %(title)s.%(ext)s" : "%(title)s.%(ext)s";

    private AppSettings ToSettings() => new()
    {
        OutputPath = OutputPath, Format = SelectedFormat, Quality = SelectedQuality switch { "自動" => "auto", "4K" => "4k", "2K" => "2k", _ => SelectedQuality },
        FilenameTemplate = FilenameTemplate, PlaylistMode = PlaylistMode, AlbumMode = AlbumMode,
        EmbedThumbnail = EmbedThumbnail, CropThumbnail = CropThumbnail,
        CookieBrowser = SelectedCookieBrowser,
        CookieProfileName = SelectedCookieProfile?.Name ?? "",
        CookieProfilePath = SelectedCookieProfile?.Path ?? ""
    };
    private void QueueSave() { if (!_isInitializing) _ = SaveAsync(); }
    private Task SaveAsync() => _settingsService.SaveAsync(ToSettings());
    private void ShowNotice(string title, string message, NoticeKind severity, TimeSpan? autoCloseAfter = null)
    {
        _noticeCancellation?.Cancel();
        _noticeCancellation?.Dispose();
        _noticeCancellation = null;

        NoticeTitle = title;
        NoticeMessage = message;
        NoticeSeverity = severity;
        IsNoticeOpen = true;

        if (autoCloseAfter is not null)
        {
            _noticeCancellation = new CancellationTokenSource();
            _ = AutoCloseNoticeAsync(autoCloseAfter.Value, _noticeCancellation.Token);
        }
    }

    private async Task AutoCloseNoticeAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            IsNoticeOpen = false;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
