@echo off
setlocal enabledelayedexpansion
title Techlight MyDesk - Database Tools

:: ============================================================================
:: Techlight MyDesk - Database Tools Menu
:: Provides quick access to all DB scripts in logical SDLC order
:: ============================================================================

:MENU
cls
echo.
echo ============================================================
echo    TECHLIGHT MYDESK - DATABASE TOOLS
echo ============================================================
echo.
echo    Local Dev:  (localdb)\MSSQLLocalDB - Techlight_MyDesk
echo    Production: techlight.digitalresponse.com.au\SQL2016
echo.
echo ------------------------------------------------------------
echo    SETUP (one-time)
echo ------------------------------------------------------------
echo      1. Install SQL Server Express (local)
echo      2. View / Edit Migration Config
echo      3. Install Python dependencies (pyodbc)
echo.
echo ------------------------------------------------------------
echo    MIGRATION (Access -^> SQL Server)
echo ------------------------------------------------------------
echo      4. Migrate Access DB to Local SQL Server
echo.
echo ------------------------------------------------------------
echo    MAINTENANCE
echo ------------------------------------------------------------
echo      5. Backup Local Database
echo      6. Backup Local Database (compressed .zip)
echo.
echo ------------------------------------------------------------
echo    DEPLOYMENT
echo ------------------------------------------------------------
echo      7. Deploy Local -^> Production (DRY RUN)
echo      8. Deploy Local -^> Production (LIVE)
echo.
echo ------------------------------------------------------------
echo    INFO
echo ------------------------------------------------------------
echo      9. View Migration Log
echo     10. Open Backups Folder
echo     11. Read Documentation
echo.
echo      Q. Quit
echo.
echo ============================================================
set /p choice="Select an option: "

if /i "%choice%"=="1"  goto INSTALL_SQL
if /i "%choice%"=="2"  goto EDIT_CONFIG
if /i "%choice%"=="3"  goto INSTALL_PY
if /i "%choice%"=="4"  goto MIGRATE
if /i "%choice%"=="5"  goto BACKUP
if /i "%choice%"=="6"  goto BACKUP_ZIP
if /i "%choice%"=="7"  goto DEPLOY_DRY
if /i "%choice%"=="8"  goto DEPLOY_LIVE
if /i "%choice%"=="9"  goto VIEW_LOG
if /i "%choice%"=="10" goto OPEN_BACKUPS
if /i "%choice%"=="11" goto VIEW_DOCS
if /i "%choice%"=="q"  goto END
if /i "%choice%"=="Q"  goto END

echo.
echo [ERROR] Invalid selection: %choice%
timeout /t 2 >nul
goto MENU

:: ============================================================================
:: Actions
:: ============================================================================

:INSTALL_SQL
cls
echo ============================================================
echo    INSTALL SQL SERVER EXPRESS
echo ============================================================
echo.
echo This will install SQL Server Express 2022 locally.
echo Requires Administrator rights.
echo.
call "%~dp0Install-SQLExpress.bat"
goto PAUSE_RETURN

:EDIT_CONFIG
cls
echo Opening migration_config.py in default editor...
start "" notepad "%~dp0migration_config.py"
goto PAUSE_RETURN

:INSTALL_PY
cls
echo ============================================================
echo    INSTALL PYTHON DEPENDENCIES
echo ============================================================
echo.
where python >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Python is not installed or not in PATH.
    echo Download from: https://python.org
    goto PAUSE_RETURN
)
python -m pip install -r "%~dp0requirements.txt"
goto PAUSE_RETURN

:MIGRATE
cls
echo ============================================================
echo    MIGRATE ACCESS DB TO SQL SERVER
echo ============================================================
echo.
echo Source: Techlight2.mdb (Access)
echo Target: (localdb)\MSSQLLocalDB - Techlight_MyDesk
echo.
echo WARNING: This will DROP and recreate tables in SQL Server!
echo.
pause
python "%~dp0migrate_access_to_sqlserver.py"
goto PAUSE_RETURN

:BACKUP
cls
echo ============================================================
echo    BACKUP LOCAL DATABASE
echo ============================================================
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0Backup-Database.ps1"
goto PAUSE_RETURN

:BACKUP_ZIP
cls
echo ============================================================
echo    BACKUP LOCAL DATABASE (compressed)
echo ============================================================
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0Backup-Database.ps1" -Compress
goto PAUSE_RETURN

:DEPLOY_DRY
cls
echo ============================================================
echo    DEPLOY TO PRODUCTION (DRY RUN)
echo ============================================================
echo.
echo This will show what WOULD happen without making changes.
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0Deploy-Database.ps1" -DryRun
goto PAUSE_RETURN

:DEPLOY_LIVE
cls
echo ============================================================
echo    DEPLOY TO PRODUCTION (LIVE)
echo ============================================================
echo.
echo *** WARNING: THIS WILL MODIFY THE PRODUCTION DATABASE ***
echo.
echo Target: techlight.digitalresponse.com.au\SQL2016
echo.
set /p confirm="Type YES to continue: "
if /i not "!confirm!"=="YES" (
    echo Cancelled.
    goto PAUSE_RETURN
)
powershell -ExecutionPolicy Bypass -File "%~dp0Deploy-Database.ps1"
goto PAUSE_RETURN

:VIEW_LOG
cls
if exist "%~dp0migration.log" (
    start "" notepad "%~dp0migration.log"
) else (
    echo No migration.log file found.
    echo Run a migration first to create one.
)
goto PAUSE_RETURN

:OPEN_BACKUPS
cls
if not exist "%~dp0Backups" mkdir "%~dp0Backups"
start "" explorer "%~dp0Backups"
goto MENU

:VIEW_DOCS
cls
echo ============================================================
echo    DOCUMENTATION
echo ============================================================
echo.
echo Available documentation:
echo   1. MIGRATION_README.md   (Database migration tools)
echo   2. SQL_INSTALL_README.md (SQL Server installation)
echo   B. Back to main menu
echo.
set /p doc="Select: "
if /i "%doc%"=="1" start "" notepad "%~dp0MIGRATION_README.md"
if /i "%doc%"=="2" start "" notepad "%~dp0SQL_INSTALL_README.md"
goto MENU

:PAUSE_RETURN
echo.
echo ------------------------------------------------------------
pause
goto MENU

:END
cls
echo.
echo Goodbye!
echo.
timeout /t 1 >nul
exit /b 0
