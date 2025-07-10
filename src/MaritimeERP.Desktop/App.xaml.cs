using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Desktop.ViewModels;
using MaritimeERP.Desktop.Views;
using MaritimeERP.Desktop.Services;
using MaritimeERP.Services;
using MaritimeERP.Services.Interfaces;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace MaritimeERP.Desktop
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private readonly ILogger<App> _logger;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public App()
        {
            try
            {
                // Allocate console for debugging
                AllocConsole();
                Console.WriteLine("Maritime ERP Application Starting...");
                
                _host = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        // Use the application directory instead of current working directory
                        var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
                        Console.WriteLine($"Looking for appsettings.json in: {appDirectory}");
                        
                        config.SetBasePath(appDirectory);
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                        
                        // Also try the source directory for development
                        var sourceConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "MaritimeERP.Desktop", "appsettings.json");
                        if (File.Exists(sourceConfigPath))
                        {
                            Console.WriteLine($"Found appsettings.json in source directory: {sourceConfigPath}");
                            config.AddJsonFile(sourceConfigPath, optional: false, reloadOnChange: true);
                        }
                        else
                        {
                            Console.WriteLine($"Source config not found at: {sourceConfigPath}");
                            // Add minimal configuration as fallback
                            config.AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                ["ConnectionStrings:DefaultConnection"] = "Data Source=maritime_erp.db"
                            });
                        }
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddLogging(builder =>
                        {
                            builder.AddConsole();
                            builder.AddDebug();
                        });
                        ConfigureServices(services, context.Configuration);
                    })
                    .Build();
                    
                Console.WriteLine("Host created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in App constructor: {ex}");
                MessageBox.Show($"Error in App constructor: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            _logger = _host.Services.GetRequiredService<ILogger<App>>();
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            Console.WriteLine("Configuring services...");
            
            // Database - Use SQLite for development simplicity
            services.AddDbContext<MaritimeERPContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (connectionString?.Contains("Host=") == true)
                {
                    options.UseNpgsql(connectionString);
                }
                else
                {
                    options.UseSqlite(connectionString ?? "Data Source=maritime_erp.db");
                }
            });

            // Services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IShipService, ShipService>();
            services.AddScoped<ISystemService, SystemService>();
            services.AddScoped<IComponentService, ComponentService>();
            services.AddScoped<ISoftwareService, SoftwareService>();
            services.AddScoped<IChangeRequestService, ChangeRequestService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ViewLocator>();
            // Add other services here as they are implemented

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddSingleton<DashboardViewModel>();
            services.AddTransient<ShipsViewModel>();
            services.AddTransient<SystemsViewModel>();
            services.AddTransient<ComponentsViewModel>();
            services.AddTransient<SoftwareViewModel>();
            services.AddTransient<ChangeRequestsViewModel>();

            // Views
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            
            Console.WriteLine("Services configured successfully");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Task.Run(async () =>
                {
                await _host.StartAsync();
                    await Task.Delay(100); // Add a small delay to ensure proper initialization
                }).Wait();

                var authService = _host.Services.GetRequiredService<IAuthenticationService>();
                var loginViewModel = new LoginViewModel(authService);
                var loginWindow = new LoginWindow(loginViewModel);
                loginViewModel.LoginSuccess += (s, _) => 
                {
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
                    loginWindow.Close();
                };
                loginWindow.ShowDialog();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application startup");
                MessageBox.Show("Error during application startup", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Task.Run(async () =>
            {
                await _host.StopAsync();
                _host.Dispose();
            }).Wait();

            base.OnExit(e);
        }
    }
}
