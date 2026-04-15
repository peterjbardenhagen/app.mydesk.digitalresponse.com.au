<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

'If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

SetWorkingDir Request.ServerVariables("URL")

Dim lngPOid
Dim strFax
Dim strEmail
Dim boolFax
Dim boolEmail
Dim boolPrint
Dim strPOStatus

lngPOid = CLng(Request("POid"))
boolFax = Trim(Request("Fax"))
boolEmail = Trim(Request("Email"))
boolPrint = Trim(Request("Print"))

If boolFax <> "" Then
	boolFax = True
Else
	boolFax = False
End If

If boolEmail <> "" Then
	boolEmail = True
Else
	boolEmail = False
End If

If boolPrint <> "" Then
	boolPrint = True
Else
	boolPrint = False
End If

If boolEmail Or boolFax Then boolForFaxEmail = True

		If boolEmail Then
			dblGSTPercentage = 10
			dblCurrencyRate = 1
			strCurrencyPrefix = "$"
		End If

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Select POStatusId From PurchaseOrders Where POid = " & lngPOid
Set rsPOS = dbConn.Execute(sql)

If boolPrint Or boolForFaxEmail Then
	Select Case rsPOS("POStatusId")
		Case 1, 2, 3
			' If draft or approved or pending (if can be approved)
			sql = "Update PurchaseOrders Set POStatusId = 4 Where POStatusId = 1 Or POStatusId = 3 And POid = " & lngPOid
			dbConn.Execute(sql)

			strPOStatus = "ISSUED"

			' Audit trail
			sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Printed', '" & ServerToEST(Now()) & "')"
			dbConn.Execute(sql)
		Case 4
			' If issued
			strPOStatus = "REPRINTED"

			' Audit trail
			sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Reprinted', '" & ServerToEST(Now()) & "')"
			dbConn.Execute(sql)
	End Select
End If

Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT PO.*, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, PO.DivisionId As DivisionId, PO.IntroText AS IT, PO.InternalNotes AS [IN], [Users].LocationId, PurchaseOrderStatus.POStatus, PurchaseOrderPaymentTypes.POPaymentType FROM (Users INNER JOIN (PurchaseOrderStatus INNER JOIN PurchaseOrders AS PO ON PurchaseOrderStatus.POStatusId = PO.POStatusId) ON Users.Code = PO.Code) INNER JOIN PurchaseOrderPaymentTypes ON PO.POPaymentTypeId = PurchaseOrderPaymentTypes.POPaymentTypeId WHERE PO.POid = " & lngPOid
Set rsPO = dbConn.Execute(sql)

If Not(rsPO.BOF and rsPO.EOF) Then
	If strPOStatus = "" Then strPOStatus = rsPO("POStatus")

	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Divisions Where DivisionId = " & rsPO("DivisionId")
	Set rsDi = dbConn.Execute(sql)

	strLogo = rsDi("Logo")
	boolRequest = rsDi("PurchaseRequests")
	If boolRequest Then Response.Redirect("ViewRequest.asp?POid=" & lngPOid)

	Set rsLoc = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsPO("LocationId")
	Set rsLoc = dbConn.Execute(sql)

	If rsLoc("POStateId") > 0 Then
		Set rsLoc2 = Server.CreateObject("ADODB.RecordSet")
		sql = "Select State From States Where StateId = " & rsLoc("POStateId")
		Set rsLoc2 = dbConn.Execute(sql)
		strPOState = rsLoc("State")
	End If

	Set rsCon = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where ContactId = " & rsPO("ContactId")
	Set rsCon = dbConn.Execute(sql)
	
    Dim strCountry
    strCountry = rsCon("Country")&""
    strCountry = LCase(strCountry)
	
    Select Case strCountry
	    Case "australia", "", "aus", "aust", "aus.", "aust."
		    boolExport = False
	    Case Else
		    boolExport = True
    End Select

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
<%

	If Not boolForFaxEmail And Not boolPrint Then

%>
			body, p, td {
				display:none;
				visibility:hidden;
			}
<%

	End If

%>
			.NoPrint {
				display:none;
				visibility:hidden;
			}
		</style>
<%

	If Request("Msg") <> "" Then

%>
		<script language="javascript">
			alert('<%= Request("Msg") %>');
		</script>
<%

	End If
	
%>
	</head>
	<body style="background-color:#ffffff;" Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
