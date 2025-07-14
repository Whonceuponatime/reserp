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
        
        public IServiceProvider ServiceProvider => _host.Services;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        private static bool _debugMode = false;

        public App()
        {
            try
            {
                // Check for debug mode - either DEBUG build or --debug argument
                var args = Environment.GetCommandLineArgs();
                _debugMode = args.Contains("--debug") || args.Contains("-d");

#if DEBUG
                _debugMode = true;
#endif

                if (_debugMode)
                {
                    try
                    {
                        // Allocate console for debugging
                        AllocConsole();
                        Console.Title = "SEACURE(CARE) Debug Console";
                        Console.WriteLine("=================================================");
                        Console.WriteLine("SEACURE(CARE) Maritime ERP - Debug Mode");
                        Console.WriteLine("=================================================");
                        Console.WriteLine($"Application started at: {DateTime.Now}");
                        Console.WriteLine($"Command line args: {string.Join(" ", args)}");
                        Console.WriteLine($"Working directory: {Environment.CurrentDirectory}");
                        Console.WriteLine($"Application directory: {AppDomain.CurrentDomain.BaseDirectory}");
                        Console.WriteLine($"User data directory: {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\SEACURE(CARE)");
                        Console.WriteLine("=================================================");
                    }
                    catch (Exception consoleEx)
                    {
                        // If console setup fails, continue without debug console
                        _debugMode = false;
                        MessageBox.Show($"Debug console setup failed: {consoleEx.Message}\nContinuing without debug console.", "Debug Setup Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                
                if (_debugMode)
                {
                    Console.WriteLine("\n[CONSTRUCTOR] Starting host creation...");
                }
                
                try
                {
                    _host = Host.CreateDefaultBuilder()
                        .ConfigureAppConfiguration((context, config) =>
                        {
                        // Configuration hierarchy (highest priority first):
                        // 1. User data directory (for installed application)
                        // 2. Application directory (for portable/dev)
                        // 3. Source directory (for development)
                        
                        var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
                        var userDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SEACURE(CARE)");
                        
                        // Start with app directory
                        config.SetBasePath(appDirectory);
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                        
                        // Check for user-specific configuration (highest priority)
                        var userConfigPath = Path.Combine(userDataDirectory, "appsettings.json");
                        if (File.Exists(userConfigPath))
                        {
                            try
                            {
                                if (_debugMode)
                                {
                                    Console.WriteLine($"[CONFIG] Found user config at: {userConfigPath}");
                                    Console.WriteLine($"[CONFIG] Validating JSON format...");
                                }
                                
                                // Validate JSON before adding to configuration
                                var jsonContent = File.ReadAllText(userConfigPath);
                                using (var doc = System.Text.Json.JsonDocument.Parse(jsonContent))
                                {
                                    // JSON is valid, proceed with loading
                                    if (_debugMode)
                                    {
                                        Console.WriteLine($"[CONFIG] JSON validation successful, attempting to load user configuration...");
                                    }
                                }
                                
#if DEBUG
                                Console.WriteLine($"Found user config at: {userConfigPath}");
#endif
                                config.AddJsonFile(userConfigPath, optional: false, reloadOnChange: true);
                                
                                if (_debugMode)
                                {
                                    Console.WriteLine($"[CONFIG] User configuration loaded successfully");
                                }
                            }
                            catch (Exception configEx)
                            {
                                if (_debugMode)
                                {
                                    Console.WriteLine($"[CONFIG ERROR] Failed to load user config: {configEx.Message}");
                                    Console.WriteLine($"[CONFIG ERROR] Renaming corrupted config file and using defaults...");
                                }
                                
                                // Rename the corrupted file so it doesn't interfere again
                                try
                                {
                                    var backupPath = userConfigPath + $".corrupted.{DateTime.Now:yyyyMMdd_HHmmss}";
                                    File.Move(userConfigPath, backupPath);
                                    
                                    if (_debugMode)
                                    {
                                        Console.WriteLine($"[CONFIG] Corrupted config moved to: {backupPath}");
                                    }
                                }
                                catch (Exception moveEx)
                                {
                                    if (_debugMode)
                                    {
                                        Console.WriteLine($"[CONFIG ERROR] Could not move corrupted config: {moveEx.Message}");
                                    }
                                }
                                
                                // Continue with fallback configuration
                            }
                        }
                        else
                        {
#if DEBUG
                            Console.WriteLine($"No user config found at: {userConfigPath}");
#endif
                            
                            // Check source directory for development
                            var sourceConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "MaritimeERP.Desktop", "appsettings.json");
                            if (File.Exists(sourceConfigPath))
                            {
                                try
                                {
                                    if (_debugMode)
                                    {
                                        Console.WriteLine($"[CONFIG] Found source config at: {sourceConfigPath}");
                                        Console.WriteLine($"[CONFIG] Attempting to load source configuration...");
                                    }
#if DEBUG
                                    Console.WriteLine($"Found source config at: {sourceConfigPath}");
#endif
                                    config.AddJsonFile(sourceConfigPath, optional: false, reloadOnChange: true);
                                    
                                    if (_debugMode)
                                    {
                                        Console.WriteLine($"[CONFIG] Source configuration loaded successfully");
                                    }
                                }
                                catch (Exception sourceConfigEx)
                                {
                                    if (_debugMode)
                                    {
                                        Console.WriteLine($"[CONFIG ERROR] Failed to load source config: {sourceConfigEx.Message}");
                                        Console.WriteLine($"[CONFIG ERROR] Falling back to default configuration...");
                                    }
                                    
                                    // Continue with fallback configuration
                                }
                            }
                            else
                            {
                                if (_debugMode)
                                {
                                    Console.WriteLine($"[CONFIG] No source config found at: {sourceConfigPath}");
                                }
#if DEBUG
                                Console.WriteLine($"No source config found at: {sourceConfigPath}");
#endif
                            }
                        }
                        
                        // Always add default configuration as fallback
                        if (_debugMode)
                        {
                            Console.WriteLine($"[CONFIG] Adding default configuration as fallback...");
                        }
                        
                        var defaultDbPath = Path.Combine(userDataDirectory, "Database", "maritime_erp.db");
                        var defaultDocPath = Path.Combine(userDataDirectory, "Documents");
                        
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = $"Data Source={defaultDbPath}",
                            ["Application:DocumentStoragePath"] = defaultDocPath,
                            ["Application:Name"] = "SEACURE(CARE)",
                            ["Application:Version"] = "1.0.0",
                            ["Application:CompanyName"] = "Maritime Solutions"
                        });
                        
                        if (_debugMode)
                        {
                            Console.WriteLine($"[CONFIG] Default database path: {defaultDbPath}");
                            Console.WriteLine($"[CONFIG] Default document path: {defaultDocPath}");
                            Console.WriteLine($"[CONFIG] Default configuration added successfully");
                        }
                        
#if DEBUG
                        Console.WriteLine($"Using default database path: {defaultDbPath}");
                        Console.WriteLine($"Using default document path: {defaultDocPath}");
#endif
                    })
                    .ConfigureServices((context, services) =>
                    {
                        services.AddLogging(builder =>
                        {
#if DEBUG
                            builder.AddConsole();
                            builder.AddDebug();
#else
                            // Only use debug logging in release builds (no console)
                            builder.AddDebug();
#endif
                        });
                        ConfigureServices(services, context.Configuration);
                    })
                    .Build();
                    
                    if (_debugMode)
                    {
                        Console.WriteLine("[CONSTRUCTOR] Host created successfully");
                        Console.WriteLine($"[CONSTRUCTOR] _host is null: {_host == null}");
                    }
                }
                catch (Exception hostEx)
                {
                    if (_debugMode)
                    {
                        Console.WriteLine($"\n[CONSTRUCTOR ERROR] Host creation failed!");
                        Console.WriteLine($"[CONSTRUCTOR ERROR] Exception: {hostEx.GetType().Name}");
                        Console.WriteLine($"[CONSTRUCTOR ERROR] Message: {hostEx.Message}");
                        Console.WriteLine($"[CONSTRUCTOR ERROR] Stack trace:\n{hostEx.StackTrace}");
                        if (hostEx.InnerException != null)
                        {
                            Console.WriteLine($"[CONSTRUCTOR ERROR] Inner exception: {hostEx.InnerException.Message}");
                        }
                    }
                    throw; // Re-throw to maintain original error flow
                }
                
#if DEBUG
                Console.WriteLine("Host created successfully");
#endif
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    Console.WriteLine($"\n[CONSTRUCTOR ERROR] Exception in App constructor!");
                    Console.WriteLine($"[CONSTRUCTOR ERROR] Exception: {ex.GetType().Name}");
                    Console.WriteLine($"[CONSTRUCTOR ERROR] Message: {ex.Message}");
                    Console.WriteLine($"[CONSTRUCTOR ERROR] Stack trace:\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[CONSTRUCTOR ERROR] Inner exception: {ex.InnerException.Message}");
                    }
                }
                
