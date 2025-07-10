@echo off
echo Applying Hardware Change Request migration...

REM Check if database exists
if exist "maritime_erp.db" (
    echo Database found: maritime_erp.db
) else (
    echo Database not found: maritime_erp.db
    echo Creating new database...
)

REM Apply migration using sqlite3
echo Applying migration...
sqlite3 maritime_erp.db < "..\..\create_hardware_change_request_migration.sql"

if %errorlevel% equ 0 (
    echo Migration applied successfully!
    
    REM Verify table was created
    echo Verifying table creation...
    sqlite3 maritime_erp.db "SELECT name FROM sqlite_master WHERE type='table' AND name='HardwareChangeRequests';"
    
    echo Done!
) else (
    echo Failed to apply migration. Error code: %errorlevel%
    echo Make sure SQLite3 is installed and in your PATH.
)

pause 