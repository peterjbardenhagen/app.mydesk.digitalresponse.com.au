<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strCode
Dim intDivisionId
Dim dteDateFrom
Dim dteDateTo
Dim strName
Dim intJobOrderStatusCode
Dim strKeyword

strCode = Trim(Request.Form("Code"))
intDivisionId = CInt(Request.Form("DivisionId"))
dteDateFrom = Request.Form("DateFrom")
dteDateTo =	Request.Form("DateTo")
intJobOrderStatusCode = CInt(Request.Form("JobOrderStatusCode"))
strKeyword = Request.Form("Keyword")

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
			</tr>
		</table>
		<br>
<%
If strCode <> "All" Then
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sqlUsers = "SELECT name FROM Users WHERE Code = '" & strCode & "'"
	Set rsUsers = dbConn.Execute(sqlUsers)

	strName = rsUsers("Name")

	rsUsers.Close
	Set rsUsers = Nothing
Else
	strName = "All Users"
End If

If intCompanyId <> 0 Then
	Set rsCU = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Company FROM Companies WHERE CompanyId = " & intCompanyId
	Set rsCU = dbConn.Execute(sql)

	strCustomer = rsCU("Company")

	rsCU.Close
	Set rsCU = Nothing
End If

If intDivisionId <> 0 And intDivisionId <> 555 Then
	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Division FROM Divisions WHERE DivisionId = " & intDivisionId
	Set rsDi = dbConn.Execute(sql)

	strDivision = rsDi("Division")

	rsDi.Close
	Set rsDi = Nothing
End If
boolDivisionManager = SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), intDivisionId)
%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">My Job Monitoring Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold">Includes <% If intCompanyId = 0 Then Response.Write("All companies") Else Response.Write(strCustomer) %>&nbsp;<% If intDivisionId = 0 Then Response.Write("and all divisions") Else Response.Write("at " & strDivision) %><br>
				Occuring between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %> as at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
			<tr>
				<td style="font-style:italic;"><br>All prices are ex. GST.<br><br></td>
			</tr>
		</table>
<%
strSql = "SELECT DISTINCT JobOrders.Qid, JobOrderContents.Days, JobOrderContents.Units, iif(JobOrders.CompanyId=142,iif(JobOrders.Company='','Not entered',JobOrders.Company),Companies.Company) As RealCompany, JobOrderContents.DateDeliveryRequested, JobOrderContents.DateDeliveryScheduled, JobOrderContents.NettPrice, JobOrderStatus.JobOrderStatus, JobOrderContents.Quantity, JobOrders.DelCompany, JobOrders.InvCompany, JobOrderContents.UnitCostSubTotal, JobOrderContents.JobOrderContentId AS [Job #], JobOrderContents.JobOrderId AS [Slip #], 'IT' & [Job #] As [Line], JobOrderContents.DateDeliveryRequested AS [Delivery Requested], JobOrderContents.DateDeliveryScheduled AS [Delivery Scheduled], JobOrderContents.ProductCode, JobOrderContents.Description, JobOrders.Project AS Project, UCase(JobOrderStatus.JobOrderStatus) As [Job Order Status], Users.Name AS Originator, JobOrders.DateAccepted AS [Date Accepted], DateDiff('D', JobOrders.DateAccepted, Now()) As [Inactive (days)], 'DELIVERYADDRESS' AS [Delivery Address], 'INVOICEADDRESS' AS [Invoice Address], JobOrderContents.UnitCost As [Unit Cost Ex GST ($)], JobOrderContents.NettPrice As [Nett Price Ex GST ($)], 'ACTION' AS [Action] FROM (JobOrderStatus INNER JOIN (Users INNER JOIN ((Divisions INNER JOIN JobOrders ON Divisions.DivisionId = JobOrders.DivisionId) INNER JOIN JobOrderContents ON JobOrders.JobOrderId = JobOrderContents.JobOrderId) ON Users.Code = JobOrders.Code) ON JobOrderStatus.JobOrderStatusCode = JobOrderContents.JobOrderStatusCode) INNER JOIN Companies ON JobOrders.CompanyId = Companies.CompanyId WHERE "
If intDivisionId > 0 Then
	strSql = strSql & " JobOrders.DivisionId = " & intDivisionId & " AND"
