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
            
            // For all other ViewModels, use ContentControl with DataTemplate
            // This allows WPF to automatically resolve ViewModels to Views using DataTemplates
            return new ContentControl { Content = viewModel };
        }
    }
} 