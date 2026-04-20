@echo off
REM Double-click launcher for the Blazor app
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0Run.ps1" %*
pause
