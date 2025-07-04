using System.Windows;
using MaritimeERP.Desktop.ViewModels;
using ComponentEntity = MaritimeERP.Core.Entities.Component;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for ComponentEditDialog.xaml
    /// </summary>
    public partial class ComponentEditDialog : Window
    {
        private readonly ComponentEditViewModel _viewModel;

        public ComponentEditDialog(ComponentEditViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Subscribe to view model events
            _viewModel.ComponentSaved += OnComponentSaved;
            _viewModel.RequestClose += OnRequestClose;
        }

        public ComponentEntity? Component => _viewModel.Component;

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SaveComponentCommand.CanExecute(null))
            {
                _viewModel.SaveComponentCommand.Execute(null);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnComponentSaved(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnRequestClose(object? sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events to prevent memory leaks
            _viewModel.ComponentSaved -= OnComponentSaved;
            _viewModel.RequestClose -= OnRequestClose;
            base.OnClosed(e);
        }
    }
} 