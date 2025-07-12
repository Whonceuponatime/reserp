# PowerShell script to apply security zone and system categories migration
# This script will update the database to use SecurityZoneText instead of SecurityZone foreign key
# and update the system categories with the new maritime-specific categories

param(
    [string]$DatabasePath = "maritime_erp.db"
)

Write-Host "Applying Security Zone and System Categories Migration..." -ForegroundColor Green

# Check if database file exists
if (-not (Test-Path $DatabasePath)) {
    Write-Host "Database file not found: $DatabasePath" -ForegroundColor Red
    Write-Host "Please make sure you're running this from the correct directory." -ForegroundColor Red
    exit 1
}

# Create backup
$BackupPath = "maritime_erp_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db"
Copy-Item $DatabasePath $BackupPath
Write-Host "Created backup: $BackupPath" -ForegroundColor Yellow

try {
    # Load SQLite assembly
    Add-Type -Path "Microsoft.Data.Sqlite.dll" -ErrorAction SilentlyContinue

    # Create connection
    $connectionString = "Data Source=$DatabasePath"
    $connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)
    $connection.Open()

    Write-Host "Connected to database successfully" -ForegroundColor Green

    # Read migration SQL
    $migrationSql = Get-Content "update_security_zone_and_categories_migration.sql" -Raw

    # Execute migration
    $command = $connection.CreateCommand()
    $command.CommandText = $migrationSql
    $result = $command.ExecuteNonQuery()

    Write-Host "Migration executed successfully!" -ForegroundColor Green
    Write-Host "Rows affected: $result" -ForegroundColor Cyan

    # Verify the changes
    Write-Host "`nVerifying migration results..." -ForegroundColor Yellow

    # Check if SecurityZoneText column exists
    $command.CommandText = "PRAGMA table_info(Systems)"
    $reader = $command.ExecuteReader()
    $hasSecurityZoneText = $false
    while ($reader.Read()) {
        if ($reader["name"] -eq "SecurityZoneText") {
            $hasSecurityZoneText = $true
            break
        }
    }
    $reader.Close()

    if ($hasSecurityZoneText) {
        Write-Host "✓ SecurityZoneText column added successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ SecurityZoneText column not found" -ForegroundColor Red
    }

    # Check system categories
    $command.CommandText = "SELECT COUNT(*) FROM SystemCategories"
    $categoryCount = $command.ExecuteScalar()
    Write-Host "✓ System categories updated: $categoryCount categories found" -ForegroundColor Green

    # Show new categories
    $command.CommandText = "SELECT Id, Name FROM SystemCategories ORDER BY Id"
    $reader = $command.ExecuteReader()
    Write-Host "`nNew System Categories:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "  $($reader["Id"]). $($reader["Name"])" -ForegroundColor White
    }
    $reader.Close()

    $connection.Close()

    Write-Host "`nMigration completed successfully!" -ForegroundColor Green
    Write-Host "The application now uses:" -ForegroundColor Yellow
    Write-Host "  - SecurityZoneText as an optional text field instead of foreign key" -ForegroundColor White
    Write-Host "  - Updated maritime-specific system categories" -ForegroundColor White
    Write-Host "`nYou can now run the application with the updated database schema." -ForegroundColor Green

} catch {
    Write-Host "Error during migration: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Restoring backup..." -ForegroundColor Yellow
    
    if ($connection -and $connection.State -eq "Open") {
        $connection.Close()
    }
    
    Copy-Item $BackupPath $DatabasePath -Force
    Write-Host "Database restored from backup" -ForegroundColor Green
    exit 1
}

Write-Host "`nBackup file saved as: $BackupPath" -ForegroundColor Yellow
Write-Host "You can delete this backup file if the migration is working correctly." -ForegroundColor Gray 