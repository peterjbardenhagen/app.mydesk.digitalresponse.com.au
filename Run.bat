@echo off
setlocal enabledelayedexpansion
title DR MyDesk Launcher
color 0B

:: Paths
set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"
set "SRC=%ROOT%\src"
set "WEB=%SRC%\MyDesk.Web"
set "TESTS=%ROOT%\tests\MyDesk.PlaywrightTests"
set "DEPLOY=%SRC%\Deployment"
set "LOGS=%WEB%\Logs"

:MENU
cls
echo.
echo  ===============================================================
echo                DR MyDesk - Business Management Platform
echo  ===============================================================
echo.
echo    [1]  Database      Migrate
echo    [2]  IIS Deploy    Build/Publish
echo    [3]  Clean ^& Build Clean bin/obj
echo    [4]  Launch        Standalone (Kestrel)
echo    [5]  Playwright    End-to-End Tests
echo    [6]  Status        System Status
echo    [7]  Logs          View logs
echo    [8]  Docs          Open Docs
echo    [Q]  Quit
echo.
set "choice="
set /p "choice=   Choose: "

if /i "%choice%"=="1" goto OPT_DATABASE
if /i "%choice%"=="3" goto OPT_CLEAN_BUILD
if /i "%choice%"=="4" goto OPT_RUN
if /i "%choice%"=="5" goto OPT_TESTS
if /i "%choice%"=="6" goto OPT_STATUS
if /i "%choice%"=="7" goto OPT_LOGS
if /i "%choice%"=="Q" exit

goto MENU

:OPT_DATABASE
echo Running database migration...
:: Add your migration commands here
pause
goto MENU

:OPT_CLEAN_BUILD
echo Cleaning...
for /d /r . %%d in (bin,obj) do @if exist "%%d" rd /s /q "%%d"
echo Building...
dotnet build "%ROOT%\MyDesk.sln"
pause
goto MENU

:OPT_RUN
echo Checking for existing server processes...
for /f "tokens=5" %%p in ('netstat -ano ^| findstr :5237') do (
    echo Killing process on port 5237 ^(PID: %%p^)...
    taskkill /F /PID %%p >nul 2>&1
)
echo Starting server...
dotnet run --project "%WEB%" --urls "http://localhost:5237"
goto MENU

:OPT_TESTS
echo Running Playwright tests...
pushd "%TESTS%"
dotnet test
popd
pause
goto MENU

:OPT_STATUS
echo Checking system status...
netstat -ano | findstr ":5237"
pause
goto MENU

:OPT_LOGS
echo Opening logs...
start "" "%LOGS%"
goto MENU
