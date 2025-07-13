using System.Windows;
using MaritimeERP.Core.Entities;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Desktop.ViewModels;

namespace MaritimeERP.Desktop.Views
{
    public partial class DocumentPreviewDialog : Window
    {
        public DocumentPreviewDialog(Document document, IDocumentService documentService)
        {
            InitializeComponent();
            
            var viewModel = new DocumentPreviewDialogViewModel(document, documentService);
            DataContext = viewModel;
            
            // Subscribe to close event
            viewModel.RequestClose += (sender, e) => Close();
        }
    }
} 