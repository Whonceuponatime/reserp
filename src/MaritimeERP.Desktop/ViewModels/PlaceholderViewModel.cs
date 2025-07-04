using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MaritimeERP.Desktop.ViewModels
{
    public class PlaceholderViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Icon { get; set; } = "Information";

        public PlaceholderViewModel(string title, string message, string icon = "Information")
        {
            Title = title;
            Message = message;
            Icon = icon;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
} 