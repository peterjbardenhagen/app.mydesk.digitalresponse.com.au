<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

SetWorkingDir Request.ServerVariables("URL")

'If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngQid
Dim strEmail
Dim boolEmail
Dim boolPrint
Dim intQuoteStatusId
Dim strQuoteStatus

lngQid = CLng(Request("Qid"))
boolEmail = Trim(Request("Email"))
boolPrint = Trim(Request("Print"))

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

If boolPrint Then
	' Audit trail
	sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Printed', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
End If

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, Quotes.CustomerNotes As CN, Quotes.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, QuoteCOS.QuoteCOSFile, QuoteStatus.QuoteStatus From ((Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) LEFT OUTER JOIN QuoteCOS ON Quotes.QuoteCOSId = QuoteCOS.QuoteCOSId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(sql)

If Not(rsQu.BOF And rsQu.EOF) Then
	intQuoteStatusId = rsQu("QuoteStatusId")
	lngDivisionId = rsQu("DivisionId")
	' If Draft then make Issued
	If boolPrint Or boolEmail Then
		' Set New Status
		Select Case intQuoteStatusId
			Case 9, 1, 8, 3 ' Pending Approval -> Issued, Draft -> Issued, Approved -> Issued
				sql = "Update Quotes Set QuoteStatusId = 2 Where Qid = " & lngQid
				dbConn.Execute(sql)
				' Audit trail
				sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Status changed to issued', '" & ServerToEST(Now()) & "')"
				dbConn.Execute(sql)
				strQuoteStatus = "ISSUED"
			Case Else
				strQuoteStatus = UCase(rsQu("QuoteStatus"))
		End Select
	Else
		strQuoteStatus = UCase(rsQu("QuoteStatus"))
	End If

	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Divisions Where DivisionId = " & rsQu("QDivisionId")
	Set rsDi = dbConn.Execute(sql)
	
	strLogo = rsDi("Logo")
	
	Set rsLoc = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsQu("LocationId")
	Set rsLoc = dbConn.Execute(sql)

	If rsLoc("POStateId") > 0 Then
		Set rsLoc2 = Server.CreateObject("ADODB.RecordSet")
		sql = "Select State From States Where StateId = " & rsLoc("POStateId")
		Set rsLoc2 = dbConn.Execute(sql)
		strPOState = rsLoc("State")
	End If

	Set rsCon = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where ContactId = " & rsQu("ContactId")
	Set rsCon = dbConn.Execute(sql)
	
	If rsQu("QuoteStatusId") = 1 Or rsQu("QuoteStatusId") = 8 Then ' 1 = Draft, 8 = Approved
		strQuoteStatusRamification = "ISSUED"
		intQuoteStatusId = 2
	Else
		strQuoteStatusRamification = "REPRINTED"
		intQuoteStatusId = 7
	End If
	
	Select Case LCase(rsCon("Country")&"")
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
	If Not boolEmail And Not boolPrint And Not Request.Cookies("UserSettings")("Code") = "TL0084" Then
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
			.page  { 
				height: 100%;
				margin: 0% 0% 0% 0%;
			}
		</style>
		<script language="javascript">
			var globalTop;
			var currentPage;
			globalTop = 0;
			currentPage = 420;
		</script>
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
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2 class="page">
<%

	If Not boolEmail Then

%>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<!--#include file="NavBar.asp"-->
				</td>
			</tr>
<%

		If rsQu("QuoteCOSId") <> 0 Then

%>
			<tr>
				<td><li><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/FilesLibrary/Files/<%= rsQu("QuoteCOSFile") %>" target="_blank">Download Conditions of Sale</a></td>
			</tr>
<%

		End If

%>
		</table>
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Quote</td>
			</tr>
		</table>
		<br class="NoPrint">
<%

	Else

%>
		<div class="NoPrint">
		<!--#include virtual="/System/CurrencySelector.asp"-->
		</div>
<%

	End If

%>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table1">
			<tr>
				<td valign="top"><img src="/images/techlight-logo.svg" width="300" height="127" border=0 alt="" style="object-fit:contain; object-position:left;"><br><br></td>
				<td valign="top" align="right">
					<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
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
				<span style="font-size:24px;">QUOTE</span>
				</td>
			</tr>
			<tr>
				<td colspan=2><br></td>
			</tr>
			<tr>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table11">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Quote #:</td>
							<td><%= rsQu("Qid") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Date:</td>
							<td nowrap><%= FormatDateU(rsQu("QuoteDate"), False) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Status:</td>
							<td style="font-weight:bold;color:red;"><%= UCase(strQuoteStatus) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Validity:</td>
							<td style="font-weight:bold;"><%= rsQu("Validity") %> days</td>
						</tr>
<%
If rsQu("Reference")&"" <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Project:</td>
							<td style="font-weight:bold;"><%= rsQu("Reference") %></td>
						</tr>
<%
End If
%>
					</table>
				</td>
			</tr>
			<tr>
				<td><br></td>
			</tr>
			<tr>
				<td valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table4">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">To:</td>
							<td><%= rsCon("FirstName") & " " & rsCon("Surname") %><br><%= rsCon("CompanyName") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Phone:</td>
							<td><%= rsCon("Phone") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Email:</td>
							<td>sales@techlight.com.au<!--<%= rsCon("Email") %>--></td>
						</tr>
					</table>
				</td>
				<td valign="top" width="50%">
					<table align="center" width="100%" border="0" borderwidth=1 bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table5">
                        <%
                        Dim strSenderName, strSenderEmail, strSenderPhone, strSenderMobile
                        strSenderName = rsQu("Name") & ""
                        strSenderEmail = rsQu("Email") & ""
                        strSenderPhone = rsQu("Phone") & ""
                        strSenderMobile = rsQu("Mobile") & ""

                        If rsQu("SenderCode") & "" <> "" Then
                            Dim rsSender
                            Set rsSender = dbConn.Execute("SELECT Name, Email, Phone, Mobile FROM Users WHERE Code = '" & rsQu("SenderCode") & "'")
                            If Not rsSender.EOF Then
                                strSenderName = rsSender("Name") & ""
                                strSenderEmail = rsSender("Email") & ""
                                strSenderPhone = rsSender("Phone") & ""
                                strSenderMobile = rsSender("Mobile") & ""
                            End If
                            rsSender.Close()
                            Set rsSender = Nothing
                        End If
                        %>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">From:</td>
							<td><%= strSenderName %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Email:</td>
							<td><%= strSenderEmail %></td>
						</tr>
						<%
						If strSenderPhone <> "" Then
						%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Phone:</td>
							<td><%= strSenderPhone %></td>
						</tr>
						<%
						End If
						If strSenderMobile <> "" Then
						%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Mobile:</td>
							<td><%= strSenderMobile %></td>
						</tr>
						<%
						End If
						%>
					</table>
				</td>
			</tr>
		</table>
		<table align="center" width="595" border="0" cellpadding="5" cellspacing="0" ID="Table6">
			<tr>
				<td>
				<br><p align="center" class="Times2Bold">Please refer to our Quote #</p>
				</td>
			</tr>
		</table>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table7">
			<tr>
				<td valign="top" width=40 nowrap style="border-bottom:2px solid black;font-weight:bold;font-size:12px;width:40px;">Item</td>
				<td valign="top" nowrap style="border-bottom:2px solid black;font-weight:bold;font-size:12px;width:60px;">Qty</td>
				<td valign="top" style="border-bottom:2px solid black;font-weight:bold;font-size:12px;">Description</td>
				<td valign="top" width=100 style="border-bottom:2px solid black;font-weight:bold;font-size:12px;text-align:right;">Nett Price</td>
				<td valign="top" width=100 style="border-bottom:2px solid black;font-weight:bold;font-size:12px;text-align:right;">Ext. Nett Price</td>
			</tr>
		</table>
<%
	' Get Items

	Set rsQTPi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From QuoteContents Where Qid = " & lngQid & " ORDER BY [Type]"
	Set rsQTPi = dbConn.Execute(sql)

	Dim i
	Dim decRunningNettPriceTotal
	i = 1
	decRunningNettPriceTotal = 0
	If Not(rsQTPi.BOF And rsQTPi.EOF) Then
		Do Until rsQTPi.EOF
			decRunningNettPriceTotal = decRunningNettPriceTotal + rsQTPi("ExtNettPrice")
            dblCurrencyRate = 1 ' hack
%>
		<div style="position:relative;display:block;page-break-after:auto;" id="ITDiv<%= i %>">
		<table align="center" width=595 cellpadding=0 cellspacing=0 border=0 id="ITTable<%= i %>">
			<tr>
				<td valign="top" style="border-bottom:1px solid black;width:40px;" width=40><%= i %></td>
				<td valign="top" style="border-bottom:1px solid black;width:60px;" nowrap><% If CLng(rsQTPi("Quantity")) = 0 And (CLng(rsQTPi("Days")) <> 0 And CLng(rsQTPi("Units")) <> 0) Then Response.Write(CLng(rsQTPi("Days")) & "&nbsp;days<br>" & CLng(rsQTPi("Units")) & "&nbsp;units") Else Response.Write(CLng(rsQTPi("Quantity"))) %></td>
				<td valign="top" style="border-bottom:1px solid black;"><% If rsQTPi("Type") <> "" Then Response.Write("<b>Type:</b> " & rsQTPi("Type") & "<br>") %><% If rsQTPi("ProductCode") <> "" Then Response.Write("<b>Item:</b> " & rsQTPi("ProductCode") & "<br>") %><%= rsQTPi("Description") %>&nbsp;</td>
				<td valign="top" style="border-bottom:1px solid black;width:100px;text-align:right;"><%= "$"%><% If Not IsNull(rsQTPi("NettPrice")) Then Response.Write(FormatNumber(dblCurrencyRate*rsQTPi("NettPrice")+0,2)) %></td>
				<td valign="top" style="border-bottom:1px solid black;width:100px;text-align:right;"><%= "$"%><%= FormatNumber(dblCurrencyRate*rsQTPi("ExtNettPrice"),2) %></td>
			</tr>
		</table>
		<br>
		</div>
<%
			If i = 1 Then
%>
		<script language="javascript">
			var offsetHeight;
			var offsetNew;
			var currentPage;

			offsetHeight = document.getElementById('ITTable<%= i %>').offsetHeight;
			offsetNew = globalTop+10;
			document.getElementById('ITDiv<%= i %>').style.top = offsetNew;
			
			globalTop += offsetNew;
		</script>
<%
			Else
%>
		<script language="javascript">
			var offsetHeightPrev;
			var offsetHeightThis;
			var offsetNew;
			var currentPage;

			offsetHeightPrev = document.getElementById('ITTable<%= i-1 %>').offsetHeight;
			offsetHeightThis = document.getElementById('ITTable<%= i %>').offsetHeight;
			offsetNew = globalTop;
			document.getElementById('ITDiv<%= i %>').style.top = offsetNew;

			globalTop = offsetNew;
			currentPage += offsetHeightThis; 

			if((currentPage+offsetHeightThis) > 720) {
				document.getElementById("ITDiv<%= i %>").style.pageBreakBefore="always";
				currentPage = 0;
			}
		</script>
<%
			End If
			i = i + 1
			rsQTPi.MoveNext
		Loop
	End If
	rsQTPi.Close
	Set rsQTPi = Nothing

	' Get Third Party Supply

	Set rsQTPi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From QuoteThirdPartyContents Where QuoteId = " & lngQid
	Set rsQTPi = dbConn.Execute(sql)

	i = 1
	If Not(rsQTPi.BOF And rsQTPi.EOF) Then
		Do Until rsQTPi.EOF
			decRunningNettPriceTotal = decRunningNettPriceTotal + CDbl(rsQTPi("ExtNettPrice"))
%>
		<div style="position:relative;display:block;page-break-after:auto;" id="TPDiv<%= i %>">
		<table align="center" width=595 cellpadding=0 cellspacing=0 border=0 id="TPTable<%= i %>">
			<tr>
				<td valign="top" style="border-bottom:1px solid black;" width=40><%= i %></td>
				<td valign="top" style="border-bottom:1px solid black;" width=60 nowrap><%= CLng(rsQTPi("Quantity")) %></td>
				<td valign="top" style="border-bottom:1px solid black;"><%= rsQTPi("Description") %>&nbsp;</td>
				<td valign="top" style="border-bottom:1px solid black;width:100px;text-align:right;"><%= "$"%><%= FormatNumber(dblCurrencyRate * CDbl(rsQTPi("NettPrice")),2) %></td>
				<td valign="top" style="border-bottom:1px solid black;width:100px;text-align:right;"><%= "$"%><%= FormatNumber(dblCurrencyRate * CDbl(rsQTPi("ExtNettPrice")),2) %></td>
			</tr>
		</table>
		<br>
		</div>
		<script language="javascript">
			var offsetHeightPrev;
			var offsetHeightThis;
			var offsetNew;
			var currentPage;

<%
			If i = 1 Then
%>
			offsetHeightPrev = 0;
			offsetHeightThis = document.getElementById('TPTable<%= i %>').offsetHeight;
<%
			Else
%>
			offsetHeightPrev = document.getElementById('TPTable<%= i-1 %>').offsetHeight;
			offsetHeightThis = document.getElementById('TPTable<%= i %>').offsetHeight;
<%
			End If
%>
			offsetNew = globalTop;
			document.getElementById('TPDiv<%= i %>').style.top = offsetNew;

			globalTop = offsetNew;
			currentPage += offsetHeightThis; 

			if((currentPage+offsetHeightThis) > 720) {
				document.getElementById("TPDiv<%= i %>").style.pageBreakBefore="always";
				currentPage = 0;
			}
		</script>
<%
			i = i + 1
			rsQTPi.MoveNext
		Loop
	End If
	rsQTPi.Close
	Set rsQTPi = Nothing

%>
		</table>
		<div id="FinalTable" style="position:relative;">
		<table align="center" width="595" cellpadding="5" cellspacing="0">
			<tr>
				<td colspan=5><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %>/images/Spacer.gif" width=595 height=1 border=0 /></td>
			</tr>
			<tr>
				<td>
					<br>
					<table border="0" cellpadding="5" cellspacing="0" ID="Table9">
<%
	If rsQu("Delivery") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Delivery:</td>
							<td valign="top" width=540><%= rsQu("Delivery") %></td>
						</tr>
<%
	End If
	If rsQu("Terms") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Terms:</td>
							<td valign="top" width=540><%= Replace(rsQu("Terms"),CHR(10),"<BR>") %></td>
						</tr>
<%
	End If
	If rsQu("CustomerNotes") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Notes:</td>
							<td valign="top" width=540><%= Replace(rsQu("CN"),CHR(10),"<BR>") %></td>
						</tr>
<%
	End If
%>
					</table>
				</td>
				<td align="right" valign="top">
					<br>
<%
    strCurrencyName = "Australia Dollars"
		dblGSTPercentage = 10
	If (strCurrencyName = "Australia Dollars" Or (strCurrencyName = "New Zealand Dollars" And lngDivisionId = 6)) Then
		If strCurrencyName = "Australia Dollars" Then
			dblGSTPercentage = 10
		ElseIf strCurrencyName = "New Zealand Dollars" And lngDivisionId = 6 Then
			dblGSTPercentage = 12.5
		End If
%>
					<table cellpadding=5 cellspacing=0 border=0 ID="Table10">
						<tr>
							<td nowrap style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Ex. GST</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(dblCurrencyRate*decRunningNettPriceTotal,2) %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td nowrap style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">GST</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(dblCurrencyRate*(decRunningNettPriceTotal*dblGSTPercentage/100),2) %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td nowrap style="border-top:2px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Inc. GST</td>
							<td align="right" style="border-top:2px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(dblCurrencyRate*decRunningNettPriceTotal*(1+(dblGSTPercentage/100)),2) %></td>
						</tr>
					</table>
<%
	Else
%>
					<table cellpadding=5 cellspacing=0 border=0 ID="Table14">
						<tr>
							<td nowrap style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$"%><%= FormatNumber(dblCurrencyRate*decRunningNettPriceTotal,2) %></td>
						</tr>
					</table>
<%
	End If
%>
				</td>
			</tr>
			<tr>
				<td colspan=4><br>The quote above is based on information provided and should quantities or technical details change we reserve the right to provide a revision.</td>
			</tr>
		</table>
		</div>

		<script language="javascript">
			document.getElementById('FinalTable').style.top = globalTop;
			if(currentPage>800){
				document.getElementById("FinalTable").style.pageBreakBefore="always";
			}
		</script>
<%
	If boolPrint Then
%>
		<form method="post" id="frmPost" name="frmPost">
			<input type="hidden" name="Qid" id="Qid">
		</form>
		<script language="javascript">

		</script>
<%
	End If
	If Not boolEmail Then
		If Not(rsQu("InternalNotes")&""="" Or rsQu("InternalNotes") = Null) Then
%>
		<table align="center" width="595" border="0" cellpadding="5" cellspacing="0" class="NoPrint" ID="Table15">
			<tr>
				<td width=50%>
				<b>Internal Notes</b><br>
				<%= Replace(rsQu("InternalNotes"), Chr(10),"<br>") %>
				</td>
			</tr>
		</table>
<%
		End If
	End If
Else
%>
	<script language="javascript">
		alert('No record exists');
		window.close();
	</script>
<%
End If
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->