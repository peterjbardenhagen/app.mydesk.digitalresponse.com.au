<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

SetWorkingDir Request.ServerVariables("URL")

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngJobOrderId
lngJobOrderId = CLng(Request("JobOrderId"))

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Select JobOrders.Company AS JCompany, JobOrders.DivisionId AS JDivisionId, Companies.Company AS CCompany, * FROM ((Locations INNER JOIN Users ON Locations.LocationId = Users.LocationId) INNER JOIN JobOrders ON Users.Code = JobOrders.Code) INNER JOIN Companies ON JobOrders.CompanyId = Companies.CompanyId Where JobOrderId = " & lngJobOrderId
Set rsJob = dbConn.Execute(sql)

sql = "Select * From Divisions Where DivisionId = " & rsJob("JDivisionId")
Set rsDi = dbConn.Execute(sql)

strLogo = rsDi("Logo")

Set rsLoc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsJob("LocationId")
Set rsLoc = dbConn.Execute(sql)

If rsLoc("POStateId") > 0 Then
	Set rsLoc2 = Server.CreateObject("ADODB.RecordSet")
	sql = "Select State From States Where StateId = " & rsLoc("POStateId")
	Set rsLoc2 = dbConn.Execute(sql)
	strPOState = rsLoc("State")
End If

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<style>
			body, p, td, th
			{
				font-family: Arial;
				font-size: 12px;
			}

			#pageLabel 
			{
				color: #000000;
			}

			.Header 
			{
				font-family: Tahoma;
				font-size: 28px;
			}

			.Header2
			{
				font-family: Tahoma;
				font-size: 18px;
				color: Teal;
			}

			.Header3
			{
				font-family: Arial;
				font-size: 12px;
				font-weight: bold;
				color: #000000;
			}

			A.Header3, A:Link.Header3, A:Visited.Header3, A:Active.Header3, A:Hover.Header3
			{
				font-family: Arial;
				font-size: 12px;
				font-weight: bold;
				color: #000000;
			}

			.Header4
			{
				font-family: Arial;
				font-size: 14px;
				font-weight: bold;
				color: #000000;
			}

			A,A:Link,A:Hover,A:Visited,A:Active 
			{
				color: #000077;
			}

			.Error 
			{	
				text-align: center;
				color: white;
				font-weight: bold;	
			}

			.Req
			{	
				color: Red;
				font-weight: bold;
			}

			input, select, textarea 
			{
				font-family: arial;
				font-size:12px;
				border-style: outset;
			}

			.HeaderRow {
				font-weight:bold;
				color:black;
				text-align:left;
				vertical-align:top;
				border-top:2px solid black;
				border-bottom:2px solid black;
				font-style:italic;
			}
			.TimesItalicBold, .HeaderRow {
				font-family: times new roman;
				font-weight: bold;
				font-style: italic;
				font-size: 14px;
			}
			.TimesHeader {
				font-family: times new roman;
				font-weight: bold;
				font-style: italic;
				font-size: 18px;
			}

			HR 
			{
				border: 2px solid black;	
			}

			.ListHeaderRow
			{
				font-weight: bold;
				border-bottom: 1px solid black;
				background-color: #ebeadb;
				color: black;
			}

		</style>
		<style media="print">
			.NoPrint {
				display:none;
				visibility:hidden;
			}
			.page  { 
				height: 100%;
				margin: 0% 0% 0% 0%;
			}
		</style>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2 class="page">
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<!--#include file="NavBar.asp"-->
				</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table1">
			<tr>
				<td valign="top"><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""><br><br></td>
				<td valign="top" align="right">
					<br>
					<table cellpadding=3 cellspacing=0 border=0>
						<tr>
							<td colspan=2>
							<%= DisplayLocationAddress(rsLoc("Address1"), rsLoc("Address2"), rsLoc("Suburb"), rsLoc("State"), rsLoc("PostCode"), rsLoc("Country"), rsLoc("PODisplay"), rsLoc("POAddress1"), rsLoc("POAddress2"), rsLoc("POSuburb"), rsLoc("POState"), rsLoc("POPostCode"), rsLoc("POCountry")) %>
							</td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
