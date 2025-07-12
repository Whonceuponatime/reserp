# Debug admin user role assignment
Write-Host "=== DEBUGGING ADMIN USER ROLE ===" -ForegroundColor Yellow
Write-Host ""

# Check if the application is running
$processes = Get-Process | Where-Object { $_.ProcessName -like "*MaritimeERP*" -or $_.ProcessName -like "*dotnet*" }
if ($processes) {
    Write-Host "⚠️  Maritime ERP application appears to be running. Please close it first." -ForegroundColor Red
    Write-Host "Running processes:"
    $processes | ForEach-Object { Write-Host "  - $($_.ProcessName) (PID: $($_.Id))" }
    Write-Host ""
}

$dbPath = "src\MaritimeERP.Desktop\maritime_erp.db"

if (Test-Path $dbPath) {
    Write-Host "✓ Database found at: $dbPath" -ForegroundColor Green
    
    # Try to query the database
    try {
        # Check if we can access the database
        $testQuery = "SELECT 1 as test"
        $null = & sqlite3 $dbPath $testQuery 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Database is accessible" -ForegroundColor Green
            Write-Host ""
            
            # Check roles table
            Write-Host "=== ROLES TABLE ===" -ForegroundColor Cyan
            $rolesQuery = "SELECT Id, Name, Description FROM Roles ORDER BY Id;"
            & sqlite3 $dbPath $rolesQuery
            Write-Host ""
            
            # Check users table with role information
            Write-Host "=== USERS WITH ROLES ===" -ForegroundColor Cyan
            $usersQuery = @"
SELECT 
    u.Id,
    u.Username,
    u.FullName,
    u.RoleId,
    r.Name as RoleName,
    u.IsActive,
    u.CreatedAt
FROM Users u 
LEFT JOIN Roles r ON u.RoleId = r.Id 
ORDER BY u.Id;
"@
            & sqlite3 $dbPath $usersQuery
            Write-Host ""
            
            # Specifically check admin user
            Write-Host "=== ADMIN USER DETAILS ===" -ForegroundColor Cyan
            $adminQuery = @"
SELECT 
    'User ID: ' || u.Id,
    'Username: ' || u.Username,
    'Full Name: ' || u.FullName,
    'Role ID: ' || u.RoleId,
    'Role Name: ' || COALESCE(r.Name, 'NO ROLE ASSIGNED'),
    'Is Active: ' || CASE WHEN u.IsActive = 1 THEN 'Yes' ELSE 'No' END,
    'Created At: ' || u.CreatedAt
FROM Users u 
LEFT JOIN Roles r ON u.RoleId = r.Id 
WHERE u.Username = 'admin';
"@
            & sqlite3 $dbPath $adminQuery
            Write-Host ""
            
            # Check if there are any role assignment issues
            Write-Host "=== DIAGNOSTIC CHECKS ===" -ForegroundColor Cyan
            
            # Check for users without roles
            $orphanUsersQuery = "SELECT COUNT(*) FROM Users WHERE RoleId IS NULL OR RoleId = 0;"
            $orphanCount = & sqlite3 $dbPath $orphanUsersQuery
            if ($orphanCount -gt 0) {
                Write-Host "⚠️  Found $orphanCount users without valid roles" -ForegroundColor Yellow
            } else {
                Write-Host "✓ All users have valid roles assigned" -ForegroundColor Green
            }
            
            # Check if admin role exists
            $adminRoleQuery = "SELECT COUNT(*) FROM Roles WHERE Name = 'Administrator';"
            $adminRoleCount = & sqlite3 $dbPath $adminRoleQuery
            if ($adminRoleCount -eq 0) {
                Write-Host "❌ Administrator role not found!" -ForegroundColor Red
            } else {
                Write-Host "✓ Administrator role exists" -ForegroundColor Green
            }
            
            # Check if admin user has admin role
            $adminUserRoleQuery = @"
SELECT COUNT(*) 
FROM Users u 
JOIN Roles r ON u.RoleId = r.Id 
WHERE u.Username = 'admin' AND r.Name = 'Administrator';
"@
            $adminUserRoleCount = & sqlite3 $dbPath $adminUserRoleQuery
            if ($adminUserRoleCount -eq 0) {
                Write-Host "❌ Admin user does not have Administrator role!" -ForegroundColor Red
                Write-Host ""
                Write-Host "=== FIXING ADMIN USER ROLE ===" -ForegroundColor Yellow
                
                # Get the Administrator role ID
                $adminRoleIdQuery = "SELECT Id FROM Roles WHERE Name = 'Administrator';"
                $adminRoleId = & sqlite3 $dbPath $adminRoleIdQuery
                
                if ($adminRoleId) {
                    Write-Host "Administrator role ID: $adminRoleId"
                    
                    # Update admin user to have Administrator role
                    $updateQuery = "UPDATE Users SET RoleId = $adminRoleId WHERE Username = 'admin';"
                    & sqlite3 $dbPath $updateQuery
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "✓ Admin user role updated successfully!" -ForegroundColor Green
                        
                        # Verify the fix
                        Write-Host ""
                        Write-Host "=== VERIFICATION ===" -ForegroundColor Cyan
                        & sqlite3 $dbPath $adminQuery
                    } else {
                        Write-Host "❌ Failed to update admin user role" -ForegroundColor Red
                    }
                } else {
                    Write-Host "❌ Could not find Administrator role ID" -ForegroundColor Red
                }
            } else {
                Write-Host "✓ Admin user has Administrator role" -ForegroundColor Green
            }
            
        } else {
            Write-Host "❌ Cannot access database" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error accessing database: $_" -ForegroundColor Red
        Write-Host "Make sure sqlite3 is installed and in your PATH" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Database not found at: $dbPath" -ForegroundColor Red
    Write-Host "Run the application first to create the database" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== NEXT STEPS ===" -ForegroundColor Yellow
Write-Host "1. Close the application if it's running"
Write-Host "2. Run this script to check/fix the admin role"
Write-Host "3. Start the application again"
Write-Host "4. Login with: admin / admin"
Write-Host "5. Check if you now have admin privileges" 