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

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Select POStatusId From PurchaseOrders Where POid = " & lngPOid
Set rsPOS = dbConn.Execute(sql)

Select Case rsPOS("POStatusId")
	Case 1,2
		strPOStatus = "NOT APPROVED"
	Case 3,4,7
		strPOStatus = "APPROVED"
	Case 5,6
		strPOStatus = "CANCELLED/DECLINED"
End Select

Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT PO.*, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, PO.DivisionId As DivisionId, PO.IntroText AS IT, PO.InternalNotes AS [IN], [Users].LocationId, PurchaseOrderStatus.POStatus, PurchaseOrderPaymentTypes.POPaymentType FROM (Users INNER JOIN (PurchaseOrderStatus INNER JOIN PurchaseOrders AS PO ON PurchaseOrderStatus.POStatusId = PO.POStatusId) ON Users.Code = PO.Code) INNER JOIN PurchaseOrderPaymentTypes ON PO.POPaymentTypeId = PurchaseOrderPaymentTypes.POPaymentTypeId WHERE PO.POid = " & lngPOid
Set rsPO = dbConn.Execute(sql)

If Not(rsPO.BOF and rsPO.EOF) Then
	If strPOStatus = "" Then strPOStatus = rsPO("POStatus")

	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Divisions Where DivisionId = " & rsPO("DivisionId")
	Set rsDi = dbConn.Execute(sql)

	strLogo = rsDi("Logo")

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
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
<%

	If Not boolForFaxEmail Then

%>
		<!--#include file="NavBar_Requests.asp"-->
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Purchase Order Request</td>
			</tr>
		</table>
		<br class="NoPrint">
<%

	End If

%>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table1">
			<tr>
				<td valign="top"><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""></td>
				<td valign="top" align="right">
					<br>
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
				<span style="font-size:24px;">PURCHASE ORDER REQUEST</span><br>
				<b>This is not a purchase order.</b>
				</td>
			</tr>
			<tr>
				<td colspan=2><br></td>
			</tr>
			<tr>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table11">
<%
	If strPOStatus = "APPROVED" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Approval #:</td>
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
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Payment Method:</td>
							<td><%= rsPO("POPaymentType") %></td>
						</tr>
<%
Dim strInternalNotes
Dim strIntroText

strInternalNotes = rsPO("InternalNotes")&""
strIntroText = rsPO("IntroText")&""

If strInternalNotes <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Internal Notes:</td>
							<td><%= Replace(strInternalNotes,vbcrlf,"<br>") %></td>
						</tr>
<%
End If
If strIntroText <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Notes:</td>
							<td><%= Replace(strIntroText,vbcrlf,"<br>") %></td>
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
							<td style="font-weight:bold;vertical-align:top;width:120px;">Eemail:</td>
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
		<br>
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
	sql = "Select * From PurchaseOrderContents Where POid = " & lngPOid
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
				<td valign="top" style="width:100px;text-align:right;"><% If IsNumberGreaterThanZero(rsQi("PriceEx")) Then Response.Write("A" & FormatCurrency(rsQi("PriceEx")+0,2)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
				<td valign="top" style="width:100px;text-align:right;"><% If IsNumberGreaterThanZero(rsQi("PriceExSubTotal")) Then Response.Write("A" & FormatCurrency(rsQi("PriceExSubTotal")+0,2)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
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
							<td valign="top" width=540><%= rsPO("IT") %><%= Replace(rsPO("IT")&"", Chr(10), "<br>") %></td>
						</tr>
<%
	End If
%>
					</table>
				</td>
				<td width=250 align="right" valign="top">
					<br>
					<table cellpadding=2 cellspacing=0 border=0 ID="Table8">
						<tr>
							<td style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Ex. GST</td>
							<td style="border-top:1px solid black;border-bottom:2px solid black;" align="right"><% If IsNumberGreaterThanZero(decRunningPriceTotal) Then Response.Write("A" & FormatCurrency(decRunningPriceTotal)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
						</tr>
<%
	If rsPO("GST") Then
%>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">GST</td>
							<td style="border-top:1px solid black;border-bottom:2px solid black;" align="right"><% If IsNumberGreaterThanZero(decRunningPriceTotal) Then Response.Write("A" & FormatCurrency(decRunningPriceTotal*.1,2)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td style="border-top:2px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Inc. GST</td>
							<td style="border-top:2px solid black;border-bottom:2px solid black;" align="right"><% If IsNumberGreaterThanZero(decRunningPriceTotal) Then Response.Write("A" & FormatCurrency(decRunningPriceTotal*1.1,2)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
						</tr>
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
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->