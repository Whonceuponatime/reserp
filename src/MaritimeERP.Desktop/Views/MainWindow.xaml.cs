using System.Windows;
using System.Windows.Controls;
using MaritimeERP.Desktop.ViewModels;
using MaritimeERP.Desktop.Services;
using System.ComponentModel;

namespace MaritimeERP.Desktop.Views
{
    public partial class MainWindow : Window
    {
        private readonly ViewLocator _viewLocator;
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel, ViewLocator viewLocator)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _viewLocator = viewLocator;
            DataContext = _viewModel;

            // Subscribe to view model changes
            _viewModel.PropertyChanged += ViewModelPropertyChanged;

            // Initialize the dashboard view immediately
            UpdateContent();
        }

        private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentViewModel))
            {
                UpdateContent();
            }
        }

        private void UpdateContent()
        {
            if (_viewModel.CurrentViewModel != null)
            {
                try
                {
                    var view = _viewLocator.GetViewForViewModel(_viewModel.CurrentViewModel);
                    MainContent.Content = view;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error loading view: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
} 