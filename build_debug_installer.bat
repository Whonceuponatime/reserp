@echo off
echo Building SEACURE(CARE) DEBUG Installer...
echo ==========================================

echo.
echo Step 1: Publishing application as single file...
cd src\MaritimeERP.Desktop
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
cd ..\..

if not exist "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\win-x64\publish\MaritimeERP.Desktop.exe" (
    echo ERROR: Single file publish failed
    pause
    exit /b 1
)

echo.
echo Step 2: Creating DEBUG installer with Inno Setup...
if not exist "Installer" mkdir "Installer"
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "MaritimeERP_Setup_Debug.iss"

if errorlevel 1 (
    echo ERROR: Installer creation failed
    pause
    exit /b 1
)

echo.
echo SUCCESS: DEBUG installer created!
echo.
echo This installer includes:
echo   - Automatic debug console on startup
echo   - Desktop shortcut with debug mode
echo   - Detailed error logging
echo   - Perfect for troubleshooting on other computers
echo.
echo Location: Installer\SEACURE_CARE_Setup_v1.0.0_DEBUG.exe
echo.
echo To use on target computer:
echo   1. Install using this installer
echo   2. Run the "SEACURE(CARE) Debug" shortcut
echo   3. A console window will show detailed startup information
echo   4. Send console output back for analysis
echo.
pause 