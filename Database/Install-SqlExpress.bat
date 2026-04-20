@echo off
:: SQL Server Express Installation Launcher
:: This batch file runs the PowerShell installer with Administrator rights

title SQL Server Express 2022 Installation

echo ============================================
echo   SQL Server Express 2022 Installation
echo ============================================
echo.
echo This will install SQL Server Express with:
echo   - Instance: localhost/sqlserver
echo   - Windows Authentication: Enabled
echo   - SQL Authentication: sa/password
echo.
echo IMPORTANT: This script must be run as Administrator
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Administrator rights required!
    echo.
    echo Please:
    echo 1. Right-click on this batch file
echo 2. Select "Run as administrator"
    echo.
    pause
    exit /b 1
)

echo [INFO] Administrator rights confirmed
echo.

:: Run PowerShell installer
powershell -ExecutionPolicy Bypass -File "%~dp0Install-SqlExpress.ps1"

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Installation failed with exit code %errorlevel%
    pause
    exit /b %errorlevel%
)

echo.
echo ============================================
echo Installation completed!
echo ============================================
pause
