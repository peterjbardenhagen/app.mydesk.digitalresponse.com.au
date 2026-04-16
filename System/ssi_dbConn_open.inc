<!--#include file="ADOVBS.inc"-->
<%
'On Error Resume Next
%>
<!--#include virtual="/System/ssi_Errors.asp"-->
<%

' ============================================================================
' Database Connection Include - Hardened for Stability
' ============================================================================
' This file establishes database connections with comprehensive error handling
' and validation to ensure stability and prevent connection failures
' ============================================================================

On Error Resume Next

Dim MyDB
Dim MyFSO
Dim dbConn
Dim dbConn2
Dim strConn
Dim strGlobalState
Dim strGlobalPrefix
'Dim strWorkingDir

' Initialize variables to prevent null reference errors
MyDB = ""
strGlobalState = ""
strGlobalPrefix = ""
strWorkingDir = ""

' Get state and prefix with null checks and error handling
If Not IsEmpty(Session("State")) And Session("State") <> "" Then
	strGlobalState = Session("State")
Else
	On Error Resume Next
	If Not Request.Cookies("ClientSettings") Is Nothing Then
		If Not IsEmpty(Request.Cookies("ClientSettings")("State")) Then
			strGlobalState = Request.Cookies("ClientSettings")("State")
		End If
	End If
	If Err.Number <> 0 Then strGlobalState = ""
	On Error Resume Next
End If

If Not IsEmpty(Session("Prefix")) And Session("Prefix") <> "" Then
	strGlobalPrefix = Session("Prefix")
Else
	On Error Resume Next
	If Not Request.Cookies("ClientSettings") Is Nothing Then
		If Not IsEmpty(Request.Cookies("ClientSettings")("Prefix")) Then
			strGlobalPrefix = Request.Cookies("ClientSettings")("Prefix")
		End If
	End If
	If Err.Number <> 0 Then strGlobalPrefix = ""
	On Error Resume Next
End If

' Get working directory with fallback
If Not IsEmpty(Session("WorkingDir")) And Session("WorkingDir") <> "" Then
	strWorkingDir = Session("WorkingDir")
Else
	On Error Resume Next
	If Not Request.Cookies("ClientSettings") Is Nothing Then
		If Not IsEmpty(Request.Cookies("ClientSettings")("WorkingDir")) Then
			strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
		End If
	End If
	If Err.Number <> 0 Or strWorkingDir = "" Then
		strWorkingDir = "/Clients/SalesEngineTL"
	End If
	On Error Resume Next
End If

If strGlobalState <> "" Then
	' Techlight MyDesk - Single client database
	If strGlobalPrefix = "TL" Then
		On Error Resume Next
		MyDB = Server.MapPath("/Database")
		If Err.Number = 0 Then
			MyDB = MyDB & "\" & "Techlight2.mdb"
		Else
			' Fallback to current directory if Database folder doesn't exist
			MyDB = Server.MapPath(".") & "\Techlight2.mdb"
		End If
		If Err.Number <> 0 Then
			Response.Write "Error: Unable to locate database file. Please contact administrator."
			Response.End
		End If
		On Error Resume Next
	End If

	' Validate database file exists before attempting connection
	Set MyFSO = Server.CreateObject("Scripting.FileSystemObject")
	If Err.Number = 0 And Not MyFSO Is Nothing Then
		If MyFSO.FileExists(MyDB) Then
			' Create database connection with error handling
			Set dbConn = Server.CreateObject("ADODB.Connection")
			If Err.Number = 0 And Not dbConn Is Nothing Then
				strConn = "Driver={Microsoft Access Driver (*.mdb)};"
				strConn = strConn & "DBQ=" & MyDB & ";"
				
				' Set connection properties
				dbConn.ConnectionTimeout = 15
				dbConn.CommandTimeout = 30
				dbConn.ConnectionString = strConn
				dbConn.Mode = 3
				
				' Open connection with error handling
				On Error Resume Next
				dbConn.Open
				
				If Err.Number <> 0 Then
					' Connection failed - log error and redirect
					Response.Clear
					Response.Write "Database connection failed. Error: " & Err.Description
					Response.Write "<br>Please try again or contact the administrator."
					Response.End
				End If
				On Error Resume Next
			Else
				' Failed to create ADODB.Connection object
				Response.Clear
				Response.Write "Error: Unable to create database connection object."
				Response.Write "<br>Please ensure ADODB is properly installed."
				Response.End
			End If
		Else
			' Database file doesn't exist - redirect to updating page
			Response.Clear
			Response.Redirect(strWorkingDir & "/Updating.asp")
			Response.End
		End If
	Else
		' Failed to create FileSystemObject
		Response.Clear
		Response.Write "Error: Unable to access file system."
		Response.End
	End If
	
	' Cleanup FSO object
	Set MyFSO = Nothing
Else
	' No state information - redirect to home
	Response.Clear
	Response.Redirect("/")
	Response.End
End If

' Ensure error handling is reset
On Error GoTo 0
	
%>