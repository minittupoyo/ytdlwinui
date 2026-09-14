using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Windows.Storage.Pickers;
using YtdlWinUI.ViewModels;

namespace YtdlWinUI;

public sealed partial class MainPage : Page
{
    private bool _resetSettingsRequested;
    public MainPageViewModel ViewModel { get; } = new();
    public MainPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.InitializeAsync();
    }

    public static Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public static bool Not(bool value) => !value;

    private async void PickOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.Downloads };
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);
        Windows.Storage.StorageFolder? folder = await picker.PickSingleFolderAsync();
        if (folder is not null) ViewModel.SetOutputPath(folder.Path);
    }

    private void ViewSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        bool showSettings = sender.SelectedItem?.Text == "設定";
        SettingsView.Visibility = showSettings ? Visibility.Visible : Visibility.Collapsed;
        LogView.Visibility = showSettings ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void OpenAppSettings_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshDependencyStatus();
        _resetSettingsRequested = false;
        await AppSettingsDialog.ShowAsync();
        if (!_resetSettingsRequested) return;

        var confirmation = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "ダウンロード設定を初期化しますか？",
            Content = "保存先、形式、品質、ファイル名、Cookie、プレイリストなどの設定を既定値へ戻します。この操作は元に戻せません。外部ツールとログは削除しません。",
            PrimaryButtonText = "初期化",
            CloseButtonText = "キャンセル",
            DefaultButton = ContentDialogButton.Close
        };
        AutomationProperties.SetAutomationId(confirmation, "ConfirmResetSettingsDialog");
        if (await confirmation.ShowAsync() == ContentDialogResult.Primary)
            ViewModel.ResetSettings();
    }

    private void RequestResetSettings_Click(object sender, RoutedEventArgs e)
    {
        _resetSettingsRequested = true;
        AppSettingsDialog.Hide();
    }
}
