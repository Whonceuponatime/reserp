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

            // Register services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IShipService, ShipService>();
            services.AddScoped<ISystemService, SystemService>();
            services.AddScoped<IComponentService, ComponentService>();
            services.AddScoped<ISoftwareService, SoftwareService>();
            services.AddScoped<IChangeRequestService, ChangeRequestService>();
            services.AddScoped<ISystemChangePlanService, SystemChangePlanService>();
            services.AddScoped<IHardwareChangeRequestService, HardwareChangeRequestService>();
            services.AddScoped<ISoftwareChangeRequestService, SoftwareChangeRequestService>();
            services.AddScoped<ISecurityReviewStatementService, SecurityReviewStatementService>();
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
            services.AddTransient<SecurityReviewStatementViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<AuditLogsViewModel>();
            services.AddTransient<HardwareChangeRequestDialogViewModel>();
            services.AddTransient<SoftwareChangeRequestDialogViewModel>();
            services.AddTransient<SystemChangePlanDialogViewModel>();
            services.AddTransient<SecurityReviewStatementDialogViewModel>();

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
                    
                    // Initialize database
                    await InitializeDatabaseAsync();
                    
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

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                Console.WriteLine("Initializing database...");
                
                using var scope = _host.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<MaritimeERPContext>();
                
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
                
                // Clean up and ensure only correct roles exist
                Console.WriteLine("Cleaning up roles...");
                
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
                
                // Now safely handle invalid roles by reassigning users first
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
                
                Console.WriteLine("Role cleanup completed");
                
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
