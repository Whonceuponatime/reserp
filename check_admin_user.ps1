# Check admin user in database
$dbPath = "src\MaritimeERP.Desktop\maritime_erp.db"

if (Test-Path $dbPath) {
    Write-Host "Database found at: $dbPath"
    Write-Host ""
    
    # Check if sqlite3 is available
    try {
        $result = & sqlite3 $dbPath "SELECT 'Database accessible' as status;" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Database is accessible"
            Write-Host ""
            
            # Check roles
            Write-Host "=== ROLES ==="
            & sqlite3 $dbPath "SELECT Id, Name, Description FROM Roles ORDER BY Id;"
            Write-Host ""
            
            # Check admin user
            Write-Host "=== ADMIN USER ==="
            & sqlite3 $dbPath "SELECT u.Id, u.Username, u.FullName, u.Email, r.Name as Role, u.IsActive, u.CreatedAt FROM Users u LEFT JOIN Roles r ON u.RoleId = r.Id WHERE u.Username = 'admin';"
            Write-Host ""
            
            # Check all users
            Write-Host "=== ALL USERS ==="
            & sqlite3 $dbPath "SELECT u.Id, u.Username, u.FullName, r.Name as Role, u.IsActive FROM Users u LEFT JOIN Roles r ON u.RoleId = r.Id ORDER BY u.Id;"
            Write-Host ""
            
            Write-Host "=== LOGIN INSTRUCTIONS ==="
            Write-Host "Username: admin"
            Write-Host "Password: admin (or admin123)"
            Write-Host ""
            Write-Host "If login fails, the application will create the admin user automatically on first run."
        } else {
            Write-Host "✗ Cannot access database with sqlite3"
            Write-Host "Error: $result"
        }
    } catch {
        Write-Host "✗ sqlite3 not available in PATH"
        Write-Host "Database exists but cannot be queried directly"
        Write-Host ""
        Write-Host "=== SOLUTION ==="
        Write-Host "1. Try logging in with: admin / admin"
        Write-Host "2. If that fails, try: admin / admin123"
        Write-Host "3. The application will create the admin user automatically"
    }
} else {
    Write-Host "✗ Database not found at: $dbPath"
    Write-Host "The database will be created automatically when you run the application"
    Write-Host ""
    Write-Host "=== SOLUTION ==="
    Write-Host "1. Run the application: dotnet run"
    Write-Host "2. Login with: admin / admin"
} 