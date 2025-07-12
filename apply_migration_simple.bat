@echo off
echo Applying Security Zone and System Categories Migration...

REM Check if database exists
if not exist "maritime_erp.db" (
    echo Database file not found: maritime_erp.db
    echo Please make sure you're running this from the correct directory.
    pause
    exit /b 1
)

REM Create backup
set BACKUP_NAME=maritime_erp_backup_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%%time:~6,2%.db
set BACKUP_NAME=%BACKUP_NAME: =0%
copy "maritime_erp.db" "%BACKUP_NAME%"
echo Created backup: %BACKUP_NAME%

REM Apply migration
echo Applying migration...
sqlite3 maritime_erp.db < update_security_zone_and_categories_migration.sql

if %ERRORLEVEL% equ 0 (
    echo Migration completed successfully!
    echo.
    echo The application now uses:
    echo   - SecurityZoneText as an optional text field instead of foreign key
    echo   - Updated maritime-specific system categories
    echo.
    echo You can now run the application with the updated database schema.
    echo.
    echo Backup file saved as: %BACKUP_NAME%
    echo You can delete this backup file if the migration is working correctly.
) else (
    echo Migration failed! Restoring backup...
    copy "%BACKUP_NAME%" "maritime_erp.db"
    echo Database restored from backup
)

pause 