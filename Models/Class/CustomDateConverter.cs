using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace TmCGPTD
{
    public class CustomDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                var dateFormatInfo = culture.DateTimeFormat;
                var separator = dateFormatInfo.DateSeparator;

                var shortPattern = dateFormatInfo.ShortDatePattern.Replace("yyyy", "yy");
                return date.ToString(shortPattern, culture);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }


}
