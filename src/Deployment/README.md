# DR MyDesk — Deployment Guide

Deployment scripts and tools for DR MyDesk (.NET 8 Blazor Server).

---

## Deployment Process (Start to Finish)

### Phase 1: Local Development (IIS)

**1. Database Setup**
```powershell
# First time only - run database installation
cd Migration
.\Install.ps1
```

**2. Local IIS Deployment**
```powershell
# Run as Administrator
cd C:\Development\Techlight.digitalresponse.com.au\src\Deployment
.\Deploy.ps1
```

This will:
- Build Release version
- Create IIS App Pool: `Techlight.MyDesk`
- Create IIS Site on port 80
- Deploy to `C:\inetpub\wwwroot\Techlight.MyDesk`
- Start the application

**Access:** http://localhost

---

### Phase 2: Production Server (VM)

**Prerequisites on Production Server:**
- Windows Server 2019+ with IIS
- .NET 8 Hosting Bundle ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- SQL Server 2019+
- Administrator privileges

**1. Copy Files to Server**
```powershell
# From development machine, copy entire publish folder
robocopy "C:\Development\Techlight.digitalresponse.com.au\src\Deployment\publish" "\\SERVER\C$\Deploy\MyDesk" /MIR
```

**2. Run Deploy.ps1 on Server**
```powershell
# On production server, run as Administrator
cd C:\Deploy\MyDesk
.\Deploy.ps1
```

**3. Update Production Config**

Edit `C:\inetpub\wwwroot\Techlight.MyDesk\appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "TechlightDb": "Server=PROD-SQL-01;Database=Techlight_MyDesk;User Id=mydesk_app;Password=***;"
  },
  "Azure": {
    "OpenAIApiKey": "***",
    "OpenAIEndpoint": "https://your-prod-resource.openai.azure.com"
  },
  "Email": {
    "SmtpHost": "smtp.office365.com",
    "SmtpPort": "587",
    "SmtpUser": "noreply@yourcompany.com.au",
    "SmtpPass": "***"
  }
}
```

**4. Configure SSL Certificate**
- In IIS Manager → Sites → Techlight.MyDesk
- Bindings → Add HTTPS binding
- Select SSL certificate
- Update URL to https://mydesk.yourcompany.com.au

---

## Files in This Folder

| File | Purpose |
|------|---------|
| `Deploy.ps1` | **Main deployment script** — builds, configures IIS, deploys |
| `Migration/` | Database migration scripts (Access → SQL Server) |
| `Migration/Install.ps1` | Database installation script |
| `Migration/README.md` | Migration documentation (historical) |

---

## Deploy.ps1 Usage

### Basic Deployment
```powershell
.\Deploy.ps1
```

### With Environment Parameter
```powershell
.\Deploy.ps1 -Environment Production
```

### What It Does
1. ✓ Checks for Administrator privileges (exits if not elevated)
2. ✓ Builds Release version via `dotnet publish`
3. ✓ Creates IIS App Pool `Techlight.MyDesk` (if doesn't exist)
4. ✓ Creates IIS Site on port 80 (if doesn't exist)
5. ✓ Stops App Pool
6. ✓ Copies files to `C:\inetpub\wwwroot\Techlight.MyDesk`
7. ✓ Starts App Pool
8. ✓ Reports success

---

## Database Migration (Legacy)

**Status:** Migration from Classic ASP + Access **COMPLETE** (April 2026)

The `Migration/` folder contains:
- `PostMigrationFixes.sql` — Post-migration cleanup
- `Cleanup-LegacyTables.sql` — Remove old tables
- `Install.ps1` — Database setup script
- `README.md` — Migration documentation

These are for **historical reference only**. New deployments do not require migration.

---

## Troubleshooting

### Deploy.ps1 Errors

**Error:** "This script must run as Administrator"
- **Fix:** Right-click PowerShell → Run as Administrator

**Error:** "appcmd.exe not found"
- **Fix:** Install IIS via Windows Features

**Error:** "Access denied" when creating directory
- **Fix:** Ensure running as Administrator

**Error:** Build failed
- **Fix:** Check .NET 8 SDK is installed: `dotnet --version`

### IIS Issues

**Site not starting**
- Check App Pool is running
- Check `appsettings.json` connection string
- Review Event Viewer → Windows Logs → Application

**500.19 error**
- Install .NET 8 Hosting Bundle
- Restart IIS: `iisreset`

**Database connection fails**
- Verify SQL Server is running
- Check connection string in `appsettings.Production.json`
- Ensure SQL Server allows remote connections (if on separate server)

---

## Production Checklist

Before deploying to production:

- [ ] .NET 8 Hosting Bundle installed
- [ ] SQL Server configured and accessible
- [ ] Connection string updated in `appsettings.Production.json`
- [ ] SMTP settings configured for email
- [ ] Azure OpenAI keys configured (if using Ask AI)
- [ ] SSL certificate installed and HTTPS binding configured
- [ ] Firewall rules allow HTTP/HTTPS traffic
- [ ] DNS points to production server
- [ ] Backup strategy in place for database
- [ ] Monitoring/logging configured

---

## Support

- **Email:** info@digitalresponse.com.au
- **Hours:** Monday–Friday, 9am–5pm AEST
- **Documentation:** See main README.md

---

**Last Updated:** April 2026  
**Deployment Method:** IIS + ASP.NET Core Module  
**Platform:** .NET 8 Blazor Server
