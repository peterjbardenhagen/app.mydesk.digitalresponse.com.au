<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

SetWorkingDir Request.ServerVariables("URL")

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

If boolEmail Then boolEmail = True

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

' Check for Despatch details
sql = "Select * From Despatch Where InvoiceId = " & lngInvoiceId
Set rs = dbConn.Execute(sql)

If (rs.BOF And rs.EOF) Then
	Response.Redirect("EnterDespatchDetails.asp?InvoiceId=" & lngInvoiceId)
End If

If boolPrint Then
	' Audit trail
	sql = "Insert Into InvoiceAudit (InvoiceId, Code, Action, DateEntered) Values (" & lngInvoiceId & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Delivery Note Printed', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
End If

Set rsInv = Server.CreateObject("ADODB.RecordSet")
sql = "Select Invoices.*, Invoices.CustomerNotes As CN, Invoices.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, InvoiceStatus.InvoiceStatus From ((Invoices INNER JOIN Users ON Invoices.Code = Users.Code) INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId) Where InvoiceId = " & lngInvoiceId
Set rsInv = dbConn.Execute(sql)

If Not(rsInv.BOF And rsInv.EOF) Then
	intInvoiceStatusId = rsInv("InvoiceStatusId")

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

	Select Case LCase(rsInv("InvCountry")&"")
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
				<!--#include file="NavBarDeliveryNote.asp"-->
				</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Delivery Note</td>
			</tr>
		</table>
		<br class="NoPrint">
<%

	End If

%>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table1">
			<tr>
				<td valign="top"><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""><br><br></td>
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
				<span style="font-size:24px;">DELIVERY NOTE</span>
				</td>
			</tr>
			<tr>
				<td colspan=2><br></td>
			</tr>
			<tr>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table11">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;">Despatch Date:</td>
							<td><%= FormatDateU(rs("DespatchDate"), False) %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;">Invoice #:</td>
							<td><%= rsInv("InvoiceId") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;">Quote #:</td>
							<td><%= rsInv("Qid") %></td>
						</tr>
					</table>
				</td>
				<td class="Times2Bold" valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table8">
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;">Customer:</td>
							<td valign="top"><%= rsInv("InvCompany") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;" nowrap>Customer Order #:</td>
							<td valign="top"><%= rsInv("CustomerPO") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;">Carrier:</td>
							<td valign="top"><%= rs("Carrier") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;">Carrier Ref.:</td>
							<td valign="top"><%= rs("CarrierRef") %></td>
						</tr>
						<tr>
							<td style="font-weight:bold;vertical-align:top;width:105px;">Package Details:</td>
							<td valign="top"><%= rs("PackageDetails") %></td>
						</tr>
					</table>
				</td>
			</tr>
			<tr>
				<td><br></td>
			</tr>
			<tr>
				<td valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table5">
						<tr>
							<td>
							<b>DELIVER TO</b><br>
							<%= Replace(rsInv("InvCompany"),"'","''") %><br/>
							<%= Replace(Replace(rsInv("DelAddress"),"'","''"),CHR(10),"<br/>") %>				
							</td>
						</tr>
<%
If rsInv("Attention") <> "" Then
%>
						<tr>
							<td><b>Attention:</b> <%= rsInv("Attention") %></td>
						</tr>
<%
End If
%>
					</table>
				</td>
				<td valign="top" width="50%">
					<table align="center" width="100%" border="0" bordercolor="#00000" cellpadding="3" cellspacing="0" ID="Table4">
						<tr>
							<td>
							<b>INVOICED TO</b><br>
							<%= Replace(rsInv("InvCompany"),"'","''") %><br/>
							<%= Replace(Replace(rsInv("InvAddress"),"'","''"),CHR(10),"<br/>") %>				
							</td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
		<br><br><br>
		<table align="center" width="595" border="0" cellpadding="0" cellspacing="0" ID="Table7">
			<tr>
				<td valign="top" width=40 nowrap style="border-bottom:2px solid black;font-weight:bold;font-size:12px;width:40px;">Item</td>
				<td valign="top" style="border-bottom:2px solid black;font-weight:bold;font-size:12px;">Description</td>
				<td valign="top" nowrap style="text-align:center;border-bottom:2px solid black;font-weight:bold;font-size:12px;width:35px;">Ord.</td>
				<td valign="top" nowrap style="text-align:center;border-bottom:2px solid black;font-weight:bold;font-size:12px;width:35px;">Supp.</td>
				<td valign="top" nowrap style="text-align:center;border-bottom:2px solid black;font-weight:bold;font-size:12px;width:35px;">Back</td>
			</tr>
		</table>
<%
	' Get Items

	Set rsQTPi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From InvoiceContents Where InvoiceId = " & lngInvoiceId
	Set rsQTPi = dbConn.Execute(sql)

	Dim i
	i = 1
	If Not(rsQTPi.BOF And rsQTPi.EOF) Then
		Do Until rsQTPi.EOF
%>
		<div style="position:relative;display:block;page-break-after:auto;" id="ITDiv<%= i %>">
		<table align="center" width=595 cellpadding=0 cellspacing=0 border=0 id="ITTable<%= i %>">
			<tr>
				<td valign="top" style="border-bottom:1px solid black;width:40px;" width=40><%= i %></td>
				<td valign="top" style="border-bottom:1px solid black;"><% If rsQTPi("ProductCode") <> "" Then Response.Write("<b>" & rsQTPi("ProductCode") & "</b><br>") %><%= rsQTPi("Description") %></td>
				<td valign="top" style="text-align:center;border-bottom:1px solid black;width:35px;" nowrap><% If CLng(rsQTPi("Ordered")) > 0 Then Response.Write(rsQTPi("Ordered")) Else Response.Write(0) %></td>
				<td valign="top" style="text-align:center;border-bottom:1px solid black;width:35px;" nowrap><% If CLng(rsQTPi("Quantity")) = 0 And (CLng(rsQTPi("Days")) <> 0 And CLng(rsQTPi("Units")) <> 0) Then Response.Write(CLng(rsQTPi("Days")) & "&nbsp;days<br>" & CLng(rsQTPi("Units")) & "&nbsp;units") Else Response.Write(CLng(rsQTPi("Quantity"))) %></td>
				<td valign="top" style="text-align:center;border-bottom:1px solid black;width:35px;" nowrap><% If CLng(rsQTPi("Ordered")) > 0 Then Response.Write(rsQTPi("BackOrder")) Else Response.Write(0) %></td>
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
%>
					</table>
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
	</body>
</html>
<%
rs.Close
Set rs = Nothing
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->