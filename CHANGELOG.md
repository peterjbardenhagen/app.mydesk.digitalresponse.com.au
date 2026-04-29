# DR MyDesk — Project Reorganization

**Date:** April 21, 2026  
**Version:** 3.0.0

---

## Changes Made

### 1. Eliminated Duplicates ✓

**Removed:**
- `src\Database\` folder (duplicate of `src\Deployment\Migration\`)
- `README-NEW-STRUCTURE.md` (merged into main README)

**Kept (Canonical):**
- `src\Deployment\Migration\` — All database scripts and migration docs

### 2. Consolidated Documentation ✓

**Created:**
- `README.md` — Single source of truth, DR MyDesk focused (no legacy version talk)
- `TESTING.md` — Merged from `tests\MyDesk.PlaywrightTests\USAGE.md` + simplified
- `src\Deployment\README.md` — Complete deployment guide (local IIS → production VM)

**Removed:**
- `README-NEW-STRUCTURE.md` (obsolete)

### 3. Interactive Run.bat Menu ✓

**Root `Run.bat` now provides:**

```
[1] Run DR MyDesk                (Local Development Server)
[2] Run Tests                    (Playwright E2E Tests)
[3] Testing Documentation        (TESTING.md)
[4] Project README               (README.md)
[5] Configuration Files          (appsettings.json, navmenu.json, etc.)

── SQL Database ────────────────────────────────────────────────
[6] Migration - Access to SQL    (Legacy migration scripts)
[7] Install Database             (Install.ps1)
[8] Deploy to IIS                (Deploy.ps1 - requires Admin)

[Q] Quit
```

**Process Flow (Start to Finish):**
1. **Local Development** → Option 1 (Run DR MyDesk)
2. **Testing** → Option 2 (Run Tests)
3. **Database Setup** → Option 7 (Install.ps1)
4. **Local IIS Deploy** → Option 8 (Deploy.ps1)
5. **Production Deploy** → Copy publish folder to VM → Run Deploy.ps1 on server

### 4. Naming Standards ✓

**All folders follow PascalCase:**
- `src\MyDesk.Web\`
- `src\MyDesk.Shared\`
- `src\Deployment\`
- `src\Documentation\`
- `tests\MyDesk.PlaywrightTests\`

**All files follow conventions:**
- Scripts: `Deploy.ps1`, `Install.ps1`, `Run.bat`
- Docs: `README.md`, `TESTING.md`, `CHANGELOG.md`
- Config: `appsettings.json`, `navmenu.json`, `targets.json`

### 5. Fixed Deploy.ps1 ✓

**Issues Resolved:**
- ✓ Added Administrator elevation check
- ✓ Replaced `WebAdministration` module with `appcmd.exe` (universal)
- ✓ Fixed project path (`..\MyDesk.Web` instead of old `..\src\Techlight.MyDesk.Web`)
- ✓ Added `-Force` to directory creation
- ✓ Clear error messages with instructions

---

## File Structure (Current)

```
C:\Development\Techlight.digitalresponse.com.au\
├── src\                              # Main source
│   ├── MyDesk.Web\                   # Blazor app
│   ├── MyDesk.Shared\                # Shared library
│   ├── Deployment\                   # Deployment scripts
│   │   ├── Deploy.ps1                # IIS deployment (FIXED)
│   │   ├── README.md                 # Deployment guide (UPDATED)
│   │   └── Migration\                # SQL migration scripts
│   │       ├── Install.ps1
│   │       ├── PostMigrationFixes.sql
│   │       ├── Cleanup-LegacyTables.sql
│   │       └── README.md
│   ├── Documentation\
│   ├── Run.bat                       # Local dev launcher
│   └── MyDesk.slnx
│
├── tests\                            # Playwright tests
│   └── MyDesk.PlaywrightTests\
│
├── Run.bat                           # Interactive menu (NEW)
├── Run-Tests.bat                     # Test runner
├── README.md                         # Main docs (UPDATED)
├── TESTING.md                        # Test docs (NEW)
└── CHANGELOG.md                      # This file (NEW)
```

---

## What Was Removed

- ❌ `src\Database\` (duplicate)
- ❌ `README-NEW-STRUCTURE.md` (obsolete)
- ❌ Old `Run.bat` (replaced with menu version)

---

## Next Steps

1. **Run the app:**
   ```batch
   .\Run.bat
   # Choose option 1
   ```

2. **Deploy to local IIS:**
   ```batch
   .\Run.bat
   # Choose option 8
   ```

3. **Run tests:**
   ```batch
   .\Run.bat
   # Choose option 2
   ```

---

**All changes committed:** April 21, 2026  
**Status:** ✓ Complete
