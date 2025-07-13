# Build SEACURE(CARE) Installer - PowerShell Version
Write-Host "Building SEACURE(CARE) Installer" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green

# Define Inno Setup path
$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\iscc.exe"

# Check if Inno Setup is installed
if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "Error: Inno Setup Compiler not found at: $InnoSetupPath" -ForegroundColor Red
    Write-Host "Please install Inno Setup from: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "Installer") { Remove-Item "Installer" -Recurse -Force }
if (Test-Path "src\MaritimeERP.Desktop\bin\Debug\net8.0-windows") { 
    Remove-Item "src\MaritimeERP.Desktop\bin\Debug\net8.0-windows" -Recurse -Force 
}

# Build the application in Release mode
Write-Host "Building application in Release mode..." -ForegroundColor Yellow
Set-Location "src\MaritimeERP.Desktop"

dotnet clean --configuration Release
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Build failed" -ForegroundColor Red
    Set-Location "..\..\"
    Read-Host "Press Enter to exit"
    exit 1
}

Set-Location "..\..\"

# Check if build artifacts exist
$ExePath = "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\MaritimeERP.Desktop.exe"
if (-not (Test-Path $ExePath)) {
    Write-Host "Error: Built executable not found" -ForegroundColor Red
    Write-Host "Expected: $ExePath" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Create installer directory
if (-not (Test-Path "Installer")) { New-Item -ItemType Directory -Path "Installer" }

# Run Inno Setup
Write-Host "Creating installer with Inno Setup..." -ForegroundColor Yellow
& $InnoSetupPath "MaritimeERP_Setup.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Installer creation failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "Installer created successfully!" -ForegroundColor Green
Write-Host "Location: $PWD\Installer\SEACURE_CARE_Setup_v1.0.0.exe" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Ask if user wants to test the installer
$test = Read-Host "Do you want to run the installer now? (y/n)"
if ($test.ToLower() -eq "y") {
    Start-Process "$PWD\Installer\SEACURE_CARE_Setup_v1.0.0.exe"
}

Read-Host "Press Enter to exit" 