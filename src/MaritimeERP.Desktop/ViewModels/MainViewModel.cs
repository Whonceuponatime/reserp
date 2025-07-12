using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;
using ComponentEntity = MaritimeERP.Core.Entities.Component;
using System.Collections.Generic;

namespace MaritimeERP.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;
        private User? _currentUser;
        private NavigationItem? _selectedNavigation;
        private object? _currentViewModel;
        private string _pageTitle = "Dashboard";

        // Cache view models to persist state across navigation
        private readonly Dictionary<string, object> _viewModelCache = new();

        public MainViewModel(IAuthenticationService authenticationService, IServiceProvider serviceProvider, INavigationService navigationService)
        {
            _authenticationService = authenticationService;
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;
            _currentUser = _authenticationService.CurrentUser;
            
            // Initialize the navigation service with this MainViewModel instance
            if (_navigationService is MaritimeERP.Desktop.Services.NavigationService navService)
            {
                navService.SetMainViewModel(this);
            }
            
            InitializeNavigation();
            InitializeCommands();
            
            // Preload all ViewModels to eliminate tab switching delays
            _ = Task.Run(async () => await PreloadViewModelsAsync());
            
            // Set default navigation
            SelectedNavigation = NavigationItems.FirstOrDefault();
        }

        /// <summary>
        /// Preload all ViewModels to eliminate initial tab switching delays
        /// </summary>
        private async Task PreloadViewModelsAsync()
        {
            try
            {
                Console.WriteLine("Starting ViewModel preloading...");
                
                // Preload all ViewModels in parallel for better performance
                var preloadTasks = new List<Task>();
                
                // Always preload Dashboard and core views
                preloadTasks.Add(Task.Run(() => CreateDashboardViewModel()));
                preloadTasks.Add(Task.Run(() => CreateShipsViewModel()));
                preloadTasks.Add(Task.Run(() => CreateSystemsViewModel()));
                preloadTasks.Add(Task.Run(() => CreateComponentsViewModel()));
                preloadTasks.Add(Task.Run(() => CreateSoftwareViewModel()));
                preloadTasks.Add(Task.Run(() => CreateChangeRequestsViewModel()));
                preloadTasks.Add(Task.Run(() => CreateDocumentsViewModel()));
                
                // Only preload admin views if user is admin
                var currentUser = CurrentUser;
                var isAdmin = currentUser?.Role?.Name == "Administrator";
                
                if (isAdmin)
                {
                    preloadTasks.Add(Task.Run(() => CreateUserManagementViewModel()));
                    preloadTasks.Add(Task.Run(() => CreateAuditLogsViewModel()));
                }
                
                // Wait for all preloading to complete
                await Task.WhenAll(preloadTasks);
                
                Console.WriteLine("ViewModel preloading completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during ViewModel preloading: {ex.Message}");
            }
        }

        public User? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        public NavigationItem? SelectedNavigation
        {
            get => _selectedNavigation;
            set
            {
                if (SetProperty(ref _selectedNavigation, value))
                {
                    NavigateToPage();
                }
            }
        }

        public object? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

        public ICommand LogoutCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand SettingsCommand { get; private set; } = null!;

        private void InitializeNavigation()
        {
            NavigationItems.Clear();
            
            // Use the CurrentUser property which should have the latest user data
            var currentUser = CurrentUser;
            var isAdmin = currentUser?.Role?.Name == "Administrator";
            var isEngineer = currentUser?.Role?.Name == "Engineer";
            
            // Debug logging to help diagnose role issues
            Console.WriteLine($"InitializeNavigation - User: {currentUser?.Username}, Role: {currentUser?.Role?.Name}, IsAdmin: {isAdmin}");
            
            // Dashboard - visible to all users
            NavigationItems.Add(new NavigationItem
            {
                Title = "Dashboard",
                Icon = "📊",
                Page = "Dashboard",
                IsVisible = true
            });

            // Fleet Management - Engineers get read-only view
            NavigationItems.Add(new NavigationItem
            {
                Title = "Fleet Management",
                Icon = "🚢",
                Page = "Ships",
                IsVisible = true
            });

            // Systems - Engineers get read-only view
            NavigationItems.Add(new NavigationItem
            {
                Title = "Systems",
                Icon = "⚙️",
                Page = "Systems", 
                IsVisible = true
            });

            // Components - Engineers get read-only view
            NavigationItems.Add(new NavigationItem
            {
                Title = "Components",
                Icon = "🔧",
                Page = "Components",
                IsVisible = true
            });

            // Software - Engineers get read-only view
            NavigationItems.Add(new NavigationItem
            {
                Title = "Software",
                Icon = "💻",
                Page = "Software",
                IsVisible = true
            });

            // Change Requests - Engineers can submit, Admins can approve
            NavigationItems.Add(new NavigationItem
            {
                Title = "Change Requests",
                Icon = "📝",
                Page = "ChangeRequests",
                IsVisible = true
            });

            // Documents - Engineers can upload, Admins can approve
            NavigationItems.Add(new NavigationItem
            {
                Title = "Documents",
                Icon = "📄",
                Page = "Documents",
                IsVisible = true
            });

            // Admin-only sections
            NavigationItems.Add(new NavigationItem
            {
                Title = "User Management",
                Icon = "👥",
                Page = "UserManagement",
                IsVisible = isAdmin
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Audit & Security Logs",
                Icon = "📋",
                Page = "AuditLogs",
                IsVisible = isAdmin
            });
        }

        private void InitializeCommands()
        {
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            SettingsCommand = new RelayCommand(ShowSettings);
        }

        private async void NavigateToPage()
        {
            if (SelectedNavigation == null)
                return;

            PageTitle = SelectedNavigation.Title;
            
            // Get cached view model - should be instantly available after preloading
            var viewModel = SelectedNavigation.Page switch
            {
                "Dashboard" => GetOrCreateDashboardViewModel(),
                "Ships" => GetOrCreateShipsViewModel(),
                "Systems" => GetOrCreateSystemsViewModel(),
                "Components" => GetOrCreateComponentsViewModel(),
                "Software" => GetOrCreateSoftwareViewModel(),
                "ChangeRequests" => GetOrCreateChangeRequestsViewModel(),
                "Documents" => GetOrCreateDocumentsViewModel(),
                "UserManagement" => GetOrCreateUserManagementViewModel(),
                "AuditLogs" => GetOrCreateAuditLogsViewModel(),
                "Reports" => CreateReportsViewModel(),
                "Users" => CreateUsersViewModel(),
                _ => GetOrCreateDashboardViewModel()
            };

            CurrentViewModel = viewModel;
        }

        // Updated methods to use cached ViewModels
        private object GetOrCreateDashboardViewModel()
        {
            return _viewModelCache.TryGetValue("Dashboard", out var viewModel) ? viewModel : CreateDashboardViewModel();
        }

        private object GetOrCreateShipsViewModel()
        {
            return _viewModelCache.TryGetValue("Ships", out var viewModel) ? viewModel : CreateShipsViewModel();
        }

        private object GetOrCreateSystemsViewModel()
        {
            return _viewModelCache.TryGetValue("Systems", out var viewModel) ? viewModel : CreateSystemsViewModel();
        }

        private object GetOrCreateComponentsViewModel()
        {
            return _viewModelCache.TryGetValue("Components", out var viewModel) ? viewModel : CreateComponentsViewModel();
        }

        private object GetOrCreateSoftwareViewModel()
        {
            return _viewModelCache.TryGetValue("Software", out var viewModel) ? viewModel : CreateSoftwareViewModel();
        }

        private object GetOrCreateChangeRequestsViewModel()
        {
            return _viewModelCache.TryGetValue("ChangeRequests", out var viewModel) ? viewModel : CreateChangeRequestsViewModel();
        }

        private object GetOrCreateDocumentsViewModel()
        {
            return _viewModelCache.TryGetValue("Documents", out var viewModel) ? viewModel : CreateDocumentsViewModel();
        }

        private object GetOrCreateUserManagementViewModel()
        {
            return _viewModelCache.TryGetValue("UserManagement", out var viewModel) ? viewModel : CreateUserManagementViewModel();
        }

        private object GetOrCreateAuditLogsViewModel()
        {
            return _viewModelCache.TryGetValue("AuditLogs", out var viewModel) ? viewModel : CreateAuditLogsViewModel();
        }

        private object CreateDashboardViewModel()
        {
            // Always create fresh dashboard for real-time data
            if (_viewModelCache.TryGetValue("Dashboard", out var oldViewModel) && oldViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            _viewModelCache["Dashboard"] = viewModel;
            return viewModel;
        }

        private object CreateShipsViewModel()
        {
            if (!_viewModelCache.TryGetValue("Ships", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<ShipsViewModel>();
                _viewModelCache["Ships"] = viewModel;
            }
            return viewModel;
        }

        private object CreateSystemsViewModel()
        {
            if (!_viewModelCache.TryGetValue("Systems", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<SystemsViewModel>();
                _viewModelCache["Systems"] = viewModel;
            }
            return viewModel;
        }

        private object CreateComponentsViewModel()
        {
            if (!_viewModelCache.TryGetValue("Components", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<ComponentsViewModel>();
                _viewModelCache["Components"] = viewModel;
            }
            return viewModel;
        }

        private object CreateComponentsViewModel(ShipSystem? systemFilter = null)
        {
            var componentsViewModel = (ComponentsViewModel)CreateComponentsViewModel();
            if (systemFilter != null)
            {
                componentsViewModel.SetSystemFilter(systemFilter);
            }
            return componentsViewModel;
        }

        /// <summary>
        /// Navigate to a specific page with optional filter parameters
        /// </summary>
        /// <param name="pageName">The name of the page to navigate to</param>
        /// <param name="systemFilter">Optional system filter for Components page</param>
        public void NavigateToPageWithFilter(string pageName, ShipSystem? systemFilter = null)
        {
            // Find the navigation item
            var navigationItem = NavigationItems.FirstOrDefault(item => item.Page == pageName);
            if (navigationItem == null)
                return;

            // Set the page title
            PageTitle = navigationItem.Title;
            
            // Create the appropriate view model with filters
            CurrentViewModel = navigationItem.Page switch
            {
                "Components" => CreateComponentsViewModel(systemFilter),
                "Dashboard" => GetOrCreateDashboardViewModel(),
                "Ships" => GetOrCreateShipsViewModel(),
                "Systems" => GetOrCreateSystemsViewModel(),
                "Software" => GetOrCreateSoftwareViewModel(),
                "ChangeRequests" => GetOrCreateChangeRequestsViewModel(),
                "Documents" => GetOrCreateDocumentsViewModel(),
                "UserManagement" => GetOrCreateUserManagementViewModel(),
                "AuditLogs" => GetOrCreateAuditLogsViewModel(),
                "Reports" => CreateReportsViewModel(),
                "Users" => CreateUsersViewModel(),
                _ => GetOrCreateDashboardViewModel()
            };

            // Update the selected navigation item
            SelectedNavigation = navigationItem;
        }

        /// <summary>
        /// Navigate to a specific page with optional component filter parameters
        /// </summary>
        /// <param name="pageName">The name of the page to navigate to</param>
        /// <param name="componentFilter">Optional component filter for Software page</param>
        public void NavigateToPageWithComponentFilter(string pageName, ComponentEntity? componentFilter = null)
        {
            // Find the navigation item
            var navigationItem = NavigationItems.FirstOrDefault(item => item.Page == pageName);
            if (navigationItem == null)
                return;

            // Set the page title
            PageTitle = navigationItem.Title;
            
            // Create the appropriate view model with filters
            CurrentViewModel = navigationItem.Page switch
            {
                "Software" => CreateSoftwareViewModel(componentFilter),
                "Dashboard" => GetOrCreateDashboardViewModel(),
                "Ships" => GetOrCreateShipsViewModel(),
                "Systems" => GetOrCreateSystemsViewModel(),
                "Components" => GetOrCreateComponentsViewModel(),
                "ChangeRequests" => GetOrCreateChangeRequestsViewModel(),
                "Documents" => GetOrCreateDocumentsViewModel(),
                "UserManagement" => GetOrCreateUserManagementViewModel(),
                "AuditLogs" => GetOrCreateAuditLogsViewModel(),
                "Reports" => CreateReportsViewModel(),
                "Users" => CreateUsersViewModel(),
                _ => GetOrCreateDashboardViewModel()
            };

            // Update the selected navigation item
            SelectedNavigation = navigationItem;
        }

        private object CreateSoftwareViewModel()
        {
            if (!_viewModelCache.TryGetValue("Software", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<SoftwareViewModel>();
                _viewModelCache["Software"] = viewModel;
            }
            return viewModel;
        }

        private object CreateSoftwareViewModel(ComponentEntity? componentFilter = null)
        {
            var softwareViewModel = (SoftwareViewModel)CreateSoftwareViewModel();
            if (componentFilter != null)
            {
                softwareViewModel.SetComponentFilter(componentFilter);
            }
            return softwareViewModel;
        }

        private object CreateChangeRequestsViewModel()
        {
            if (!_viewModelCache.TryGetValue("ChangeRequests", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<ChangeRequestsViewModel>();
                _viewModelCache["ChangeRequests"] = viewModel;
            }
            return viewModel;
        }

        private object CreateUserManagementViewModel()
        {
            if (!_viewModelCache.TryGetValue("UserManagement", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<UserManagementViewModel>();
                _viewModelCache["UserManagement"] = viewModel;
            }
            return viewModel;
        }

        private object CreateAuditLogsViewModel()
        {
            if (!_viewModelCache.TryGetValue("AuditLogs", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<AuditLogsViewModel>();
                _viewModelCache["AuditLogs"] = viewModel;
            }
            return viewModel;
        }

        private object CreateDocumentsViewModel()
        {
            if (!_viewModelCache.TryGetValue("Documents", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<DocumentsViewModel>();
                _viewModelCache["Documents"] = viewModel;
            }
            return viewModel;
        }

        private object CreateReportsViewModel()
        {
            return new PlaceholderViewModel("Reports & Analytics", "View reports and analytics", "📈");
        }

        private object CreateUsersViewModel()
        {
            return new PlaceholderViewModel("User Management", "Manage system users and roles", "👥");
        }

        private async Task LogoutAsync()
        {
            await _authenticationService.LogoutAsync();
            // Clear cached view models on logout
            _viewModelCache.Clear();
            // The App.xaml.cs will handle showing the login window
        }

        private async Task RefreshAsync()
        {
            CurrentUser = await _authenticationService.GetCurrentUserAsync();
            
            // Debug logging
            Console.WriteLine($"RefreshAsync - User: {CurrentUser?.Username}, Role: {CurrentUser?.Role?.Name}");
            
            // Clear cache to force refresh of all view models
            _viewModelCache.Clear();
            InitializeNavigation();
            NavigateToPage();
        }

        private void ShowSettings()
        {
            PageTitle = "Settings";
            CurrentViewModel = new PlaceholderViewModel("Settings", "Application settings and preferences", "⚙️");
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

        public void ClearViewModelCache()
        {
            _viewModelCache.Clear();
        }

        public async void RefreshNavigationAfterLogin()
        {
            // First try to get the current user from the authentication service
            var authUser = _authenticationService.CurrentUser;
            Console.WriteLine($"RefreshNavigationAfterLogin - AuthService.CurrentUser: {authUser?.Username}, Role: {authUser?.Role?.Name}");
            
            // If the current user from auth service has role info, use it directly
            if (authUser?.Role != null)
            {
                CurrentUser = authUser;
                Console.WriteLine($"RefreshNavigationAfterLogin - Using AuthService user directly");
            }
            else
            {
                // Otherwise try to get fresh data
                CurrentUser = await _authenticationService.GetCurrentUserAsync();
                Console.WriteLine($"RefreshNavigationAfterLogin - After GetCurrentUserAsync: {CurrentUser?.Username}, Role: {CurrentUser?.Role?.Name}");
            }
            
            InitializeNavigation();
            
            // Preload ViewModels after login to ensure fast tab switching
            _ = Task.Run(async () => await PreloadViewModelsAsync());
            
            // Set default navigation if none selected
            if (SelectedNavigation == null)
            {
                SelectedNavigation = NavigationItems.FirstOrDefault();
            }
        }
    }

    public class NavigationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }
} 