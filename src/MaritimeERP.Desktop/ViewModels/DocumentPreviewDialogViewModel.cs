using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MaritimeERP.Core.Entities;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Desktop.Commands;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.CompilerServices;

namespace MaritimeERP.Desktop.ViewModels
{
    public class DocumentPreviewDialogViewModel : INotifyPropertyChanged
    {
        private readonly Document _document;
        private readonly IDocumentService _documentService;
        private string _statusMessage = string.Empty;
        private BitmapImage? _imageSource;
        private string _textContent = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? RequestClose;

        public DocumentPreviewDialogViewModel(Document document, IDocumentService documentService)
        {
            _document = document;
            _documentService = documentService;

            // Initialize commands
            DownloadCommand = new RelayCommand(async () => await DownloadDocumentAsync());
            ApproveCommand = new RelayCommand(async () => await ApproveDocumentAsync());
            RejectCommand = new RelayCommand(async () => await RejectDocumentAsync());
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));

            // Initialize properties
            DocumentName = _document.Name;
            Category = _document.Category?.Name ?? "Unknown";
            ShipName = _document.Ship?.ShipName ?? "Not assigned";
            FileType = _document.FileExtension.ToUpper();
            FileSize = _document.FileSizeDisplay;
            UploadedBy = _document.UploaderDisplay;
            UploadDate = _document.UploadedAtDisplay;
            Status = _document.StatusDisplay;
            Description = _document.Description ?? string.Empty;
            Comments = _document.Comments ?? string.Empty;
            
            DocumentDetails = $"{Category} • {ShipName} • {FileSize}";
            
            // Load document preview
            _ = LoadDocumentPreviewAsync();
        }

        #region Properties

        public string DocumentName { get; }
        public string DocumentDetails { get; }
        public string Category { get; }
        public string ShipName { get; }
        public string FileType { get; }
        public string FileSize { get; }
        public string UploadedBy { get; }
        public string UploadDate { get; }
        public string Status { get; }
        public string Description { get; }
        public string Comments { get; }

        public bool HasDescription => !string.IsNullOrEmpty(Description);
        public bool HasComments => !string.IsNullOrEmpty(Comments);

        public BitmapImage? ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public string TextContent
        {
            get => _textContent;
            set
            {
                _textContent = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsImageFile => IsImageFileType(_document.FileExtension);
        public bool IsTextFile => IsTextFileType(_document.FileExtension);
        public bool IsOtherFile => !IsImageFile && !IsTextFile;

        public bool CanApprove => !_document.IsApproved && IsAdmin();
        public bool CanReject => !_document.IsApproved && IsAdmin();

        #endregion

        #region Commands

        public ICommand DownloadCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand CloseCommand { get; }

        #endregion

        #region Methods

        private async Task LoadDocumentPreviewAsync()
        {
            try
            {
                StatusMessage = "Loading document preview...";

                var content = await _documentService.GetDocumentContentAsync(_document.Id);
                
                if (IsImageFile)
                {
                    await LoadImagePreviewAsync(content);
                }
                else if (IsTextFile)
                {
                    LoadTextPreview(content);
                }
                
                StatusMessage = $"Document loaded • {FileSize}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading document: {ex.Message}";
            }
        }

        private async Task LoadImagePreviewAsync(byte[] content)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(content);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    ImageSource = bitmap;
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading image: {ex.Message}";
            }
        }

        private void LoadTextPreview(byte[] content)
        {
            try
            {
                // Try UTF-8 first, then fall back to other encodings
                string text;
                try
                {
                    text = Encoding.UTF8.GetString(content);
                }
                catch
                {
                    text = Encoding.Default.GetString(content);
                }

                // Limit text preview to first 10,000 characters for performance
                if (text.Length > 10000)
                {
                    text = text.Substring(0, 10000) + "\n\n... (Content truncated for preview)";
                }

                TextContent = text;
            }
            catch (Exception ex)
            {
                TextContent = $"Error loading text content: {ex.Message}";
            }
        }

        private async Task DownloadDocumentAsync()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Document",
                    FileName = _document.FileName,
                    Filter = $"*{_document.FileExtension}|*{_document.FileExtension}|All Files|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    StatusMessage = "Downloading document...";
                    
                    var content = await _documentService.GetDocumentContentAsync(_document.Id);
                    await File.WriteAllBytesAsync(saveDialog.FileName, content);
                    
                    StatusMessage = "Document downloaded successfully";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error downloading document: {ex.Message}";
                MessageBox.Show($"Error downloading document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApproveDocumentAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to approve the document '{_document.Name}'?",
                    "Approve Document",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StatusMessage = "Approving document...";
                    
                    var currentUser = GetCurrentUser();
                    if (currentUser != null)
                    {
                        await _documentService.ApproveDocumentAsync(_document.Id, currentUser.Id);
                        StatusMessage = "Document approved successfully";
                        
                        MessageBox.Show("Document approved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        RequestClose?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving document: {ex.Message}";
                MessageBox.Show($"Error approving document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RejectDocumentAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to reject the document '{_document.Name}'?",
                    "Reject Document",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    StatusMessage = "Rejecting document...";
                    
                    var currentUser = GetCurrentUser();
                    if (currentUser != null)
                    {
                        await _documentService.RejectDocumentAsync(_document.Id, currentUser.Id, "Rejected from document preview");
                        StatusMessage = "Document rejected successfully";
                        
                        MessageBox.Show("Document rejected successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        RequestClose?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error rejecting document: {ex.Message}";
                MessageBox.Show($"Error rejecting document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsImageFileType(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif" };
            return Array.Exists(imageExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsTextFileType(string extension)
        {
            var textExtensions = new[] { ".txt", ".csv", ".log", ".xml", ".json", ".html", ".css", ".js", ".sql", ".md" };
            return Array.Exists(textExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAdmin()
        {
            // Get current user from authentication service
            var authService = Application.Current.Services.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
            return authService?.CurrentUser?.Role?.Name == "Administrator";
        }

        private static User? GetCurrentUser()
        {
            var authService = Application.Current.Services.GetService(typeof(IAuthenticationService)) as IAuthenticationService;
            return authService?.CurrentUser;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
} 