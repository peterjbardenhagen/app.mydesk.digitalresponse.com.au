@echo off
setlocal enabledelayedexpansion
title DR MyDesk Launcher
color 0B

:: ============================================================================
::  DR MyDesk - Launcher
::  Version 3.1 - April 2026
:: ============================================================================

:: Paths
set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "SRC=%ROOT%\src"
set "WEB=%SRC%\MyDesk.Web"
set "SHARED=%SRC%\MyDesk.Shared"
set "DEPLOY=%SRC%\Deployment"
set "TESTS=%ROOT%\tests\MyDesk.PlaywrightTests"
set "APPCMD=%SystemRoot%\System32\inetsrv\appcmd.exe"
set "LOGS=%WEB%\Logs"

:: Check admin
net session >nul 2>&1
if %errorlevel%==0 (
    set "IS_ADMIN=YES"
) else (
    set "IS_ADMIN=NO"
)

:: Check .NET SDK
set "DOTNET_VER="
for /f "delims=" %%v in ('dotnet --version 2^>nul') do set "DOTNET_VER=%%v"

:MENU
cls
echo.
echo  ===============================================================
echo.
echo                DR MyDesk - Business Management Platform
echo                   Version 3.1  -  .NET 10 Blazor Server
echo.
echo  ===============================================================
echo.

if "%IS_ADMIN%"=="YES" (
    echo    [OK]   Administrator        Full access to all options
) else (
    echo    [ !]   Standard User        Options 1, 2 will request elevation
)

if defined DOTNET_VER (
    echo    [OK]   .NET SDK !DOTNET_VER!       Ready to build
) else (
    echo    [!!]   .NET SDK missing    Install from https://dotnet.microsoft.com
)

echo.
echo  ---------------------------------------------------------------
echo.
echo    SETUP ^& MAINTENANCE
echo.
echo      [1]  Database      Migrate Access DB to SQL Server
echo      [2]  IIS Deploy    Build and publish to local IIS
echo      [3]  Clean ^& Build Clean bin/obj and build solution
echo.
echo    RUN ^& TEST
echo.
echo      [4]  Launch        Standalone (Kestrel) or IIS
echo      [5]  Playwright    End-to-End Tests (UI, All, Trace)
echo.
echo    UTILITIES
echo.
echo      [6]  Status        Check IIS, Ports, DB, Environment
echo      [7]  Logs          View latest application logs
echo      [8]  Docs          Open README, Roadmap, Trace
echo.
echo    DEPLOY TO PRODUCTION
echo.
echo      [9]  Deploy       Deploy to techlight.digitalresponse.com.au (IIS)
echo.
echo      [Q]  Quit
echo.
echo  ---------------------------------------------------------------
echo.

set "choice="
set /p "choice=   Choose an option: "

if not defined choice goto MENU
if /i "%choice%"=="1" goto OPT_DATABASE
if /i "%choice%"=="2" goto OPT_IIS_DEPLOY
if /i "%choice%"=="3" goto OPT_CLEAN_BUILD
if /i "%choice%"=="4" goto OPT_RUN
if /i "%choice%"=="5" goto OPT_TESTS
if /i "%choice%"=="6" goto OPT_STATUS
if /i "%choice%"=="7" goto OPT_LOGS
if /i "%choice%"=="8" goto OPT_DOCS
if /i "%choice%"=="9" goto OPT_DEPLOY_PROD
if /i "%choice%"=="Q" goto END

echo.
echo    Invalid choice.
timeout /t 2 >nul
goto MENU

:: ============================================================================
::  OPTION 1: DATABASE
:: ============================================================================
:OPT_DATABASE
cls
echo.
echo  ===============================================================
echo   Database Migration - Access DB to SQL Server
echo  ===============================================================
echo.

if "%IS_ADMIN%"=="NO" (
    echo    Administrator privileges required.
    echo.
    echo    Relaunching as Administrator...
    powershell -Command "Start-Process cmd -ArgumentList '/c cd /d %ROOT% ^&^& Run.bat' -Verb RunAs" >nul 2>&1
    exit
)

set "INSTALL_PS=%DEPLOY%\Migration\Install.ps1"
if not exist "%INSTALL_PS%" (
    echo    ERROR: Install.ps1 not found.
    call :PAUSE_RETURN
    goto MENU
)

echo    Running database installation script...
powershell -ExecutionPolicy Bypass -File "%INSTALL_PS%"
call :PAUSE_RETURN
goto MENU

:: ============================================================================
::  OPTION 2: IIS DEPLOY
:: ============================================================================
:OPT_IIS_DEPLOY
cls
echo.
echo  ===============================================================
echo   IIS Deployment - Build and publish to local IIS
echo  ===============================================================
echo.

