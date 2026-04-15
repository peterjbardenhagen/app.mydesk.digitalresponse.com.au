<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("UserSettings")("UserTypeId") => 4 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strCurrentPassword
Dim strNewPassword
Dim sql

strCurrentPassword = Trim(Replace(Request("CurrentPassword"),"'","''"))
strNewPassword = Trim(Replace(Request("NewPassword"),"'","''"))

Set rsCheck = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND [Name] = '" & Request.Cookies("UserSettings")("Name") & "' And [PW] = '" & strCurrentPassword & "'"
Set rsCheck = dbConn.Execute(sql)

If Not(rsCheck.BOF And rsCheck.EOF) Then
	sql = "Update Users Set DatePasswordChanged = '" & ServerToEST(Now()) & "', [PW] = '" & strNewPassword & "' Where [Name] = '" & Request.Cookies("UserSettings")("Name") & "' And [PW] = '" & strCurrentPassword & "'"
	dbConn.Execute(sql)
	MyRedirect("EditPassword.asp?Msg=Password+updated.")
Else
	MyRedirect("EditPassword.asp?Msg=Current+Password+is+incorrect.+Please+try+again.")
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
