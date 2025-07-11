using System.Windows;
using System.Windows.Controls;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop.Views
{
    public partial class DocumentUploadDialog : Window
    {
        public DocumentUploadDialog(string filePath, IDocumentService documentService, IShipService shipService)
        {
            // For now, create a simple dialog with basic functionality
            Title = "Upload Document";
            Width = 500;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            
            // Create a simple content for now
            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = "Document Upload", 
                FontSize = 20, 
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });
            
            stackPanel.Children.Add(new TextBlock 
            { 
                Text = $"Selected file: {System.IO.Path.GetFileName(filePath)}", 
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            var uploadButton = new Button 
            { 
                Content = "Upload", 
                Width = 100, 
                Height = 35,
                Margin = new Thickness(0, 20, 0, 0)
            };
            
            uploadButton.Click += (s, e) => 
            {
                DialogResult = true;
                Close();
            };
            
            stackPanel.Children.Add(uploadButton);
            
            Content = stackPanel;
        }
    }
} 