<!--#include file="ADOVBS.inc"-->
<%
'On Error Resume Next
%>
<!--#include virtual="/System/ssi_Errors.asp"-->
<%

Dim MyDB
Dim MyFSO
Dim dbConn
Dim dbConn2
Dim strConn
Dim strGlobalState
Dim strGlobalPrefix

If Session("State") <> "" Then
	strGlobalState = Session("State")
	strGlobalPrefix = Session("Prefix")
Else
	strGlobalState = Request.Cookies("ClientSettings")("State")
	strGlobalPrefix = Request.Cookies("ClientSettings")("Prefix")
End If

If strGlobalState <> "" Then
	' Techlight MyDesk - Single client database
	If strGlobalPrefix = "TL" Then
		MyDB = Server.MapPath("/Database") & "\" & "Techlight2.mdb"
	End If

	Set MyFSO = Server.CreateObject("Scripting.FileSystemObject")

	
	If MyFSO.FileExists(MyDB) Then
		Set dbConn = Server.CreateObject("ADODB.Connection")
		strConn = "Driver={Microsoft Access Driver (*.mdb)};"
		strConn = strConn & "DBQ=" & MyDB & ";" 
		'strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & MyDB & ";"
		dbConn.ConnectionTimeout = 15
		dbConn.CommandTimeout = 30
		dbConn.ConnectionString = strConn
		'response.write strconn
		'Response.End
		dbConn.Mode = 3
		
		dbConn.Open

		'Set dbConn2 = Server.CreateObject("ADODB.Connection")
		'strConn = "Driver={Microsoft Access Driver (*.mdb)};"
		'strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" & MyDB & ";"
		'strConn = strConn & "DBQ=" & MyDB & ";" 
		'dbConn2.ConnectionTimeout = 15
		'dbConn2.CommandTimeout = 30
		'dbConn2.ConnectionString = strConn
		'dbConn2.Open
	Else
		If Session("WorkingDir") <> "" Then
			MyRedirect(Session("WorkingDir") & "/Updating.asp")
		Else
			MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/Updating.asp")
		End If
	End If
Else
	MyRedirect("/")
End If
	
%>