using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Desktop.Views;
using MaritimeERP.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SecurityReviewStatementsViewModel : INotifyPropertyChanged
    {
        private readonly ISecurityReviewStatementService _securityReviewStatementService;
        private readonly IShipService _shipService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IChangeRequestService _changeRequestService;
        private readonly ILogger<SecurityReviewStatementsViewModel> _logger;

        // Collections
        public ObservableCollection<SecurityReviewStatement> SecurityReviewStatements { get; } = new();
        public ObservableCollection<Ship> Ships { get; } = new();

        // Selected items
        private SecurityReviewStatement? _selectedSecurityReviewStatement;
        public SecurityReviewStatement? SelectedSecurityReviewStatement
        {
            get => _selectedSecurityReviewStatement;
            set
            {
                if (SetProperty(ref _selectedSecurityReviewStatement, value))
                {
                    OnSelectedSecurityReviewStatementChanged();
                }
            }
        }

        // Filter properties
        private string _searchTerm = string.Empty;
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetProperty(ref _searchTerm, value))
                {
                    ApplyFilters();
                }
            }
        }

        private Ship? _filterShip;
        public Ship? FilterShip
        {
            get => _filterShip;
            set
            {
                if (SetProperty(ref _filterShip, value))
                {
                    ApplyFilters();
                }
            }
        }

        private string _filterStatus = "All";
        public string FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                {
                    ApplyFilters();
                }
            }
        }

        // Status properties
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Statistics
        private int _totalStatements;
        public int TotalStatements
        {
            get => _totalStatements;
            set => SetProperty(ref _totalStatements, value);
        }

        private int _pendingReview;
        public int PendingReview
        {
            get => _pendingReview;
            set => SetProperty(ref _pendingReview, value);
        }

        private int _approved;
        public int Approved
        {
            get => _approved;
            set => SetProperty(ref _approved, value);
        }

        private int _myStatements;
        public int MyStatements
        {
            get => _myStatements;
            set => SetProperty(ref _myStatements, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand AddSecurityReviewStatementCommand { get; }
        public ICommand EditSecurityReviewStatementCommand { get; }
        public ICommand DeleteSecurityReviewStatementCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand { get; }

        public SecurityReviewStatementsViewModel(
            ISecurityReviewStatementService securityReviewStatementService,
            IShipService shipService,
            IAuthenticationService authenticationService,
            IChangeRequestService changeRequestService,
            ILogger<SecurityReviewStatementsViewModel> logger)
        {
            _securityReviewStatementService = securityReviewStatementService;
            _shipService = shipService;
            _authenticationService = authenticationService;
            _changeRequestService = changeRequestService;
            _logger = logger;

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            AddSecurityReviewStatementCommand = new RelayCommand(AddSecurityReviewStatement, () => !IsLoading);
            EditSecurityReviewStatementCommand = new RelayCommand<SecurityReviewStatement>(EditSecurityReviewStatement, (srs) => srs != null && !IsLoading);
            DeleteSecurityReviewStatementCommand = new RelayCommand(async () => await DeleteSecurityReviewStatementAsync(), () => SelectedSecurityReviewStatement != null && !IsLoading);
            ViewDetailsCommand = new RelayCommand(ViewDetails, () => SelectedSecurityReviewStatement != null);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());

            // Load initial data
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Loading security review statements...";
                });

                // Load all data in parallel
                var statementsTask = _securityReviewStatementService.GetAllSecurityReviewStatementsAsync();
                var shipsTask = _shipService.GetAllShipsAsync();

                await Task.WhenAll(statementsTask, shipsTask);

                var statements = await statementsTask;
                var ships = await shipsTask;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Update collections
                    SecurityReviewStatements.Clear();
                    foreach (var statement in statements)
                    {
                        SecurityReviewStatements.Add(statement);
                    }

                    Ships.Clear();
                    foreach (var ship in ships)
                    {
                        Ships.Add(ship);
                    }

                    // Update statistics
                    TotalStatements = statements.Count;
                    PendingReview = statements.Count(s => s.IsUnderReview && !s.IsApproved);
                    Approved = statements.Count(s => s.IsApproved);

                    // Calculate my statements
                    var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                    MyStatements = statements.Count(s => s.UserId == currentUserId);

                    StatusMessage = $"Loaded {SecurityReviewStatements.Count} security review statements";
                    IsLoading = false;
                });

                ApplyFilters();
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                    StatusMessage = "Error loading data";
                });
                
                _logger.LogError(ex, "Error loading security review statements");
                MessageBox.Show($"Error loading security review statements: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSecurityReviewStatement()
        {
            try
            {
                var viewModel = new SecurityReviewStatementDialogViewModel(
                    _securityReviewStatementService,
                    _authenticationService,
                    _shipService,
                    _changeRequestService);

                var dialog = new SecurityReviewStatementDialog(viewModel);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Refresh the statements list to show the new entry
                    _ = LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening security review statement dialog");
                MessageBox.Show($"Error opening security review statement dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditSecurityReviewStatement(SecurityReviewStatement statement)
        {
            try
            {
                var viewModel = new SecurityReviewStatementDialogViewModel(
                    _securityReviewStatementService,
                    _authenticationService,
                    _shipService,
                    _changeRequestService);

                // Set the statement to edit
                viewModel.SecurityReviewStatement = statement;
                viewModel.IsEditMode = true;

                var dialog = new SecurityReviewStatementDialog(viewModel);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Refresh the statements list to show the updated entry
                    _ = LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening security review statement dialog for editing");
                MessageBox.Show($"Error opening security review statement dialog: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteSecurityReviewStatementAsync()
        {
            if (SelectedSecurityReviewStatement == null) return;

            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete Security Review Statement '{SelectedSecurityReviewStatement.RequestNumber}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var success = await _securityReviewStatementService.DeleteSecurityReviewStatementAsync(SelectedSecurityReviewStatement.Id);
                    if (success)
                    {
                        MessageBox.Show("Security Review Statement deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadDataAsync();
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete Security Review Statement.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting security review statement");
                MessageBox.Show($"Error deleting security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewDetails()
        {
            if (SelectedSecurityReviewStatement == null) return;

            try
            {
                var viewModel = new SecurityReviewStatementDialogViewModel(
                    _securityReviewStatementService,
                    _authenticationService,
                    _shipService,
                    _changeRequestService);

                // Set the statement to view (read-only)
                viewModel.SecurityReviewStatement = SelectedSecurityReviewStatement;
                viewModel.IsEditMode = true; // Set to true to show data, but make form read-only

                var dialog = new SecurityReviewStatementDialog(viewModel);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening security review statement details");
                MessageBox.Show($"Error opening security review statement details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            // This would be implemented with a CollectionView for filtering
            // For now, we'll just update the status message
            var filteredCount = SecurityReviewStatements.Count;
            StatusMessage = $"Showing {filteredCount} security review statements";
        }

        private void ClearFilters()
        {
            SearchTerm = string.Empty;
            FilterShip = null;
            FilterStatus = "All";
        }

        private void OnSelectedSecurityReviewStatementChanged()
        {
            // Update command states when selection changes
            ((RelayCommand)DeleteSecurityReviewStatementCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ViewDetailsCommand).RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
} 