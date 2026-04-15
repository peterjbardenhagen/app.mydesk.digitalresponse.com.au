<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim intRFQId
intRFQId = CInt(Request("RFQId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

' Get RFQ
Set rsRFQ = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT RFQ.*, Contacts_WithCustomersAndSuppliers_V2.*, RFQStatus.* FROM RFQStatus INNER JOIN (RFQ INNER JOIN Contacts_WithCustomersAndSuppliers_V2 ON RFQ.ContactId = Contacts_WithCustomersAndSuppliers_V2.ContactId) ON RFQStatus.RFQStatusId = RFQ.RFQStatusId WHERE (((RFQ.RFQGroupId) In (Select RFQGroupId From RFQ Where RFQId = " & intRFQId & ")))"
Set rsRFQ = dbConn.Execute(sql)

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<script language="javascript">
			function CreatePurchaseOrder() {
				try {
					if(window.opener && !window.opener.closed) {
						window.opener.document.parentWindow.RedirectPage_Global('Transporter.asp?RFQId=<%= rsRFQ("RFQId") %>&DivisionId=<%= rsRFQ("DivisionId") %>');
						setTimeout("RefreshWindowClose()",1500);
					}
				} catch(error) {
					alert('Access denied. Close this window, open and try again.');
				}
			}
		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="RefreshWindowClose();" ID="Button1" NAME="Button1"></td>
			</tr>
		</table>
		<br>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">Compare Request for Quotes</span><br><br>
				</td>
			</tr>
		</table>
<%
If Not(rsRFQ.BOF And rsRFQ.EOF) Then
%>
		<table width="770" cellpadding=5 cellspacing=0 border=0>
			<tr>
				<td class="HeaderRow">Id</td>
				<td class="HeaderRow">Items</td>
				<td class="HeaderRow" style="text-align:right;">Total Ex</td>
				<td class="HeaderRow" style="text-align:right;">Total Inc</td>
			</tr>
			<tr>
				<td colspan=5></td>
			</tr>
<%
	Do Until rsRFQ.EOF
%>
			<tr>
				<td colspan=5>
					<table cellpadding=0>
						<tr>
							<td><b>Supplier:</b></td>
							<td width=5><img src="/Images/Spacer.gif" width=5 height=1 alt="" border=0></td>
							<td><%= rsRFQ("CompanyName") %></td>
						</tr>
						<tr>
							<td><b>Status:</b></td>
							<td width=5><img src="/Images/Spacer.gif" width=5 height=1 alt="" border=0></td>
							<td><%= rsRFQ("RFQStatus") %></td>
						</tr>
					</table>
				</td>
			</tr>
<%
		If rsRFQ("RFQStatusId") = 22 Then
%>
			<tr>
				<td colspan=5 style="color:red;">This has not been issued to the supplier yet.</td>
			</tr>
<%
		Else
%>
			<tr>
				<td valign="top"><%= rsRFQ("RFQId") %></td>
				<td valign="top" width=450>
<%
			' Get RFQ Contents
			Set rsRFQI = Server.CreateObject("ADODB.RecordSet")
			sql = "Select * From RFQContents Where RFQid = " & rsRFQ("RFQId")
			Set rsRFQI = dbConn.Execute(sql)
			If Not(rsRFQI.BOF And rsRFQI.EOF) Then
%>
					<table width="100%" cellpadding=3 cellspacing=0>
						<tr>
							<td valign="top" style="font-weight:bold;">Quantity</td>
							<td valign="top" style="font-weight:bold;">Description</td>
							<td valign="top" style="font-weight:bold;text-align:right;" nowrap>Price Ex</td>
							<td valign="top" style="font-weight:bold;text-align:right;" nowrap>Price Ex Sub Total</td>
						</tr>
<%
				Do Until rsRFQI.EOF
%>
						<tr>
							<td valign="top"><%= rsRFQI("Quantity") %></td>
							<td valign="top"><%= rsRFQI("Description") %></td>
							<td valign="top" style="text-align:right;"><%= formatCurrency(rsRFQI("PriceEx"),2) %></td>
							<td valign="top" style="text-align:right;"><%= formatCurrency(rsRFQI("PriceEx")*rsRFQI("Quantity"),2) %></td>
						</tr>
<%
					rsRFQI.MoveNext
				Loop
%>
					</table>
<%
				rsRFQI.Close
				Set rsRFQI = Nothing
			End If
%>
				</td>
				<td valign="top" align="right"><%= formatCurrency(rsRFQ("TotalEx"),2) %></td>
				<td valign="top" align="right"><%= formatCurrency(rsRFQ("TotalInc"),2) %></td>
			</tr>
<%
		End If
%>
			<tr>
				<td colspan=5 align="right">
				<input type="button" value="Create Purchase Order" onclick="CreatePurchaseOrder();">
				<hr>
				</td>
			</tr>
<%
		rsRFQ.MoveNext
	Loop
%>
		</table>
<%
End If
rsRFQ.Close
Set rsRFQ = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
