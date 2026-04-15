<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("UserSettings")("UserTypeId") => 4 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

On Error Resume Next

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strCode
Dim sql
Dim strMsg

strCode = Trim(Replace(Request("Code"), "'", "''"))

sql = "Update Users Set Deleted = true, Active = false Where Code = '" & strCode & "'"
dbConn.Execute(sql)

sql = "Delete From Users Where Code = '" & strCode & "'"
dbConn.Execute(sql)

If GetErrorCode(err.Description) = 1 Then
	strMsg = "User+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it.+User+set+to+inactive+instead."
Else
	strMsg = "Record+deleted"
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%
MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/Users/?Msg=" & strMsg)
%>