#if DEBUG
                Console.WriteLine($"Error in App constructor: {ex}");
#endif
                MessageBox.Show($"Error in App constructor: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            if (_debugMode)
            {
                Console.WriteLine("[CONSTRUCTOR] Getting logger service...");
            }
            
            try
            {
                _logger = _host.Services.GetRequiredService<ILogger<App>>();
                
                if (_debugMode)
                {
                    Console.WriteLine("[CONSTRUCTOR] Logger service obtained successfully");
                    Console.WriteLine($"[CONSTRUCTOR] _logger is null: {_logger == null}");
                    Console.WriteLine("[CONSTRUCTOR] App constructor completed successfully");
                }
            }
            catch (Exception loggerEx)
            {
                if (_debugMode)
                {
                    Console.WriteLine($"\n[CONSTRUCTOR ERROR] Logger service creation failed!");
                    Console.WriteLine($"[CONSTRUCTOR ERROR] Exception: {loggerEx.GetType().Name}");
                    Console.WriteLine($"[CONSTRUCTOR ERROR] Message: {loggerEx.Message}");
                    Console.WriteLine($"[CONSTRUCTOR ERROR] Stack trace:\n{loggerEx.StackTrace}");
                    if (loggerEx.InnerException != null)
                    {
                        Console.WriteLine($"[CONSTRUCTOR ERROR] Inner exception: {loggerEx.InnerException.Message}");
                    }
                }
                throw; // Re-throw to maintain original error flow
            }
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
#if DEBUG
            Console.WriteLine("Configuring services...");
#endif
            
            // Database - Use SQLite for development with improved connection handling
            services.AddDbContext<MaritimeERPContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                if (connectionString?.Contains("Host=") == true)
                {
                    options.UseNpgsql(connectionString);
                }
                else
                {
                    var sqliteConnectionString = connectionString ?? "Data Source=maritime_erp.db";
                    
                    // Ensure database directory exists
                    if (sqliteConnectionString.StartsWith("Data Source="))
                    {
                        var dbPath = sqliteConnectionString.Substring("Data Source=".Length).Split(';')[0];
                        var dbDirectory = Path.GetDirectoryName(dbPath);
                        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
                        {
#if DEBUG
                            Console.WriteLine($"Creating database directory: {dbDirectory}");
#endif
                            Directory.CreateDirectory(dbDirectory);
                        }
                    }
                    
                    // Add SQLite-specific options to prevent locking
                    if (!sqliteConnectionString.Contains("Cache="))
                    {
                        sqliteConnectionString += ";Cache=Shared;";
                    }
                    if (!sqliteConnectionString.Contains("Mode="))
                    {
                        sqliteConnectionString += "Mode=ReadWriteCreate;";
                    }
                    if (!sqliteConnectionString.Contains("Pooling="))
                    {
                        sqliteConnectionString += "Pooling=true;";
                    }
                    
#if DEBUG
                    Console.WriteLine($"Using SQLite connection: {sqliteConnectionString}");
#endif
                    options.UseSqlite(sqliteConnectionString);
                    options.EnableSensitiveDataLogging(false);
                    options.EnableServiceProviderCaching(false);
                }
            }, ServiceLifetime.Transient);

            // Register services - AuthenticationService as Singleton to maintain user state
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<MaritimeERP.Core.Interfaces.IDataChangeNotificationService, DataChangeNotificationService>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IAuditLogService, AuditLogService>();
            services.AddTransient<IShipService, ShipService>();
            services.AddTransient<ISystemService, SystemService>();
            services.AddTransient<IComponentService, ComponentService>();
            services.AddTransient<ISoftwareService, SoftwareService>();
            services.AddTransient<IChangeRequestService, ChangeRequestService>();
            services.AddTransient<ISystemChangePlanService, SystemChangePlanService>();
            services.AddTransient<IHardwareChangeRequestService, HardwareChangeRequestService>();
            services.AddTransient<ISoftwareChangeRequestService, SoftwareChangeRequestService>();
            services.AddTransient<ISecurityReviewStatementService, SecurityReviewStatementService>();
            services.AddTransient<IDocumentService, DocumentService>();
            services.AddTransient<ILoginLogService, LoginLogService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ViewLocator>();
            services.AddSingleton<IDataCacheService, DataCacheService>();
            // Add other services here as they are implemented

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ShipsViewModel>();
            services.AddTransient<ShipDetailsViewModel>();
            services.AddTransient<SystemsViewModel>();
            services.AddTransient<ComponentsViewModel>();
            services.AddTransient<SoftwareViewModel>();
            services.AddTransient<ChangeRequestsViewModel>();
            services.AddTransient<SecurityReviewStatementViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<AuditLogsViewModel>();
            services.AddTransient<DocumentsViewModel>();
            services.AddTransient<LoginLogsViewModel>();
            services.AddTransient<HardwareChangeRequestDialogViewModel>();
            services.AddTransient<SoftwareChangeRequestDialogViewModel>();
            services.AddTransient<SystemChangePlanDialogViewModel>();
            services.AddTransient<SecurityReviewStatementDialogViewModel>();

            // Views
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            
#if DEBUG
            Console.WriteLine("Services configured successfully");
