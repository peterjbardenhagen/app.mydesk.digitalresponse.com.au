<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg

strMsg = Trim(Request("Msg"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<!DOCTYPE html>
<html lang="en">
	<head>
		<meta charset="UTF-8">
		<meta name="viewport" content="width=device-width, initial-scale=1.0">
		<title>Part Codes - Techlight MyDesk</title>
		<link rel="preconnect" href="https://fonts.googleapis.com">
		<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
		<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="/Clients/SalesEngineTL/Dashboard.asp">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="/Clients/SalesEngineTL/Setup/">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Part Codes</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Part Codes</h1>
			<a href="Add.asp" class="tl-btn tl-btn-primary">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
				Add Part Code
			</a>
		</div>

		<div class="tl-main">
<%
If strMsg <> "" Then
%>
						<div class="tl-alert tl-alert-info" style="margin-bottom: 20px;">
							<%= strMsg %>
						</div>
<%
End If

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From PartCodes Inner Join Divisions On Divisions.DivisionId = PartCodes.DivisionId Order By Division, PartCode"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
%>
						<div class="tl-card">
							<table class="tl-data-table">
								<thead>
									<tr>
										<th width="150">Part Code</th>
										<th>Division</th>
										<th width="120" style="text-align: right;">Action</th>
									</tr>
								</thead>
								<tbody>
<%
	Do Until rs.EOF
%>
								<tr>
									<td><strong><%= rs("PartCode") %></strong></td>
									<td><%= rs("Division") %></td>
									<td style="text-align: right;">
										<div class="tl-btn-group">
											<a href="Edit.asp?PartCodeId=<%= rs("PartCodeId") %>" class="tl-btn-icon" title="Edit">
												<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path></svg>
											</a>
											<a href="Del_Proc.asp?PartCodeId=<%= rs("PartCodeId") %>" class="tl-btn-icon tl-btn-icon-danger" title="Delete" onclick="return confirm('Are you sure you want to delete this part code?');">
												<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
											</a>
										</div>
									</td>
								</tr>
<%
		rs.MoveNext
	Loop
%>
								</tbody>
							</table>
						</div>
<%
Else
%>
						<div class="tl-empty-state">
							<p>There are no Part Codes</p>
						</div>
<%
End If
%>
		</div>
	</div>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
