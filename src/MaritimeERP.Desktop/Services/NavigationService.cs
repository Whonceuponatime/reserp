using MaritimeERP.Core.Entities;
using MaritimeERP.Desktop.ViewModels;
using MaritimeERP.Services.Interfaces;
using ComponentEntity = MaritimeERP.Core.Entities.Component;

namespace MaritimeERP.Desktop.Services
{
    public class NavigationService : INavigationService
    {
        private WeakReference<MainViewModel>? _mainViewModelRef;

        public void SetMainViewModel(MainViewModel mainViewModel)
        {
            _mainViewModelRef = new WeakReference<MainViewModel>(mainViewModel);
        }

        public void NavigateToPage(string pageName)
        {
            if (_mainViewModelRef?.TryGetTarget(out var mainViewModel) == true)
            {
                // Find the navigation item
                var navigationItem = mainViewModel.NavigationItems.FirstOrDefault(item => item.Page == pageName);
                if (navigationItem != null)
                {
                    mainViewModel.SelectedNavigation = navigationItem;
                }
            }
        }

        public void NavigateToPageWithFilter(string pageName, ShipSystem? systemFilter = null)
        {
            if (_mainViewModelRef?.TryGetTarget(out var mainViewModel) == true)
            {
                mainViewModel.NavigateToPageWithFilter(pageName, systemFilter);
            }
        }

        public void NavigateToPageWithComponentFilter(string pageName, ComponentEntity? componentFilter = null)
        {
            if (_mainViewModelRef?.TryGetTarget(out var mainViewModel) == true)
            {
                mainViewModel.NavigateToPageWithComponentFilter(pageName, componentFilter);
            }
        }
    }
} 