if "%IS_ADMIN%"=="NO" (
    echo    Administrator privileges required.
    echo.
    echo    Relaunching as Administrator...
    powershell -Command "Start-Process cmd -ArgumentList '/c cd /d %ROOT% ^&^& Run.bat' -Verb RunAs" >nul 2>&1
    exit
)

if not exist "%APPCMD%" (
    echo    ERROR: IIS is not installed.
    call :PAUSE_RETURN
    goto MENU
)

powershell -ExecutionPolicy Bypass -File "%DEPLOY%\Deploy.ps1"
call :PAUSE_RETURN
goto MENU

:: ============================================================================
::  OPTION 3: CLEAN ^& BUILD
:: ============================================================================
:OPT_CLEAN_BUILD
cls
echo.
echo  ===============================================================
echo   Clean ^& Build Solution
echo  ===============================================================
echo.
echo    [1]  Full Clean      Delete all bin/obj folders
echo    [2]  Build All       Restore and build entire solution
echo    [3]  Restore Only    NuGet packages restore
echo.
echo    [B]  Back
echo.

set "cchoice="
set /p "cchoice=   Choose: "

if /i "%cchoice%"=="1" (
    echo    Cleaning...
    for /d /r . %%d in (bin,obj) do @if exist "%%d" (
        echo      Deleting %%d
        rd /s /q "%%d" 2>nul
    )
    echo    Clean complete.
    timeout /t 2 >nul
    goto OPT_CLEAN_BUILD
)
if /i "%cchoice%"=="2" (
    echo    Building solution...
    dotnet build "%ROOT%\MyDesk.slnx"
    call :PAUSE_RETURN
    goto OPT_CLEAN_BUILD
)
if /i "%cchoice%"=="3" (
    echo    Restoring...
    dotnet restore "%ROOT%\MyDesk.slnx"
    echo    Restore complete.
    timeout /t 2 >nul
    goto OPT_CLEAN_BUILD
)
if /i "%cchoice%"=="B" goto MENU
goto OPT_CLEAN_BUILD

:: ============================================================================
::  OPTION 4: RUN
:: ============================================================================
:OPT_RUN
cls
echo.
echo  ===============================================================
echo   Run DR MyDesk
echo  ===============================================================
echo.
echo    [1]  Quick Launch (Kestrel)  http://localhost:5235
echo    [2]  IIS Site (Local)        http://localhost
echo.
echo    [B]  Back
echo.

set "rchoice="
set /p "rchoice=   Choose: "

if /i "%rchoice%"=="1" goto RUN_STANDALONE
if /i "%rchoice%"=="2" goto RUN_IIS
if /i "%rchoice%"=="B" goto MENU
goto OPT_RUN

:RUN_STANDALONE
cls
echo  Starting Kestrel server...
netstat -ano | findstr ":5235" | findstr "LISTENING" >nul 2>&1
if %errorlevel%==0 (
    echo    Port 5235 is in use. Killing existing process...
    for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5235" ^| findstr "LISTENING"') do (
        taskkill /F /PID %%a >nul 2>&1
    )
)
start "" /B cmd /c "timeout /t 5 >nul && start http://localhost:5235"
pushd "%WEB%"
dotnet run --urls "http://localhost:5235"
popd
goto MENU

:RUN_IIS
cls
if not exist "%APPCMD%" ( echo ERROR: IIS missing. && pause && goto MENU )
if "%IS_ADMIN%"=="NO" (
    powershell -Command "Start-Process cmd -ArgumentList '/c cd /d %ROOT% ^&^& Run.bat' -Verb RunAs"
    exit
)
"%APPCMD%" start apppool /apppool.name:MyDesk >nul 2>&1
"%APPCMD%" start site /site.name:MyDesk >nul 2>&1
echo    IIS site started.
start "" "http://localhost"
timeout /t 2 >nul
goto MENU

:: ============================================================================
::  OPTION 5: PLAYWRIGHT TESTS
:: ============================================================================
:OPT_TESTS
cls
echo.
echo  ===============================================================
echo   Playwright End-to-End Tests
echo  ===============================================================
echo.
echo    [1]  Run All Tests (Headless)
echo    [2]  Open Playwright UI (Interactive)
echo    [3]  Trace Viewer (Open latest trace)
echo    [4]  Install Browsers (One-time setup)
echo.
echo    [B]  Back
echo.

set "tchoice="
set /p "tchoice=   Choose: "

