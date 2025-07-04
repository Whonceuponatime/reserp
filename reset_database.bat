@echo off
echo Maritime ERP Database Reset Script
echo ===================================
echo.
echo This script will delete the existing database to allow
echo recreation with the new simplified Ship schema.
echo.
echo Current database files to be deleted:
if exist maritime_erp.db echo - maritime_erp.db (root)
if exist maritime_erp.db-shm echo - maritime_erp.db-shm (root)
if exist maritime_erp.db-wal echo - maritime_erp.db-wal (root)
if exist src\MaritimeERP.Desktop\maritime_erp.db echo - src\MaritimeERP.Desktop\maritime_erp.db
if exist src\MaritimeERP.Desktop\maritime_erp.db-shm echo - src\MaritimeERP.Desktop\maritime_erp.db-shm
if exist src\MaritimeERP.Desktop\maritime_erp.db-wal echo - src\MaritimeERP.Desktop\maritime_erp.db-wal
echo.

set /p confirm="Continue with database reset? (y/N): "
if /i "%confirm%"=="y" goto DELETE
if /i "%confirm%"=="yes" goto DELETE
echo Database reset cancelled.
goto END

:DELETE
echo.
echo Deleting database files...

REM Delete from root directory
if exist maritime_erp.db (
    del maritime_erp.db
    echo ✓ Deleted maritime_erp.db (root)
) else (
    echo - maritime_erp.db not found (root)
)

if exist maritime_erp.db-shm (
    del maritime_erp.db-shm
    echo ✓ Deleted maritime_erp.db-shm (root)
) else (
    echo - maritime_erp.db-shm not found (root)
)

if exist maritime_erp.db-wal (
    del maritime_erp.db-wal
    echo ✓ Deleted maritime_erp.db-wal (root)
) else (
    echo - maritime_erp.db-wal not found (root)
)

REM Delete from Desktop project directory
if exist src\MaritimeERP.Desktop\maritime_erp.db (
    del src\MaritimeERP.Desktop\maritime_erp.db
    echo ✓ Deleted src\MaritimeERP.Desktop\maritime_erp.db
) else (
    echo - src\MaritimeERP.Desktop\maritime_erp.db not found
)

if exist src\MaritimeERP.Desktop\maritime_erp.db-shm (
    del src\MaritimeERP.Desktop\maritime_erp.db-shm
    echo ✓ Deleted src\MaritimeERP.Desktop\maritime_erp.db-shm
) else (
    echo - src\MaritimeERP.Desktop\maritime_erp.db-shm not found
)

if exist src\MaritimeERP.Desktop\maritime_erp.db-wal (
    del src\MaritimeERP.Desktop\maritime_erp.db-wal
    echo ✓ Deleted src\MaritimeERP.Desktop\maritime_erp.db-wal
) else (
    echo - src\MaritimeERP.Desktop\maritime_erp.db-wal not found
)

echo.
echo ✅ Database reset complete!
echo.
echo Next steps:
echo 1. Run the Maritime ERP application
echo 2. The database will be recreated automatically with the new schema
echo 3. Login with: admin / admin123
echo 4. You can now add ships with the simplified form
echo.

:END
pause 