<%

	If Not boolForFaxEmail Then

%>
		<!--#include file="NavBar.asp"-->
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Purchase Order</td>
			</tr>
		</table>
		<br class="NoPrint">
<%

	Else
		strCurrencyName = Trim(Request("CurrencyName"))
		dblCurrencyRate = CDbl(Request("CurrencyRate"))
		strCurrencyPrefix = Trim(Request("CurrencyPrefix"))
	End If

    dblCurrencyRate = 1

%>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table1">
			<tr>
				<td valign="top"><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""></td>
				<td valign="top" align="right">
					<table cellpadding=3 cellspacing=0 border=0 ID="Table9">
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
				<span style="font-size:24px;">PURCHASE ORDER</span>
				</td>
			</tr>
			<tr>
				<td colspan=2><br></td>
			</tr>
			<tr>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table11">
<%
	If Not rsPO("POStatusId") = 1 Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Purchase Order #:</td>
							<td><%= rsPO("POid") %></td>
						</tr>
<%
	End If
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Date:</td>
							<td><%= FormatDateU(rsPO("PODate"), False) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Status:</td>
							<td style="color:red;font-weight:bold;"><%= UCase(strPOStatus) %></td>
						</tr>
<%
	If Request.Cookies("ClientSettings")("Prefix") = "TL" Then
		strProject = rsPO("Project")
		If strProject <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Project:</td>
							<td><%= rsPO("Project") %></td>
						</tr>
<%
		End If
	End If
	If Year(rsPO("DateRequired")) > 2005 Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Date Required:</td>
							<td><%= FormatDateU(rsPO("DateRequired"), False) %></td>
						</tr>
<%
	End If
%>
					</table>
				</td>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table3">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Delivery Address:</td>
<%
	If rsPO("DeliverToLocation") <> "" Then
%>
							<td style="font-weight:bold;"><%= UCase(Replace(rsPO("DeliverToLocation"), CHR(10), "<br>")) %></td>
<%
	Else
		Set rsDelLoc = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsPO("DeliverToLocationId")
		Set rsDelLoc = dbConn.Execute(sql)
%>
							<td style="font-weight:bold;">
							<%= rsDelLoc("Address1") %><br>
<%

		If rsDelLoc("Address2") <> "" Then

%>
							<%= rsDelLoc("Address2") %><br>
<%

		End If

%>
							<%= rsDelLoc("Suburb") %><br>
							<%= rsDelLoc("State") %><br>
							<%= rsDelLoc("Country") %>
							</td>
<%
	End If
%>
						</tr>
					</table>
				</td>
			</tr>
			<tr>
				<td><br></td>
			</tr>
			<tr>
				<td valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table2">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">To:</td>
							<td><%= rsCon("FirstName") & " " & rsCon("Surname") %><br><%= rsCon("CompanyName") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Phone:</td>
							<td><%= rsCon("Phone") %></td>
						</tr>
<%
	If Not IsNull(rsCon("Email")) And Not rsCon("Email") = "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Email:</td>
							<td>sales@techlight.com.au<!--<%= rsCon("Email") %>--></td>
						</tr>
<%
	End If
%>
					</table>
				</td>
				<td valign="top" width="50%">
					<table align="center" width="100%" border="0" borderwidth=1 bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table4">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">From:</td>
							<td><%= rsPO("Name") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Email:</td>
							<td><%= rsPO("Email") %></td>
						</tr>
<%
If rsPO("Phone") <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Phone:</td>
							<td><%= rsPO("Phone") %></td>
						</tr>
<%
End If
If rsPO("Mobile") <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Mobile:</td>
							<td><%= rsPO("Mobile") %></td>
						</tr>
<%
End If
%>
					</table>
				</td>
			</tr>
		</table>
		<table align="center" width="595" border="0" cellpadding="5" cellspacing="0" ID="Table5">
			<tr>
				<td>
				<br>
				<p align="center" class="Times2Bold">Please quote our Purchase Order # for further enquiries</p><br>
				</td>
			</tr>
		</table>
		<table align="center" width="595" border="1" borderwidth=1 bordercolor="#00000" cellpadding="5" cellspacing="0" ID="Table6">
			<tr>
				<td width=15 style="font-weight:bold;font-size:12px;">Item</td>
				<td width=15 style="font-weight:bold;font-size:12px;">Qty</td>
				<td style="font-weight:bold;font-size:12px;">Description</td>
				<td width=100 style="font-weight:bold;font-size:12px;text-align:right;">Price Ex. GST</td>
				<td width=100 style="font-weight:bold;font-size:12px;text-align:right;">Sub Total Ex. GST</td>
			</tr>
