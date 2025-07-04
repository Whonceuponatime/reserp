using System.Windows;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    /// <summary>
    /// Interaction logic for ShipEditDialog.xaml
    /// </summary>
    public partial class ShipEditDialog : Window
    {
        public ShipEditDialog()
        {
            InitializeComponent();
            
            // Handle ViewModel dialog result changes
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is ShipEditViewModel viewModel)
            {
                // Subscribe to property changes to handle DialogResult
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(ShipEditViewModel.DialogResult))
                    {
                        if (viewModel.DialogResult.HasValue)
                        {
                            DialogResult = viewModel.DialogResult;
                            Close();
                        }
                    }
                };
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // This method is no longer used as we use Command binding
            // But kept for compatibility if needed
            if (DataContext is ShipEditViewModel viewModel)
            {
                if (viewModel.SaveCommand.CanExecute(null))
                {
                    viewModel.SaveCommand.Execute(null);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // This method is no longer used as we use Command binding
            // But kept for compatibility if needed
            DialogResult = false;
            Close();
        }
    }
} 