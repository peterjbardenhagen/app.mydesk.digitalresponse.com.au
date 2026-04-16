<%
' ===============================================================================
' Techlight MyDesk - Standard Page Initialization
' ===============================================================================
' PURPOSE: Single include for ALL pages. Pulls everything in correct order.
' USAGE: <!--#include virtual="/System/ssi_Init.asp"--> at top of every page
' ===============================================================================

Option Explicit
%>

<%
'-------------------------------------------------------------------------------
' LAYER 1: Constants (no dependencies)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/Constants.asp"-->

<%
'-------------------------------------------------------------------------------
' LAYER 2: Response Headers (no dependencies)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_ResponseHeaders.inc"-->

<%
'-------------------------------------------------------------------------------
' LAYER 3: Database Connection (depends on Constants)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_dbConn_open.inc"-->

<%
'-------------------------------------------------------------------------------
' LAYER 4: Functions (depends on Constants and dbConn)
'-------------------------------------------------------------------------------
%>
<!--#include virtual="/System/ssi_Functions.asp"-->

<%
'-------------------------------------------------------------------------------
' LAYER 5: Session Setup (only user-specific data)
'-------------------------------------------------------------------------------
' NOTE: Only set user-specific session vars here
' All static values come from Constants.asp
If Session("LoggedIn") = "True" And Session("Code") = "" Then
    ' Session exists but user data missing - recover from cookies if possible
    On Error Resume Next
    Session("Code") = Request.Cookies("UserSettings")("Code")
    Session("Name") = Request.Cookies("UserSettings")("Name")
    Session("Email") = Request.Cookies("UserSettings")("Email")
    Session("Initials") = Request.Cookies("UserSettings")("Initials")
    On Error GoTo 0
End If
%>
