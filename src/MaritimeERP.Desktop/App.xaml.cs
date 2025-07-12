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
            services.AddSingleton<DashboardViewModel>();
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
            
            Console.WriteLine("Services configured successfully");
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Start the host asynchronously
                await _host.StartAsync();
                
                // Initialize database asynchronously
                await InitializeDatabaseAsync();
                
                // Show login window
                ShowLoginWindow();

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application startup");
                MessageBox.Show($"Error during application startup: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private void ShowLoginWindow()
        {
            try
            {
                var authService = _host.Services.GetRequiredService<IAuthenticationService>();
                var loginViewModel = new LoginViewModel(authService);
                var loginWindow = new LoginWindow(loginViewModel);
                
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
                
                loginWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing login window");
                MessageBox.Show($"Error showing login window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                Console.WriteLine("Initializing database...");
                
                using var scope = _host.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
                
                // Set command timeout to prevent hanging
                context.Database.SetCommandTimeout(30);
                
                // Ensure database is created
                await context.Database.EnsureCreatedAsync();
                
                // Check if HardwareChangeRequests table exists, if not create it
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='HardwareChangeRequests'";
                var result = await command.ExecuteScalarAsync();
                
                if (result == null)
                {
                    Console.WriteLine("Creating HardwareChangeRequests table...");
                    
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
                Console.WriteLine("Database initialization completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
                _logger.LogError(ex, "Error initializing database");
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
