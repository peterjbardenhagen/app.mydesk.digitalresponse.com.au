<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

SetWorkingDir Request.ServerVariables("URL")

Dim lngPOid

lngPOid = CLng(Request("POid"))

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT [PO].*, PO.IntroText AS IT, PO.InternalNotes AS [IN], [Users].*, PurchaseOrderStatus.POStatus, PurchaseOrderPaymentTypes.POPaymentType FROM ((Users INNER JOIN (PurchaseOrderStatus INNER JOIN PurchaseOrders AS PO ON PurchaseOrderStatus.POStatusId = PO.POStatusId) ON Users.Code = PO.Code) INNER JOIN PurchaseOrderPaymentTypes ON PO.POPaymentTypeId = PurchaseOrderPaymentTypes.POPaymentTypeId) WHERE PO.POid = " & lngPOid
Set rsPO = dbConn.Execute(sql)

If Not(rsPO.BOF and rsPO.EOF) Then
	Set rsCon = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Contacts_WithCustomersAndSuppliers Where ContactId = " & rsPO("ContactId")
	Set rsCon = dbConn.Execute(sql)

	Set rsDi = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Divisions Where DivisionId = " & rsPO("DivisionId")
	Set rsDi = dbConn.Execute(sql)

	strLogo = rsDi("Logo")
	boolRequest = rsDi("PurchaseRequests")
	
	rsDi.Close
	Set rsDi = Nothing

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
		If boolRequest Then
%>
		<!--#include file="NavBar_Requests.asp"-->
<%
		Else
%>
		<!--#include file="NavBar.asp"-->
<%
		End If
%>
		<br class="NoPrint">
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Purchase Order History</td>
			</tr>
		</table>
		<br class="NoPrint">
<%

	End If

%>
		<table width=700 cellpadding=5 cellspacing=0 border=0>
<%

	If rsPO("RFQId") > 0 Then

%>
			<tr>
				<td valign="top" colspan=3 style="font-weight:bold;">Request For Quotes From</td>
			</tr>
<%

		sql = "Select * From RFQ Inner Join Contacts_WithCustomersAndSuppliers_V2 On RFQ.ContactId = Contacts_WithCustomersAndSuppliers_V2.ContactId Where RFQid = " & rsPO("RFQId")
		Set rsRFQ = dbConn.Execute(sql)
		
		If Not(rsRFQ.BOF And rsRFQ.EOF) Then
			Do Until rsRFQ.EOF

%>
			<tr>
				<td valign="top"><%= rsRFQ("CompanyName") %></td>
			</tr>
<%

				rsRFQ.MoveNext
			Loop
		End If
		
		rsRFQ.Close
		Set rsRFQ = Nothing

%>
			<tr>
				<td valign="top"><br></td>
			</tr>
<%

	End If

	sql = "Select * From PurchaseOrderAudit Inner Join Users On Users.Code = PurchaseOrderAudit.Code Where POid = " & lngPOid & " Order By PurchaseOrderAudit.DateEntered Desc"
	Set rsAud = dbConn.Execute(sql)
	
	If Not (rsAud.BOF And rsAud.EOF) Then
%>
			<tr>
				<td valign="top" colspan=3 style="font-weight:bold;">Audit Trail</td>
			</tr>
			<tr>
				<td valign="top" width=150 style="font-weight:bold;">By Whom</td>
				<td valign="top" style="font-weight:bold;">Action</td>
				<td valign="top" width=250 style="font-weight:bold;">Date Entered</td>
			</tr>
<%
		Do Until rsAud.EOF
%>
			<tr>
				<td valign="top" width=150><%= rsAud("Name") %></td>
				<td valign="top"><%= rsAud("Action") %></td>
				<td valign="top" width=250><%= FormatDateTime(rsAud("DateEntered"),1) %>&nbsp;<%= FormatDateTime(rsAud("DateEntered"),3) %></td>
			</tr>
<%
			rsAud.MoveNext
		Loop
	End If
	rsAud.Close
	Set rsAud = Nothing

%>
			<tr>
				<td valign="top" colspan=3><br></td>
			</tr>
<%

	sql = "Select * From Comments Inner Join Users On Users.Code = Comments.FromCode Where ItemId = " & lngPOid & " And TableId = 8 Order By Comments.DateEntered Desc"
	Set rsComments = dbConn.Execute(sql)
	
	If Not (rsComments.BOF And rsComments.EOF) Then
%>
			<tr>
				<td valign="top" colspan=3 style="font-weight:bold;">Comments</td>
			</tr>
			<tr>
				<td valign="top" width=150 style="font-weight:bold;">By Whom</td>
				<td valign="top" style="font-weight:bold;">Comment</td>
				<td valign="top" width=250 style="font-weight:bold;">Date Entered</td>
			</tr>
<%
		Do Until rsComments.EOF
%>
			<tr>
				<td valign="top" width=150><%= rsComments("Name") %></td>
				<td valign="top"><%= rsComments("Comment") %></td>
				<td valign="top" width=250><%= FormatDateTime(rsComments("DateEntered"),1) %>&nbsp;<%= FormatDateTime(rsComments("DateEntered"),3) %></td>
			</tr>
<%
			rsComments.MoveNext
		Loop
	End If
	rsComments.Close
	Set rsComments = Nothing
%>			
			</tr>
		</table>
	</body>
</html>
<%
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->