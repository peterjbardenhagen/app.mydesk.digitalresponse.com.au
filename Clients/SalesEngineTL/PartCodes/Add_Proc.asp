<% 

'Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim intDivisionId
Dim strPartCode
Dim sql

intDivisionId = CInt(Request("DivisionId"))
strPartCode = Trim(Replace(Request("PartCode"),"'","''"))

sql = "Insert Into PartCodes (PartCode, DivisionId) Values ('" & strPartCode & "', " & intDivsionId & ");"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Part+Code+added")

%>