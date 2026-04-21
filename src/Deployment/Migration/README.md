# Techlight MyDesk - Migration History

This folder contains legacy deployment and migration scripts from the Classic ASP version of Techlight MyDesk.

## Status: MIGRATION COMPLETE

**Migration Date:** April 2026  
**New Stack:** .NET 8 Blazor Server + MudBlazor + SQL Server  
**Legacy Stack:** Classic ASP + Access Database → SQL Server

## Contents

### Setup.ps1
Original setup script for the Classic ASP version. Handled:
- IIS configuration with Classic ASP support
- SQL Server Express installation
- Access database migration
- File permission setup

**Superseded by:** `..\Deploy.ps1`

### Install.ps1
Original installation script. Handled:
- Prerequisites installation
- Database setup
- Application configuration

**Superseded by:** `..\Deploy.ps1`

## Migration Summary

### What Changed

| Component | Legacy | New |
|-----------|--------|-----|
| Framework | Classic ASP | .NET 8 Blazor Server |
| UI | HTML/CSS/JS | MudBlazor Components |
| Database | Access → SQL Server | SQL Server (direct) |
| Hosting | IIS + ASP | IIS + ASP.NET Core Module |
| Deployment | Setup.ps1 / Install.ps1 | Deploy.ps1 |

### Migration Process

1. ✅ Database migrated from Access to SQL Server
2. ✅ Application rewritten in .NET 8 Blazor
3. ✅ UI modernized with MudBlazor
4. ✅ Deployment scripts updated
5. ✅ Legacy scripts archived to this folder

## Current State

The Techlight MyDesk application now runs on:
- **.NET 8 Blazor Server**
- **MudBlazor UI Components**
- **SQL Server Database**
- **IIS with ASP.NET Core Module**

All legacy components have been removed or migrated.

## For New Deployments

Use the new deployment script:
```powershell
cd ..\Deploy.ps1
```

## For Historical Reference

These scripts are kept to document:
- The evolution of the application
- Historical deployment procedures
- Migration effort completed

**Do not use these scripts for production deployment.**

---

**Last Updated:** April 2026  
**Migration:** COMPLETE