<%

	If rsDi("ABN") <> "" Then

%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">ABN:</td>
							<td><%= rsDi("ABN") %></td>
						</tr>
<%

	End If

%>
					</table>
				</td>
			</tr>
			<tr height=10>
				<td></td>
			</tr>
			<tr>
				<td colspan=2>
				<span style="font-size:24px;">PICKING SLIP</span>
				</td>
			</tr>
			<tr>
				<td colspan=2><br></td>
			</tr>
			<tr>
				<td class="Times2Bold" valign="top" colspan=2>
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table11">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:100px;font-size:16px;">Slip #:</td>
							<td style="font-size:16px;"><%= rsJob("JobOrderId") %></td>
						</tr>
						<tr>
							<td colspan=2><br></td>
						</tr>
						<tr>
							<td width=20% style="font-weight:bold;vertical-align:top;">Date Accepted:</td>
							<td width=30%><%= FormatDateU(rsJob("DateAccepted"), False) %></td>
							<td width=20% style="font-weight:bold;vertical-align:top;">Date Printed:</td>
							<td width=30%><%= FormatDateU(ServerToEST(Now()), True) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:100px;">Customer:</td>
							<td colspan=3><% If rsJob("CompanyId") = 142 Then Response.Write(rsJob("JCompany")) Else Response.Write(rsJob("CCompany")) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:100px;">Customer PO #:</td>
							<td colspan=3><%= rsJob("CustomerPO") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:100px;">Project:</td>
							<td colspan=3><%= rsJob("Project") %></td>
						</tr>
					</table>
					<br>
				</td>
			</tr>
			<tr>
				<td width="50%" valign="top">
					<table align="center" width="100%" cellpadding="3" cellspacing="0">
						<tr>
							<td>
							<b>Invoice</b><br>
<%

	Dim strA
	If rsJob("InvAddress1") = "To be advised" Then
	    strA = "To be advised"
	Else
		strA = rsJob("InvCompany") & vbcrlf
	    strA = strA & rsJob("InvAddress1") & "<br>" & vbclrf
	    If rsJob("InvAddress2") <> "" Then
		    strA = strA & rsJob("InvAddress2") & "<br>"
	    End If
	    strA = strA & rsJob("InvSuburb") & ", " & rsJob("InvState") & " " & rsJob("InvPostCode") & "<br>" & vbcrlf
	    strA = strA & rsJob("InvCountry") & vbcrlf
    End If
	Response.Write(strA)

%>
							</td>
						</tr>
					</table>
				</td>
				<td width="50%" valign="top">
					<table align="center" width="100%" cellpadding="3" cellspacing="0" ID="Table2">
						<tr>
							<td>
							<b>Deliver</b><br>
<%

	Dim strB
	If rsJob("DelAddress1") = "To be advised" Then
	    strB = "To be advised"
	Else
	    strB = rsJob("DelCompany") & vbcrlf
	    strB = strB & rsJob("DelAddress1") & "<br>" & vbclrf
	    If rsJob("DelAddress2") <> "" Then
		    strB = strB & rsJob("DelAddress2") & "<br>"
	    End If
	    strB = strB & rsJob("DelSuburb") & ", " & rsJob("DelState") & " " & rsJob("DelPostCode") & "<br>" & vbcrlf
	    strB = strB & rsJob("DelCountry") & vbcrlf
    End If
	Response.Write(strB)

%>
							</td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
		<table align="center" width="595" cellpadding="5" cellspacing="0" ID="Table6">
<%
	' Get Items

	Set rsJobItems = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From JobOrderContents Where JobOrderId = " & lngJobOrderId
	Set rsJobItems = dbConn.Execute(sql)

	Dim i
	Dim decRunningNettPriceTotal
	i = 1
	decRunningNettPriceTotal = 0
	If Not(rsJobItems.BOF And rsJobItems.EOF) Then
