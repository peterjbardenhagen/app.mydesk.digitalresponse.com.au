<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

'On Error Resume Next

Dim lngId
Dim sql
Dim strMsg
Dim strErrorDescription

lngId = CLng(Request("Id"))

sql = "Select * From PurchaseOrders Where POId = " & lngId
Set rsCheck = dbConn.Execute(sql)

If Request.Cookies("UserSettings")("UserTypeId") = 5 Or Request.Cookies("UserSettings")("UserTypeId") = 6 Then
	boolCanDelete = True
Else
	If rsCheck("POStatusId") = 1 Or rsCheck("POStatusId") = 2 Or rsCheck("POStatusId") = 3 Then
		boolCanDelete = True
	Else
		boolCanDelete = False
	End If
End If

If boolCanDelete Then
	sql = "Delete From Comments Where ItemId = " & lngId & " And TableId = 8"
	dbConn.Execute(sql)

	sql = "Delete From PurchaseOrderAudit Where POid = " & lngId
	dbConn.Execute(sql)

	sql = "Delete From PurchaseOrderApproval Where POid = " & lngId
	dbConn.Execute(sql)

	sql = "Delete From PurchaseOrderContents Where POid = " & lngId
	dbConn.Execute(sql)

	sql = "Delete From PurchaseOrders Where POid = " & lngId
	dbConn.Execute(sql)

	strErrorDescription = err.Description

	If GetErrorCode(strErrorDescription) = 1 Then
		strMsg = "Record cannot be deleten, as there are historical records that depend on it."
	Else
		strMsg = "Record deleted"
	End If
Else
	strMsg = "Record cannot be deleten as it has been issued to the supplier or cancelled."
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