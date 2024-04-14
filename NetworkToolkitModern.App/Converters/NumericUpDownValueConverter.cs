using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NetworkToolkitModern.App.Converters;

public class NumericUpDownValueConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return default(int);
        return ((IConvertible)value).ToInt32(culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            if (targetType == typeof(int))
                return 0;
            throw new ArgumentNullException($"Unsupported type: {targetType.FullName}");
        }

        if (targetType == typeof(string)) return ((IConvertible)value).ToInt32(culture);

        if (targetType == typeof(int))
            return ((IConvertible)value).ToInt32(culture);
        throw new ArgumentNullException($"Unsupported type: {targetType.FullName}");
    }
}