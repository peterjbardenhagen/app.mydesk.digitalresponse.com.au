<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim lngId
Dim rs
Dim sql
Dim strMsg
Dim strErrorDescription

lngId = CLng(Request("Id"))

sql = "Delete From Companies Where CompanyId = " & lngId
dbConn.Execute(sql)

strErrorDescription = err.Description

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

If GetErrorCode(strErrorDescription) = 1 Then
	strMsg = "Record cannot be deleten, as there are historical records that depend on it."
Else
	strMsg = "Record deleted"
End If

%>
<html>
<head>
<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
<script language="javascript">
	alert('<%= strMsg %>');
	RefreshIFrame_Global_Opener();
	window.close();
</script>
</head>
<body>
</body>
</html>