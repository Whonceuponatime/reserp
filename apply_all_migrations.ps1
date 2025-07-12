# Apply all database migrations
# This script applies the SecurityZone and StatusId column migrations

$ErrorActionPreference = "Stop"

Write-Host "Applying database migrations..." -ForegroundColor Green

try {
    # Get the database file path
    $dbPath = "maritime_erp.db"
    
    if (-not (Test-Path $dbPath)) {
        Write-Host "Database file not found at: $dbPath" -ForegroundColor Red
        Write-Host "Please ensure the application has been run at least once to create the database." -ForegroundColor Yellow
        exit 1
    }
    
    # Apply the migration using sqlite3
    Write-Host "Applying migrations to database: $dbPath" -ForegroundColor Yellow
    
    # Check if sqlite3 is available
    $sqliteCmd = Get-Command sqlite3 -ErrorAction SilentlyContinue
    if (-not $sqliteCmd) {
        Write-Host "sqlite3 command not found. Please install SQLite3 command line tools." -ForegroundColor Red
        exit 1
    }
    
    # Apply the migration
    sqlite3 $dbPath ".read apply_all_migrations.sql"
    
    Write-Host "Database migrations applied successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "Error applying migrations: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} 