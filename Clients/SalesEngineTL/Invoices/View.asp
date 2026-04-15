<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

SetWorkingDir Request.ServerVariables("URL")

'If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngInvoiceId
Dim strEmail
Dim boolEmail
Dim boolPrint
Dim intInvoiceStatusId
Dim strInvoiceStatus

lngInvoiceId = CLng(Request("InvoiceId"))
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

    strCurrencyName = "Australia Dollars"
			dblGSTPercentage = 10
			dblCurrencyRate = 1
			strCurrencyPrefix = "$"

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

If boolPrint Then
	' Audit trail
	sql = "Insert Into InvoiceAudit (InvoiceId, Code, Action, DateEntered) Values (" & lngInvoiceId & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Invoice Printed', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
End If

Set rsInv = Server.CreateObject("ADODB.RecordSet")
sql = "Select Invoices.*, Invoices.CustomerNotes As CN, Invoices.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, InvoiceStatus.InvoiceStatus From ((Invoices INNER JOIN Users ON Invoices.Code = Users.Code) INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId) Where InvoiceId = " & lngInvoiceId
Set rsInv = dbConn.Execute(sql)

If Not(rsInv.BOF And rsInv.EOF) Then
	intInvoiceStatusId = rsInv("InvoiceStatusId")
	lngDivisionId = rsInv("DivisionId")

	' If Draft then make Issued
	If boolPrint Or boolEmail Then
		' Set New Status
		Select Case intInvoiceStatusId
			Case 1, 8, 3 ' Draft -> Issued, Approved -> Issued
				sql = "Update Invoices Set InvoiceStatusId = 2 Where InvoiceId = " & lngInvoiceId
				dbConn.Execute(sql)
				' Audit trail
				sql = "Insert Into InvoiceAudit (InvoiceId, Code, Action, DateEntered) Values (" & lngInvoiceId & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Invoice status changed to issued', '" & ServerToEST(Now()) & "')"
				dbConn.Execute(sql)
				
				strInvoiceStatus = "ISSUED"
			Case Else
				strInvoiceStatus = UCase(rsInv("InvoiceStatus"))
		End Select
	Else
		strInvoiceStatus = UCase(rsInv("InvoiceStatus"))
	End If

	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Divisions Where DivisionId = " & rsInv("QDivisionId")
	Set rsDi = dbConn.Execute(sql)
	
	strLogo = rsDi("Logo")

	Set rsLoc = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & rsInv("LocationId")
	Set rsLoc = dbConn.Execute(sql)

	If rsLoc("POStateId") > 0 Then
		Set rsLoc2 = Server.CreateObject("ADODB.RecordSet")
		sql = "Select State From States Where StateId = " & rsLoc("POStateId")
		Set rsLoc2 = dbConn.Execute(sql)
		strPOState = rsLoc("State")
	End If

	If rsDi("DivisionId") = 6 Then
		Select Case LCase(rsInv("InvCountry")&"")
			Case "new zealand", "", "nz", "nz.", "n.z."
				boolExport = False
			Case Else
				boolExport = True
		End Select

	Else
		Select Case LCase(rsInv("InvCountry")&"")
			Case "australia", "", "aus", "aust", "aus.", "aust.", "","qld","tas","vic","nsw"
				boolExport = False
			Case Else
				boolExport = True
		End Select
	End If
%>
<html>
	<head>
		<title>INVOICE#<%= lngInvoiceId %></title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
<%
	If Not boolEmailboolPrint Then
%>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
<%
	End If
