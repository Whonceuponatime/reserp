using System.Windows;
using System.Windows.Controls;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class ShipsView : UserControl
    {
        public ShipsView()
        {
            InitializeComponent();
        }

        public ShipsView(ShipsViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
} 