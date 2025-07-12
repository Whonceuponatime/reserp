using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Desktop.Commands;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class DocumentUploadDialogViewModel : ViewModelBase
    {
        private readonly IDocumentService _documentService;
        private readonly IShipService _shipService;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<DocumentUploadDialogViewModel> _logger;
        private readonly string _filePath;

        public DocumentUploadDialogViewModel(
            string filePath,
            IDocumentService documentService,
            IShipService shipService,
            IAuthenticationService authService,
            ILogger<DocumentUploadDialogViewModel> logger)
        {
            _filePath = filePath;
            _documentService = documentService;
            _shipService = shipService;
            _authService = authService;
            _logger = logger;

            DocumentCategories = new ObservableCollection<DocumentCategory>();
            Ships = new ObservableCollection<Ship>();

            // Commands
            UploadCommand = new RelayCommand(async () => await UploadDocumentAsync(), () => CanUpload);
            CancelCommand = new RelayCommand(() => DialogResult = false);
            ValidateFileCommand = new RelayCommand(async () => await ValidateFileAsync());

            // Initialize with file info
            InitializeFileInfo();

            // Load reference data
            Task.Run(async () => await LoadReferenceDataAsync());
        }

        #region Properties

        public ObservableCollection<DocumentCategory> DocumentCategories { get; }
        public ObservableCollection<Ship> Ships { get; }

        private string _documentName = string.Empty;
        public string DocumentName
        {
            get => _documentName;
            set
            {
                _documentName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUpload));
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        private DocumentCategory? _selectedCategory;
        public DocumentCategory? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUpload));
                Task.Run(async () => await ValidateFileAsync());
            }
        }

        private Ship? _selectedShip;
        public Ship? SelectedShip
        {
            get => _selectedShip;
            set
            {
                _selectedShip = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUpload));
            }
        }

        private string _fileName = string.Empty;
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged();
            }
        }

        private string _fileSize = string.Empty;
        public string FileSize
        {
            get => _fileSize;
            set
            {
                _fileSize = value;
                OnPropertyChanged();
            }
        }

        private string _fileType = string.Empty;
        public string FileType
        {
            get => _fileType;
            set
            {
                _fileType = value;
                OnPropertyChanged();
            }
        }

        private string _validationMessage = string.Empty;
        public string ValidationMessage
        {
            get => _validationMessage;
            set
            {
                _validationMessage = value;
                OnPropertyChanged();
            }
        }

        private bool _isValid = true;
        public bool IsValid
        {
            get => _isValid;
            set
            {
                _isValid = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUpload));
            }
        }

        private bool _isUploading;
        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                _isUploading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanUpload));
            }
        }

        private bool _dialogResult;
        public bool DialogResult
        {
            get => _dialogResult;
            set
            {
                _dialogResult = value;
                OnPropertyChanged();
            }
        }

        public bool CanUpload => !IsUploading && 
                                 !string.IsNullOrWhiteSpace(DocumentName) && 
                                 SelectedCategory != null && 
                                 SelectedShip != null && 
                                 SelectedShip.Id > 0 && 
                                 IsValid;

        #endregion

        #region Commands

        public ICommand UploadCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ValidateFileCommand { get; }

        #endregion

        #region Methods

        private void InitializeFileInfo()
        {
            try
            {
                var fileInfo = new FileInfo(_filePath);
                FileName = fileInfo.Name;
                FileSize = FormatFileSize(fileInfo.Length);
                FileType = fileInfo.Extension.ToUpperInvariant();
                
                // Use filename as default document name (without extension)
                DocumentName = Path.GetFileNameWithoutExtension(fileInfo.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing file info for {FilePath}", _filePath);
                ValidationMessage = "Error reading file information";
                IsValid = false;
            }
        }

        private async Task LoadReferenceDataAsync()
        {
            try
            {
                // Load document categories
                var categories = await _documentService.GetAllCategoriesAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DocumentCategories.Clear();
                    foreach (var category in categories.OrderBy(c => c.DisplayOrder))
                    {
                        DocumentCategories.Add(category);
                    }
                });

                // Load ships
                var ships = await _shipService.GetAllShipsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Ships.Clear();
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }
                    
                    // Don't select any ship by default - user must choose
                    SelectedShip = null;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reference data");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ValidationMessage = "Error loading reference data";
                    IsValid = false;
                });
            }
        }

        private async Task ValidateFileAsync()
        {
            try
            {
                if (SelectedCategory == null)
                {
                    ValidationMessage = "Please select a document category";
                    IsValid = false;
                    return;
                }

                using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentType(Path.GetExtension(_filePath));
                
                var isValid = await _documentService.ValidateFileAsync(fileStream, FileName, contentType, SelectedCategory.Id);
                
                if (isValid)
                {
                    ValidationMessage = "File validation successful";
                    IsValid = true;
                }
                else
                {
                    ValidationMessage = "File validation failed. Please check file type and size limits.";
                    IsValid = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file");
                ValidationMessage = $"Validation error: {ex.Message}";
                IsValid = false;
            }
        }

        private async Task UploadDocumentAsync()
        {
            try
            {
                IsUploading = true;
                ValidationMessage = "Uploading document...";

                // Validate ship selection
                if (SelectedShip == null || SelectedShip.Id <= 0)
                {
                    ValidationMessage = "Please select a ship for this document";
                    MessageBox.Show("Ship assignment is mandatory. Please select a ship for this document.", 
                        "Ship Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var document = new Document
                {
                    Name = DocumentName,
                    Description = Description,
                    CategoryId = SelectedCategory!.Id,
                    ShipId = SelectedShip.Id,
                    UploadedByUserId = _authService.CurrentUser!.Id,
                    ContentType = GetContentType(Path.GetExtension(_filePath)),
                    IsActive = true,
                    IsApproved = false,
                    StatusId = 1 // Pending Approval
                };

                using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
                
                await _documentService.CreateDocumentAsync(document, fileStream, FileName);

                ValidationMessage = "Document uploaded successfully";
                DialogResult = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                ValidationMessage = $"Upload error: {ex.Message}";
                MessageBox.Show($"Error uploading document: {ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsUploading = false;
            }
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".csv" => "text/csv",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".tiff" => "image/tiff",
                ".dwg" => "application/dwg",
                ".dxf" => "application/dxf",
                _ => "application/octet-stream"
            };
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024 * 1024):F1} MB";
            return $"{bytes / (1024 * 1024 * 1024):F1} GB";
        }

        #endregion
    }
} 