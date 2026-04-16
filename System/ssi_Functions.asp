<%
' ===============================================================================
' Techlight MyDesk - Master Functions Include
' ===============================================================================
' REQUIREMENTS: None (self-contained)
' DEPENDENCIES: Pulls in all function modules and constants
' ===============================================================================

'Option Explicit

'-------------------------------------------------------------------------------
' LAYER 0: Constants (MUST be first - provides TL_* constants to all includes)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/Constants.asp"-->

<%
'-------------------------------------------------------------------------------
' LAYER 1: Legacy Compatibility (provides strWorkingDir for old code)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_LegacyCompat.asp"-->

<%
'-------------------------------------------------------------------------------
' LAYER 2: Core Function Modules
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_Errors.asp"-->
<!--#include virtual="/System/ssi_SafeExecute.inc"-->
<!--#include virtual="/Timezone.asp"-->
<!--#include virtual="/System/ssi_Alerts.asp"-->
<!--#include virtual="/System/ssi_Functions_Core.asp"-->
<!--#include virtual="/System/ssi_Functions_User.asp"-->
<!--#include virtual="/System/ssi_Functions_Quote.asp"-->
<!--#include virtual="/System/ssi_Functions_PO.asp"-->
<!--#include virtual="/System/ssi_Functions_UI.asp"-->
<!--#include virtual="/System/ssi_Functions_Activity.asp"-->
<!--#include virtual="/System/ssi_Functions_Files.asp"-->

<%
' NOTE: Function modules compartmentalized by purpose
%>
