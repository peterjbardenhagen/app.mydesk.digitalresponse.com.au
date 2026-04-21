@echo off
setlocal enabledelayedexpansion
title DR MyDesk Launcher

:: ============================================================================
::  DR MyDesk - Launcher
::  Version 3.0 - April 2026
:: ============================================================================

:: Paths
set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "SRC=%ROOT%\src"
set "WEB=%SRC%\MyDesk.Web"
set "DEPLOY=%SRC%\Deployment"
set "TESTS=%ROOT%\tests\MyDesk.PlaywrightTests"
set "APPCMD=%SystemRoot%\System32\inetsrv\appcmd.exe"

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
echo                   Version 3.0  -  .NET 8 Blazor Server
echo.
echo  ===============================================================
echo.

if "%IS_ADMIN%"=="YES" (
    echo    [OK]   Administrator        Full access to all options
) else (
    echo    [ !]   Standard User        Options 1, 2, 3 will request elevation
)

if defined DOTNET_VER (
    echo    [OK]   .NET SDK !DOTNET_VER!       Ready to build
) else (
    echo    [!!]   .NET SDK missing    Install from https://dotnet.microsoft.com
)

echo.
echo  ---------------------------------------------------------------
echo.
echo    SETUP  (run once, in order)
echo.
echo      [1]  Database      Migrate Access DB to SQL Server
echo      [2]  IIS Deploy    Build and publish to local IIS
echo      [3]  Run Tests     Playwright E2E tests (72+)
echo.
echo    RUN THE APP
echo.
echo      [4]  Launch        Standalone (Kestrel) or IIS
echo.
echo    INFO
echo.
echo      [5]  Status        Check IIS site, ports, processes
echo      [6]  Open Docs     README.md / TESTING.md
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
if /i "%choice%"=="3" goto OPT_TESTS
if /i "%choice%"=="4" goto OPT_RUN
if /i "%choice%"=="5" goto OPT_STATUS
if /i "%choice%"=="6" goto OPT_DOCS
if /i "%choice%"=="Q" goto END

echo.
echo    Invalid choice. Try 1, 2, 3, 4, 5, 6 or Q.
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
    echo    Expected: %INSTALL_PS%
    call :PAUSE_RETURN
    goto MENU
)

echo    This will run the database installation script:
echo    %INSTALL_PS%
echo.
set "reply="
set /p "reply=   Continue? (Y/N): "
if /i not "%reply%"=="Y" goto MENU

echo.
powershell -ExecutionPolicy Bypass -File "%INSTALL_PS%"
if errorlevel 1 (
    echo.
    echo    Database setup reported errors.
) else (
    echo.
    echo    Database setup complete.
)
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
    echo    ERROR: IIS is not installed on this machine.
    echo.
    echo    To install IIS:
    echo      1. Open "Turn Windows features on or off"
    echo      2. Enable: Internet Information Services
    echo      3. Enable ASP.NET Core Module under World Wide Web Services
    call :PAUSE_RETURN
    goto MENU
)

set "DEPLOY_PS=%DEPLOY%\Deploy.ps1"
if not exist "%DEPLOY_PS%" (
    echo    ERROR: Deploy.ps1 not found.
    echo    Expected: %DEPLOY_PS%
    call :PAUSE_RETURN
    goto MENU
)

echo    This will:
echo      - Build Release version of DR MyDesk
echo      - Create IIS App Pool "Techlight.MyDesk"
echo      - Deploy to C:\inetpub\wwwroot\Techlight.MyDesk
echo      - Start site on http://localhost
echo.
set "reply="
set /p "reply=   Deploy now? (Y/N): "
if /i not "%reply%"=="Y" goto MENU

echo.
echo    Running Deploy.ps1...
echo.
powershell -ExecutionPolicy Bypass -File "%DEPLOY_PS%"

if errorlevel 1 (
    echo.
    echo    Deployment failed. See errors above.
    call :PAUSE_RETURN
    goto MENU
)

echo.
echo    Deployment successful!
echo    Opening http://localhost in your browser...
timeout /t 2 >nul
start "" "http://localhost"
call :PAUSE_RETURN
goto MENU

