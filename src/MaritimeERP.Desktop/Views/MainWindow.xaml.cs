using System.Windows;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
} 