%>
		<style>
			body, p, td, th
			{
				font-family: 'HelveticaNeue', Arial;
				font-size: 12px;
			}

			#pageLabel 
			{
				color: #000000;
			}

			.Header 
			{
				font-family: 'HelveticaNeue', Arial;
				font-size: 28px;
			}

			.Header2
			{
				font-family: 'HelveticaNeue', Arial;
				font-size: 18px;
				color: Teal;
			}

			.Header3
			{
				font-family: 'HelveticaNeue', Arial;
				font-size: 12px;
				font-weight: bold;
				color: #000000;
			}

			A.Header3, A:Link.Header3, A:Visited.Header3, A:Active.Header3, A:Hover.Header3
			{
				font-family: 'HelveticaNeue', Arial;
				font-size: 12px;
				font-weight: bold;
				color: #000000;
			}

			.Header4
			{
				font-family: 'HelveticaNeue', Arial;
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
				font-family: 'HelveticaNeue', Arial;
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

	If Not boolEmail And Not boolPrint Then

%>
			body, p, td {
				display:none;
				visibility:hidden;
			}
<%

	End If
	
	Dim delCompany
	Dim delAddress
	Dim delAddress1
	Dim delAddress2
	Dim delSuburb
	Dim delState
	Dim delPostCode
	Dim delCountry

	Dim invCompany
	Dim invAddress
	Dim invAddress1
	Dim invAddress2
	Dim invSuburb
	Dim invState
	Dim invPostCode
	Dim invCountry

	delCompany = rsInv("DelCompany") & ""
	delAddress = rsInv("DelAddress") & ""
	delAddress1 = rsInv("DelAddress1") & ""
	delAddress2 = rsInv("DelAddress2") & ""
	delSuburb = rsInv("DelSuburb") & ""
	delState = rsInv("DelState") & ""
	delPostCode = rsInv("DelPostCode") & ""
	delCountry = rsInv("DelCountry") & ""
	
	invCompany = rsInv("InvCompany") & ""
	invAddress = rsInv("InvAddress") & ""
	invAddress1 = rsInv("InvAddress1") & ""
	invAddress2 = rsInv("InvAddress2") & ""
	invSuburb = rsInv("InvSuburb") & ""
	invState = rsInv("InvState") & ""
	invPostCode = rsInv("InvPostCode") & ""
	invCountry = rsInv("InvCountry") & ""
		
	If Len(delAddress) > 0 Then
		delAddress = Replace(delAddress,CHR(10),"<br/>")
		delAddress = Replace(delAddress,CHR(13),"<br/>")
		delAddress = Replace(delAddress,vbCrLf,"<br/>")
		delAddress = Replace(delAddress,"<br/><br/>","<br/>")
		delAddress = delCompany & "<br/>" & delAddress
	End If

	If Len(invAddress) > 0 Then
		invAddress = Replace(invAddress,CHR(10),"<br/>")
		invAddress = Replace(invAddress,CHR(13),"<br/>")
		invAddress = Replace(invAddress,vbCrLf,"<br/>")
		invAddress = Replace(invAddress,"<br/><br/>","<br/>")
		invAddress = invCompany & "<br/>" & invAddress
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
			function EditInvoice() {
				//	try {
					if(window.opener && !window.opener.closed) {
//					    alert(window.opener.document.parentWindow.name);
					    if(window.opener.document.parentWindow.name == 'MyIFrame') {
					        window.opener.document.parentWindow.RedirectPage_Global('Transporter_EditInvoice.asp?Parent=True&InvoiceId=<%= lngInvoiceId %>');		
					    } else {
					    window.opener.document.parentWindow.MainFrame.RedirectPage_Global('Transporter_EditInvoice.asp?Parent=False&InvoiceId=<%= lngInvoiceId %>');
						}
						setTimeout("window.close()",1500);
					}
			//	} catch(error) {
			//		alert('Access denied. Close this window, open and try again.');
				//}
			}
			var invAddress;
			var delAddress;
			function invoiceAddress() {
				var s;
				var sState;
				var t1;
				t1 = "<%= Replace(Replace(invAddress,CHR(10),""),CHR(13),"") %>";
				if(t1.length > 0) {
					s = '<%= invAddress %>'
					return s;
				}
				if('<%= Replace(delAddress1,"'","''") %>' != 'To be advised') {
				    s = '<%= Replace(invCompany,"'","''") %>' + '<br>';
				    sState = '<%= Replace(invState,"'","''") %>';
					if (sState == 'Other' || sState == 'Other,') { sState = '' }
				    s += '<%= Replace(invAddress1,"'","''") %>' + '<br>';
				    if(<%= Len(Replace(invAddress2,"'","''")) %>>0){
					    s += '<%= Replace(invAddress2,"'","''") %>' + '<br>';
				    }
				    s += '<%= Replace(invSuburb,"'","''") %>' + ' ' + sState + '<br>' + '<%= Replace(invCountry,"'","''") %>' + ' ' + '<%= invPostCode %>' + '\n';
				} else {
				    s = 'To be advised';
				}
				//InvoiceAddress_Select_InJobOrder(s, blah blah blah');
				//window.close();
				invAddress = s;
				return s;
			}
			invoiceAddress();
			
			function deliveryAddress() {
				var s;
				var sState;
				var t1;
				t1 = "<%= Replace(Replace(delAddress,CHR(10),""),CHR(13),"") %>";
				if(t1.length > 0) {
					s = '<%= delAddress %>'
					return s;
				}
				if('<%= Replace(delAddress1,"'","''") %>' != 'To be advised') {
				    s = '<%= Replace(delCompany,"'","''") %>' + '<br>';
				    sState = '<%= Replace(delState,"'","''") %>';
					if (sState == 'Other' || sState == 'Other,') { sState = '' }
				    s += '<%= Replace(delAddress1,"'","''") %>' + '<br>';
				    if(<%= Len(Replace(delAddress2,"'","''")) %>>0){
					    s += '<%= Replace(delAddress2,"'","''") %>' + '<br>';
				    }
				    s += '<%= Replace(delSuburb,"'","''") %>' + ' ' + sState + '<br>' + '<%= Replace(delCountry,"'","''") %>' + ' ' + '<%= delPostCode %>' + '\n';
				} else {
				    s = 'To be advised';
				}
				//deliveryAddress_Select_InJobOrder(s, blah blah blah');
				//window.close();
				delAddress = s;
				return s;
			}
			deliveryAddress();
		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2 class="page">
<%

	If Not boolEmail Then
				dblCurrencyRate = 1

%>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<!--#include file="NavBar.asp"-->
				</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Invoice</td>
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
				<td valign="top"><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""><br><br></td>
				<td valign="top" align="right">
<%
	If rsDi("DivisionId") = 6 Then
%>
					<table cellpadding=3 cellspacing=0 border=0>
						<tr>
							<td colspan=2>20 B Theban Place<br>Totaravale<br>New Zealand<br><br>GST Number: 94-021-308</td>
						</tr>
					</table>

<%
	Else
%>
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
<%
	End If
%>
				</td>
			</tr>
			<tr height=10>
				<td></td>
			</tr>
			<tr>
				<td colspan=2>
				<span style="font-size:24px;">TAX INVOICE</span>
				</td>
			</tr>
			<tr>
				<td colspan=2><br></td>
			</tr>
			<tr>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table11">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Date:</td>
							<td><%= FormatDateU(rsInv("InvoiceDate"), False) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Invoice #:</td>
							<td><%= rsInv("InvoiceId") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Quote #:</td>
							<td><%= rsInv("Qid") %></td>
						</tr>
					</table>
				</td>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table8">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Customer:</td>
							<td valign="top"><%= rsInv("InvCompany") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Order #:</td>
							<td valign="top"><%= rsInv("CustomerPO") %></td>
						</tr>
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
							<td style="font-weight:bold;vertical-align:top;width:60px;">Invoice To:</td>
							<td width=200>
								<script language="javascript">
									document.write(invoiceAddress());
/*									var address = invoiceAddress();
									if(address == "InvAddress") {
										document.write("<%= rsInv("InvCompany") %>");
										document.write("<%= rsInv("InvAddress") %>");
									}
*/
								</script>
							</td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Deliver To:</td>
							<td width=200>
								<table width="100%" cellpadding=0 cellspacing=0 border=0>
									<tr>
										<td nowrap>
											<script language="javascript">
												document.write(deliveryAddress());
/*												var address = deliveryAddress();
												if(address == "DelAddress") {
													document.write("<%= rsInv("DelCompany") %>");
													document.write("<%= rsInv("DelAddress") %>");
												}*/
											</script>
										</td>
									</tr>
<%
If rsInv("Attention") <> "" Then
%>
									<tr>
										<td>
											<table cellpadding=0 cellspacing=0 border=0>
												<tr>
													<td style="width:60px;font-weight:bold;vertical-align:top;">Attention:</td>
													<td><%= rsInv("Attention") %></td>
												</tr>
											</table>
										</td>
									</tr>
<%
End If
%>
								</table>
							</td>
						</tr>
					</table>
				</td>
				<td valign="top" width="50%">
					<table align="center" width="100%" border="0" borderwidth=1 bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table5">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">From:</td>
							<td>Bert Beijnon<!--<%= rsInv("Name") %>--></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Email:</td>
							<td>bertb@techlight.com.au<!--<%= rsInv("Email") %>--></td>
						</tr>
<%
'If rsInv("Phone") <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Phone:</td>
							<td>+61(0) 418 736454<!--<%= rsInv("Phone") %>--></td>
						</tr>
<%
'End If
If rsInv("Mobile") <> "" Then
%>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:60px;">Mobile:</td>
							<td><%= rsInv("Mobile") %></td>
						</tr>

<%
End If
%>
					</table>
				</td>
			</tr>
		</table>
		<br><br><br>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table7">
			<tr>
				<td valign="top" width=40 nowrap style="border-bottom:2px solid black;font-weight:bold;font-size:12px;width:40px;">Item</td>
				<td valign="top" nowrap style="text-align:center;border-bottom:2px solid black;font-weight:bold;font-size:12px;width:35px;">Ord.</td>
				<td valign="top" nowrap style="text-align:center;border-bottom:2px solid black;font-weight:bold;font-size:12px;width:35px;">Supp.</td>
				<td valign="top" style="border-bottom:2px solid black;font-weight:bold;font-size:12px;">Description</td>
				<td valign="top" width=100 style="border-bottom:2px solid black;font-weight:bold;font-size:12px;text-align:right;">Nett Price</td>
				<td valign="top" width=100 style="border-bottom:2px solid black;font-weight:bold;font-size:12px;text-align:right;">Ext. Nett Price</td>
			</tr>
		</table>
<%
	' Get Items

	Set rsQTPi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From InvoiceContents Where InvoiceId = " & lngInvoiceId
	Set rsQTPi = dbConn.Execute(sql)

dblCurrencyRate = 1

	Dim i
	Dim decRunningNettPriceTotal
	i = 1
	decRunningNettPriceTotal = 0
	If Not(rsQTPi.BOF And rsQTPi.EOF) Then
		Do Until rsQTPi.EOF
			decRunningNettPriceTotal = decRunningNettPriceTotal + rsQTPi("ExtNettPrice")
%>
		<div style="position:relative;display:block;page-break-after:auto;" id="ITDiv<%= i %>">
		<table align="center" width=595 cellpadding=0 cellspacing=0 border=0 id="ITTable<%= i %>">
			<tr>
				<td valign="top" style="border-bottom:1px solid black;width:40px;" width=40><%= i %></td>
				<td valign="top" style="text-align:center;border-bottom:1px solid black;width:35px;" nowrap><% If CLng(rsQTPi("Ordered")) > 0 Then Response.Write(rsQTPi("Ordered")) Else Response.Write(0) %></td>
				<td valign="top" style="text-align:center;border-bottom:1px solid black;width:35px;" nowrap><% If CLng(rsQTPi("Quantity")) = 0 And (CLng(rsQTPi("Days")) <> 0 And CLng(rsQTPi("Units")) <> 0) Then Response.Write(CLng(rsQTPi("Days")) & "&nbsp;days<br>" & CLng(rsQTPi("Units")) & "&nbsp;units") Else Response.Write(CLng(rsQTPi("Quantity"))) %></td>
				<td valign="top" style="border-bottom:1px solid black;"><% If rsQTPi("ProductCode") <> "" Then Response.Write("<b>" & rsQTPi("ProductCode") & "</b><br>") %><%= rsQTPi("Description") %></td>
				<td valign="top" style="border-bottom:1px solid black;width:100px;text-align:right;"><% If Not IsNull(rsQTPi("NettPrice")) Then Response.Write("$" & FormatNumber(dblCurrencyRate*rsQTPi("NettPrice")+0,2)) %></td>
				<td valign="top" style="border-bottom:1px solid black;width:100px;text-align:right;"><%= "$" & FormatNumber(dblCurrencyRate*rsQTPi("ExtNettPrice")+0,2) %></td>
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

%>
		</table>
		<div id="FinalTable" style="position:relative;">
		<table align="center" width="595" cellpadding="5" cellspacing="0">
			<tr>
				<td>
					<br>
					<table border="0" cellpadding="5" cellspacing="0" ID="Table9">
<%
	If rsInv("Terms") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Terms:</td>
							<td valign="top" width=540><%= Replace(rsInv("Terms"),CHR(10),"<BR>") %></td>
						</tr>
<%
	End If
	If rsInv("CustomerNotes") <> "" Then
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Notes:</td>
							<td valign="top" width=540><%= Replace(rsInv("CN"),CHR(10),"<BR>") %></td>
						</tr>
<%
	End If
%>
						<tr>
							<td valign="top" style="font-weight:bold;width:150px;">Direct Debit Details:</td>
							<td valign="top" width=540>
TECHLIGHT PTY LTD<br />
ACC #149205 BSB#034 064<br />
WESTPAC BANKING CORPORATION<br />
1374 GYMPIE ROAD, ASPLEY, BRISBANE, AUSTRALIA. 4034
							</td>
						</tr>
					</table>
				</td>
				<td colspan=4 align="right" valign="top">
					<br>

<%
    strCurrencyName = "Australia Dollars"
			dblGSTPercentage = 10
			dblCurrencyRate = 1
			strCurrencyPrefix = "$"
	If Not(boolExport) Then
    'If ((Not(boolExport) And Not(lngDivisionId = 6) And strCurrencyName = "Australia Dollars") Or (strCurrencyName = "New Zealand Dollars" And lngDivisionId = 6)) Then
		If strCurrencyName = "Australia Dollars" Then
			dblGSTPercentage = 10
		ElseIf lngDivisionId = 6 Then
			dblGSTPercentage = 12.5
		End If
%>
					<table cellpadding=5 cellspacing=0 border=0 ID="Table6">
						<tr>
							<td nowrap style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Ex. GST</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$" %><%= FormatNumber(dblCurrencyRate*decRunningNettPriceTotal,2) %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td nowrap style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">GST</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$" %><%= FormatNumber(dblCurrencyRate*(decRunningNettPriceTotal*dblGSTPercentage/100),2) %></td>
						</tr>
						<tr>
							<td><br></td>
						</tr>
						<tr>
							<td nowrap style="border-top:2px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total Inc. GST</td>
							<td align="right" style="border-top:2px solid black;border-bottom:2px solid black;"><%= "$" %><%= FormatNumber(dblCurrencyRate*decRunningNettPriceTotal*(1+(dblGSTPercentage/100)),2) %></td>
						</tr>
					</table>
<%
	Else
%>
					<table cellpadding=5 cellspacing=0 border=0 ID="Table12">
						<tr>
							<td nowrap style="border-top:1px solid black;border-bottom:2px solid black;font-weight:bold;text-align:right;">Total</td>
							<td align="right" style="border-top:1px solid black;border-bottom:2px solid black;"><%= "$" %><%= FormatNumber(dblCurrencyRate*decRunningNettPriceTotal,2) %></td>
						</tr>
					</table>
<%
	End If
%>

				</td>
			</tr>
			<tr>
				<td colspan=4><br><%= rsDi("InvoiceTerms") %></td>
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
			<input type="hidden" name="InvoiceId" id="InvoiceId">
		</form>
		<script language="javascript">

		</script>
<%
	End If
	If Not boolEmail AND Request.Cookies("ClientSettings")("HasInternalNotes") = "true" Then
		If Not(rsInv("InternalNotes")&""="" Or rsInv("InternalNotes") = Null) Then
%>
		<table align="center" width="595" border="0" cellpadding="5" cellspacing="0" class="NoPrint" ID="Table15">
			<tr>
				<td width=50%>
				<b>Internal Notes</b><br>
				<%= Replace(rsInv("InternalNotes"), Chr(10),"<br>") %>
				</td>
			</tr>
		</table>
<%
		End If
	End If
End If
%>


<%
	If Not boolEmail And Not boolPrint Then
		If Request("Msg") <> "" Then

%>
		<script language="javascript">
			alert('<%= Request("Msg") %>');
		</script>
<%
		End If
	End If
	
%>


	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->