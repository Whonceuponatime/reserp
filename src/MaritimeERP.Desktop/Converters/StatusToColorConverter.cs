using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MaritimeERP.Desktop.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "draft" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray - #9E9E9E
                    "submitted" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange - #FF9800
                    "under review" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue - #2196F3
                    "approved" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green - #4CAF50
                    "rejected" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red - #F44336
                    "completed" => new SolidColorBrush(Color.FromRgb(96, 125, 139)), // Blue Grey - #607D8B
                    _ => new SolidColorBrush(Color.FromRgb(224, 224, 224)) // Light Gray - Default
                };
            }
            
            return new SolidColorBrush(Color.FromRgb(224, 224, 224)); // Light Gray - Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 