%>
			<tr>
				<td colspan=10 style="border-bottom:2px solid black;font-size:14px;font-weight:bold;"><br>STANDARD ITEMS:</td>
			</tr>
			<tr>
				<td valign="top" width=15 style="font-weight:bold;font-size:12px;">Item</td>
				<td valign="top" nowrap style="font-weight:bold;font-size:12px;">Qty</td>
				<td valign="top" style="font-weight:bold;font-size:12px;">Description</td>
				<td valign="top" width=100 style="font-weight:bold;font-size:12px;text-align:right;">Nett Price</td>
				<td valign="top" width=100 style="font-weight:bold;font-size:12px;text-align:right;">Ext. Nett Price</td>
			</tr>
<%
		Do Until rsJobItems.EOF
			decRunningNettPriceTotal = decRunningNettPriceTotal + rsJobItems("ExtNettPrice")
%>
			<tr>
				<td valign="top" width=15 style="font-size:16px;font-weight:bold;">IT<%= rsJobItems("JobOrderContentId") %></td>
				<td valign="top" nowrap><% If CLng(rsJobItems("Quantity")) = 0 And (CLng(rsJobItems("Days")) <> 0 And CLng(rsJobItems("Units")) <> 0) Then Response.Write(CLng(rsJobItems("Days")) & " days<br>" & CLng(rsJobItems("Units")) & " units") Else Response.Write(CLng(rsJobItems("Quantity"))) %></td>
				<td valign="top"><% If rsJobItems("ProductCode") <> "" Then Response.Write(rsJobItems("ProductCode")) Else Response.Write("<span style=""color:red;"">NO PRODUCT CODE</span>") %><br><br><% If rsJobItems("Type") <> "" Then Response.Write("<b>TYPE: " & rsJobItems("Type") & "</b><br>") %><%= rsJobItems("Description") %></td>
				<td valign="top" style="width:100px;text-align:right;">A<% If Not IsNull(rsJobItems("NettPrice")) Then Response.Write(FormatCurrency(rsJobItems("NettPrice")+0,2)) %></td>
				<td valign="top" style="width:100px;text-align:right;">A<%= FormatCurrency(rsJobItems("ExtNettPrice"),2) %></td>
			</tr>
			<tr>
				<td colspan=10 style="border-bottom:1px solid black;">
<%
			If DateDiff("Y", rsJobItems("DateDeliveryScheduled"), Now()) < 100 Then
				Response.Write("<b>Date Delivery Scheduled:</b> ")
				Response.Write(FormatDateU(rsJobItems("DateDeliveryScheduled"), False) & "<br><br>")
			Else
				Response.Write("<b>Date Delivery Scheduled:</b> ")
				Response.Write("<span style=""color:red;"">NOT SCHEDULED</span><br><br>")
			End If

			Set rsComments = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 * From Users INNER JOIN (JobOrderComments INNER JOIN JobOrderStatus ON JobOrderComments.JobOrderStatusCode = JobOrderStatus.JobOrderStatusCode) ON Users.Code = JobOrderComments.Code Where JobOrderContentId = " & rsJobItems("JobOrderContentId") & " Order By DateEntered Desc"
			Set rsComments = dbConn.Execute(sql)
			If Not(rsComments.BOF And rsComments.EOF) Then
%>
				<b>Most recent comment:</b><br>
				<%= FormatDateU(rsComments("DateEntered"), False) %> - <b><%= UCase(rsComments("JobOrderStatus")) %></b> - <%= rsComments("Comment") %> - By <%= rsComments("Name") %>
<%
			Else
%>
				No comments have been recorded.
<%
			End If
			rsComments.Close
			Set rsComments = Nothing
%>				
				</td>
			</tr>
