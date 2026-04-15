<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim lngId
Dim sql
Dim strMsg
Dim strErrorDescription

lngId = CLng(Request("Id"))

sql = "Select RFQStatusId From RFQ Where RFQid = " & lngId
Set rsCheck = dbConn.Execute(sql)

If Request.Cookies("UserSettings")("UserTypeId") = 5 Or Request.Cookies("UserSettings")("UserTypeId") = 6 Then
	boolCanDelete = True
Else
	If rsCheck("RFQStatusId") = 22 Then
		boolCanDelete = True
	Else
		boolCanDelete = False
	End If
End If

If boolCanDelete Then
	sql = "Delete From Comments Where ItemId = " & lngId & " And TableId = 7"
	dbConn.Execute(sql)

	sql = "Delete From RFQContents Where RFQId = " & lngId
	dbConn.Execute(sql)

	sql = "Delete From RFQ Where RFQId = " & lngId
	dbConn.Execute(sql)

	strErrorDescription = err.Description
	If GetErrorCode(strErrorDescription) = 1 Then
		strMsg = "Record cannot be deleten, as there are historical records that depend on it."
	Else
		strMsg = "Record deleted"
	End If
Else
	strMsg = "Cannot delete this RFQ, as it has been finalised."
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
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