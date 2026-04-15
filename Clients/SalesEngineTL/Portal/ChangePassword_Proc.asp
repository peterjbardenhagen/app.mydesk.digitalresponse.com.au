<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Session("LoggedIn") = False
Response.Cookies("LoggedIn") = ""

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strCurrentPassword
Dim strNewPassword
Dim sql

strCurrentPassword = Trim(Replace(Request("CurrentPassword"),"'","''"))
strNewPassword = Trim(Replace(Request("NewPassword"),"'","''"))

Set rsCheck = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND [Name] = '" & Replace(Session("Name"), "'", "''") & "' And [PW] = '" & strCurrentPassword & "'"
Set rsCheck = dbConn.Execute(sql)

If Not(rsCheck.BOF And rsCheck.EOF) Then
	sql = "Update Users Set DatePasswordChanged = '" & ServerToEST(Now()) & "', [PW] = '" & strNewPassword & "' Where [Name] = '" & Replace(Session("Name"), "'", "''") & "' And [PW] = '" & strCurrentPassword & "'"
	dbConn.Execute(sql)
	MyRedirect(Session("WorkingDir") & "/?Msg=Password+changed.+Please+login+again+using+your+new+password.")
Else
%>
<script language="javascript">
	alert("Cannot change password. Current Password entered is not correct. Click OK to go back.");
	history.go(-1);
</script>
<%
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->