using System.Windows;
using MaritimeERP.Desktop.ViewModels;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MaritimeERP.Desktop.Views
{
    public partial class DocumentUploadDialog : Window
    {
        public DocumentUploadDialog(string filePath, IDocumentService documentService, IShipService shipService)
        {
            InitializeComponent();
            
            // Get dependencies from the service provider
            var serviceProvider = ((App)Application.Current).ServiceProvider;
            var authService = serviceProvider.GetRequiredService<IAuthenticationService>();
            var logger = serviceProvider.GetRequiredService<ILogger<DocumentUploadDialogViewModel>>();
            
            // Create and set the view model
            var viewModel = new DocumentUploadDialogViewModel(filePath, documentService, shipService, authService, logger);
            DataContext = viewModel;
            
            // Subscribe to dialog result changes
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DocumentUploadDialogViewModel.DialogResult))
                {
                    DialogResult = viewModel.DialogResult;
                }
            };
        }
    }
} 