<%

	Set rsQi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From PurchaseOrderContents Where POid = " & lngPOid & " ORDER BY Description"
	Set rsQi = dbConn.Execute(sql)

	Dim i
	Dim decRunningPriceTotal
	i = 1
	decRunningPriceTotal = 0
	If Not(rsQi.BOF And rsQi.EOF) Then
		Do Until rsQi.EOF
			decRunningPriceTotal = decRunningPriceTotal + rsQi("PriceExSubTotal")
%>
			<tr>
				<td valign="top" width=15><%= i %></td>
				<td valign="top" width=15><%= rsQi("Quantity") %></td>
				<td valign="top"><%= rsQi("Description") %></td>
				<td valign="top" style="width:100px;text-align:right;"><% If IsNumeric(rsQi("PriceEx")) Then If rsQi("PriceEx") > 0 Then Response.Write("$"&FormatNumber(dblCurrencyRate*rsQi("PriceEx")+0,2)) Else Response.Write("<span style=""color:red;font-weight:bold;"">" & "$" & FormatNumber(dblCurrencyRate*rsQi("PriceEx")+0,2) & "</span>") End If End If %></td>
				<td valign="top" style="width:100px;text-align:right;"><% If IsNumeric(rsQi("PriceExSubTotal")) Then If rsQi("PriceExSubTotal") > 0 Then Response.Write("$"&FormatNumber(dblCurrencyRate*rsQi("PriceExSubTotal")+0,2)) Else Response.Write("<span style=""color:red;font-weight:bold;"">" & "$"& FormatNumber(dblCurrencyRate*rsQi("PriceExSubTotal")+0,2) & "</span>") End If End If %></td>
			</tr>
<%
			i = i + 1
			rsQi.MoveNext
		Loop
	End If
	rsQi.Close
	Set rsQi = Nothing
%>
		</table>
		<table align="center" width="595" cellpadding="5" cellspacing="0" ID="Table12">
			<tr>
				<td valign="top">
				<br>
					<table border="0" cellpadding="5" cellspacing="0" ID="Table7">
<%
	If rsPO("Terms") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Terms:</td>
							<td valign="top" width=540><%= Replace(rsPO("Terms")&"", Chr(10), "<br>") %></td>
						</tr>
<%
	End If
	If rsPO("IntroText") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Notes:</td>
							<td valign="top" width=540><%= Replace(rsPO("IT")&"", Chr(10), "<br>") %></td>
						</tr>
<%
	End If
%>
					</table>
				</td>
				<td width=250 align="right" valign="top">
					<br>
<%
	If Not(boolExport) Then
		dblGSTPercentage = 10
		If strCurrencyName = "Australia Dollars" Then
			dblGSTPercentage = 10
		ElseIf strCurrencyName = "New Zealand Dollars" And lngDivisionId = 6 Then
			dblGSTPercentage = 12.5
		End If
%>
					<table cellpadding=3 cellspacing=0 border=0 ID="Table10">
						<tr>
							<td style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Ex. GST</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(dblCurrencyRate*decRunningPriceTotal,2) %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">GST</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(decRunningPriceTotal*dblGSTPercentage/100,2) %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td style="border-top:2px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Inc. GST</td>
							<td align="right" style="border-top:2px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(dblCurrencyRate*decRunningPriceTotal*(1+(dblGSTPercentage/100)),2) %></td>
						</tr>
					</table>
<%
	Else
%>
					<table cellpadding=5 cellspacing=0 border=0 ID="Table14">
						<tr>
							<td style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total <% If Not(boolExport) Then Response.Write "Ex. GST" %></td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(dblCurrencyRate*decRunningPriceTotal,2) %></td>
						</tr>
					</table>
<%
	End If
%>
					</table>
				</td>
			</tr>
		</table>
<%
	If boolPrint Then
%>
		<form method="post" id="frmPost" name="frmPost">
			<input type="hidden" name="POid" id="POid">
		</form>
		<script language="javascript">
		</script>
<%
	End If
%>
	</body>
</html>
<%
Else
%>
	<script language="javascript">
		alert('No record exists');
		window.close();
	</script>
<%
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->