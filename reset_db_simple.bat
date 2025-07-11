@echo off
echo Deleting database files...
del /f /q src\MaritimeERP.Desktop\maritime_erp.db 2>nul
del /f /q src\MaritimeERP.Desktop\maritime_erp.db-shm 2>nul
del /f /q src\MaritimeERP.Desktop\maritime_erp.db-wal 2>nul
echo Database files deleted.
echo Run the application to recreate the database with the new schema.
pause 