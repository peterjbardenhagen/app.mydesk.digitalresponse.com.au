<!--#include virtual="Timezone.asp"-->
<!--#include virtual="System/ssi_Alerts.asp"-->
<!--#include virtual="System/ssi_Functions_Core.asp"-->
<!--#include virtual="System/ssi_Functions_User.asp"-->
<!--#include virtual="System/ssi_Functions_Quote.asp"-->
<!--#include virtual="System/ssi_Functions_PO.asp"-->
<!--#include virtual="System/ssi_Functions_UI.asp"-->
<!--#include virtual="System/ssi_Functions_Activity.asp"-->
<!--#include virtual="System/ssi_Functions_Files.asp"-->
<%

' NOTE: This file is now a master include file that includes all the compartmentalized function files.
' The functions have been split into the following files:
' - ssi_Functions_Core.asp    : Basic utilities (GetProtocol, MyRedirect, etc.)
' - ssi_Functions_User.asp    : User management functions
' - ssi_Functions_Quote.asp   : Quote-related functions
' - ssi_Functions_PO.asp      : Purchase Order functions
' - ssi_Functions_UI.asp      : UI helper functions
' - ssi_Functions_Activity.asp: Activity/Timesheet functions
' - ssi_Functions_Files.asp   : File management functions

%>
