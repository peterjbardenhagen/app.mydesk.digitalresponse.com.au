<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngCallReportId
lngCallReportId = CLng(Request("CallReportId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT CallReports.*, CallReportTypes.*, Contacts.FirstName + ' ' + Contacts.Surname AS [ContactName], Users.Name AS [StaffMember] FROM (Users INNER JOIN (Contacts INNER JOIN CallReports ON Contacts.ContactId = CallReports.ContactId) ON Users.Code = CallReports.Code) INNER JOIN CallReportTypes ON CallReports.CallReportTypeId = CallReportTypes.CallReportTypeId WHERE CallReportId = " & lngCallReportId
Set rs = dbConn.Execute(sql)

%>

<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
		</table>
		<br>
		<span class="TimesHeader">Call Report</span><br>
		<br>
		<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table5">
			<tr>
				<td valign="top" style="font-weight:bold;" nowrap>User</td>
				<td valign="top"><%= rs("StaffMember") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" nowrap width=120>Real Date Entered</td>
				<td valign="top"><%= FormatDateU(rs("RealDateEntered"), False) %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" nowrap>Date of Call</td>
				<td valign="top"><%= FormatDateU(rs("DateEntered"), False) %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" nowrap>Call Report Type</td>
				<td valign="top"><%= rs("CallReportType") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" nowrap>Contact</td>
				<td valign="top"><%= rs("ContactName") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" nowrap>Call Purpose</td>
				<td valign="top"><%= Replace(rs("CallPurpose"), Chr(10), "<br>") %></td>
			</tr>
			<tr>
				<td valign="top" style="font-weight:bold;" nowrap>Comment</td>
				<td valign="top"><%= Replace(rs("Comment"), Chr(10), "<br>") %></td>
			</tr>
		</table>
<%
rs.Close
Set rs = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->