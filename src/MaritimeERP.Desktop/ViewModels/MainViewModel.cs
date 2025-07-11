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
            
            // Set default navigation
            SelectedNavigation = NavigationItems.FirstOrDefault();
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
            
            var isAdmin = CurrentUser?.Role?.Name == "Administrator";
            var isEngineer = CurrentUser?.Role?.Name == "Engineer";
            
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
                Title = "Auditable Logs",
                Icon = "📋",
                Page = "AuditLogs",
                IsVisible = isAdmin
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Login Logs",
                Icon = "🔐",
                Page = "LoginLogs",
                IsVisible = isAdmin
            });
        }

        private void InitializeCommands()
        {
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            SettingsCommand = new RelayCommand(ShowSettings);
        }

        private void NavigateToPage()
        {
            if (SelectedNavigation == null)
                return;

            PageTitle = SelectedNavigation.Title;
            
            // Create appropriate view model based on selected page
            CurrentViewModel = SelectedNavigation.Page switch
            {
                "Dashboard" => CreateDashboardViewModel(),
                "Ships" => CreateShipsViewModel(),
                "Systems" => CreateSystemsViewModel(),
                "Components" => CreateComponentsViewModel(),
                "Software" => CreateSoftwareViewModel(),
                "ChangeRequests" => CreateChangeRequestsViewModel(),
                "Documents" => CreateDocumentsViewModel(),
                "UserManagement" => CreateUserManagementViewModel(),
                "AuditLogs" => CreateAuditLogsViewModel(),
                "LoginLogs" => CreateLoginLogsViewModel(),
                "Reports" => CreateReportsViewModel(),
                "Users" => CreateUsersViewModel(),
                _ => CreateDashboardViewModel()
            };
        }

        private object CreateDashboardViewModel()
        {
            if (!_viewModelCache.TryGetValue("Dashboard", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
                _viewModelCache["Dashboard"] = viewModel;
            }
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
                "Dashboard" => CreateDashboardViewModel(),
                "Ships" => CreateShipsViewModel(),
                "Systems" => CreateSystemsViewModel(),
                "Software" => CreateSoftwareViewModel(),
                "ChangeRequests" => CreateChangeRequestsViewModel(),
                "Documents" => CreateDocumentsViewModel(),
                "UserManagement" => CreateUserManagementViewModel(),
                "AuditLogs" => CreateAuditLogsViewModel(),
                "LoginLogs" => CreateLoginLogsViewModel(),
                "Reports" => CreateReportsViewModel(),
                "Users" => CreateUsersViewModel(),
                _ => CreateDashboardViewModel()
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
                "Dashboard" => CreateDashboardViewModel(),
                "Ships" => CreateShipsViewModel(),
                "Systems" => CreateSystemsViewModel(),
                "Components" => CreateComponentsViewModel(),
                "ChangeRequests" => CreateChangeRequestsViewModel(),
                "Documents" => CreateDocumentsViewModel(),
                "UserManagement" => CreateUserManagementViewModel(),
                "AuditLogs" => CreateAuditLogsViewModel(),
                "LoginLogs" => CreateLoginLogsViewModel(),
                "Reports" => CreateReportsViewModel(),
                "Users" => CreateUsersViewModel(),
                _ => CreateDashboardViewModel()
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

        private object CreateLoginLogsViewModel()
        {
            if (!_viewModelCache.TryGetValue("LoginLogs", out var viewModel))
            {
                viewModel = _serviceProvider.GetRequiredService<LoginLogsViewModel>();
                _viewModelCache["LoginLogs"] = viewModel;
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
    }

    public class NavigationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }
} 