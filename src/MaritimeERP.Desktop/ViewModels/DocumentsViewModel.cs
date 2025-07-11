using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.IO;
using MaritimeERP.Core.Entities;
using MaritimeERP.Services.Interfaces;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Desktop.Views;
using Microsoft.Extensions.Logging;
using System.Windows;
using Microsoft.Win32;

namespace MaritimeERP.Desktop.ViewModels
{
    public class DocumentsViewModel : ViewModelBase
    {
        private readonly IDocumentService _documentService;
        private readonly IShipService _shipService;
        private readonly IAuthenticationService _authService;
        private readonly ILogger<DocumentsViewModel> _logger;

        public DocumentsViewModel(
            IDocumentService documentService,
            IShipService shipService,
            IAuthenticationService authService,
            ILogger<DocumentsViewModel> logger)
        {
            _documentService = documentService;
            _shipService = shipService;
            _authService = authService;
            _logger = logger;

            Documents = new ObservableCollection<Document>();
            DocumentCategories = new ObservableCollection<DocumentCategory>();
            Ships = new ObservableCollection<Ship>();
            FilteredDocuments = new ObservableCollection<Document>();

            // Commands
            LoadDocumentsCommand = new RelayCommand(async () => await LoadDocumentsAsync());
            UploadDocumentCommand = new RelayCommand(async () => await UploadDocumentAsync());
            ApproveDocumentCommand = new RelayCommand<Document>(async (doc) => await ApproveDocumentAsync(doc));
            RejectDocumentCommand = new RelayCommand<Document>(async (doc) => await RejectDocumentAsync(doc));
            DeleteDocumentCommand = new RelayCommand<Document>(async (doc) => await DeleteDocumentAsync(doc));
            PreviewDocumentCommand = new RelayCommand<Document>(async (doc) => await PreviewDocumentAsync(doc));
            DownloadDocumentCommand = new RelayCommand<Document>(async (doc) => await DownloadDocumentAsync(doc));
            FilterDocumentsCommand = new RelayCommand(() => FilterDocuments());
            ClearFiltersCommand = new RelayCommand(() => ClearFilters());

            // Initialize
            Task.Run(async () => await InitializeAsync());
        }

        #region Properties

        public ObservableCollection<Document> Documents { get; }
        public ObservableCollection<Document> FilteredDocuments { get; }
        public ObservableCollection<DocumentCategory> DocumentCategories { get; }
        public ObservableCollection<Ship> Ships { get; }

