<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim intTMailId
Dim sql
Dim boolPortal
Dim strMsg

intTMailId = CLng(Request("TMailId"))
If CStr(Request("Portal")) = "True" Then boolPortal = True

sql = "Delete From TMail Where TMailId = " & intTMailId
dbConn.Execute(sql)

If GetErrorCode(err.Description) = 1 Then
	strMsg = "Record+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it."
Else
	strMsg = "Record+deleted"
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

If boolPortal Then
	strDirect = Request.Cookies("ClientSettings")("WorkingDir") & "/PortalFrame.asp"
Else
	strDirect = "Default.asp"
End If

MyRedirect(strDirect & "?Msg=" & strMsg)

%>