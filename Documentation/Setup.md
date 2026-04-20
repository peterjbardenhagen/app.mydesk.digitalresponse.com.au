# Techlight MyDesk - Local Development Setup Guide

## Prerequisites

### Required Software

| Software | Version | Purpose | Download |
|----------|---------|---------|----------|
| Windows 10/11 Pro or Enterprise | Any | IIS requires Pro/Enterprise | - |
| IIS (Internet Information Services) | 10+ | Web server | Enable via Windows Features |
| ASP Classic Support | - | For VBScript pages | Part of IIS |
| .NET Framework 4.8 | 4.8 | For MyDeskASPNet | [Download](https://dotnet.microsoft.com/download/dotnet-framework) |
| Microsoft Access Database Engine | 2016 | For Access DB connectivity | [Download](https://www.microsoft.com/en-us/download/details.aspx?id=54920) |
| Visual Studio 2022 | Community | IDE (optional) | [Download](https://visualstudio.microsoft.com/) |

---

## Installation Steps

### 1. Enable IIS with ASP Classic

#### Via PowerShell (Admin):
```powershell
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASP -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45 -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIExtensions -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIFilter -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WindowsAuthentication -All
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole -All
```

#### Via GUI:
1. Open **Control Panel** > **Programs** > **Turn Windows features on or off**
2. Check the following:
   - ☑ **Internet Information Services**
     - ☑ **Web Management Tools** > **IIS Management Console**
     - ☑ **World Wide Web Services**
       - ☑ **Application Development Features**
         - ☑ **ASP**
         - ☑ **ASP.NET 4.8**
         - ☑ **ISAPI Extensions**
         - ☑ **ISAPI Filters**
       - ☑ **Common HTTP Features** (all)
       - ☑ **Security** > **Windows Authentication**

### 2. Install .NET Framework 4.8

Download and install from: https://dotnet.microsoft.com/download/dotnet-framework/net48

Verify installation:
```powershell
Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP' -Recurse |
  Get-ItemProperty -Name Version -ErrorAction SilentlyContinue |
  Where-Object { $_.Version -like '4.8*' }
```

### 3. Install Microsoft Access Database Engine

Download the Access Database Engine (for ODBC connectivity to .mdb files):
https://www.microsoft.com/en-us/download/details.aspx?id=54920

Choose the version matching your Office install (32-bit or 64-bit).

---

## IIS Configuration

### 1. Create Application Pool for ASP Classic

```powershell
Import-Module WebAdministration
New-Item -Path "IIS:\AppPools\TechlightMyDesk" -ItemType "AppPool"
Set-ItemProperty -Path "IIS:\AppPools\TechlightMyDesk" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\TechlightMyDesk" -Name "enable32BitAppOnWin64" -Value "true"
Set-ItemProperty -Path "IIS:\AppPools\TechlightMyDesk" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
```

### 2. Create Application Pool for ASP.NET

```powershell
New-Item -Path "IIS:\AppPools\TechlightMyDeskNet" -ItemType "AppPool"
Set-ItemProperty -Path "IIS:\AppPools\TechlightMyDeskNet" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty -Path "IIS:\AppPools\TechlightMyDeskNet" -Name "enable32BitAppOnWin64" -Value "true"
Set-ItemProperty -Path "IIS:\AppPools\TechlightMyDeskNet" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
```

### 3. Create Main Website

```powershell
# Create website
$sitePath = "C:\Development\Techlight.digitalresponse.com.au"
New-Website -Name "TechlightMyDesk" -Port 80 -PhysicalPath $sitePath -ApplicationPool "TechlightMyDesk"

# Create MyDeskASPNet virtual application
New-WebApplication -Name "MyDeskASPNet" -Site "TechlightMyDesk" -PhysicalPath "$sitePath\MyDeskASPNet" -ApplicationPool "TechlightMyDeskNet"
```

### 4. Configure Authentication

```powershell
# Enable Anonymous Authentication
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/anonymousAuthentication" -Name "enabled" -Value "true" -PSPath "IIS:\Sites\TechlightMyDesk"

# Disable Windows Authentication (use Anonymous for forms-based auth)
Set-WebConfigurationProperty -Filter "/system.webServer/security/authentication/windowsAuthentication" -Name "enabled" -Value "false" -PSPath "IIS:\Sites\TechlightMyDesk"
```

### 5. Configure Default Documents

```powershell
Add-WebConfigurationProperty -PSPath 'IIS:\Sites\TechlightMyDesk' -Filter "system.webServer/defaultDocument" -Name "files" -Value @{value="Default.asp"}
Add-WebConfigurationProperty -PSPath 'IIS:\Sites\TechlightMyDesk' -Filter "system.webServer/defaultDocument" -Name "files" -Value @{value="Default.aspx"}
Add-WebConfigurationProperty -PSPath 'IIS:\Sites\TechlightMyDesk' -Filter "system.webServer/defaultDocument" -Name "files" -Value @{value="index.asp"}
```

---

## Directory Structure Setup

### 1. Project Location

Create the following structure:
```
C:\Development\
└── Techlight.digitalresponse.com.au\
    ├── Clients\                    (main app code)
    │   ├── SalesEngine\           (legacy shared)
    │   └── SalesEngineTL\         (active code)
    ├── System\                     (shared includes)
    ├── MyDeskASPNet\              (.NET app for PDFs)
    ├── Database\                  (Access DB - create this)
    └── ...
```

### 2. Create Database Directory

```powershell
New-Item -ItemType Directory -Path "C:\Database" -Force
# Copy Techlight2.mdb to this location
```

### 3. Set Permissions

```powershell
# Grant IIS_IUSRS read/write to project folder
$path = "C:\Development\Techlight.digitalresponse.com.au"
$identity = "IIS_IUSRS"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl = Get-Acl $path
$acl.SetAccessRule($accessRule)
Set-Acl $path $acl

# Grant access to Database folder
$dbPath = "C:\Database"
$acl2 = Get-Acl $dbPath
$acl2.SetAccessRule($accessRule)
Set-Acl $dbPath $acl2
```

---

## Database Configuration

### 1. ODBC DSN (Optional but recommended)

Create System DSN for easier connection:

1. Open **ODBC Data Sources (64-bit)** or **(32-bit)**
2. Go to **System DSN** tab > **Add**
3. Select **Microsoft Access Driver (*.mdb, *.accdb)**
4. Data Source Name: `TechlightMyDesk`
5. Database: Select `C:\Database\Techlight2.mdb`

### 2. Connection String Update

Update `/System/ssi_dbConn_open_TL.inc` if database location differs:
```vbscript
MyDB = "C:\Database\Techlight2.mdb"
```

---

## Localhost Configuration

### 1. Hosts File Entry (Optional)

Add to `C:\Windows\System32\drivers\etc\hosts`:
```
127.0.0.1  techlight.digitalresponse.com.au.local
127.0.0.1  techlight.local
```

### 2. Bindings

Add to IIS site:
```powershell
New-WebBinding -Name "TechlightMyDesk" -Protocol "http" -Port 80 -IPAddress "*" -HostHeader "techlight.local"
```

---

## Testing the Setup

### 1. Test ASP Classic

Browse to: `http://localhost/Clients/SalesEngineTL/Portal/Validate.asp`

Should show login form (even with error, it means ASP is working).

### 2. Test Database Connection

Create test file `test_db.asp`:
```asp
<%@ Language=VBScript %>
<!--#include virtual="/System/ssi_dbConn_open_TL.inc"-->
<%
If Err.Number <> 0 Then
    Response.Write "Error: " & Err.Description
Else
    Response.Write "Database connection successful!"
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
```

Browse to: `http://localhost/test_db.asp`

### 3. Test .NET Application

Browse to: `http://localhost/MyDeskASPNet/UnitTests.aspx`

Should show the unit tests page without errors.

---

## Common Issues & Solutions

### Issue: "ActiveX component can't create object" 
**Cause:** Access Database Engine not installed or 32/64-bit mismatch
**Solution:** Install Access Database Engine matching IIS bitness

### Issue: "HTTP Error 404.3 - Not Found"
**Cause:** ASP not enabled in IIS
**Solution:** Enable ASP feature in Windows

### Issue: "Permission denied" on database
**Cause:** IIS_IUSRS doesn't have write access
**Solution:** Grant Modify permissions to C:\Database folder

### Issue: PDF generation fails
**Cause:** ABCpdf not installed or MyDeskASPNet app pool misconfigured
**Solution:** Ensure .NET 4.8 app pool, 32-bit enabled

---

## Visual Studio Setup (Optional)

### 1. Open Project

File > Open > Website > File System > Select `C:\Development\Techlight.digitalresponse.com.au`

### 2. Configure External Web Server

Project Properties > Start Options > Start URL:
```
http://localhost/Clients/SalesEngineTL/Portal/Validate.asp
```

### 3. Attach to Process for Debugging ASP.NET

Debug > Attach to Process > `w3wp.exe` (requires admin)

---

## Quick Verification Checklist

- [ ] IIS installed and running
- [ ] ASP Classic enabled
- [ ] .NET Framework 4.8 installed
- [ ] Access Database Engine installed
- [ ] Website created in IIS
- [ ] MyDeskASPNet virtual application created
- [ ] Application pools configured (Classic for ASP, .NET for MyDeskASPNet)
- [ ] 32-bit enabled on app pools
- [ ] Database file in C:\Database
- [ ] IIS_IUSRS has permissions to project folder
- [ ] IIS_IUSRS has permissions to C:\Database
- [ ] Default documents configured (Default.asp)
- [ ] Anonymous authentication enabled
- [ ] Can browse to http://localhost/
- [ ] Can browse to http://localhost/MyDeskASPNet/