<%
			i = i + 1
			rsJobItems.MoveNext
		Loop
	End If
	rsJobItems.Close
	Set rsJobItems = Nothing

	' Get Third Party Supply

	Set rsJobItems = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From JobOrderThirdPartyContents Where JobOrderId = " & lngJobOrderId
	Set rsJobItems = dbConn.Execute(sql)

	i = 1
	If Not(rsJobItems.BOF And rsJobItems.EOF) Then
%>
			<tr>
				<td colspan=10 style="border-bottom:2px solid black;font-size:14px;font-weight:bold;"><br>THIRD PARTY SUPPLY ITEMS:</td>
			</tr>
			<tr>
				<td valign="top" width=15 style="font-weight:bold;font-size:12px;">Item</td>
				<td valign="top" nowrap style="font-weight:bold;font-size:12px;">Qty</td>
				<td valign="top" style="font-weight:bold;font-size:12px;">Description</td>
				<td valign="top" width=100 style="font-weight:bold;font-size:12px;text-align:right;">Nett Price</td>
				<td valign="top" width=100 style="font-weight:bold;font-size:12px;text-align:right;">Ext. Nett Price</td>
			</tr>
<%
		Do Until rsJobItems.EOF
			decRunningNettPriceTotal = decRunningNettPriceTotal + CDbl(rsJobItems("ExtNettPrice"))
%>
			<tr>
				<td valign="top" width=15 style="font-size:16px;font-weight:bold;">TP<%= rsJobItems("JobOrderThirdPartyId") %></td>
				<td valign="top" nowrap><%= CLng(rsJobItems("Quantity")) %></td>
				<td valign="top"><% If rsJobItems("ProductCode") <> "" Then Response.Write(rsJobItems("ProductCode")) Else Response.Write("<span style=""color:red;"">NO PRODUCT CODE</span>") %><br><br><% If rsJobItems("Type") <> "" Then Response.Write("<b>TYPE: " & rsJobItems("Type") & "</b><br>") %><%= rsJobItems("Description") %></td>
				<td valign="top" style="width:100px;text-align:right;">A<%= FormatCurrency(rsJobItems("NettPrice"),2) %></td>
				<td valign="top" style="width:100px;text-align:right;">A<%= FormatCurrency(rsJobItems("ExtNettPrice"),2) %></td>
			</tr>
			<tr>
				<td colspan=10 style="border-bottom:1px solid black;">
<%
			If DateDiff("Y", rsJobItems("DateDeliveryScheduled"), Now()) < 100 Then
				Response.Write("<b>Date Delivery Scheduled:</b> ")
				Response.Write(FormatDateU(rsJobItems("DateDeliveryScheduled"), False) & "<br><br>")
			Else
				Response.Write("<b>Date Delivery Scheduled:</b> ")
				Response.Write("<span style=""color:red;"">NOT SCHEDULED</span><br><br>")
			End If

			Set rsComments = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 * From Users INNER JOIN (JobOrderThirdPartyComments INNER JOIN JobOrderStatus ON JobOrderThirdPartyComments.JobOrderStatusCode = JobOrderStatus.JobOrderStatusCode) ON Users.Code = JobOrderThirdPartyComments.Code Where JobOrderThirdPartyId = " & rsJobItems("JobOrderThirdPartyId") & " Order By DateEntered Desc"
			Set rsComments = dbConn.Execute(sql)
			If Not(rsComments.BOF And rsComments.EOF) Then
%>
				<b>Most recent comment:</b><br>
				<%= FormatDateU(rsComments("DateEntered"), False) %> - <b><%= UCase(rsComments("JobOrderStatus")) %></b> - <%= rsComments("Comment") %> - By <%= rsComments("Name") %>
<%
			Else
%>
				No comments have been recorded.
<%
			End If
			rsComments.Close
			Set rsComments = Nothing
%>				
				</td>
			</tr>
<%
			i = i + 1
			rsJobItems.MoveNext
		Loop
	End If
	rsJobItems.Close
	Set rsJobItems = Nothing

%>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->