<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

SetWorkingDir Request.ServerVariables("URL")

'If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngRFQId
Dim strFax
Dim strEmail
Dim boolFax
Dim boolEmail
Dim boolPrint

lngRFQId = CLng(Request("RFQId"))
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

If boolPrint Then
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "Select RFQStatusId From RFQ Where RFQId = " & lngRFQId
	Set rs = dbConn.Execute(sql)
	If Not(rs.BOF And rs.EOF) Then
		Select Case rs("RFQStatusId")
			Case 21, 22, 23
				sql = "Update RFQ Set RFQStatusId = 23 Where RFQid = " & lngRFQid
				dbConn.Execute(sql)
		End Select
	End If
	rs.Close
	Set rs = Nothing
End If

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select RFQ.*, RFQ.IntroText As SN, RFQ.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, RFQStatus.RFQStatus From ((RFQ INNER JOIN Users ON RFQ.Code = Users.Code) INNER JOIN RFQStatus ON RFQStatus.RFQStatusId = RFQ.RFQStatusId) Where RFQId = " & lngRFQId
Set rsQu = dbConn.Execute(sql)

If Not(rsQu.BOF And rsQu.EOF) Then
	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Divisions Where DivisionId = " & rsQu("QDivisionId")
	Set rsDi = dbConn.Execute(sql)

	Set rsLoc = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsQu("LocationId")
	Set rsLoc = dbConn.Execute(sql)

	Set rsCon = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where ContactId = " & rsQu("ContactId")
	Set rsCon = dbConn.Execute(sql)
%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Session("WorkingDir") %>/System/Global.js"></script>
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
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="RefreshWindowClose();" ID="Button1" NAME="Button1"> <input type="button" value=" View RFQ " onclick="document.location.href='View.asp?RFQId=<%= lngRFQId %>';" ID="Button4" NAME="Button1"> <input type="button" value=" Email " onclick="document.location.href='Email.asp?RFQId=<%= lngRFQId %>';" ID="Button3" NAME="Button2"> <% If boolPrint Then %><input type="button" value="Print" style="font-weight:bold;color:red;" onclick="print();" ID="Button2" NAME="Button2"> (Make sure that you set the orientation to portrait)<% Else %><input type="button" value=" Print " onclick="if(confirm('If you proceed the RFQ\'s status will be set to issued.\nAre you sure you want to proceed?')){document.location.href='View.asp?RFQid=<%= lngRFQid %>&Print=True'}" ID="Button6" NAME="Button1"><% End If %></td>
			</tr>
		</table>
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Request For Quote</td>
			</tr>
		</table>
		<br class="NoPrint">
<%
	End If
%>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table1">
			<tr>
				<td>
				<img src="https://<%= Request.ServerVariables("SERVER_NAME") %><%= Session("WorkingDir") %>/images/<%= rsDi("Logo") %>" border=0 alt=""><br><br>
				</td>
				<td valign="top" align="right">
					<br>
					<table cellpadding=3 cellspacing=0 border=0>
						<tr>
							<td colspan=2>
							<%= DisplayLocationAddress(rsLoc("Address1"), rsLoc("Address2"), rsLoc("Suburb"), rsLoc("State"), rsLoc("PostCode"), rsLoc("Country"), rsLoc("PODisplay"), rsLoc("POAddress1"), rsLoc("POAddress2"), rsLoc("POSuburb"), rsLoc("POState"), rsLoc("POPostCode"), rsLoc("POCountry")) %>			
							</td>
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
				<span style="font-size:24px;">REQUEST FOR QUOTE</span>
				</td>
			</tr>
			<tr>
				<td colspan=2><br></td>
			</tr>
			<tr>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table11">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">RFQ #:</td>
							<td><%= rsQu("RFQId") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Date:</td>
							<td><%= FormatDateU(rsQu("RFQDate"), False) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Status:</td>
							<td style="font-weight:bold;color:red;"><%= UCase(rsQu("RFQStatus")) %></td>
						</tr>
					</table>
				</td>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table8">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:120px;">Delivery Address:</td>
<%
	If rsQu("DeliverToLocation") <> "" Then
%>
							<td style="font-weight:bold;"><%= UCase(Replace(rsQu("DeliverToLocation"), CHR(10), "<br>")) %></td>
<%
	Else
		Set rsDelLoc = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsQu("DeliverToLocationId")
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
							<td style="font-weight:bold;vertical-align:top;width:60px;">To:</td>
							<td><%= rsCon("FirstName") & " " & rsCon("Surname") %><br><%= rsCon("CompanyName") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Phone:</td>
							<td><%= rsCon("Phone") %></td>
						</tr>
<%
	If Not IsNull(rsCon("Fax")) And Not rsCon("Fax") = "" Then
%>
<!--
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Fax:</td>
							<td><%= rsCon("Fax") %></td>
						</tr>
-->
<%
	End If
	If Not IsNull(rsCon("Email")) And Not rsCon("Email") = "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Email:</td>
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
							<td style="font-weight:bold;vertical-align:top;width:60px;">From:</td>
							<td><%= rsQu("Name") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">E-mail:</td>
							<td><%= rsQu("Email") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Phone:</td>
							<td><%= rsQu("Phone") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Mobile:</td>
							<td><%= rsQu("Mobile") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Fax:</td>
							<td><%= rsQu("Fax") %></td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
		<table align="center" width="595" border="0" cellpadding="5" cellspacing="0" ID="Table5">
			<tr>
				<td>
				<br>
				<p align="center" class="Times2Bold">Please quote our RFQ # for further enquiries</p><br>
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
	sql = "Select * From RFQContents Where RFQId = " & lngRFQId
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
				<td valign="top" style="width:100px;text-align:right;"><% If IsNumberGreaterThanZero(rsQi("PriceEx")) Then Response.Write(FormatCurrency(rsQi("PriceEx")+0,2)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
				<td valign="top" style="width:100px;text-align:right;"><% If IsNumberGreaterThanZero(rsQi("PriceExSubTotal")) Then Response.Write(FormatCurrency(rsQi("PriceExSubTotal")+0,2)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
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
	If rsQu("Terms") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Terms:</td>
							<td valign="top" width=540><%= rsQu("Terms") %></td>
						</tr>
<%
	End If
	
	strSN = rsQu("SN")
	If strSN <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Notes:</td>
							<td valign="top" width=540><%= strSN %></td>
						</tr>
<%
	End If
%>
					</table>
				</td>
				<td width=250 align="right" valign="top">
					<br>
					<table cellpadding=5 cellspacing=0 border=0>
						<tr>
							<td style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Ex. GST</td>
							<td style="border-top:1px solid black;border-bottom:2px solid black;"><% If IsNumberGreaterThanZero(decRunningPriceTotal) Then Response.Write(FormatCurrency(decRunningPriceTotal)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td style="border-top:2px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Inc. GST</td>
							<td style="border-top:2px solid black;border-bottom:2px solid black;"><% If IsNumberGreaterThanZero(decRunningPriceTotal) Then Response.Write(FormatCurrency(decRunningPriceTotal*1.1,2)) Else Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;") %></td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
<%
	If boolPrint Then
%>
		<form method="post" id="frmPost" name="frmPost">
			<input type="hidden" name="RFQId" id="RFQId">
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