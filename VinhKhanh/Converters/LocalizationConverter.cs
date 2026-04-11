using System.Globalization;
using Microsoft.Maui.Controls;
using VinhKhanh.Services;

namespace VinhKhanh.Converters;

public class LocalizationConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string key)
            return string.Empty;

        var language = value as string ?? LocalizationService.Instance.CurrentLanguage;
        return LocalizationService.Instance.GetString(key, language);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}