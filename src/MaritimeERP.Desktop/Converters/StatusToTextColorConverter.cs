using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MaritimeERP.Desktop.Converters
{
    public class StatusToTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "draft" => new SolidColorBrush(Colors.White), // White text on gray background
                    "submitted" => new SolidColorBrush(Colors.White), // White text on orange background
                    "under review" => new SolidColorBrush(Colors.White), // White text on blue background
                    "approved" => new SolidColorBrush(Colors.White), // White text on green background
                    "rejected" => new SolidColorBrush(Colors.White), // White text on red background
                    "completed" => new SolidColorBrush(Colors.White), // White text on blue grey background
                    _ => new SolidColorBrush(Colors.Black) // Black text on light background - Default
                };
            }
            
            return new SolidColorBrush(Colors.Black); // Black text - Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 