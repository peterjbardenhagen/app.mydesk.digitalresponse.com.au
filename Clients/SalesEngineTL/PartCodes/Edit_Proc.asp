<% 

Option Explicit

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

Dim intPartCodeId
Dim intDivisionId
Dim strPartCode
Dim sql

intPartCodeId = CInt(Request("PartCodeId"))
intDivisionId = CInt(Request("DivisionId"))
strPartCode = Trim(Replace(Request("PartCode"),"'","''"))

sql = "Update PartCodes Set PartCode = '" & strPartCode & "', DivisionId = " & intDivisionId & " Where PartCodeId = " & intPartCodeId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Part+Code+updated")

%>