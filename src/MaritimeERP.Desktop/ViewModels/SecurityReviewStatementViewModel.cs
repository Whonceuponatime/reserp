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
using System.Collections.Generic;

namespace MaritimeERP.Desktop.ViewModels
{
    public class SecurityReviewStatementViewModel : INotifyPropertyChanged
    {
        private readonly ISecurityReviewStatementService _securityReviewStatementService;
        private readonly IShipService _shipService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IChangeRequestService _changeRequestService;
        private readonly ILogger<SecurityReviewStatementViewModel> _logger;

        // Collections
        public ObservableCollection<SecurityReviewStatement> SecurityReviewStatements { get; } = new();
        public ObservableCollection<Ship> Ships { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } = new();

        // Selected items
        private SecurityReviewStatement? _selectedStatement;
        public SecurityReviewStatement? SelectedStatement
        {
            get => _selectedStatement;
            set
            {
                if (SetProperty(ref _selectedStatement, value))
                {
                    RefreshCommands();
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

        private string? _filterStatus;
        public string? FilterStatus
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

        private int _approvedStatements;
        public int ApprovedStatements
        {
            get => _approvedStatements;
            set => SetProperty(ref _approvedStatements, value);
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand AddSecurityReviewCommand { get; }
        public ICommand EditStatementCommand { get; }
        public ICommand ViewStatementCommand { get; }
        public ICommand DeleteStatementCommand { get; }
        public ICommand SubmitForReviewCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public SecurityReviewStatementViewModel(
            ISecurityReviewStatementService securityReviewStatementService,
            IShipService shipService,
            IAuthenticationService authenticationService,
            IChangeRequestService changeRequestService,
            ILogger<SecurityReviewStatementViewModel> logger)
        {
            _securityReviewStatementService = securityReviewStatementService ?? throw new ArgumentNullException(nameof(securityReviewStatementService));
            _shipService = shipService ?? throw new ArgumentNullException(nameof(shipService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _changeRequestService = changeRequestService ?? throw new ArgumentNullException(nameof(changeRequestService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync(), () => !IsLoading);
            AddSecurityReviewCommand = new RelayCommand(AddSecurityReview, () => !IsLoading);
            EditStatementCommand = new RelayCommand<SecurityReviewStatement>(EditStatement, (s) => s != null && !IsLoading);
            ViewStatementCommand = new RelayCommand<SecurityReviewStatement>(ViewStatement, (s) => s != null && !IsLoading);
            DeleteStatementCommand = new RelayCommand<SecurityReviewStatement>(async (s) => await DeleteStatementAsync(s), (s) => s != null && !IsLoading);
            SubmitForReviewCommand = new RelayCommand(async () => await SubmitForReviewAsync(), () => CanSubmitForReview());
            ApproveCommand = new RelayCommand(async () => await ApproveAsync(), () => CanApprove());
            RejectCommand = new RelayCommand(async () => await RejectAsync(), () => CanReject());
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            // Initialize status options
            StatusOptions.Add("Draft");
            StatusOptions.Add("Under Review");
            StatusOptions.Add("Approved");
            StatusOptions.Add("Rejected");

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
                    ApprovedStatements = statements.Count(s => s.IsApproved);

                    StatusMessage = $"Loaded {SecurityReviewStatements.Count} security review statements";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security review statements data");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error loading data";
                });
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private void AddSecurityReview()
        {
            var viewModel = new SecurityReviewStatementDialogViewModel(_securityReviewStatementService, _authenticationService, _shipService, _changeRequestService);
            var dialog = new SecurityReviewStatementDialog(viewModel);
            var result = dialog.ShowDialog();

            if (result == true)
            {
                // Refresh the statements list to show the new entry
                _ = LoadDataAsync();
            }
        }

                private void EditStatement(SecurityReviewStatement? statement)
        {
            if (statement == null) return;

            var viewModel = new SecurityReviewStatementDialogViewModel(_securityReviewStatementService, _authenticationService, _shipService, _changeRequestService);
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

        private void ViewStatement(SecurityReviewStatement? statement)
        {
            if (statement == null) return;

            var viewModel = new SecurityReviewStatementDialogViewModel(_securityReviewStatementService, _authenticationService, _shipService, _changeRequestService);
            viewModel.SecurityReviewStatement = statement;
            viewModel.IsEditMode = true;
            
            var dialog = new SecurityReviewStatementDialog(viewModel);
            dialog.ShowDialog();
        }

        private async Task DeleteStatementAsync(SecurityReviewStatement? statement)
        {
            if (statement == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete security review statement '{statement.RequestNumber}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "Deleting security review statement...";

                    await _securityReviewStatementService.DeleteSecurityReviewStatementAsync(statement.Id);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SecurityReviewStatements.Remove(statement);
                        TotalStatements--;
                        StatusMessage = "Security review statement deleted successfully";
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting security review statement {Id}", statement.Id);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Error deleting security review statement";
                        MessageBox.Show($"Error deleting security review statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task SubmitForReviewAsync()
        {
            if (SelectedStatement == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Submitting for review...";

                var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                await _securityReviewStatementService.SubmitForReviewAsync(SelectedStatement.Id, currentUserId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedStatement.IsUnderReview = true;
                    StatusMessage = "Statement submitted for review successfully";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting statement for review");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error submitting for review";
                    MessageBox.Show($"Error submitting for review: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ApproveAsync()
        {
            if (SelectedStatement == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Approving statement...";

                var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                await _securityReviewStatementService.ApproveAsync(SelectedStatement.Id, currentUserId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedStatement.IsApproved = true;
                    SelectedStatement.IsUnderReview = false;
                    StatusMessage = "Statement approved successfully";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving statement");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error approving statement";
                    MessageBox.Show($"Error approving statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RejectAsync()
        {
            if (SelectedStatement == null) return;

            // Get rejection reason from user using a simple input dialog
            var reason = Microsoft.VisualBasic.Interaction.InputBox(
                "Please enter the reason for rejection:",
                "Reject Security Review Statement",
                "");

            if (!string.IsNullOrWhiteSpace(reason))
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "Rejecting statement...";

                    var currentUserId = _authenticationService.CurrentUser?.Id ?? 0;
                    await _securityReviewStatementService.RejectAsync(SelectedStatement.Id, currentUserId, reason);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SelectedStatement.IsUnderReview = false;
                        SelectedStatement.IsApproved = false;
                        StatusMessage = "Statement rejected successfully";
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rejecting statement");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Error rejecting statement";
                        MessageBox.Show($"Error rejecting statement: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private void ApplyFilters()
        {
            // This would apply filters to the collection
            // For now, we'll just reload the data
            _ = LoadDataAsync();
        }

        private void ClearFilters()
        {
            SearchTerm = string.Empty;
            FilterShip = null;
            FilterStatus = null;
        }

        private bool CanSubmitForReview()
        {
            return SelectedStatement != null && !SelectedStatement.IsUnderReview && !SelectedStatement.IsApproved && !IsLoading;
        }

        private bool CanApprove()
        {
            return SelectedStatement != null && SelectedStatement.IsUnderReview && !SelectedStatement.IsApproved && !IsLoading;
        }

        private bool CanReject()
        {
            return SelectedStatement != null && SelectedStatement.IsUnderReview && !SelectedStatement.IsApproved && !IsLoading;
        }

        private void RefreshCommands()
        {
            // Refresh all commands that depend on selection
            ((RelayCommand)SubmitForReviewCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ApproveCommand).RaiseCanExecuteChanged();
            ((RelayCommand)RejectCommand).RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
} 