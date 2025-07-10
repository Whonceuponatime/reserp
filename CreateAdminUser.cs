using System;
using BCrypt.Net;

// Simple program to create admin user
class Program
{
    static void Main()
    {
        string password = "admin123";
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine("Hashed password for 'admin123':");
        Console.WriteLine(hashedPassword);
        
        Console.WriteLine("\nSQL to insert admin user:");
        Console.WriteLine($"INSERT INTO Users (Username, PasswordHash, FullName, Email, RoleId, IsActive, CreatedAt) VALUES ('admin', '{hashedPassword}', 'Administrator', 'admin@maritime.com', 1, 1, datetime('now'));");
    }
} 