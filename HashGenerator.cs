using System;
using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "admin123";
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine($"BCrypt hash for 'admin123': {hashedPassword}");
        
        // Test verification
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        Console.WriteLine($"Verification test: {isValid}");
        
        Console.WriteLine($"\nSQL to update admin user:");
        Console.WriteLine($"UPDATE Users SET PasswordHash = '{hashedPassword}' WHERE Username = 'admin';");
    }
} 