#endif
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                if (_debugMode)
                {
                    Console.WriteLine("\n[STARTUP] Starting application host...");
                    Console.WriteLine($"[STARTUP] _host is null: {_host == null}");
                    Console.WriteLine($"[STARTUP] _logger is null: {_logger == null}");
                    Console.WriteLine($"[STARTUP] About to call _host.StartAsync()...");
                }

                // Check if host is null before proceeding
                if (_host == null)
                {
                    throw new InvalidOperationException("Host is null - application cannot start");
                }

                // Start the host asynchronously
                await _host.StartAsync();
                
                if (_debugMode)
                {
                    Console.WriteLine("[STARTUP] Host started successfully");
                    Console.WriteLine("[STARTUP] Initializing database...");
                }
                    
                // Initialize database asynchronously
                if (_debugMode)
                {
                    Console.WriteLine("[STARTUP] About to initialize database...");
                }
                
                await InitializeDatabaseAsync();
                
                if (_debugMode)
                {
                    Console.WriteLine("[STARTUP] Database initialized successfully");
                    Console.WriteLine("[STARTUP] Showing login window...");
                }
                    
                // Show login window
                if (_debugMode)
                {
                    Console.WriteLine("[STARTUP] About to show login window...");
                }
                
                ShowLoginWindow();

                if (_debugMode)
                {
                    Console.WriteLine("[STARTUP] Application startup completed successfully");
                    Console.WriteLine("Press Ctrl+C to close debug console (won't close app)");
                    Console.WriteLine("=================================================\n");
                }

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    Console.WriteLine($"\n[ERROR] Application startup failed!");
                    Console.WriteLine($"[ERROR] Exception: {ex.GetType().Name}");
                    Console.WriteLine($"[ERROR] Message: {ex.Message}");
                    Console.WriteLine($"[ERROR] Stack trace:\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[ERROR] Inner exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=================================================");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }

                _logger?.LogError(ex, "Error during application startup");
                MessageBox.Show($"Error during application startup: {ex.Message}\n\nSee debug console for details.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private void ShowLoginWindow()
        {
            try
            {
                if (_debugMode)
                {
                    Console.WriteLine("[SHOW_LOGIN] Getting authentication service...");
                    Console.WriteLine($"[SHOW_LOGIN] _host is null: {_host == null}");
                    Console.WriteLine($"[SHOW_LOGIN] _host.Services is null: {_host?.Services == null}");
                }
                
                var authService = _host.Services.GetRequiredService<IAuthenticationService>();
                
                if (_debugMode)
                {
                    Console.WriteLine("[SHOW_LOGIN] Authentication service obtained successfully");
                    Console.WriteLine($"[SHOW_LOGIN] authService is null: {authService == null}");
                    Console.WriteLine("[SHOW_LOGIN] Creating login view model...");
                }
                var loginViewModel = new LoginViewModel(authService);
                
                if (_debugMode)
                {
                    Console.WriteLine("[SHOW_LOGIN] Login view model created successfully");
                    Console.WriteLine($"[SHOW_LOGIN] loginViewModel is null: {loginViewModel == null}");
                    Console.WriteLine("[SHOW_LOGIN] Creating login window...");
                }
                
                var loginWindow = new LoginWindow(loginViewModel);
                
                if (_debugMode)
                {
                    Console.WriteLine("[SHOW_LOGIN] Login window created successfully");
                    Console.WriteLine($"[SHOW_LOGIN] loginWindow is null: {loginWindow == null}");
                    Console.WriteLine("[SHOW_LOGIN] Setting up login success event handler...");
                }
                
                loginViewModel.LoginSuccess += async (s, _) => 
                {
                    try
                    {
                        // Create main window on UI thread
                        await Dispatcher.InvokeAsync(() =>
                {
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                            
                            // Refresh navigation after successful login to ensure admin menus appear
                            if (mainWindow.DataContext is MainViewModel mainViewModel)
                            {
                                mainViewModel.RefreshNavigationAfterLogin();
                            }
                            
                    mainWindow.Show();
                    loginWindow.Close();
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error showing main window after login");
                        MessageBox.Show($"Error opening main window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                
                if (_debugMode)
                {
                    Console.WriteLine("[SHOW_LOGIN] Event handler set up successfully");
                    Console.WriteLine("[SHOW_LOGIN] About to show login dialog...");
                }
                
                loginWindow.ShowDialog();
                
                if (_debugMode)
                {
                    Console.WriteLine("[SHOW_LOGIN] Login dialog closed");
                }
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    Console.WriteLine($"\n[SHOW_LOGIN ERROR] Exception in ShowLoginWindow!");
                    Console.WriteLine($"[SHOW_LOGIN ERROR] Exception: {ex.GetType().Name}");
                    Console.WriteLine($"[SHOW_LOGIN ERROR] Message: {ex.Message}");
                    Console.WriteLine($"[SHOW_LOGIN ERROR] Stack trace:\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[SHOW_LOGIN ERROR] Inner exception: {ex.InnerException.Message}");
                    }
                }
                
                _logger.LogError(ex, "Error showing login window");
                MessageBox.Show($"Error showing login window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                if (_debugMode)
                {
                    Console.WriteLine("[DATABASE] Initializing database...");
                }
                
                using var scope = _host.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
                
                if (_debugMode)
                {
                    Console.WriteLine($"[DATABASE] Database connection string: {context.Database.GetDbConnection().ConnectionString}");
                    Console.WriteLine("[DATABASE] Setting command timeout to 30 seconds...");
                }
                
                // Set command timeout to prevent hanging
                context.Database.SetCommandTimeout(30);
                
                if (_debugMode)
                {
                    Console.WriteLine("[DATABASE] Ensuring database is created...");
                }
                
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();
                
                if (_debugMode)
                {
                    Console.WriteLine("[DATABASE] Database creation completed successfully");
                }
                
                // Check if HardwareChangeRequests table exists, if not create it
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='HardwareChangeRequests'";
                var result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    if (_debugMode)
                    {
                        Console.WriteLine("[DATABASE] Creating HardwareChangeRequests table...");
                    }
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE HardwareChangeRequests (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            RequestNumber TEXT NOT NULL UNIQUE,
                            CreatedDate DATETIME NOT NULL,
                            RequesterUserId INTEGER NOT NULL,
                            ShipId INTEGER,
                            Department TEXT,
                            PositionTitle TEXT,
                            RequesterName TEXT,
                            InstalledCbs TEXT,
                            InstalledComponent TEXT,
                            Reason TEXT,
                            BeforeHwManufacturerModel TEXT,
                            BeforeHwName TEXT,
                            BeforeHwOs TEXT,
                            AfterHwManufacturerModel TEXT,
                            AfterHwName TEXT,
                            AfterHwOs TEXT,
                            WorkDescription TEXT,
                            SecurityReviewComment TEXT,
                            PreparedByUserId INTEGER,
                            ReviewedByUserId INTEGER,
                            ApprovedByUserId INTEGER,
                            PreparedAt DATETIME,
                            ReviewedAt DATETIME,
                            ApprovedAt DATETIME,
                            Status TEXT NOT NULL DEFAULT 'Draft',
                            FOREIGN KEY (RequesterUserId) REFERENCES Users(Id),
                            FOREIGN KEY (PreparedByUserId) REFERENCES Users(Id),
                            FOREIGN KEY (ReviewedByUserId) REFERENCES Users(Id),
                            FOREIGN KEY (ApprovedByUserId) REFERENCES Users(Id),
                            FOREIGN KEY (ShipId) REFERENCES Ships(Id) ON DELETE SET NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_HardwareChangeRequests_RequestNumber ON HardwareChangeRequests(RequestNumber);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_HardwareChangeRequests_RequesterUserId ON HardwareChangeRequests(RequesterUserId);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_HardwareChangeRequests_Status ON HardwareChangeRequests(Status);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_HardwareChangeRequests_ShipId ON HardwareChangeRequests(ShipId);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("HardwareChangeRequests table created successfully");
                }
                else
                {
                    Console.WriteLine("HardwareChangeRequests table already exists");
                    
                    // Check if ShipId column exists, if not add it
                    command.CommandText = "PRAGMA table_info(HardwareChangeRequests)";
                    using var hwReader = await command.ExecuteReaderAsync();
                    var hwColumns = new List<string>();
                    while (await hwReader.ReadAsync())
                    {
                        hwColumns.Add(hwReader.GetString(1)); // Column name is at index 1
                    }
                    await hwReader.CloseAsync();
                    
                    if (!hwColumns.Contains("ShipId"))
                    {
                        Console.WriteLine("Adding ShipId column to HardwareChangeRequests table...");
                        command.CommandText = "ALTER TABLE HardwareChangeRequests ADD COLUMN ShipId INTEGER REFERENCES Ships(Id) ON DELETE SET NULL;";
                        await command.ExecuteNonQueryAsync();
                        
                        command.CommandText = "CREATE INDEX IX_HardwareChangeRequests_ShipId ON HardwareChangeRequests(ShipId);";
                        await command.ExecuteNonQueryAsync();
                        
                        Console.WriteLine("ShipId column added successfully to HardwareChangeRequests");
                    }
                }
                
                // Check if SoftwareChangeRequests table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SoftwareChangeRequests'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating SoftwareChangeRequests table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE SoftwareChangeRequests (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            RequestNumber TEXT NOT NULL UNIQUE,
                            CreatedDate DATETIME NOT NULL,
                            RequesterUserId INTEGER NOT NULL,
                            ShipId INTEGER,
                            Department TEXT,
                            PositionTitle TEXT,
                            RequesterName TEXT,
                            InstalledCbs TEXT,
                            InstalledComponent TEXT,
                            Reason TEXT,
                            BeforeSwManufacturer TEXT,
                            BeforeSwName TEXT,
                            BeforeSwVersion TEXT,
                            AfterSwManufacturer TEXT,
                            AfterSwName TEXT,
                            AfterSwVersion TEXT,
                            WorkDescription TEXT,
                            SecurityReviewComment TEXT,
                            PreparedByUserId INTEGER,
                            ReviewedByUserId INTEGER,
                            ApprovedByUserId INTEGER,
                            PreparedAt DATETIME,
                            ReviewedAt DATETIME,
                            ApprovedAt DATETIME,
                            Status TEXT NOT NULL DEFAULT 'Draft',
                            FOREIGN KEY (RequesterUserId) REFERENCES Users(Id),
                            FOREIGN KEY (PreparedByUserId) REFERENCES Users(Id),
                            FOREIGN KEY (ReviewedByUserId) REFERENCES Users(Id),
                            FOREIGN KEY (ApprovedByUserId) REFERENCES Users(Id),
                            FOREIGN KEY (ShipId) REFERENCES Ships(Id) ON DELETE SET NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_SoftwareChangeRequests_RequestNumber ON SoftwareChangeRequests(RequestNumber);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_SoftwareChangeRequests_RequesterUserId ON SoftwareChangeRequests(RequesterUserId);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_SoftwareChangeRequests_Status ON SoftwareChangeRequests(Status);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_SoftwareChangeRequests_ShipId ON SoftwareChangeRequests(ShipId);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("SoftwareChangeRequests table created successfully");
                }
                else
                {
                    Console.WriteLine("SoftwareChangeRequests table already exists");
                    
                    // Check if ShipId column exists, if not add it
                    command.CommandText = "PRAGMA table_info(SoftwareChangeRequests)";
                    using var swReader = await command.ExecuteReaderAsync();
                    var swColumns = new List<string>();
                    while (await swReader.ReadAsync())
                    {
                        swColumns.Add(swReader.GetString(1)); // Column name is at index 1
                    }
                    await swReader.CloseAsync();
                    
                    if (!swColumns.Contains("ShipId"))
                    {
                        Console.WriteLine("Adding ShipId column to SoftwareChangeRequests table...");
                        command.CommandText = "ALTER TABLE SoftwareChangeRequests ADD COLUMN ShipId INTEGER REFERENCES Ships(Id) ON DELETE SET NULL;";
                        await command.ExecuteNonQueryAsync();
                        
                        command.CommandText = "CREATE INDEX IX_SoftwareChangeRequests_ShipId ON SoftwareChangeRequests(ShipId);";
                        await command.ExecuteNonQueryAsync();
                        
                        Console.WriteLine("ShipId column added successfully to SoftwareChangeRequests");
                    }
                }
                
                // Check if SystemChangePlans table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SystemChangePlans'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating SystemChangePlans table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE SystemChangePlans (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            RequestNumber TEXT NOT NULL UNIQUE,
                            CreatedDate DATETIME NOT NULL,
                            ShipId INTEGER,
                            IsCreated BOOLEAN NOT NULL DEFAULT 1,
                            IsUnderReview BOOLEAN NOT NULL DEFAULT 0,
                            IsApproved BOOLEAN NOT NULL DEFAULT 0,
                            Department TEXT NOT NULL DEFAULT '',
                            PositionTitle TEXT NOT NULL DEFAULT '',
                            RequesterName TEXT NOT NULL DEFAULT '',
                            InstalledCbs TEXT NOT NULL DEFAULT '',
                            InstalledComponent TEXT NOT NULL DEFAULT '',
                            Reason TEXT NOT NULL DEFAULT '',
                            BeforeManufacturerModel TEXT NOT NULL DEFAULT '',
                            BeforeHwSwName TEXT NOT NULL DEFAULT '',
                            BeforeVersion TEXT NOT NULL DEFAULT '',
                            AfterManufacturerModel TEXT NOT NULL DEFAULT '',
                            AfterHwSwName TEXT NOT NULL DEFAULT '',
                            AfterVersion TEXT NOT NULL DEFAULT '',
                            PlanDetails TEXT NOT NULL DEFAULT '',
                            SecurityReviewComments TEXT NOT NULL DEFAULT '',
                            UserId INTEGER,
                            UpdatedDate DATETIME NOT NULL,
                            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
                            FOREIGN KEY (ShipId) REFERENCES Ships(Id) ON DELETE SET NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_SystemChangePlans_RequestNumber ON SystemChangePlans(RequestNumber);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_SystemChangePlans_UserId ON SystemChangePlans(UserId);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_SystemChangePlans_ShipId ON SystemChangePlans(ShipId);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("SystemChangePlans table created successfully");
                }
                else
                {
                    Console.WriteLine("SystemChangePlans table already exists");
                    
                    // Check if ShipId column exists, if not add it
                    command.CommandText = "PRAGMA table_info(SystemChangePlans)";
                    using var sysReader = await command.ExecuteReaderAsync();
                    var sysColumns = new List<string>();
                    while (await sysReader.ReadAsync())
                    {
                        sysColumns.Add(sysReader.GetString(1)); // Column name is at index 1
                    }
                    await sysReader.CloseAsync();
                    
                    if (!sysColumns.Contains("ShipId"))
                    {
                        Console.WriteLine("Adding ShipId column to SystemChangePlans table...");
                        command.CommandText = "ALTER TABLE SystemChangePlans ADD COLUMN ShipId INTEGER REFERENCES Ships(Id) ON DELETE SET NULL;";
                        await command.ExecuteNonQueryAsync();
                        
                        command.CommandText = "CREATE INDEX IX_SystemChangePlans_ShipId ON SystemChangePlans(ShipId);";
                        await command.ExecuteNonQueryAsync();
                        
                        Console.WriteLine("ShipId column added successfully");
                    }
                }
                
                // Check if SecurityReviewStatements table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SecurityReviewStatements'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating SecurityReviewStatements table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE SecurityReviewStatements (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            RequestNumber TEXT NOT NULL UNIQUE,
                            CreatedDate DATETIME NOT NULL,
                            ShipId INTEGER,
                            IsCreated BOOLEAN NOT NULL DEFAULT 1,
                            IsUnderReview BOOLEAN NOT NULL DEFAULT 0,
                            IsApproved BOOLEAN NOT NULL DEFAULT 0,
                            ReviewDate DATETIME NOT NULL,
                            ReviewerDepartment TEXT NOT NULL DEFAULT '',
                            ReviewerPosition TEXT NOT NULL DEFAULT '',
                            ReviewerName TEXT NOT NULL DEFAULT '',
                            ReviewItems TEXT NOT NULL DEFAULT '',
                            ReviewResults TEXT NOT NULL DEFAULT '',
                            ReviewNotes TEXT NOT NULL DEFAULT '',
                            OverallResult TEXT NOT NULL DEFAULT '',
                            ReviewOpinion TEXT NOT NULL DEFAULT '',
                            UserId INTEGER,
                            UpdatedDate DATETIME NOT NULL,
                            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
                            FOREIGN KEY (ShipId) REFERENCES Ships(Id) ON DELETE SET NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_SecurityReviewStatements_RequestNumber ON SecurityReviewStatements(RequestNumber);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_SecurityReviewStatements_UserId ON SecurityReviewStatements(UserId);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_SecurityReviewStatements_ShipId ON SecurityReviewStatements(ShipId);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("SecurityReviewStatements table created successfully");
                }
                else
                {
                    Console.WriteLine("SecurityReviewStatements table already exists");
                    
                    // Check if ShipId column exists, if not add it
                    command.CommandText = "PRAGMA table_info(SecurityReviewStatements)";
                    using var secReader = await command.ExecuteReaderAsync();
                    var secColumns = new List<string>();
                    while (await secReader.ReadAsync())
                    {
                        secColumns.Add(secReader.GetString(1)); // Column name is at index 1
                    }
                    await secReader.CloseAsync();
                    
                    if (!secColumns.Contains("ShipId"))
                    {
                        Console.WriteLine("Adding ShipId column to SecurityReviewStatements table...");
                        command.CommandText = "ALTER TABLE SecurityReviewStatements ADD COLUMN ShipId INTEGER REFERENCES Ships(Id) ON DELETE SET NULL;";
                        await command.ExecuteNonQueryAsync();
                        
                        command.CommandText = "CREATE INDEX IX_SecurityReviewStatements_ShipId ON SecurityReviewStatements(ShipId);";
                        await command.ExecuteNonQueryAsync();
                        
                        Console.WriteLine("ShipId column added successfully to SecurityReviewStatements");
                    }
                }
                
                // Check if AuditLogs table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='AuditLogs'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating AuditLogs table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE AuditLogs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            EntityType TEXT NOT NULL,
                            Action TEXT NOT NULL,
                            EntityId TEXT NOT NULL,
                            EntityName TEXT,
                            TableName TEXT,
                            OldValues TEXT,
                            NewValues TEXT,
                            Timestamp DATETIME NOT NULL,
                            UserId INTEGER,
                            UserName TEXT,
                            IpAddress TEXT,
                            UserAgent TEXT,
                            AdditionalInfo TEXT,
                            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_AuditLogs_EntityType ON AuditLogs(EntityType);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("AuditLogs table created successfully");
                }
                else
                {
                    Console.WriteLine("AuditLogs table already exists");
                }
                
                // Check if DocumentCategories table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='DocumentCategories'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating DocumentCategories table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE DocumentCategories (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Description TEXT,
                            Category TEXT NOT NULL,
                            IsRequired BOOLEAN NOT NULL DEFAULT 0,
                            AllowedFileTypes TEXT DEFAULT 'pdf,doc,docx',
                            MaxFileSizeBytes INTEGER DEFAULT 52428800,
                            IsActive BOOLEAN NOT NULL DEFAULT 1,
                            DisplayOrder INTEGER NOT NULL DEFAULT 0,
                            CreatedAt DATETIME NOT NULL,
                            UpdatedAt DATETIME
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_DocumentCategories_Category ON DocumentCategories(Category);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_DocumentCategories_DisplayOrder ON DocumentCategories(DisplayOrder);";
                    await command.ExecuteNonQueryAsync();
                    
                    // Insert seed data
                    var seedData = new[]
                    {
                        (1, "Zones and Conduit Diagram", "Detailed diagrams showing vessel zones and conduit layouts for cyber security planning", "Approved Supplier Documentation", 1, "pdf,dwg,dxf,png,jpg,jpeg", 104857600, 1, 1),
                        (2, "Cyber Security Design Description", "Comprehensive document describing the cyber security design and architecture", "Approved Supplier Documentation", 1, "pdf,doc,docx", 52428800, 1, 2),
                        (3, "Vessel Asset Inventory", "Complete inventory of all vessel assets including IT and OT systems", "Approved Supplier Documentation", 1, "pdf,doc,docx,xlsx,csv", 26214400, 1, 3),
                        (4, "Risk Assessment for Exclusion of CBSs", "Risk assessment documentation for the exclusion of Critical Business Systems", "Approved Supplier Documentation", 1, "pdf,doc,docx", 52428800, 1, 4),
                        (5, "Description of Compensating Countermeasures", "Documentation describing compensating countermeasures for identified security gaps", "Approved Supplier Documentation", 1, "pdf,doc,docx", 52428800, 1, 5),
                        (6, "Ship Cyber Resilience Test Procedure", "Detailed procedures for testing ship cyber resilience and security measures", "Approved Supplier Documentation", 1, "pdf,doc,docx", 52428800, 1, 6)
                    };
                    
                    foreach (var (id, name, description, category, isRequired, allowedFileTypes, maxFileSize, isActive, displayOrder) in seedData)
                    {
                        command.CommandText = $@"
                            INSERT OR IGNORE INTO DocumentCategories 
                            (Id, Name, Description, Category, IsRequired, AllowedFileTypes, MaxFileSizeBytes, IsActive, DisplayOrder, CreatedAt) 
                            VALUES ({id}, '{name}', '{description}', '{category}', {isRequired}, '{allowedFileTypes}', {maxFileSize}, {isActive}, {displayOrder}, datetime('now'))";
                        await command.ExecuteNonQueryAsync();
                    }
                    
                    Console.WriteLine("DocumentCategories table created successfully");
                }
                else
                {
                    Console.WriteLine("DocumentCategories table already exists");
                }
                
                // Check if Documents table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Documents'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating Documents table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE Documents (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Description TEXT,
                            FileName TEXT NOT NULL,
                            FileExtension TEXT NOT NULL,
                            FileSizeBytes INTEGER NOT NULL,
                            FileHash TEXT NOT NULL,
                            FilePath TEXT NOT NULL,
                            ContentType TEXT NOT NULL,
                            CategoryId INTEGER NOT NULL,
                            ShipId INTEGER,
                            UploadedByUserId INTEGER NOT NULL,
                            UploadedAt DATETIME NOT NULL,
                            UpdatedAt DATETIME,
                            IsActive BOOLEAN NOT NULL DEFAULT 1,
                            IsApproved BOOLEAN NOT NULL DEFAULT 0,
                            ApprovedByUserId INTEGER,
                            ApprovedAt DATETIME,
                            Comments TEXT,
                            Version INTEGER NOT NULL DEFAULT 1,
                            PreviousVersionId INTEGER,
                            FOREIGN KEY (CategoryId) REFERENCES DocumentCategories(Id) ON DELETE RESTRICT,
                            FOREIGN KEY (ShipId) REFERENCES Ships(Id) ON DELETE SET NULL,
                            FOREIGN KEY (UploadedByUserId) REFERENCES Users(Id) ON DELETE RESTRICT,
                            FOREIGN KEY (ApprovedByUserId) REFERENCES Users(Id) ON DELETE SET NULL,
                            FOREIGN KEY (PreviousVersionId) REFERENCES Documents(Id) ON DELETE SET NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_Documents_FileHash ON Documents(FileHash);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_Documents_UploadedAt ON Documents(UploadedAt);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_Documents_CategoryId ON Documents(CategoryId);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_Documents_ShipId ON Documents(ShipId);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("Documents table created successfully");
                }
                else
                {
                    Console.WriteLine("Documents table already exists");
                }
                
                // Check if DocumentVersions table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='DocumentVersions'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating DocumentVersions table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE DocumentVersions (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            DocumentId INTEGER NOT NULL,
                            VersionNumber INTEGER NOT NULL,
                            FileName TEXT NOT NULL,
                            FilePath TEXT NOT NULL,
                            FileSizeBytes INTEGER NOT NULL,
                            FileHash TEXT NOT NULL,
                            ContentType TEXT NOT NULL,
                            UploadedByUserId INTEGER NOT NULL,
                            UploadedAt DATETIME NOT NULL,
                            ChangeDescription TEXT,
                            IsActive BOOLEAN NOT NULL DEFAULT 1,
                            FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
                            FOREIGN KEY (UploadedByUserId) REFERENCES Users(Id) ON DELETE RESTRICT
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_DocumentVersions_DocumentId ON DocumentVersions(DocumentId);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_DocumentVersions_VersionNumber ON DocumentVersions(VersionNumber);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_DocumentVersions_UploadedAt ON DocumentVersions(UploadedAt);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("DocumentVersions table created successfully");
                }
                else
                {
                    Console.WriteLine("DocumentVersions table already exists");
                }
                
                // Check if LoginLogs table exists, if not create it
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='LoginLogs'";
                result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating LoginLogs table...");
                    
                    // Create the table manually
                    command.CommandText = @"
                        CREATE TABLE LoginLogs (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UserId INTEGER,
                            Username TEXT NOT NULL,
                            Action TEXT NOT NULL,
                            IsSuccessful BOOLEAN NOT NULL,
                            FailureReason TEXT,
                            IpAddress TEXT,
                            UserAgent TEXT,
                            Device TEXT,
                            Location TEXT,
                            Timestamp DATETIME NOT NULL,
                            AdditionalInfo TEXT,
                            SessionDurationMinutes INTEGER,
                            IsSecurityEvent BOOLEAN NOT NULL DEFAULT 0,
                            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
                        );";
                    await command.ExecuteNonQueryAsync();
                    
                    // Create indexes
                    command.CommandText = "CREATE INDEX IX_LoginLogs_Username ON LoginLogs(Username);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_LoginLogs_Action ON LoginLogs(Action);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_LoginLogs_Timestamp ON LoginLogs(Timestamp);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_LoginLogs_IsSuccessful ON LoginLogs(IsSuccessful);";
                    await command.ExecuteNonQueryAsync();
                    
                    command.CommandText = "CREATE INDEX IX_LoginLogs_IsSecurityEvent ON LoginLogs(IsSecurityEvent);";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("LoginLogs table created successfully");
                }
                else
                {
                    Console.WriteLine("LoginLogs table already exists");
                }
                
                // Clean up and ensure only correct roles exist (non-critical operation)
                try
                {
                    Console.WriteLine("Attempting role cleanup...");
                    
                    // Temporarily disable foreign key constraints for cleanup
                    command.CommandText = "PRAGMA foreign_keys = OFF";
                    await command.ExecuteNonQueryAsync();
                    
                    // Ensure the correct roles exist first
                    var validRoles = new[]
                    {
                        (1, "Administrator", "Full system access - can manage users, approve/reject forms, edit all data"),
                        (2, "Engineer", "Normal user - can submit forms and view data (read-only)")
                    };
                    
                    foreach (var (id, name, description) in validRoles)
                    {
                        command.CommandText = $"SELECT COUNT(*) FROM Roles WHERE Name = '{name}'";
                        var count = (long)(await command.ExecuteScalarAsync() ?? 0);
                        
                        if (count == 0)
                        {
                            Console.WriteLine($"Creating role: {name}");
                            command.CommandText = $"INSERT OR IGNORE INTO Roles (Id, Name, Description) VALUES ({id}, '{name}', '{description}')";
                            await command.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            Console.WriteLine($"Role '{name}' already exists");
                        }
                    }
                    
                    // Get current roles
                    command.CommandText = "SELECT Id, Name FROM Roles";
                    using var roleReader = await command.ExecuteReaderAsync();
                    var currentRoles = new List<(int Id, string Name)>();
                    while (await roleReader.ReadAsync())
                    {
                        currentRoles.Add((roleReader.GetInt32(0), roleReader.GetString(1)));
                    }
                    await roleReader.CloseAsync();
                    
                    Console.WriteLine($"Found {currentRoles.Count} roles in database");
                    
                    // Clean up invalid roles safely
                    var validRoleNames = validRoles.Select(r => r.Item2).ToArray();
                    foreach (var role in currentRoles)
                    {
                        if (!validRoleNames.Contains(role.Name))
                        {
                            Console.WriteLine($"Found invalid role: {role.Name} (ID: {role.Id})");
                            
                            // Check if any users are assigned to this role
                            command.CommandText = $"SELECT COUNT(*) FROM Users WHERE RoleId = {role.Id}";
                            var userCount = (long)(await command.ExecuteScalarAsync() ?? 0);
                            
                            if (userCount > 0)
                            {
                                Console.WriteLine($"Reassigning {userCount} users from invalid role '{role.Name}' to 'Engineer' role");
                                // Reassign users to Engineer role (ID: 2)
                                command.CommandText = $"UPDATE Users SET RoleId = 2 WHERE RoleId = {role.Id}";
                                await command.ExecuteNonQueryAsync();
                            }
                            
                            // Now safely delete the invalid role
                            Console.WriteLine($"Deleting invalid role: {role.Name}");
                            command.CommandText = $"DELETE FROM Roles WHERE Id = {role.Id}";
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    
                    // Re-enable foreign key constraints
                    command.CommandText = "PRAGMA foreign_keys = ON";
                    await command.ExecuteNonQueryAsync();
                    
                    Console.WriteLine("Role cleanup completed successfully");
                }
                catch (Exception roleEx)
                {
                    Console.WriteLine($"Role cleanup failed (non-critical): {roleEx.Message}");
                    
                    // Re-enable foreign key constraints even if cleanup failed
                    try
                    {
                        command.CommandText = "PRAGMA foreign_keys = ON";
                        await command.ExecuteNonQueryAsync();
                    }
                    catch
                    {
                        // Ignore errors when re-enabling constraints
                    }
                    
                    // Ensure minimum required roles exist even if cleanup failed
                    try
                    {
                        command.CommandText = "INSERT OR IGNORE INTO Roles (Id, Name, Description) VALUES (1, 'Administrator', 'Full system access'), (2, 'Engineer', 'Normal user')";
                        await command.ExecuteNonQueryAsync();
                        Console.WriteLine("Ensured minimum required roles exist");
                    }
                    catch
                    {
                        Console.WriteLine("Could not ensure minimum roles - application will continue with existing roles");
                    }
                }
                
                await connection.CloseAsync();
                
                if (_debugMode)
                {
                    Console.WriteLine("[DATABASE] Database initialization completed successfully");
                }
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    Console.WriteLine($"\n[DATABASE ERROR] Database initialization failed!");
                    Console.WriteLine($"[DATABASE ERROR] Exception: {ex.GetType().Name}");
                    Console.WriteLine($"[DATABASE ERROR] Message: {ex.Message}");
                    Console.WriteLine($"[DATABASE ERROR] Stack trace:\n{ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"[DATABASE ERROR] Inner exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("=================================================");
                }

                _logger?.LogError(ex, "Error initializing database");
                throw; // Re-throw to be caught by OnStartup
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_debugMode)
            {
                Console.WriteLine("\n[EXIT] Application is shutting down...");
            }

            try
            {
                if (_host != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _host.StopAsync();
                            _host.Dispose();
                        }
                        catch (Exception ex)
                        {
                            if (_debugMode)
                            {
                                Console.WriteLine($"[EXIT ERROR] Error during host shutdown: {ex.Message}");
                            }
                        }
                    }).Wait();
                }
                else
                {
                    if (_debugMode)
                    {
                        Console.WriteLine("[EXIT] Host is null, skipping shutdown");
                    }
                }
            }
            catch (Exception ex)
            {
                if (_debugMode)
                {
                    Console.WriteLine($"[EXIT ERROR] Error during exit: {ex.Message}");
                }
            }

            if (_debugMode)
            {
                Console.WriteLine("[EXIT] Application shutdown completed");
                try
                {
                    FreeConsole();
                }
                catch
                {
                    // Ignore console freeing errors
                }
            }

            base.OnExit(e);
        }
    }
}