Else
	If Not(Request.Cookies("DivisionId") = 2) Then
		strSql = strSql & " JobOrders.DivisionId = " & Request.Cookies("DivisionId") & " AND"
	End If
End If
If strCode <> "All" Then
	strSql = strSql & " Users.Code = '" & strCode & "' AND"
End If
strSql = strSql & " (JobOrders.DateAccepted >= #" & DBDate(dteDateFrom) & "# AND JobOrders.DateAccepted < #" & DBDate(dteDateTo) & "#)"
If intJobOrderStatusCode <> 0 And intJobOrderStatusCode <> 555 Then
	strSql = strSql & " AND JobOrderContents.JobOrderStatusCode = " & intJobOrderStatusCode
ElseIf intJobOrderStatusCode = 555 Then
	strSql = strSql & "	AND JobOrderContents.JobOrderStatusCode < 70"
End If

strSql = strSql & " UNION ALL "

strSql = strSql & "SELECT DISTINCT JobOrders.Qid, 0 As Days, 0 As Units, iif(JobOrders.CompanyId=142,iif(JobOrders.Company='','Not entered',JobOrders.Company),Companies.Company) As RealCompany, JobOrderThirdPartyContents.DateDeliveryRequested, JobOrderThirdPartyContents.DateDeliveryScheduled, JobOrderThirdPartyContents.NettPrice, JobOrderStatus.JobOrderStatus, JobOrderThirdPartyContents.Quantity, JobOrders.DelCompany, JobOrders.InvCompany, JobOrderThirdPartyContents.UnitCostSubTotal, JobOrderThirdPartyContents.JobOrderThirdPartyId AS [Job #], JobOrderThirdPartyContents.JobOrderId AS [Slip #], 'TP' & [Job #] As [Line], JobOrderThirdPartyContents.DateDeliveryRequested AS [Delivery Requested], JobOrderThirdPartyContents.DateDeliveryScheduled AS [Delivery Scheduled], JobOrderThirdPartyContents.OurPartNumber As [ProductCode], JobOrderThirdPartyContents.Description, JobOrders.Project AS Project, UCase(JobOrderStatus.JobOrderStatus) As [Job Order Status], Users.Name AS Originator, JobOrders.DateAccepted AS [Date Accepted], DateDiff('D', JobOrders.DateAccepted, Now()) As [Inactive (days)], 'DELIVERYADDRESS' AS [Delivery Address], 'INVOICEADDRESS' AS [Invoice Address], JobOrderThirdPartyContents.UnitCost As [Unit Cost Ex GST ($)], JobOrderThirdPartyContents.NettPrice As [Nett Price Ex GST ($)], 'ACTION' AS [Action] FROM Companies INNER JOIN (JobOrderStatus INNER JOIN (Users INNER JOIN ((Divisions INNER JOIN JobOrders ON Divisions.DivisionId = JobOrders.DivisionId) INNER JOIN JobOrderThirdPartyContents ON JobOrders.JobOrderId = JobOrderThirdPartyContents.JobOrderId) ON Users.Code = JobOrders.Code) ON JobOrderStatus.JobOrderStatusCode = JobOrderThirdPartyContents.JobOrderStatusCode) ON Companies.CompanyId = JobOrders.CompanyId WHERE "
If intDivisionId > 0 Then
	strSql = strSql & " JobOrders.DivisionId = " & intDivisionId & " AND"
Else
	If Not(Request.Cookies("DivisionId") = 2) Then
		strSql = strSql & " JobOrders.DivisionId = " & Request.Cookies("DivisionId") & " AND"
	End If
End If
If strCode <> "All" Then
	strSql = strSql & " Users.Code = '" & strCode & "' AND"