        private Document? _selectedDocument;
        public Document? SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                _selectedDocument = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanApprove));
                OnPropertyChanged(nameof(CanReject));
                OnPropertyChanged(nameof(CanDelete));
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
                FilterDocuments();
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
                FilterDocuments();
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterDocuments();
            }
        }

        private bool _showApprovedOnly;
        public bool ShowApprovedOnly
        {
            get => _showApprovedOnly;
            set
            {
                _showApprovedOnly = value;
                OnPropertyChanged();
                FilterDocuments();
            }
        }

        private bool _showPendingOnly;
        public bool ShowPendingOnly
        {
            get => _showPendingOnly;
            set
            {
                _showPendingOnly = value;
                OnPropertyChanged();
                FilterDocuments();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // Statistics
        private int _totalDocuments;
        public int TotalDocuments
        {
            get => _totalDocuments;
            set
            {
                _totalDocuments = value;
                OnPropertyChanged();
            }
        }

        private int _approvedDocuments;
        public int ApprovedDocuments
        {
            get => _approvedDocuments;
            set
            {
                _approvedDocuments = value;
                OnPropertyChanged();
            }
        }

        private int _pendingDocuments;
        public int PendingDocuments
        {
            get => _pendingDocuments;
            set
            {
                _pendingDocuments = value;
                OnPropertyChanged();
            }
        }

        private string _totalFileSize = string.Empty;
        public string TotalFileSize
        {
            get => _totalFileSize;
            set
            {
                _totalFileSize = value;
                OnPropertyChanged();
            }
        }

        // Permissions
        public bool CanApprove => _authService.CurrentUser?.Role?.Name == "Administrator" && 
                                  SelectedDocument != null && !SelectedDocument.IsApproved;
        
        public bool CanReject => _authService.CurrentUser?.Role?.Name == "Administrator" && 
                                 SelectedDocument != null && !SelectedDocument.IsApproved;
        
        public bool CanDelete => _authService.CurrentUser?.Role?.Name == "Administrator" || 
                                 (SelectedDocument != null && SelectedDocument.UploadedByUserId == _authService.CurrentUser?.Id);

        #endregion

        #region Commands

        public ICommand LoadDocumentsCommand { get; }
        public ICommand UploadDocumentCommand { get; }
        public ICommand ApproveDocumentCommand { get; }
        public ICommand RejectDocumentCommand { get; }
        public ICommand DeleteDocumentCommand { get; }
        public ICommand PreviewDocumentCommand { get; }
        public ICommand DownloadDocumentCommand { get; }
        public ICommand FilterDocumentsCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        #endregion

        #region Methods

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading documents...";

                // Load reference data
                await LoadDocumentCategoriesAsync();
                await LoadShipsAsync();
                await LoadDocumentsAsync();
                await LoadStatisticsAsync();

                StatusMessage = "Documents loaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing documents view");
                StatusMessage = "Error loading documents";
                MessageBox.Show($"Error loading documents: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDocumentsAsync()
        {
            try
            {
                var documents = await _documentService.GetAllDocumentsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Documents.Clear();
                    foreach (var document in documents)
                    {
                        Documents.Add(document);
                    }
                    FilterDocuments();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading documents");
                throw;
            }
        }

        private async Task LoadDocumentCategoriesAsync()
        {
            try
            {
                var categories = await _documentService.GetAllCategoriesAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DocumentCategories.Clear();
                    DocumentCategories.Add(new DocumentCategory { Id = 0, Name = "All Categories" });
                    foreach (var category in categories)
                    {
                        DocumentCategories.Add(category);
                    }
                    
                    // Set default selection to "All Categories"
                    SelectedCategory = DocumentCategories.FirstOrDefault();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading document categories");
                throw;
            }
        }

        private async Task LoadShipsAsync()
        {
            try
            {
                var ships = await _shipService.GetAllShipsAsync();
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Ships.Clear();
                    Ships.Add(new Ship { Id = 0, ShipName = "All Ships" });
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }
                    
                    // Set default selection to "All Ships"
                    SelectedShip = Ships.FirstOrDefault();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ships");
                throw;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                TotalDocuments = await _documentService.GetTotalDocumentCount();
                
                var statusStats = await _documentService.GetDocumentStatsByStatus();
                ApprovedDocuments = statusStats.ContainsKey("Approved") ? statusStats["Approved"] : 0;
                PendingDocuments = statusStats.ContainsKey("Pending Approval") ? statusStats["Pending Approval"] : 0;

                var totalSize = await _documentService.GetTotalFileSize();
                TotalFileSize = FormatFileSize(totalSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statistics");
            }
        }

        private async Task UploadDocumentAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Document to Upload",
                    Filter = "All Documents|*.pdf;*.doc;*.docx;*.xlsx;*.xls;*.ppt;*.pptx;*.txt;*.rtf;*.csv;*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tiff;*.dwg;*.dxf|" +
                            "PDF Files|*.pdf|" +
                            "Word Documents|*.doc;*.docx|" +
                            "Excel Files|*.xlsx;*.xls|" +
                            "PowerPoint Files|*.ppt;*.pptx|" +
                            "Text Files|*.txt;*.rtf|" +
                            "CSV Files|*.csv|" +
                            "Image Files|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tiff|" +
                            "CAD Files|*.dwg;*.dxf|" +
                            "All Files|*.*",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    var uploadDialog = new DocumentUploadDialog(dialog.FileName, _documentService, _shipService);
                    if (uploadDialog.ShowDialog() == true)
                    {
                        await LoadDocumentsAsync();
                        await LoadStatisticsAsync();
                        StatusMessage = "Document uploaded successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                StatusMessage = "Error uploading document";
                MessageBox.Show($"Error uploading document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ApproveDocumentAsync(Document document)
        {
            try
            {
                if (document == null) return;

                var result = MessageBox.Show($"Are you sure you want to approve the document '{document.Name}'?", 
                    "Confirm Approval", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _documentService.ApproveDocumentAsync(document.Id, _authService.CurrentUser!.Id);
                    await LoadDocumentsAsync();
                    await LoadStatisticsAsync();
                    StatusMessage = "Document approved successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving document");
                StatusMessage = "Error approving document";
                MessageBox.Show($"Error approving document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RejectDocumentAsync(Document document)
        {
            try
            {
                if (document == null) return;

                var reason = InputDialog.ShowDialog(
                    "Please provide a reason for rejecting this document:",
                    "Rejection Reason");

                if (!string.IsNullOrEmpty(reason))
                {
                    await _documentService.RejectDocumentAsync(document.Id, _authService.CurrentUser!.Id, reason);
                    await LoadDocumentsAsync();
                    await LoadStatisticsAsync();
                    StatusMessage = "Document rejected successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting document");
                StatusMessage = "Error rejecting document";
                MessageBox.Show($"Error rejecting document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteDocumentAsync(Document document)
        {
            try
            {
                if (document == null) return;

                var result = MessageBox.Show($"Are you sure you want to delete the document '{document.Name}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await _documentService.DeleteDocumentAsync(document.Id);
                    await LoadDocumentsAsync();
                    await LoadStatisticsAsync();
                    StatusMessage = "Document deleted successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                StatusMessage = "Error deleting document";
                MessageBox.Show($"Error deleting document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PreviewDocumentAsync(Document document)
        {
            try
            {
                if (document == null) return;

                // For now, just show document details
                var details = $"Document: {document.Name}\n" +
                             $"Category: {document.Category?.Name}\n" +
                             $"Ship: {document.Ship?.ShipName ?? "Not assigned"}\n" +
                             $"File Size: {document.FileSizeDisplay}\n" +
                             $"Uploaded: {document.UploadedAtDisplay}\n" +
                             $"Uploaded by: {document.UploaderDisplay}\n" +
                             $"Status: {document.StatusDisplay}\n" +
                             $"Version: {document.Version}\n";

                if (!string.IsNullOrEmpty(document.Description))
                {
                    details += $"Description: {document.Description}\n";
                }

                if (!string.IsNullOrEmpty(document.Comments))
                {
                    details += $"Comments: {document.Comments}\n";
                }

                MessageBox.Show(details, "Document Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing document");
                StatusMessage = "Error previewing document";
                MessageBox.Show($"Error previewing document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DownloadDocumentAsync(Document document)
        {
            try
            {
                if (document == null) return;

                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Document",
                    FileName = document.FileName,
                    Filter = $"*{document.FileExtension}|*{document.FileExtension}|All Files|*.*"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var content = await _documentService.GetDocumentContentAsync(document.Id);
                    await File.WriteAllBytesAsync(saveDialog.FileName, content);
                    StatusMessage = "Document downloaded successfully";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document");
                StatusMessage = "Error downloading document";
                MessageBox.Show($"Error downloading document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterDocuments()
        {
            var filtered = Documents.AsEnumerable();

            // Filter by category
            if (SelectedCategory != null && SelectedCategory.Id > 0)
            {
                filtered = filtered.Where(d => d.CategoryId == SelectedCategory.Id);
            }

            // Filter by ship
            if (SelectedShip != null && SelectedShip.Id > 0)
            {
                filtered = filtered.Where(d => d.ShipId == SelectedShip.Id);
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLowerInvariant();
                filtered = filtered.Where(d => 
                    d.Name.ToLowerInvariant().Contains(searchLower) ||
                    d.FileName.ToLowerInvariant().Contains(searchLower) ||
                    (d.Description?.ToLowerInvariant().Contains(searchLower) ?? false) ||
                    (d.Category?.Name.ToLowerInvariant().Contains(searchLower) ?? false));
            }

            // Filter by approval status
            if (ShowApprovedOnly)
            {
                filtered = filtered.Where(d => d.IsApproved);
            }
            else if (ShowPendingOnly)
            {
                filtered = filtered.Where(d => !d.IsApproved);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                FilteredDocuments.Clear();
                foreach (var document in filtered)
                {
                    FilteredDocuments.Add(document);
                }
            });
        }

        private void ClearFilters()
        {
            SelectedCategory = DocumentCategories.FirstOrDefault();
            SelectedShip = Ships.FirstOrDefault();
            SearchText = string.Empty;
            ShowApprovedOnly = false;
            ShowPendingOnly = false;
            FilterDocuments();
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