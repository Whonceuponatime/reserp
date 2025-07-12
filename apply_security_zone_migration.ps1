# PowerShell script to apply SecurityZone migration
# This script converts SecurityZone from foreign key to text field

Write-Host "Starting SecurityZone migration..." -ForegroundColor Green

# Change to the project directory
Set-Location -Path "src\MaritimeERP.Desktop"

# Check if database exists
if (-not (Test-Path "maritime_erp.db")) {
    Write-Host "Database not found. Please run the application first to create the database." -ForegroundColor Red
    exit 1
}

# Backup the database
$backupPath = "maritime_erp_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').db"
Copy-Item "maritime_erp.db" $backupPath
Write-Host "Database backed up to: $backupPath" -ForegroundColor Yellow

# Apply the migration using sqlite3
try {
    # Read the migration script
    $migrationScript = Get-Content "..\..\update_security_zone_migration.sql" -Raw
    
    # Apply migration using sqlite3 command
    $migrationScript | sqlite3 "maritime_erp.db"
    
    Write-Host "SecurityZone migration applied successfully!" -ForegroundColor Green
    Write-Host "Changes made:" -ForegroundColor Cyan
    Write-Host "- SecurityZone is now an optional text field instead of foreign key" -ForegroundColor Cyan
    Write-Host "- Existing SecurityZone data has been migrated to text values" -ForegroundColor Cyan
    Write-Host "- SecurityZones table has been removed" -ForegroundColor Cyan
}
catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    Write-Host "Restoring backup..." -ForegroundColor Yellow
    Copy-Item $backupPath "maritime_erp.db" -Force
    Write-Host "Database restored from backup." -ForegroundColor Yellow
    exit 1
}

Write-Host "Migration completed successfully!" -ForegroundColor Green 