:: ============================================================================
::  OPTION 3: TESTS
:: ============================================================================
:OPT_TESTS
cls
echo.
echo  ===============================================================
echo   Playwright Tests - 72+ end-to-end tests
echo  ===============================================================
echo.

if not exist "%TESTS%" (
    echo    ERROR: Test project not found.
    echo    Expected: %TESTS%
    call :PAUSE_RETURN
    goto MENU
)

echo    This will run all Playwright tests covering:
echo      - Login / Dashboard / Quotes / Invoices / POs
echo      - Navigation / Accessibility / E2E Workflows
echo.
echo    NOTE: The app must be running at http://localhost:5235
echo          or http://localhost (IIS) before tests will pass.
echo.
set "reply="
set /p "reply=   Run tests? (Y/N): "
if /i not "%reply%"=="Y" goto MENU

echo.
pushd "%TESTS%"
dotnet test --logger "console;verbosity=normal"
set "TEST_RESULT=%errorlevel%"
popd

echo.
if "%TEST_RESULT%"=="0" (
    echo    All tests passed!
) else (
    echo    Some tests failed. Check output above.
    echo    Screenshots: %TESTS%\screenshots
)
call :PAUSE_RETURN
goto MENU

:: ============================================================================
::  OPTION 4: RUN DR MYDESK
:: ============================================================================
:OPT_RUN
cls
echo.
echo  ===============================================================
echo   Run DR MyDesk - Choose how to launch
echo  ===============================================================
echo.
echo    [1]  Standalone (Kestrel)    Quick dev server
echo         URL: http://localhost:5235
echo         No Administrator needed
echo.
echo    [2]  Local IIS               Production-like
echo         URL: http://localhost
echo         Requires prior deploy (Option 2 on main menu)
echo         Requires Administrator
echo.
echo    [B]  Back to main menu
echo.

set "runchoice="
set /p "runchoice=   Choose: "

if /i "%runchoice%"=="1" goto RUN_STANDALONE
if /i "%runchoice%"=="2" goto RUN_IIS
if /i "%runchoice%"=="B" goto MENU
goto OPT_RUN

:RUN_STANDALONE
cls
echo.
echo  ===============================================================
echo   Standalone Server - Kestrel on http://localhost:5235
echo  ===============================================================
echo.

if not defined DOTNET_VER (
    echo    ERROR: .NET SDK not found.
    echo    Install .NET 8 SDK from https://dotnet.microsoft.com/download
    call :PAUSE_RETURN
    goto MENU
)

if not exist "%WEB%" (
    echo    ERROR: Web project not found at %WEB%
    call :PAUSE_RETURN
    goto MENU
)

:: Check if port 5235 is in use
netstat -ano | findstr ":5235" | findstr "LISTENING" >nul 2>&1
if %errorlevel%==0 (
    echo    WARNING: Port 5235 is already in use.
    echo    Another DR MyDesk instance may be running.
    echo.
    set "reply="
    set /p "reply=   Kill existing process and start fresh? (Y/N): "
    if /i "!reply!"=="Y" (
        echo    Stopping existing process...
        for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5235" ^| findstr "LISTENING"') do (
            taskkill /F /PID %%a >nul 2>&1
        )
        timeout /t 1 >nul
    ) else (
        goto MENU
    )
)

echo    Starting Kestrel on http://localhost:5235...
echo    Press Ctrl+C to stop.
echo.
timeout /t 2 >nul

:: Open browser after 6 seconds
start "" /B cmd /c "timeout /t 6 >nul && start http://localhost:5235"

pushd "%WEB%"
dotnet run --urls "http://localhost:5235"
popd

echo.
echo    Server stopped.
call :PAUSE_RETURN
goto MENU

:RUN_IIS
cls
echo.
echo  ===============================================================
echo   Local IIS - http://localhost
echo  ===============================================================
echo.

if not exist "%APPCMD%" (
    echo    ERROR: IIS is not installed.
    call :PAUSE_RETURN
    goto MENU
)

