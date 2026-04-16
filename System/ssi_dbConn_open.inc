<!--#include file="ADOVBS.inc"-->
<%
' ============================================================================
' Database Connection - Uses Constants.asp (must be included BEFORE this file)
' ============================================================================
' REQUIREMENTS: Constants.asp must be included first
' DEPENDENCIES: TL_DATABASE_PATH, TL_DB_FILENAME, TL_WORKING_DIR
' ============================================================================

On Error Resume Next

' Explicit variable declarations
Dim strDbPath, objFSO, objConn, strConnString

' Build database path using constants
strDbPath = Server.MapPath(TL_DATABASE_PATH) & "\" & TL_DB_FILENAME
If Err.Number <> 0 Then strDbPath = Server.MapPath(".") & "\" & TL_DB_FILENAME

' Verify database file exists
Set objFSO = Server.CreateObject("Scripting.FileSystemObject")
If Err.Number <> 0 Or objFSO Is Nothing Then
    Response.Write "Error: Unable to access file system."
    Response.End
End If

If Not objFSO.FileExists(strDbPath) Then
    Response.Redirect(TL_WORKING_DIR & "/Updating.asp")
    Response.End
End If

' Create and open connection
Set objConn = Server.CreateObject("ADODB.Connection")
If Err.Number <> 0 Or objConn Is Nothing Then
    Response.Write "Error: Unable to create database connection."
    Response.End
End If

strConnString = "Driver={Microsoft Access Driver (*.mdb)};DBQ=" & strDbPath & ";"
objConn.ConnectionTimeout = TL_DB_TIMEOUT
objConn.CommandTimeout = TL_CMD_TIMEOUT
objConn.ConnectionString = strConnString
objConn.Mode = 3
objConn.Open

If Err.Number <> 0 Then
    Response.Write "Database connection failed. Please contact administrator."
    Response.End
End If

' Export connection for use in parent page
Set dbConn = objConn

Set objFSO = Nothing
On Error GoTo 0
%>