# Techlight MyDesk - System Architecture

## Overview

Single-client system with **constants-based configuration** instead of session/cookie storage for static values.

---

## File Structure

### Core Constants (Layer 0)
**File:** `Constants.asp`

All static values in one place:
```asp
Const TL_WORKING_DIR = "/Clients/SalesEngineTL"
Const TL_PREFIX = "TL"
Const TL_STATE = "AUS"
Const TL_STYLESHEET = "Style.css"
Const TL_COLOR_PRIMARY = "#00a8b5"
```

### Legacy Compatibility (Layer 1)
**File:** `ssi_LegacyCompat.asp`

Provides backward compatibility during transition:
```asp
Dim strWorkingDir
strWorkingDir = TL_WORKING_DIR  ' Old variable, new value

' Sets ClientSettings cookies for old code (temporary)
Response.Cookies("ClientSettings")("WorkingDir") = TL_WORKING_DIR
```

### Master Include (Layer 2)
**File:** `ssi_Functions.asp`

Automatically includes Constants + LegacyCompat + all function modules:
```asp
<!--#include virtual="/System/Constants.asp"-->
<!--#include virtual="/System/ssi_LegacyCompat.asp"-->
<!--#include virtual="/System/ssi_Functions_Core.asp"-->
' ... etc
```

---

## Standard Page Structure

Every page should follow this exact pattern:

```asp
<%
Option Explicit
%>

<!--#include virtual="/System/Constants.asp"-->
<!--#include virtual="/System/ssi_ResponseHeaders.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->

<%
' Your page code here
' Use TL_* constants, not Session/Cookies
%>
```

---

## Migration Status

### ✅ Completed
- Constants.asp (new)
- ssi_LegacyCompat.asp (new)
- ssi_dbConn_open.inc (updated)
- ssi_Header_Techlight.inc (updated)
- ssi_Functions_UI.asp (updated)
- ssi_Functions_Core.asp (updated)
- ssi_Functions.asp (updated)
- Var.asp (simplified)
- Default.asp (restructured)
- PortalFrame.asp (in progress)

### ⏳ Pending (170 files)
All files using `Request.Cookies("ClientSettings")` need updating.

They work NOW because of LegacyCompat layer, but should be updated to use `TL_*` constants directly.

---

## Benefits

| Metric | Before | After |
|--------|--------|-------|
| Session variables per user | ~10 | ~3 (only user-specific) |
| Cookie size per request | ~500 bytes | ~200 bytes |
| WorkingDir lookups | Cookie/Session check | Constant (immediate) |
| Code complexity | High (null-checking) | Low (single source) |
| Debugging | Hard (typos silent) | Easy (Option Explicit) |

---

## Rules

1. **Option Explicit** - Every ASP block starts with this
2. **No If/Loop across includes** - Always complete in same file
3. **Dim all variables** - Never use undeclared variables
4. **Use TL_* constants** - Never Session/Cookies for static values
5. **Standard include order** - Constants → Headers → DB → Functions

---

## Critical Files to Update First

1. `PortalFrame.asp` (48 matches) - In progress
2. `Clients/SalesEngineTL/PortalFrame.asp` (31 matches)
3. `IFrame.asp` (multiple files)
4. `Errors/500-100.asp` (7 matches)

These have the highest impact on performance.
