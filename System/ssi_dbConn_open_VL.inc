<!--#include file="ADOVBS.inc"-->
<%

Dim MyDB
Dim MyFSO
Dim dbConn
Dim dbConn2
Dim strConn

MyDB = Server.MapPath("/Database") & "\" & "Vantage.mdb"

Set MyFSO = Server.CreateObject("Scripting.FileSystemObject")

Set dbConn = Server.CreateObject("ADODB.Connection")
strConn = "Driver={Microsoft Access Driver (*.mdb)};"
strConn = strConn & "DBQ=" & MyDB & ";" 
dbConn.ConnectionTimeout = 15
dbConn.CommandTimeout = 30
dbConn.ConnectionString = strConn
dbConn.Open

%>