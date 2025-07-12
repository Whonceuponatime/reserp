using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace MaritimeERP.Desktop
{
    public static class MigrationRunner
    {
        public static void ApplyMigration()
        {
            var dbPath = "maritime_erp.db";
            var migrationScript = @"
-- Maritime ERP Database Structure Update
-- This script updates the database to:
-- 1. Change SecurityZone from required foreign key to optional text field
-- 2. Update system categories to the new maritime-specific categories

BEGIN TRANSACTION;

-- Step 1: Add new SecurityZoneText column to Systems table
ALTER TABLE Systems ADD COLUMN SecurityZoneText TEXT;

-- Step 2: Migrate existing security zone data to text field
UPDATE Systems 
SET SecurityZoneText = (
    SELECT sz.Name 
    FROM SecurityZones sz 
    WHERE sz.Id = Systems.SecurityZoneId
);

-- Step 3: Update SystemCategories with new maritime-specific categories
-- Clear existing categories
DELETE FROM SystemCategories;

-- Insert new maritime system categories
INSERT INTO SystemCategories (Id, Name, Description) VALUES 
(1, 'Propulsion', 'Main and auxiliary propulsion systems'),
(2, 'Steering', 'Steering gear and control systems'),
(3, 'Anchoring and mooring', 'Anchoring, mooring, and positioning systems'),
(4, 'Electrical power generation and distribution', 'Power generation, distribution, and electrical systems'),
(5, 'Fire detection and extinguishing systems', 'Fire safety, detection, and suppression systems'),
(6, 'Bilge and ballast systems, loading computer', 'Bilge pumping, ballast management, and cargo loading systems'),
(7, 'Watertight integrity and flooding detection', 'Watertight doors, flooding detection, and hull integrity systems'),
(8, 'Lighting (e.g. emergency lighting, low locations, navigation lights, etc.)', 'All lighting systems including emergency, navigation, and general illumination'),
(9, 'Safety Systems', 'General safety equipment and emergency response systems'),
(10, 'Navigational systems required by statutory regulations', 'Mandatory navigation equipment as per international regulations'),
(11, 'Internal and external communication systems required by class rules and statutory regulations', 'Communication equipment mandated by classification societies and regulations');

-- Reset the auto-increment counter for SystemCategories
UPDATE sqlite_sequence SET seq = 11 WHERE name = 'SystemCategories';

-- Step 4: Update any existing systems to use valid category IDs
-- Set all existing systems to 'Safety Systems' category as a default
UPDATE Systems SET CategoryId = 9 WHERE CategoryId NOT IN (1,2,3,4,5,6,7,8,9,10,11);

COMMIT;
";

            try
            {
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine($"Database file not found: {dbPath}");
                    return;
                }

                // Create backup
                var backupPath = $"maritime_erp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                File.Copy(dbPath, backupPath);
                Console.WriteLine($"Database backed up to: {backupPath}");

                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();

                // Split and execute statements
                var statements = migrationScript.Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var statement in statements)
                {
                    var trimmedStatement = statement.Trim();
                    if (!string.IsNullOrEmpty(trimmedStatement) && 
                        !trimmedStatement.StartsWith("--") && 
                        trimmedStatement != "BEGIN TRANSACTION" && 
                        trimmedStatement != "COMMIT")
                    {
                        Console.WriteLine($"Executing: {trimmedStatement.Substring(0, Math.Min(50, trimmedStatement.Length))}...");
                        
                        using var command = connection.CreateCommand();
                        command.CommandText = trimmedStatement;
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("Database migration completed successfully!");
                Console.WriteLine("Security zones are now optional text fields");
                Console.WriteLine("System categories have been updated to maritime-specific categories");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during migration: {ex.Message}");
                throw;
            }
        }
    }
} 