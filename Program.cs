using System;
using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "admin123";
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hash}");
        
        // Test verification
        bool isValid = BCrypt.Net.BCrypt.Verify(password, hash);
        Console.WriteLine($"Verification: {isValid}");
        
        Console.WriteLine($"\nSQL Command:");
        Console.WriteLine($"UPDATE Users SET PasswordHash = '{hash}' WHERE Username = 'admin';");
    }
} 