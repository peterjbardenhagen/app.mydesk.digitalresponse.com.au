<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

If Not Request.Cookies("UserSettings")("UserTypeId") = 6 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim intDivisionId
Dim sql
Dim strMsg

intDivisionId = CLng(Request("DivisionId"))

sql = "Delete From Divisions Where DivisionId = " & intDivisionId
dbConn.Execute(sql)

If GetErrorCode(err.Description) = 1 Then
	strMsg = "Record+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it."
Else
	strMsg = "Record+deleted"
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=" & strMsg)

%>