End If
strSql = strSql & " (JobOrders.DateAccepted >= #" & DBDate(dteDateFrom) & "# AND JobOrders.DateAccepted < #" & DBDate(dteDateTo) & "#)"
If intJobOrderStatusCode <> 0 And intJobOrderStatusCode <> 555 Then
	strSql = strSql & " AND JobOrderThirdPartyContents.JobOrderStatusCode = " & intJobOrderStatusCode
ElseIf intJobOrderStatusCode = 555 Then
	strSql = strSql & "	AND JobOrderThirdPartyContents.JobOrderStatusCode < 70"
End If
strSql = strSql & " ORDER BY [Slip #] DESC"

Set rsJob = dbConn.Execute(strSql)

If Not(rsJob.BOF And rsJob.EOF) Then
%>
		<table width="1000" cellpadding=3 cellspacing=0 border=0>
			<tr>
				<td class="HeaderRow" style="width:80px;">Job #</td>
				<td class="HeaderRow" style="width:80px;">Quote #</td>
				<td class="HeaderRow">Details</td>
				<td class="HeaderRow" style="width:80px;text-align:right;">Qty</td>
				<td class="HeaderRow" style="width:80px;text-align:right;">Value</td>
				<td class="HeaderRow" style="width:80px;">Required</td>
				<td class="HeaderRow" style="width:80px;">Desp. Due</td>
				<td class="HeaderRow" style="width:80px;">Status</td>
				<td class="HeaderRow" style="width:80px;">Days Inactive</td>
			</tr>
<%
	decRunningUnitCostTotal = 0
	Do Until rsJob.EOF
		decRunningUnitCostTotal = decRunningUnitCostTotal + rsJob("UnitCostSubTotal")
		If rsJob("Units") > 0 Then
			intQty = rsJob("Units") * rsJob("Days")
		Else
			intQty = rsJob("Quantity")
		End If
%>
			<tr>
				<td valign="top"><%= rsJob("Slip #") %></td>
				<td valign="top"><a href="#" onclick="ViewQuote('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsJob("Qid") %>);"><%= rsJob("Qid") %></td>
				<td valign="top"><b><%= UCase(rsJob("RealCompany")) %></b><br><%= UCase(rsJob("Description")) %></td>
				<td valign="top" style="text-align:right;"><% If CLng(rsJob("Quantity")) = 0 And (CLng(rsJob("Days")) <> 0 And CLng(rsJob("Units")) <> 0) Then Response.Write(CLng(rsJob("Days")) & " days<br>" & CLng(rsJob("Units")) & " units") Else Response.Write(CLng(rsJob("Quantity"))) %></td>
				<td valign="top" style="text-align:right;"><%= FormatCurrency(rsJob("NettPrice")*intQty, 2) %></td>
				<td valign="top" nowrap><% If DateDiff("y", rsJob("DateDeliveryRequested"), Now()) < 100 Then Response.Write(FormatDateU(rsJob("DateDeliveryRequested"),False)) Else Response.Write("&nbsp;") %></td>
				<td valign="top" nowrap><% If DateDiff("y", rsJob("DateDeliveryScheduled"), Now()) < 100 Then Response.Write(FormatDateU(rsJob("DateDeliveryScheduled"),False)) Else Response.Write("&nbsp;") %></td>
				<td valign="top" nowrap><%= UCase(rsJob("JobOrderStatus")) %></td>
				<td valign="top" nowrap><% If rsJob("Inactive (days)") > 14 Then Response.Write "<span style=""color:red;font-weight:bold;"">" & rsJob("Inactive (days)") & "</span>" Else Response.Write rsJob("Inactive (days)") %></td>
			</tr>
			<tr height=2>
				<td colspan=10>
					<table width="100%" height=2 cellpadding=0 cellspacing=0 border=0 ID="Table4">
						<tr>
							<td bgcolor="#000000"><img src="/Images/Black.gif" width=994 height=1 border=0 alt=""></td>
						</tr>
					</table>
				</td>
			</tr>
<%
		rsJob.MoveNext
	Loop
%>
			<tr>
<%
End If
rsJob.Close
Set rsJob = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->