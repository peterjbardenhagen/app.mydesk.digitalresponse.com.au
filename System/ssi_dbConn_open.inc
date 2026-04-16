<!--#include file="ADOVBS.inc"-->
<%
' ============================================================================
' Database Connection - Simplified for Techlight Single Client
' ============================================================================

On Error Resume Next

Dim MyDB, MyFSO, dbConn, strConn

' Hardcoded values for Techlight (single client system)
strWorkingDir = "/Clients/SalesEngineTL"
strGlobalPrefix = "TL"
strGlobalState = "AUS"

' Get database path
MyDB = Server.MapPath("/Database") & "\Techlight2.mdb"
If Err.Number <> 0 Then MyDB = Server.MapPath(".") & "\Techlight2.mdb"

' Check database exists
Set MyFSO = Server.CreateObject("Scripting.FileSystemObject")
If Err.Number <> 0 Or MyFSO Is Nothing Then
    Response.Write "Error: Unable to access file system."
    Response.End
End If

If Not MyFSO.FileExists(MyDB) Then
    Response.Redirect(strWorkingDir & "/Updating.asp")
    Response.End
End If

' Create and open connection
Set dbConn = Server.CreateObject("ADODB.Connection")
If Err.Number <> 0 Or dbConn Is Nothing Then
    Response.Write "Error: Unable to create database connection."
    Response.End
End If

strConn = "Driver={Microsoft Access Driver (*.mdb)};DBQ=" & MyDB & ";"
dbConn.ConnectionTimeout = 15
dbConn.CommandTimeout = 30
dbConn.ConnectionString = strConn
dbConn.Mode = 3
dbConn.Open

If Err.Number <> 0 Then
    Response.Write "Database connection failed. Please contact administrator."
    Response.End
End If

Set MyFSO = Nothing
On Error GoTo 0
%>