if /i "%tchoice%"=="1" (
    echo    Running all tests...
    pushd "%TESTS%"
    dotnet test
    popd
    call :PAUSE_RETURN
    goto OPT_TESTS
)
if /i "%tchoice%"=="2" (
    echo    Opening Playwright UI...
    pushd "%TESTS%"
    set PWDEBUG=0
    npx playwright test --ui
    popd
    goto OPT_TESTS
)
if /i "%tchoice%"=="3" (
    echo    Searching for latest trace...
    pushd "%TESTS%"
    for /f "delims=" %%a in ('dir /s /b *.zip ^| findstr /i "trace"') do (
        set "LAST_TRACE=%%a"
    )
    if defined LAST_TRACE (
        npx playwright show-trace "!LAST_TRACE!"
    ) else (
        echo    No trace files found in %TESTS%
        timeout /t 3 >nul
    )
    popd
    goto OPT_TESTS
)
if /i "%tchoice%"=="4" (
    echo    Installing Playwright browsers...
    pushd "%TESTS%"
    dotnet build
    powershell -ExecutionPolicy Bypass -File "bin/Debug/net8.0/playwright.ps1" install
    popd
    call :PAUSE_RETURN
    goto OPT_TESTS
)
if /i "%tchoice%"=="B" goto MENU
goto OPT_TESTS

:: ============================================================================
::  OPTION 6: STATUS
:: ============================================================================
:OPT_STATUS
cls
echo.
echo  ===============================================================
echo   System Status
echo  ===============================================================
echo.
echo    .NET SDK:    !DOTNET_VER!
echo    Privileges:  %IS_ADMIN%
echo.
echo    Port 5235:   
netstat -ano | findstr ":5235" | findstr "LISTENING" >nul 2>&1 && echo    [ACTIVE] || echo    [FREE]
echo    Port 80:     
netstat -ano | findstr ":80 " | findstr "LISTENING" >nul 2>&1 && echo    [ACTIVE] || echo    [FREE]
echo.
if exist "%APPCMD%" (
    echo    IIS Site:
    "%APPCMD%" list site "MyDesk"
)
echo.
echo    Latest Log:
dir /b /o-d "%LOGS%\app-*.log" 2>nul | findstr "^" >nul && (
    for /f "delims=" %%a in ('dir /b /o-d "%LOGS%\app-*.log"') do (
        echo      %%a
        goto :SKIP_LOG_LIST
    )
) || echo      No logs found.
:SKIP_LOG_LIST
call :PAUSE_RETURN
goto MENU

:: ============================================================================
::  OPTION 7: LOGS
:: ============================================================================
:OPT_LOGS
cls
echo.
echo  ===============================================================
echo   Application Logs
echo  ===============================================================
echo.
echo    [1]  Open Logs Folder
echo    [2]  View Latest App Log (Tail)
echo    [3]  View Latest Error Log
echo    [4]  Clear All Logs
echo.
echo    [B]  Back
echo.

set "lchoice="
set /p "lchoice=   Choose: "

if /i "%lchoice%"=="1" start "" "%LOGS%"
if /i "%lchoice%"=="2" (
    for /f "delims=" %%a in ('dir /b /o-d "%LOGS%\app-*.log"') do (
        powershell -Command "Get-Content '%LOGS%\%%a' -Wait -Tail 50"
        goto MENU
    )
)
if /i "%lchoice%"=="3" (
    for /f "delims=" %%a in ('dir /b /o-d "%LOGS%\errors-*.log"') do (
        start notepad "%LOGS%\%%a"
        goto MENU
    )
)
if /i "%lchoice%"=="4" (
    del /q "%LOGS%\*.log"
    echo    Logs cleared.
    timeout /t 2 >nul
)
if /i "%lchoice%"=="B" goto MENU
goto OPT_LOGS

:: ============================================================================
::  OPTION 8: DOCS
:: ============================================================================
:OPT_DOCS
cls
echo.
echo    [1]  Main README
echo    [2]  Product Roadmap
echo    [3]  Testing Guide
echo    [4]  IIS Deployment Guide
echo.
echo    [B]  Back
echo.

set "dchoice="
set /p "dchoice=   Choose: "

if /i "%dchoice%"=="1" start "" "%ROOT%\README.md"
if /i "%dchoice%"=="2" start "" "%ROOT%\PRODUCT_ROADMAP.md"
if /i "%dchoice%"=="3" start "" "%ROOT%\TESTING.md"
if /i "%dchoice%"=="4" start "" "%DEPLOY%\README.md"
if /i "%dchoice%"=="B" goto MENU
goto OPT_DOCS

:: ============================================================================
::  HELPERS
:: ============================================================================
:PAUSE_RETURN
echo.
echo  ---------------------------------------------------------------
echo  Press any key to return to the menu...
pause >nul
exit /b 0

:END
cls
echo.
echo    Thanks for using DR MyDesk. Goodbye!
echo.
timeout /t 1 >nul
exit /b 0
