using Microsoft.EntityFrameworkCore;
using MaritimeERP.Core.Entities;
using MaritimeERP.Data;
using BCrypt.Net;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Creating admin user...");
        
        var optionsBuilder = new DbContextOptionsBuilder<MaritimeERPContext>();
        optionsBuilder.UseSqlite("Data Source=maritime_erp.db");
        
        using var context = new MaritimeERPContext(optionsBuilder.Options);
        
        // Delete existing admin user
        var existingAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (existingAdmin != null)
        {
            context.Users.Remove(existingAdmin);
            await context.SaveChangesAsync();
            Console.WriteLine("Removed existing admin user");
        }
        
        // Create new admin user
        string password = "admin123";
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine($"Generated hash: {hashedPassword}");
        
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = hashedPassword,
            FullName = "Administrator",
            Email = "admin@maritime.com",
            RoleId = 1, // Administrator role
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        
        Console.WriteLine("Admin user created successfully!");
        
        // Test verification
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        Console.WriteLine($"Password verification test: {isValid}");
    }
} 