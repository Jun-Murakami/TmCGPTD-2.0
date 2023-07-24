using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TmCGPTD
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            throw new InvalidOperationException("The target must be a boolean");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            throw new InvalidOperationException("The target must be a boolean");
        }
    }
}
