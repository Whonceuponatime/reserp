@echo off
echo Building SEACURE(CARE) Simple Installer...
echo ========================================

echo.
echo Step 1: Building solution in Release mode...
dotnet build MaritimeERP.sln -c Release

echo.
echo Step 2: Creating installer with Inno Setup...
if not exist "Installer" mkdir "Installer"
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "MaritimeERP_Setup_Simple.iss"

echo.
echo Done!
pause 