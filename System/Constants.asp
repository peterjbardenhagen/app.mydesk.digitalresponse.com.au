<%
' ===============================================================================
' Techlight MyDesk - Global Constants - Updated
' ===============================================================================
' ALL hardcoded values for single-client Techlight system
' NO session variables. NO cookies. Just constants.
' ===============================================================================

'-------------------------------------------------------------------------------
' System Paths (never changes)
'-------------------------------------------------------------------------------
Const TL_WORKING_DIR = "/Clients/SalesEngineTL"
Const TL_SYSTEM_PATH = "/System"
Const TL_DATABASE_PATH = "/Database"

'-------------------------------------------------------------------------------
' Client Identification (always Techlight)
'-------------------------------------------------------------------------------
Const TL_PREFIX = "TL"
Const TL_STATE = "AUS"
Const TL_COMPANY_NAME = "Techlight"

'-------------------------------------------------------------------------------
' Visual/Theming Constants
'-------------------------------------------------------------------------------
Const TL_COLOR_PRIMARY = "#00a8b5"
Const TL_COLOR_PRIMARY_DARK = "#008a94"
Const TL_COLOR_HOME = "#005b89"
Const TL_STYLESHEET = "Style.css"

'-------------------------------------------------------------------------------
' Database Constants
'-------------------------------------------------------------------------------
Const TL_DB_FILENAME = "Techlight2.mdb"
Const TL_DB_TIMEOUT = 15
Const TL_CMD_TIMEOUT = 30

'-------------------------------------------------------------------------------
' Security / Approval
'-------------------------------------------------------------------------------
Const TL_APPROVAL_PASSWORD = "approveme"

%>
