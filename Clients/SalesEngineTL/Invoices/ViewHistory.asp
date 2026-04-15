<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

SetWorkingDir Request.ServerVariables("URL")

Dim lngInvoiceId
Dim intInvoiceStatusId

lngInvoiceId = CLng(Request("InvoiceId"))
intInvoiceStatusId = CInt(Request("InvoiceStatusId"))
boolViewDeliveryNote = False

If Request("ViewDeliveryNote") <> "" Then boolViewDeliveryNote = True

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsInv = Server.CreateObject("ADODB.RecordSet")
sql = "Select Invoices.*, Invoices.CustomerNotes As CN, Invoices.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, InvoiceStatus.InvoiceStatus From ((Invoices INNER JOIN Users ON Invoices.Code = Users.Code) INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId) Where InvoiceId = " & lngInvoiceId
Set rsInv = dbConn.Execute(sql)

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
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
<%
If boolViewDeliveryNote Then
%>
				<!--#include file="NavBarDeliveryNote.asp"-->
<%
Else
%>
				<!--#include file="NavBar.asp"-->
<%
End If
%>
				</td>
			</tr>
		</table>
		<br>
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Invoice History</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=700 cellpadding=5 cellspacing=0 border=0>
<%
sql = "Select * From InvoiceAudit Inner Join Users On Users.Code = InvoiceAudit.Code Where InvoiceId = " & lngInvoiceId & " Order By InvoiceAudit.DateEntered Desc"
Set rsInva = dbConn.Execute(sql)

If Not (rsInva.BOF And rsInva.EOF) Then
%>
			<tr>
				<td valign="top" colspan=3 style="font-weight:bold;">Audit Trail</td>
			</tr>
			<tr>
				<td valign="top" width=150 style="font-weight:bold;">By Whom</td>
				<td valign="top" style="font-weight:bold;">Action</td>
				<td valign="top" style="font-weight:bold;" nowrap>Date Entered</td>
			</tr>
<%
	Do Until rsInva.EOF
%>
			<tr>
				<td valign="top" width=150><%= rsInva("Name") %></td>
				<td valign="top"><%= rsInva("Action") %></td>
				<td valign="top" nowrap><%= FormatDateTime(rsInva("DateEntered"),1) %>&nbsp;<%= FormatDateTime(rsInva("DateEntered"),3) %></td>
			</tr>
<%
		rsInva.MoveNext
	Loop
End If
rsInva.Close
Set rsInva = Nothing
%>
			<tr>
				<td valign="top" colspan=3><br></td>
			</tr>
<%
sql = "Select * From Comments Inner Join Users On Users.Code = Comments.FromCode Where ItemId = " & lngInvoiceId & " And TableId = 6 Order By Comments.DateEntered Desc"
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
<!--#include virtual="/System/ssi_dbConn_close.inc"-->