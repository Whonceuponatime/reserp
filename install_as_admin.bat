@echo off
echo ========================================
echo      SEACURE(CARE) Installation
echo ========================================
echo.
echo This installer requires administrator privileges.
echo Please click "Yes" when prompted by UAC.
echo.
pause

:: Check if installer exists
if not exist "Installer\SEACURE_CARE_Setup_v1.0.0.exe" (
    echo Error: Installer not found at Installer\SEACURE_CARE_Setup_v1.0.0.exe
    echo Please run build_installer.ps1 first to create the installer.
    pause
    exit /b 1
)

:: Run installer with administrator privileges
echo Launching installer with administrator privileges...
powershell -Command "Start-Process 'Installer\SEACURE_CARE_Setup_v1.0.0.exe' -Verb RunAs"

echo.
echo Installation completed!
pause 