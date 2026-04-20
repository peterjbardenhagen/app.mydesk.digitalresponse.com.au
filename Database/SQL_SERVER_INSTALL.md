# SQL Server Express 2022 Installation Guide

## Quick Install (Automated)

### Option 1: Double-Click Installation
1. **Right-click** `Install-SqlExpress.bat`
2. Select **"Run as administrator"**
3. Follow the prompts
4. Wait 10-20 minutes for installation to complete

### Option 2: PowerShell Installation
```powershell
# Open PowerShell as Administrator
# Navigate to Database folder
cd "C:\Development\Techlight.digitalresponse.com.au\Database"

# Run installer
.\Install-SqlExpress.ps1
```

## Configuration Details

### Instance Name
- **Server:** `localhost\sqlserver`
- **TCP Port:** `1433` (default)

### Authentication Modes
Both authentication modes are enabled:

1. **Windows Authentication** (Recommended for local development)
   - Uses your Windows login
   - No password required

2. **SQL Server Authentication** (For legacy apps)
   - Username: `sa`
   - Password: `password`

### Connection Strings

**Windows Authentication:**
```
Server=localhost\sqlserver;Database=Techlight;Trusted_Connection=True;
```

**SQL Server Authentication:**
```
Server=localhost\sqlserver;Database=Techlight;User Id=sa;Password=password;
```

## Post-Installation Steps

### 1. Create the Database
After SQL Server is installed, create the Techlight database:

**Using SQL Server Management Studio (SSMS):**
```sql
CREATE DATABASE Techlight;
GO
```

**Or download SSMS:**
https://aka.ms/ssmsfullsetup

### 2. Verify Installation

**Test Windows Auth:**
```powershell
sqlcmd -S localhost\sqlserver -E -Q "SELECT @@VERSION"
```

**Test SQL Auth:**
```powershell
sqlcmd -S localhost\sqlserver -U sa -P password -Q "SELECT @@VERSION"
```

### 3. Run the Migration
Once SQL Server is running and the Techlight database exists:

```bash
cd "C:\Development\Techlight.digitalresponse.com.au\Database"
python migrate_access_to_sqlserver.py
```

## Troubleshooting

### "Administrator rights required"
**Solution:** Right-click → "Run as administrator"

### "Download failed"
**Solution:** Manually download from:
https://www.microsoft.com/en-us/sql-server/sql-server-downloads

Choose: **Express** → **Download** → **SQLEXPR_x64_ENU.exe**

### "Instance already exists"
**Solution:** Uninstall existing instance or choose a different instance name

**To uninstall:**
1. Control Panel → Programs → Microsoft SQL Server → Uninstall
2. Or use command: `.<installer> /ACTION=uninstall /INSTANCENAME=sqlserver`

### "Connection failed"
**Solution:** Check if services are running:
```powershell
Get-Service | Where-Object { $_.Name -like "*SQL*" }
```

Start services:
```powershell
Start-Service "MSSQL$SQLSERVER"
Start-Service "SQLSERVERAGENT"
```

### "Firewall blocked"
**Solution:** The installer automatically creates firewall rules. If manually configuring:

```powershell
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
```

## Security Warning

⚠️ **IMPORTANT:** The default SA password is `password` which is NOT secure.

**Change it immediately after installation:**

```sql
-- Connect to SQL Server and run:
ALTER LOGIN sa WITH PASSWORD = 'YourStrongPassword123!';
GO
```

Or use PowerShell:
```powershell
sqlcmd -S localhost\sqlserver -U sa -P password -Q "ALTER LOGIN sa WITH PASSWORD = 'YourStrongPassword123!'"
```

## Manual Installation (If Automated Fails)

If the automated script fails, install manually:

1. Download SQL Server Express:
   https://www.microsoft.com/en-us/sql-server/sql-server-downloads

2. Run installer: `SQLEXPR_x64_ENU.exe`

3. Choose **Custom** installation

4. Instance Configuration:
   - Named instance: `sqlserver`

5. Server Configuration:
   - SQL Server Database Engine: Start

6. Database Engine Configuration:
   - Authentication Mode: **Mixed Mode**
   - Enter SA password: `password`
   - Add Current User as SQL Server administrator

7. Complete installation

## Files Included

- `Install-SqlExpress.ps1` - PowerShell installation script
- `Install-SqlExpress.bat` - Batch file launcher (easier to run)
- `SQL_SERVER_INSTALL.md` - This documentation

## Requirements

- Windows 10/11 or Windows Server 2019+
- 2GB+ RAM
- 6GB+ disk space
- Administrator rights
- Internet connection (for download)

## Support

Microsoft SQL Server Documentation:
https://docs.microsoft.com/en-us/sql/sql-server/

SQL Server Express Download Page:
https://www.microsoft.com/en-us/sql-server/sql-server-downloads
