using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using YtdlWinUI.ViewModels;

namespace YtdlWinUI;

public sealed partial class MainPage : Page
{
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
}
