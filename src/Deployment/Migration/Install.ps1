# Techlight MyDesk - Legacy Install Script (ASP Classic)
# =======================================================
# MIGRATED: April 2026
# This script was used for installing the Classic ASP version.
# The new Blazor application uses ..\Deploy.ps1 instead.
#
# This file is kept for historical reference only.

Write-Host @"
================================================================================
LEGACY SCRIPT - NOT FOR PRODUCTION USE
================================================================================

This Install.ps1 script was used for the Classic ASP version of Techlight MyDesk.

The new .NET 8 Blazor Server application uses:
  .\Deploy.ps1

Migration completed: April 2026
New stack: .NET 8 + Blazor Server + MudBlazor + SQL Server

This file is kept in Deployment/Migration/ for historical reference only.
================================================================================
"@ -ForegroundColor Yellow

exit 0
