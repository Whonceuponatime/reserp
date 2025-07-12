# Generate BCrypt hash for admin password
Add-Type -AssemblyName System.Security

# Install BCrypt.Net if not available
try {
    Add-Type -Path ".\packages\BCrypt.Net-Next.4.0.3\lib\net8.0\BCrypt.Net-Next.dll"
} catch {
    Write-Host "BCrypt.Net not found. Please install it first."
    Write-Host "Run: Install-Package BCrypt.Net-Next -Version 4.0.3"
    exit 1
}

# Generate hash for 'admin123'
$password = "admin123"
$hash = [BCrypt.Net.BCrypt]::HashPassword($password)

Write-Host "Password: $password"
Write-Host "Hash: $hash"

# Test verification
$isValid = [BCrypt.Net.BCrypt]::Verify($password, $hash)
Write-Host "Verification test: $isValid"

Write-Host ""
Write-Host "SQL to update admin user:"
Write-Host "UPDATE Users SET PasswordHash = '$hash' WHERE Username = 'admin';" 