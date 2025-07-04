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
using MaritimeERP.Services;
using MaritimeERP.Services.Interfaces;

namespace MaritimeERP.Desktop
{
    public partial class App : Application
    {
        private readonly IHost _host;

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
                        ConfigureServices(services, context.Configuration);
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.AddDebug();
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
            // Add other services here as they are implemented

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ShipsViewModel>();
            services.AddTransient<SystemsViewModel>();

            // Views
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            
            Console.WriteLine("Services configured successfully");
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            Console.WriteLine("OnStartup called");
            
            try
            {
                if (_host == null)
                {
                    throw new InvalidOperationException("Host was not initialized properly during construction.");
                }

                Console.WriteLine("Starting host...");
                await _host.StartAsync();
                Console.WriteLine("Host started successfully");

                Console.WriteLine("Initializing database...");
                await InitializeDatabaseAsync();
                Console.WriteLine("Database initialized successfully");

                Console.WriteLine("Showing login window...");
                await ShowLoginWindowAsync();
                Console.WriteLine("Login window shown successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Startup error: {ex}");
                MessageBox.Show($"Application startup failed: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        private async Task InitializeDatabaseAsync()
        {
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
            
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Seed initial data if needed
            await SeedInitialDataAsync(context);
        }

        private async Task SeedInitialDataAsync(MaritimeERPContext context)
        {
            Console.WriteLine("Starting database seeding...");
            
            // Check if we need to seed data
            var userCount = await context.Users.CountAsync();
            Console.WriteLine($"Current user count: {userCount}");
            
            if (userCount > 0)
            {
                Console.WriteLine("Users already exist, skipping seeding");
                return;
            }

            // Check for Administrator role
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
            Console.WriteLine($"Administrator role found: {adminRole != null}");
            
            if (adminRole == null)
            {
                Console.WriteLine("Administrator role not found, checking all roles...");
                var allRoles = await context.Roles.ToListAsync();
                Console.WriteLine($"Available roles: {string.Join(", ", allRoles.Select(r => r.Name))}");
                
                // Use the first available role or create a temporary one
                if (allRoles.Any())
                {
                    adminRole = allRoles.First();
                    Console.WriteLine($"Using role: {adminRole.Name}");
                }
                else
                {
                    Console.WriteLine("No roles found, this might be a database seeding issue");
                    return;
                }
            }

            Console.WriteLine("Creating admin user...");
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = AuthenticationService.HashPassword("admin123"),
                FullName = "System Administrator",
                Email = "admin@maritime-erp.com",
                RoleId = adminRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            var changes = await context.SaveChangesAsync();
            Console.WriteLine($"Admin user created successfully. Changes saved: {changes}");
        }

        private async Task ShowLoginWindowAsync()
        {
            var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
            
            // The LoginWindow already has its LoginViewModel injected via constructor
            // Get the same instance that's being used by the window
            var loginViewModel = (LoginViewModel)loginWindow.DataContext;
            
            // Subscribe to login success event
            loginViewModel.LoginSuccess += async (sender, args) =>
            {
                Console.WriteLine("LoginSuccess event triggered in App.xaml.cs");
                loginWindow.Hide();
                await ShowMainWindowAsync();
            };

            loginWindow.Show();
        }

        private async Task ShowMainWindowAsync()
        {
            try
            {
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                var authService = _host.Services.GetRequiredService<IAuthenticationService>();
                
                if (authService.CurrentUser == null)
                {
                    await ShowLoginWindowAsync();
                    return;
                }

                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error in ShowMainWindowAsync:\n\nType: {ex.GetType().Name}\nMessage: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                MessageBox.Show(errorMessage, "Main Window Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Return to login window
                await ShowLoginWindowAsync();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }
    }
} 