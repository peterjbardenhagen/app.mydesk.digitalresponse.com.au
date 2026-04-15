# Techlight MyDesk - Project Audit

## Overview

This audit identifies unused, empty, legacy, and potentially removable files and folders from the Techlight MyDesk project.

**Last Audited:** April 15, 2026

---

## 🔴 Empty Folders (Safe to Delete)

| Folder | Path | Size | Notes |
|--------|------|------|-------|
| FusionChart | `/FusionChart/` | 0 items | Empty - charting library referenced but files missing |
| Grids | `/Grids/` | 0 items | Empty - appears to be legacy |
| My Project | `/My Project/` | 0 items | Empty Visual Studio artifact |
| anonymous/webroot | `/anonymous/webroot/` | 0 items | Empty webroot folder |
| obj | `/obj/` | 8 items | Build artifacts - can be deleted (regenerated) |

---

## 🟡 Legacy/Orphan Folders (May Have Historical Value)

| Folder | Path | Items | Status |
|--------|------|-------|--------|
| SalesEngine | `/Clients/SalesEngine/` | 7 items | **Minimal usage** - Only `Portal/Validate.asp`, `ssi_Security.inc` actively used by SalesEngineTL |
| Guest/TT | `/Guest/TT/` | ~5 items | **Orphan** - For TTL (Todd Timber) client, no longer active |
| Guest/VA | `/Guest/VA/` | ~5 items | **Orphan** - For Vantage client, no longer active |
| App_Code/Clients/SalesEngineTT | `/App_Code/Clients/SalesEngineTT/` | 1 item | **Orphan** - Contains only one orphaned .vb file |

---

## 🟢 Active Folders (Keep)

| Folder | Path | Items | Purpose |
|--------|------|-------|---------|
| SalesEngineTL | `/Clients/SalesEngineTL/` | 404 items | **Primary application code** |
| System | `/System/` | 46 items | Shared includes, functions, styles |
| MyDeskASPNet | `/MyDeskASPNet/` | 222 items | .NET PDF generation app |
| Guest | `/Guest/` | 15 items | Login/unauthenticated pages |
| images | `/images/` | 1 item | Shared images |
| Database | `/Database/` | - | MS Access DB location (external) |

---

## 🔵 Unused/Legacy Code References

These code references exist but point to non-existent folders:

### In `/System/Var.asp`:
```vbscript
' SalesEngineVA - NO FOLDER EXISTS
If InStr(Request.ServerVariables("URL"), "SalesEngineVA") > 0 Then
    Response.Cookies("WorkingDir") = "/Clients/SalesEngineVA"

' SalesEngineTT - NO FOLDER EXISTS  
ElseIf InStr(Request.ServerVariables("URL"), "SalesEngineTT") > 0 Then
    Response.Cookies("WorkingDir") = "/Clients/SalesEngineTT"

' SalesEngineTL - ✅ EXISTS (active)
ElseIf InStr(Request.ServerVariables("URL"), "SalesEngineTL") > 0 Then
    Response.Cookies("WorkingDir") = "/Clients/SalesEngineTL"
```

**Recommendation:** Remove VA and TT conditions from Var.asp

### In `/System/ssi_dbConn_open.inc`:
```vbscript
' References databases that don't exist:
- Vantage.mdb (VA)
- TTL2.mdb (TT)
- Pierlite_NSW.mdb (PL)
- Liosatos.mdb (CL)
- TGA2.mdb (TG)
- Techlight2.mdb (TL) ✅ EXISTS
```

---

## 🗑️ Files Safe to Delete

### Build Artifacts
| File | Path | Size | Notes |
|------|------|------|-------|
| Thumbs.db | `/Clients/SalesEngineTL/Images/Thumbs.db` | 5.6KB | Windows thumbnail cache |
| Thumbs.db | `/System/Thumbs.db` | 5.6KB | Windows thumbnail cache |
| Thumbs.db | `/images/Thumbs.db` | 5.6KB | Windows thumbnail cache |

### Empty/Placeholder Files
| File | Path | Size | Notes |
|------|------|------|-------|
| TTL2.new.mdb | `/System/TTL2.new.mdb` | 0 bytes | Empty placeholder file |

### Upgrade Artifacts
| Folder | Path | Items | Notes |
|--------|------|-------|-------|
| _UpgradeReport_Files | `/_UpgradeReport_Files/` | 2 items | Visual Studio upgrade report from 2005-era |

---

## ⚠️ Code Analysis - Unused References

### Unused Database Connections
In `ssi_dbConn_open.inc`, the following database connections are defined but likely unused:
- `CL` -> Liosatos.mdb
- `PL` -> Pierlite_NSW.mdb  
- `TT` -> TTL2.mdb
- `TG` -> TGA2.mdb
- `TGA` -> TGA2.mdb
- `VA` -> Vantage.mdb

Only `TL` -> Techlight2.mdb is confirmed active.

### Unused Includes
| File | Location | Status |
|------|----------|--------|
| ssi_Header_Morcare.inc | `/System/` | Not referenced |
| ssi_Header_TSA.inc | `/System/` | Not referenced |
| ssi_Header_Timber.inc | `/System/` | Not referenced |

---

## 🧹 Recommended Cleanup Actions

### Immediate (Safe)
1. **Delete empty folders:**
   ```powershell
   Remove-Item "FusionChart" -Recurse
   Remove-Item "Grids" -Recurse
   Remove-Item "My Project" -Recurse
   Remove-Item "anonymous\webroot" -Recurse
   Remove-Item "_UpgradeReport_Files" -Recurse
   ```

2. **Delete build artifacts:**
   ```powershell
   Remove-Item "Thumbs.db" -Recurse
   Remove-Item "System\TTL2.new.mdb"
   ```

3. **Clear obj folder:**
   ```powershell
   Remove-Item "obj" -Recurse
   ```

### After Verification (Caution Required)
1. **Remove SalesEngine folder** (after confirming only shared Portal code needed)
2. **Remove Guest/TT and Guest/VA** (if not used for historical reference)
3. **Clean up Var.asp** - Remove VA and TT conditions
4. **Clean up ssi_dbConn_open.inc** - Remove unused database connection logic

### Git/.gitignore Additions
Create `.gitignore` file:
```gitignore
# Build artifacts
obj/
bin/
*.log

# Windows files
Thumbs.db
Desktop.ini

# IDE
.vs/
*.user
*.suo

# Temp files
*.tmp
*.temp
```

---

## 📊 Size Summary

| Category | Approx. Size | Impact |
|----------|-------------|--------|
| Empty folders | 0 KB | Cleanup only |
| Build artifacts (obj) | ~50 KB | Cleanup only |
| Thumbs.db files | ~17 KB | Cleanup only |
| Legacy SalesEngine | ~15 KB | Review first |
| Guest/TT, Guest/VA | ~10 KB | Review first |
| **Total Safe to Delete** | **~92 KB** | - |

---

## 🎯 Priority Matrix

| Action | Priority | Risk | Effort |
|--------|----------|------|--------|
| Delete empty folders | Low | None | 5 min |
| Delete Thumbs.db | Low | None | 2 min |
| Delete TTL2.new.mdb | Low | None | 1 min |
| Clean up Var.asp | Medium | Low | 15 min |
| Review SalesEngine usage | Medium | Medium | 30 min |
| Review Guest/TT,VA usage | Low | Low | 15 min |

