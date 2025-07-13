@echo off
echo Building SEACURE(CARE) Installer
echo ================================

:: Check if Inno Setup is installed
where iscc >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Error: Inno Setup Compiler (iscc) not found in PATH
    echo Please install Inno Setup and add it to your PATH
    echo Download from: https://jrsoftware.org/isinfo.php
    pause
    exit /b 1
)

:: Clean previous builds
echo Cleaning previous builds...
if exist "Installer" rmdir /s /q "Installer"
if exist "src\MaritimeERP.Desktop\bin\Debug\net8.0-windows" rmdir /s /q "src\MaritimeERP.Desktop\bin\Debug\net8.0-windows"

:: Build the application in Release mode
echo Building application in Release mode...
cd src\MaritimeERP.Desktop
dotnet clean --configuration Release
dotnet build --configuration Release --no-restore
if %ERRORLEVEL% NEQ 0 (
    echo Error: Build failed
    cd ..\..
    pause
    exit /b 1
)
cd ..\..

:: Check if build artifacts exist
if not exist "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\MaritimeERP.Desktop.exe" (
    echo Error: Built executable not found
    echo Expected: src\MaritimeERP.Desktop\bin\Release\net8.0-windows\MaritimeERP.Desktop.exe
    pause
    exit /b 1
)

:: Create installer directory
if not exist "Installer" mkdir "Installer"

:: Run Inno Setup
echo Creating installer with Inno Setup...
iscc MaritimeERP_Setup.iss
if %ERRORLEVEL% NEQ 0 (
    echo Error: Installer creation failed
    pause
    exit /b 1
)

echo.
echo ================================
echo Installer created successfully!
echo Location: %CD%\Installer\SEACURE_CARE_Setup_v1.0.0.exe
echo ================================
echo.

:: Ask if user wants to test the installer
set /p test="Do you want to run the installer now? (y/n): "
if /i "%test%"=="y" (
    start "" "%CD%\Installer\SEACURE_CARE_Setup_v1.0.0.exe"
)

pause 