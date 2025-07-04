using Microsoft.EntityFrameworkCore;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using MaritimeERP.Services;

Console.WriteLine("Maritime ERP - Admin User Creator");
Console.WriteLine("==================================");

// Configure the database connection
var optionsBuilder = new DbContextOptionsBuilder<MaritimeERPContext>();
optionsBuilder.UseSqlite("Data Source=maritime_erp.db");

using var context = new MaritimeERPContext(optionsBuilder.Options);

// Ensure database is created
await context.Database.EnsureCreatedAsync();
Console.WriteLine("Database ensured to exist.");

// Check existing users
var userCount = await context.Users.CountAsync();
Console.WriteLine($"Current user count: {userCount}");

if (userCount > 0)
{
    var users = await context.Users.Include(u => u.Role).ToListAsync();
    Console.WriteLine("Existing users:");
    foreach (var user in users)
    {
        Console.WriteLine($"  - {user.Username} ({user.Role?.Name ?? "No Role"}) - Active: {user.IsActive}");
    }
}

// Check roles
var roles = await context.Roles.ToListAsync();
Console.WriteLine($"Available roles: {roles.Count}");
foreach (var role in roles)
{
    Console.WriteLine($"  - {role.Name}: {role.Description}");
}

// Create admin user if it doesn't exist
var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
if (adminUser == null)
{
    var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
    if (adminRole == null)
    {
        Console.WriteLine("Administrator role not found! Creating one...");
        adminRole = new Role
        {
            Name = "Administrator",
            Description = "Full system access"
        };
        context.Roles.Add(adminRole);
        await context.SaveChangesAsync();
    }

    Console.WriteLine("Creating admin user...");
    var newAdminUser = new User
    {
        Username = "admin",
        PasswordHash = AuthenticationService.HashPassword("admin123"),
        FullName = "System Administrator",
        Email = "admin@maritime-erp.com",
        RoleId = adminRole.Id,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    context.Users.Add(newAdminUser);
    var changes = await context.SaveChangesAsync();
    Console.WriteLine($"Admin user created! {changes} changes saved.");
}
else
{
    Console.WriteLine($"Admin user already exists: {adminUser.Username} - Active: {adminUser.IsActive}");
    
    // Verify password
    var isValidPassword = AuthenticationService.VerifyPassword("admin123", adminUser.PasswordHash);
    Console.WriteLine($"Password verification test: {(isValidPassword ? "PASSED" : "FAILED")}");
}

Console.WriteLine("Done! Press any key to exit...");
Console.ReadKey(); 