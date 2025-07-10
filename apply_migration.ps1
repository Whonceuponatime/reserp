# Apply Hardware Change Request Migration
Write-Host "Applying Hardware Change Request migration..."

# Check if SQLite3 is available
$sqlite3Path = "sqlite3"
try {
    & $sqlite3Path -version
    Write-Host "SQLite3 found"
} catch {
    Write-Host "SQLite3 not found in PATH. Please install SQLite3 or use the full path."
    exit 1
}

# Database path
$dbPath = "maritime_erp.db"

# Check if database exists
if (Test-Path $dbPath) {
    Write-Host "Database found: $dbPath"
} else {
    Write-Host "Database not found: $dbPath"
    Write-Host "Creating new database..."
}

# Apply migration
Write-Host "Applying migration..."
Get-Content "..\..\create_hardware_change_request_migration.sql" | & $sqlite3Path $dbPath

Write-Host "Migration applied successfully!"

# Verify table was created
Write-Host "Verifying table creation..."
$result = & $sqlite3Path $dbPath "SELECT name FROM sqlite_master WHERE type='table' AND name='HardwareChangeRequests';"
if ($result -eq "HardwareChangeRequests") {
    Write-Host "✓ HardwareChangeRequests table created successfully"
} else {
    Write-Host "✗ Failed to create HardwareChangeRequests table"
}

Write-Host "Done!" 