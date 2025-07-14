@echo off
echo Building SEACURE(CARE) Installer...
echo ================================

echo.
echo Step 1: Building solution in Release mode...
dotnet build MaritimeERP.sln -c Release
if errorlevel 1 (
    echo ERROR: Build failed
    pause
    exit /b 1
)

echo.
echo Step 2: Creating installer with Inno Setup...
if not exist "Installer" mkdir "Installer"
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "MaritimeERP_Setup.iss"
if errorlevel 1 (
    echo ERROR: Installer creation failed
    pause
    exit /b 1
)

echo.
echo SUCCESS: Installer created successfully!
echo Location: Installer\SEACURE_CARE_Setup_v1.0.0.exe
echo.
pause 