<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim intTMailId
intTMailId = CLng(Request("TMailId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT TMail.*, Users.Name As [To], Users_1.Name As [From] FROM (TMail INNER JOIN Users ON TMail.FromCode = Users.Code) INNER JOIN Users AS Users_1 ON TMail.ToCode = Users_1.Code WHERE TMail.TMailId = " & intTMailId
Set rs = dbConn.Execute(sql)

If Request.Cookies("UserSettings")("Code") = rs("ToCode") Then
	sql = "UPDATE TMail Set Read = 1 WHERE TMailId = " & intTMailId
	dbConn.Execute(sql)
End If

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
		<span class="TimesHeader">T-Mail Message</span><br>
		<br>
		<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table5">
			<tr>
				<td width=50 style="font-weight:bold;">To:</td>
				<td><%= rs("To") %></td>
			</tr>
			<tr>
				<td width=50 style="font-weight:bold;">From:</td>
				<td><%= rs("From") %></td>
			</tr>
			<tr>
				<td width=50 style="font-weight:bold;">Date:</td>
				<td><%= FormatDateU(rs("Date"), False) %></td>
			</tr>
		</table>
		<br>
		<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td>
				<b>Subject</b><br>
				<%= rs("Subject") %>
				</td>
			</tr>
			<tr>
				<td><br></td>
			</tr>
			<tr>
				<td>
				<b>Message</b><br>
				<%= Replace(rs("Message"), Chr(10), "<br>") %>
				</td>
			</tr>
		</table>
<%
rs.Close
Set rs = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->