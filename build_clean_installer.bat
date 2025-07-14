@echo off
echo Building SEACURE(CARE) Clean Single-File Installer...
echo ===================================================

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
echo Step 2: Creating clean installer with Inno Setup...
if not exist "Installer" mkdir "Installer"
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "MaritimeERP_Setup_SingleFile.iss"

if errorlevel 1 (
    echo ERROR: Installer creation failed
    pause
    exit /b 1
)

echo.
echo SUCCESS: Clean installer created!
echo.
echo Install directory will contain only:
echo   - MaritimeERP.Desktop.exe (single file ~80MB)
echo   - seacure_logo.ico
echo   - README.md
echo   - Documentation folder
echo.
echo Location: Installer\SEACURE_CARE_Setup_v1.0.0_SingleFile.exe
echo.
pause 