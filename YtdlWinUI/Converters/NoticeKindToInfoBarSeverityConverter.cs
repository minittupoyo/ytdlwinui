using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using YtdlWinUI.Models;

namespace YtdlWinUI.Converters;

public sealed class NoticeKindToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value switch
    {
        NoticeKind.Success => InfoBarSeverity.Success,
        NoticeKind.Warning => InfoBarSeverity.Warning,
        NoticeKind.Error => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational
    };

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
