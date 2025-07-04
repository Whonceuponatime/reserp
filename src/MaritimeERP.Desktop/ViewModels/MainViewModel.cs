using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.Commands;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IServiceProvider _serviceProvider;
        private User? _currentUser;
        private NavigationItem? _selectedNavigation;
        private object? _currentViewModel;
        private string _pageTitle = "Dashboard";

        public MainViewModel(IAuthenticationService authenticationService, IServiceProvider serviceProvider)
        {
            _authenticationService = authenticationService;
            _serviceProvider = serviceProvider;
            _currentUser = _authenticationService.CurrentUser;
            
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
            
            NavigationItems.Add(new NavigationItem
            {
                Title = "Dashboard",
                Icon = "📊",
                Page = "Dashboard",
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Fleet Management",
                Icon = "🚢",
                Page = "Ships",
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Systems",
                Icon = "⚙️",
                Page = "Systems", 
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Components",
                Icon = "🔧",
                Page = "Components",
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Software",
                Icon = "💻",
                Page = "Software",
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Change Requests",
                Icon = "📝",
                Page = "ChangeRequests",
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Documents",
                Icon = "📁",
                Page = "Documents",
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "Reports",
                Icon = "📈",
                Page = "Reports",
                IsVisible = true
            });

            NavigationItems.Add(new NavigationItem
            {
                Title = "User Management",
                Icon = "👥",
                Page = "Users",
                IsVisible = CurrentUser?.Role?.Name == "Administrator"
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
                "Reports" => CreateReportsViewModel(),
                "Users" => CreateUsersViewModel(),
                _ => CreateDashboardViewModel()
            };
        }

        private object CreateDashboardViewModel()
        {
            return _serviceProvider.GetRequiredService<DashboardViewModel>();
        }

        private object CreateShipsViewModel()
        {
            return _serviceProvider.GetRequiredService<ShipsViewModel>();
        }

        private object CreateSystemsViewModel()
        {
            return _serviceProvider.GetRequiredService<SystemsViewModel>();
        }

        private object CreateComponentsViewModel()
        {
            return _serviceProvider.GetRequiredService<ComponentsViewModel>();
        }

        private object CreateSoftwareViewModel()
        {
            return new PlaceholderViewModel("Software Management", "Manage software inventory", "💻");
        }

        private object CreateChangeRequestsViewModel()
        {
            return new PlaceholderViewModel("Change Requests", "Manage change requests and approvals", "📝");
        }

        private object CreateDocumentsViewModel()
        {
            return new PlaceholderViewModel("Document Management", "Manage ship documents and files", "📁");
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
            // The App.xaml.cs will handle showing the login window
        }

        private async Task RefreshAsync()
        {
            CurrentUser = await _authenticationService.GetCurrentUserAsync();
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
    }

    public class NavigationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }
} 