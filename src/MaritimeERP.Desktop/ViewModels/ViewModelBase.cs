using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MaritimeERP.Desktop.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void ShowError(string title, string message)
        {
            // TODO: Implement error dialog
        }

        protected void ShowSuccess(string title, string message)
        {
            // TODO: Implement success dialog
        }
    }
} 