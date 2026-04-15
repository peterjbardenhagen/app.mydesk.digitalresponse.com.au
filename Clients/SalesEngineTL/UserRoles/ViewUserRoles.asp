<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td bgcolor="#DDDDDD"><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
			<tr>
				<td>
					<table cellpadding=5 cellspacing=0 border=0 ID="Table1">
						<tr>
							<td valign="top" style="font-weight:bold;font-size:16px;">User Roles<br></td>
						</tr>
					</table>
					<table cellpadding=2>
						<tr>
							<td style="font-weight:bold;">User Role</td>
							<td style="font-weight:bold;text-align:right;">PO Approval Limit</td>
							<td style="font-weight:bold;text-align:right;">PO Cap Ex Approval Limit</td>
							<td style="font-weight:bold;text-align:right;">Quote Approval Limit</td>
						</tr>
						<tr>
							<td colspan=4><hr></td>
						</tr>
<%

Dim sql
Dim rs
sql = "Select * From UserRoles Order By UserRole"
Set rs = dbConn.Execute(sql)

Do Until rs.EOF

%>
						<tr>
							<td><%= rs("UserRole") %></td>
							<td style="text-align:right;"><%= FormatCurrency(rs("POApprovalLimit"),2) %></td>
							<td style="text-align:right;"><%= FormatCurrency(rs("POCapExApprovalLimit"),2) %></td>
							<td style="text-align:right;"><%= FormatCurrency(rs("QuoteApprovalLimit"),2) %></td>
						</tr>
<%

	rs.MoveNext
Loop

%>				
				</td>
			</tr>
		</table>
		<br>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
