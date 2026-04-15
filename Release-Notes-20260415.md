# Techlight MyDesk - Release Notes

**Release Date:** April 15, 2026

---

## Summary

This release includes IIS setup automation improvements, project cleanup, SendGrid credential updates, and GitHub repository initialization.

---

## Changes

### 1. Setup.ps1 - PowerShell 7 Compatibility

**Fixed:**
- Replaced `WebAdministration` module (incompatible with PowerShell 7) with `appcmd.exe` for all IIS operations
- Removed `IIS:` drive dependency that caused "drive not found" errors
- Fixed string interpolation issues with colons in binding strings

**Added:**
- `Test-ASPPHandler` function to verify ASP handler configuration
- Automatic detection and repair of missing `asp.dll`
- Automatic handler mapping for `.asp` files if missing
- Updated troubleshooting section with 404.3 error guidance

### 2. Project Cleanup (Audit.md)

**Deleted Files:**
- `System/Thumbs.db` - Windows thumbnail cache
- `images/Thumbs.db` - Windows thumbnail cache
- `Clients/SalesEngineTL/Images/Thumbs.db` - Windows thumbnail cache
- `System/TTL2.new.mdb` - Empty placeholder file

**Deleted Folders:**
- `Quotes/` - Empty folder
- `data/` - Empty folder
- `bin/` - Empty folder
- `App_Code/` - Empty folder

**Code Cleanup:**
- `System/Var.asp` - Removed VA and TT client conditions, kept only TL (Techlight)
- `System/ssi_dbConn_open.inc` - Removed unused database connections (CL, PL, TT, TG, TGA, VA), kept only TL (Techlight2.mdb)

**Created:**
- `.gitignore` - Git ignore file with patterns for build artifacts, Windows files, IDE files, PDFs, and config backups

### 3. SendGrid Credential Update

**Updated:** All 12 email processing files to use new SendGrid credentials:
- `Clients/SalesEngineTL/Quotes/Email_Proc.asp`
- `Clients/SalesEngineTL/Invoices/Email_Proc.asp`
- `Clients/SalesEngineTL/Invoices/EmailDeliveryNote_Proc.asp`
- `Clients/SalesEngineTL/Invoices/Fax_Proc.asp`
- `Clients/SalesEngineTL/JobOrders/Email_Proc.asp`
- `Clients/SalesEngineTL/PurchaseOrders/Email_Proc.asp`
- `Clients/SalesEngineTL/RFQ/Email_Proc.asp`
- `Errors/500-100.asp`
- `System/ssi_Errors.asp`
- `System/ssi_Functions.asp`
- `Temp/sendmail2.asp`
- `Temp/test.asp`

**New Credentials:**
- Username: `apikey4`
- Password: `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`

### 4. GitHub Repository

**Created:**
- Repository: `techlight.digitalresponse.com.au-new`
- URL: https://github.com/peterjbardenhagen/techlight.digitalresponse.com.au-new
- Visibility: Private
- Initial commit with 400+ files

---

## Files Modified

| File | Change |
|------|--------|
| `Setup.ps1` | Major rewrite for PS7 compatibility + ASP handler fix |
| `System/Var.asp` | Removed VA/TT client conditions |
| `System/ssi_dbConn_open.inc` | Removed unused database references |
| `System/ssi_Functions.asp` | Updated SendGrid credentials |
| `System/ssi_Errors.asp` | Updated SendGrid credentials |
| `Clients/SalesEngineTL/*/Email_Proc.asp` | Updated SendGrid credentials (6 files) |
| `Errors/500-100.asp` | Updated SendGrid credentials |
| `Temp/*.asp` | Updated SendGrid credentials (2 files) |
| `.gitignore` | Created |

---

## Known Issues

- **IIS Setup:** If you encounter HTTP 404.3 errors when loading ASP files, run `Setup.ps1` as Administrator to configure the ASP handler
- **API Keys:** SendGrid credentials are hardcoded in multiple files. Consider moving to environment variables for better security in future.

---

## Next Steps

1. Run `Setup.ps1` as Administrator to configure local IIS environment
2. Copy `Techlight2.mdb` to `C:\Database`
3. Test the application at `http://localhost/Clients/SalesEngineTL/Portal/Validate.asp`
4. For PDF generation, ensure ABCpdf component is installed

---

## Documentation

- `Setup.md` - Detailed IIS setup instructions
- `Claude.md` - Project architecture documentation
- `Audit.md` - Project audit report (cleanup recommendations)
- `MyDesk-WIP-April-2026-Implementation-Plans.md` - Implementation plans for April 2026 features