"%APPCMD%" list site "Techlight.MyDesk" >nul 2>&1
if errorlevel 1 (
    echo    WARNING: IIS site "Techlight.MyDesk" does not exist.
    echo    You need to deploy first (Option 2 on main menu).
    echo.
    set "reply="
    set /p "reply=   Go to deployment now? (Y/N): "
    if /i "!reply!"=="Y" goto OPT_IIS_DEPLOY
    goto MENU
)

if "%IS_ADMIN%"=="YES" (
    "%APPCMD%" start apppool /apppool.name:Techlight.MyDesk >nul 2>&1
    "%APPCMD%" start site /site.name:Techlight.MyDesk >nul 2>&1
    echo    IIS site is running.
    echo    Opening http://localhost...
    timeout /t 1 >nul
    start "" "http://localhost"
) else (
    echo    Requesting Administrator to start IIS...
    start powershell -Verb RunAs -Command "& '%APPCMD%' start apppool /apppool.name:Techlight.MyDesk; & '%APPCMD%' start site /site.name:Techlight.MyDesk; Start-Process 'http://localhost'; Write-Host 'DR MyDesk is running at http://localhost' -ForegroundColor Green; Read-Host 'Press Enter to close'"
)
call :PAUSE_RETURN
goto MENU

:: ============================================================================
::  OPTION 5: STATUS
:: ============================================================================
:OPT_STATUS
cls
echo.
echo  ===============================================================
echo   System Status
echo  ===============================================================
echo.
echo    ENVIRONMENT
echo.

if defined DOTNET_VER (
    echo      [OK]  .NET SDK         !DOTNET_VER!
) else (
    echo      [!!]  .NET SDK         Not installed
)

if "%IS_ADMIN%"=="YES" (
    echo      [OK]  Privileges       Administrator
) else (
    echo      [ !]  Privileges       Standard User
)

if exist "%APPCMD%" (
    echo      [OK]  IIS              Installed
) else (
    echo      [!!]  IIS              Not installed
)

echo.
echo    IIS SITE
echo.
if exist "%APPCMD%" (
    "%APPCMD%" list site "Techlight.MyDesk" >nul 2>&1
    if errorlevel 1 (
        echo      [ -]  Techlight.MyDesk   Not deployed
    ) else (
        echo      [OK]  Techlight.MyDesk   Deployed
    )
) else (
    echo      IIS not available
)

echo.
echo    PORTS
echo.
netstat -ano | findstr ":5235" | findstr "LISTENING" >nul 2>&1
if %errorlevel%==0 (
    echo      [ACTIVE] Port 5235    Kestrel is running
) else (
    echo      [FREE]   Port 5235    Available
)

netstat -ano | findstr ":80 " | findstr "LISTENING" >nul 2>&1
if %errorlevel%==0 (
    echo      [ACTIVE] Port 80      IIS is running
) else (
    echo      [FREE]   Port 80      Available
)

echo.
echo    PATHS
echo      Root:    %ROOT%
echo      Web:     %WEB%
echo      Deploy:  %DEPLOY%
echo      Tests:   %TESTS%

call :PAUSE_RETURN
goto MENU

:: ============================================================================
::  OPTION 6: DOCS
:: ============================================================================
:OPT_DOCS
cls
echo.
echo  ===============================================================
echo   Documentation
echo  ===============================================================
echo.
echo    [1]  README.md             Main project overview
echo    [2]  TESTING.md            Test suite guide
echo    [3]  Deployment README     IIS and production
echo    [4]  CHANGELOG.md          Release notes
echo.
echo    [B]  Back
echo.

set "docchoice="
set /p "docchoice=   Choose: "

if /i "%docchoice%"=="1" start "" "%ROOT%\README.md"
if /i "%docchoice%"=="2" start "" "%ROOT%\TESTING.md"
if /i "%docchoice%"=="3" start "" "%DEPLOY%\README.md"
if /i "%docchoice%"=="4" start "" "%ROOT%\CHANGELOG.md"
if /i "%docchoice%"=="B" goto MENU

timeout /t 1 >nul
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
