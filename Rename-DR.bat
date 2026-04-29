@echo off
echo ========================================
echo Renaming MyDesk*.* to MyDesk*.*
echo ========================================
echo.

REM Rename solution file
if exist "MyDesk.slnx" (
    echo Renaming MyDesk.slnx to MyDesk.slnx...
    ren "MyDesk.slnx" "MyDesk.slnx"
)

REM Rename project files in MyDesk.Web
if exist "MyDesk.Web\MyDesk.Web.csproj" (
    echo Renaming MyDesk.Web\MyDesk.Web.csproj to MyDesk.Web\MyDesk.Web.csproj...
    ren "MyDesk.Web\MyDesk.Web.csproj" "MyDesk.Web.csproj"
)

REM Rename project files in MyDesk.Shared
if exist "MyDesk.Shared\MyDesk.Shared.csproj" (
    echo Renaming MyDesk.Shared\MyDesk.Shared.csproj to MyDesk.Shared\MyDesk.Shared.csproj...
    ren "MyDesk.Shared\MyDesk.Shared.csproj" "MyDesk.Shared.csproj"
)

REM Rename web.config files
for %%F in ("MyDesk.slnx") do (
    if exist "%%~dpFweb.config" (
        echo Renaming web.config in solution directory...
        ren "%%~dpFweb.config" "web.config"
    )
)

REM Rename web.config in MyDesk.Web
if exist "MyDesk.Web\web.config" (
    echo Renaming MyDesk.Web\web.config...
    ren "MyDesk.Web\web.config" "web.config"
)

REM Rename web.config in MyDesk.Shared
if exist "MyDesk.Shared\web.config" (
    echo Renaming MyDesk.Shared\web.config...
    ren "MyDesk.Shared\web.config" "web.config"
)

REM Rename web.config in Deployment folder
if exist "Deployment\web.config" (
    echo Renaming Deployment\web.config...
    ren "Deployment\web.config" "web.config"
)

REM Rename web.config in publish folder
if exist "Deployment\publish\web.config" (
    echo Renaming Deployment\publish\web.config...
    ren "Deployment\publish\web.config" "web.config"
)

REM Rename web.config in src folder
if exist "src\web.config" (
    echo Renaming src\web.config...
    ren "src\web.config" "web.config"
)

echo.
echo ========================================
echo Renaming complete!
echo ========================================
echo.
echo Please update the following files manually:
echo   - Deploy-To-IIS.ps1
echo   - Deploy.ps1
echo   - install.ps1
echo   - deploy-files.ps1
echo   - web.config (IIS configuration)
echo   - Any references in code to MyDesk namespace
echo.
pause
