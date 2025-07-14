@echo off
echo Building SEACURE(CARE) PRODUCTION Installer...
echo ==========================================
echo.

echo Step 1: Publishing application as single file...
dotnet publish src\MaritimeERP.Desktop\MaritimeERP.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=false -p:PublishTrimmed=false -o src\MaritimeERP.Desktop\bin\Release\net8.0-windows\win-x64\publish\

if %errorlevel% neq 0 (
    echo ERROR: Application publishing failed
    pause
    exit /b 1
)

echo.
echo Step 2: Creating PRODUCTION installer with Inno Setup...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" MaritimeERP_Setup_SingleFile.iss

if %errorlevel% neq 0 (
    echo ERROR: Installer creation failed
    pause
    exit /b 1
)

echo.
echo SUCCESS: PRODUCTION installer created!
echo.
echo This installer includes:
echo   - Clean single-file deployment (~80MB executable)
echo   - No debug console or logging
echo   - Optimized for end users
echo   - SEANET branding with proper logos
echo   - Programs and Features integration
echo.
echo Location: Installer\SEACURE_CARE_Setup_v1.0.0_SingleFile.exe
echo.
echo Ready for deployment to end users!
echo.
pause 