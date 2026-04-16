<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngJobOrderStatusCode = CLng(Request("JobOrderStatusCode"))
lngJobOrderContentId = CLng(Request("JobOrderContentId"))
strComment = Trim(Replace(Request("Comment"),"'","''"))
dteDateDeliveryScheduled = Trim(Replace(Request("DateDeliveryScheduled"),"'","''"))
boolTP = CBool(Request("TP"))

If Not IsDate(dteDateDeliveryScheduled) Then
	dteDateDeliveryScheduled = "01-Jan-1900"
End If

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

If Not TP Then
	sql = "Update JobOrderContents Set DateDeliveryScheduled = '" & dteDateDeliveryScheduled & "', JobOrderStatusCode = " & lngJobOrderStatusCode & " Where JobOrderContentId = " & lngJobOrderContentId
	dbConn.Execute(sql)
	
	sql = "Insert Into JobOrderComments (JobOrderStatusCode, JobOrderContentId, Code, Comment, DateEntered) " &_
			"Values (" & lngJobOrderStatusCode & ", " & lngJobOrderContentId & ", '" & Request.Cookies("UserSettings")("Code") & "', '" & strComment & "', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
Else
	sql = "Update JobOrderThirdPartyComments Set DateDeliveryScheduled = '" & dteDateDeliveryScheduled & "', JobOrderStatusCode = " & lngJobOrderStatusCode & " Where JobOrderThirdPartyId = " & lngJobOrderContentId
	dbConn.Execute(sql)

	sql = "Insert Into JobOrderThirdPartyComments (JobOrderStatusCode, JobOrderThirdPartyId, Code, Comment, JobOrderStatusCode, DateEntered) " &_
			"Values (" & lngJobOrderStatusCode & ", " & lngJobOrderThirdPartyId & ", '" & Request.Cookies("UserSettings")("Code") & "', '" & strComment & "', " & lngJobOrderStatusCode & ", '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
End If

%>
<html>
	<head>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<script language="javascript">
			RefreshIFrame_Global_Opener();
			window.close();
		</script>
	</head>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
