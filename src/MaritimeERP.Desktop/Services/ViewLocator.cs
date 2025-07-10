using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MaritimeERP.Desktop.ViewModels;
using MaritimeERP.Desktop.Views;

namespace MaritimeERP.Desktop.Services
{
    public class ViewLocator
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewLocator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ContentControl GetViewForViewModel(object viewModel)
        {
            var viewModelType = viewModel.GetType();

            // Handle special cases first
            if (viewModelType == typeof(DashboardViewModel))
            {
                var dashboardView = new DashboardView();
                dashboardView.DataContext = viewModel;
                return dashboardView;
            }
            else if (viewModelType == typeof(PlaceholderViewModel))
            {
                // Use the DataTemplate from MainWindow.xaml
                return new ContentControl { Content = viewModel };
            }
            else if (viewModelType == typeof(SoftwareViewModel))
            {
                return (ContentControl)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(SoftwareView), viewModel);
            }

            // For all other views
            var viewName = viewModelType.Name.Replace("ViewModel", "View");
            var viewType = Type.GetType($"MaritimeERP.Desktop.Views.{viewName}, MaritimeERP.Desktop");

            if (viewType == null)
            {
                throw new ArgumentException($"View not found for ViewModel: {viewModelType.Name}");
            }

            var view = (ContentControl)Activator.CreateInstance(viewType)!;
            view.DataContext = viewModel;
            return view;
        }
    }
} 