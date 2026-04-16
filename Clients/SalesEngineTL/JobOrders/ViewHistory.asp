<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

SetWorkingDir Request.ServerVariables("URL")

Dim lngQid
lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, Quotes.CustomerNotes As CN, Quotes.DivisionId As QDivisionId, Users.*, QuoteCOS.QuoteCOSFile, QuoteStatus.QuoteStatus From ((Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) LEFT OUTER JOIN QuoteCOS ON Quotes.QuoteCOSId = QuoteCOS.QuoteCOSId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(sql)

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
		</style>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<!--#include file="NavBar.asp"-->
				</td>
			</tr>
		</table>
		<br>
		<table class="NoPrint" ID="Table13">
			<tr>
				<td class="Header4">View Quote History</td>
			</tr>
		</table>
		<br class="NoPrint">
		<table width=700 cellpadding=5 cellspacing=0 border=0>
<%
sql = "Select * From QuoteAudit Inner Join Users On Users.Code = QuoteAudit.Code Where Qid = " & lngQid & " Order By QuoteAudit.DateEntered Desc"
Set rsQua = dbConn.Execute(sql)

If Not (rsQua.BOF And rsQua.EOF) Then
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
	Do Until rsQua.EOF
%>
			<tr>
				<td valign="top" width=150><%= rsQua("Name") %></td>
				<td valign="top"><%= rsQua("Action") %></td>
				<td valign="top" width=250><%= FormatDateTime(rsQua("DateEntered"),1) %>&nbsp;<%= FormatDateTime(rsQua("DateEntered"),3) %></td>
			</tr>
<%
		rsQua.MoveNext
	Loop
End If
rsQua.Close
Set rsQua = Nothing
%>
			<tr>
				<td valign="top" colspan=3><br></td>
			</tr>
<%
sql = "Select * From Comments Inner Join Users On Users.Code = Comments.FromCode Where ItemId = " & lngQid & " And TableId = 6 Order By Comments.DateEntered Desc"
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