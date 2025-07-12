using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MaritimeERP.Desktop.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private bool _isDataLoaded = false;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsDataLoaded
        {
            get => _isDataLoaded;
            protected set => SetProperty(ref _isDataLoaded, value);
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

        /// <summary>
        /// Load data lazily when first accessed
        /// </summary>
        public virtual async Task EnsureDataLoadedAsync()
        {
            if (!IsDataLoaded && !IsLoading)
            {
                await LoadDataAsync();
            }
        }

        /// <summary>
        /// Override this method to implement data loading
        /// </summary>
        protected virtual async Task LoadDataAsync()
        {
            // Default implementation - override in derived classes
            await Task.CompletedTask;
            IsDataLoaded = true;
        }

        /// <summary>
        /// Refresh data by clearing the loaded flag and reloading
        /// </summary>
        public virtual async Task RefreshDataAsync()
        {
            IsDataLoaded = false;
            await LoadDataAsync();
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