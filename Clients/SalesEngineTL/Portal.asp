<%
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

Dim strWorkingDir
strWorkingDir = Session("WorkingDir")
%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Techlight MyDesk - Dashboard</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
	<script language="JavaScript" src="/System/Global.js"></script>
</head>
<body>

<!--#include virtual="/System/ssi_Header.inc"-->

<main class="tl-main">

	<!-- Welcome Section -->
	<section class="tl-page-header">
		<h1 class="tl-page-title">Welcome, <%= Session("Name") %></h1>
		<p class="tl-page-subtitle">
			You have successfully logged into Techlight MyDesk. 
			<% If Session("Admin") Then %><span class="tl-status tl-status-issued" style="margin-left: 8px;">Administrator</span><% End If %>
		</p>
		
		<% If strMsg <> "" Then %>
		<div class="tl-alert tl-alert-info" style="margin-top: 16px;">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
				<circle cx="12" cy="12" r="10"></circle>
				<line x1="12" y1="16" x2="12" y2="12"></line>
				<line x1="12" y1="8" x2="12.01" y2="8"></line>
			</svg>
			<%= strMsg %>
		</div>
		<% End If %>
	</section>

	<!-- Quick Actions Grid -->
	<section style="margin-bottom: 32px;">
		<h2 style="font-size: 1.125rem; font-weight: 600; color: var(--dark); margin-bottom: 16px;">Quick Actions</h2>
		<div class="tl-grid tl-grid-4">
			
			<!-- Contacts -->
			<a href="<%= strWorkingDir %>/Contacts/" class="tl-feature-card">
				<div class="tl-feature-icon">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
						<circle cx="9" cy="7" r="4"></circle>
						<path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
						<path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Contacts</h3>
					<p class="tl-feature-desc">Manage customer contacts and companies</p>
				</div>
			</a>
			
			<!-- Quotes -->
			<a href="<%= strWorkingDir %>/Quotes/" class="tl-feature-card">
				<div class="tl-feature-icon">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
						<polyline points="14 2 14 8 20 8"></polyline>
						<line x1="16" y1="13" x2="8" y2="13"></line>
						<line x1="16" y1="17" x2="8" y2="17"></line>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Quotes</h3>
					<p class="tl-feature-desc">Create and manage customer quotes</p>
				</div>
			</a>
			
			<!-- Invoices -->
			<a href="<%= strWorkingDir %>/Invoices/" class="tl-feature-card">
				<div class="tl-feature-icon">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
						<line x1="3" y1="9" x2="21" y2="9"></line>
						<line x1="9" y1="21" x2="9" y2="9"></line>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Invoices</h3>
					<p class="tl-feature-desc">Generate invoices and track payments</p>
				</div>
			</a>
			
			<!-- Purchasing -->
			<a href="<%= strWorkingDir %>/Purchasing/" class="tl-feature-card">
				<div class="tl-feature-icon">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
						<line x1="3" y1="6" x2="21" y2="6"></line>
						<path d="M16 10a4 4 0 0 1-8 0"></path>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Purchasing</h3>
					<p class="tl-feature-desc">Manage purchase orders and suppliers</p>
				</div>
			</a>
			
			<!-- Call Reports -->
			<a href="<%= strWorkingDir %>/CallReports/" class="tl-feature-card">
				<div class="tl-feature-icon" style="background: linear-gradient(135deg, #cca05a 0%, #b98745 100%);">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Call Reports</h3>
					<p class="tl-feature-desc">Log and track customer interactions</p>
				</div>
			</a>
			
			<!-- Jobs -->
			<a href="<%= strWorkingDir %>/Jobs/" class="tl-feature-card">
				<div class="tl-feature-icon" style="background: linear-gradient(135deg, #cca05a 0%, #b98745 100%);">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect>
						<path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Jobs</h3>
					<p class="tl-feature-desc">Track project jobs and tasks</p>
				</div>
			</a>
			
			<!-- Users (Admin Only) -->
			<% If Session("Admin") Then %>
			<a href="<%= strWorkingDir %>/Users/" class="tl-feature-card">
				<div class="tl-feature-icon" style="background: linear-gradient(135deg, #334155 0%, #1a2a3a 100%);">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
						<circle cx="12" cy="7" r="4"></circle>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Users</h3>
					<p class="tl-feature-desc">Manage system users and permissions</p>
				</div>
			</a>
			
			<!-- Setup -->
			<a href="<%= strWorkingDir %>/Setup/" class="tl-feature-card">
				<div class="tl-feature-icon" style="background: linear-gradient(135deg, #334155 0%, #1a2a3a 100%);">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<circle cx="12" cy="12" r="3"></circle>
						<path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
					</svg>
				</div>
				<div class="tl-feature-content">
					<h3 class="tl-feature-title">Setup</h3>
					<p class="tl-feature-desc">Configure system settings</p>
				</div>
			</a>
			<% End If %>
		</div>
	</section>

	<!-- Dashboard Widgets -->
	<div class="tl-grid tl-grid-2">
		
		<!-- Recent Activity -->
		<div class="tl-card">
			<div class="tl-card-header">
				<h3 class="tl-card-title">Quick Access</h3>
			</div>
			<div class="tl-card-body">
				<div style="display: flex; flex-direction: column; gap: 12px;">
					<a href="<%= strWorkingDir %>/CallReports/Add.asp" class="tl-btn tl-btn-secondary" style="justify-content: flex-start;">
						<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-right: 8px;">
							<path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"></path>
						</svg>
						Log a Call Report
					</a>
					<a href="<%= strWorkingDir %>/Quotes/Add.asp" class="tl-btn tl-btn-secondary" style="justify-content: flex-start;">
						<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-right: 8px;">
							<line x1="12" y1="5" x2="12" y2="19"></line>
							<line x1="5" y1="12" x2="19" y2="12"></line>
						</svg>
						Create New Quote
					</a>
					<a href="<%= strWorkingDir %>/Expenses/Add.asp" class="tl-btn tl-btn-secondary" style="justify-content: flex-start;">
						<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-right: 8px;">
							<line x1="12" y1="1" x2="12" y2="23"></line>
							<path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path>
						</svg>
						Submit Expense
					</a>
				</div>
			</div>
		</div>

		<!-- System Status -->
		<div class="tl-card">
			<div class="tl-card-header">
				<h3 class="tl-card-title">System Status</h3>
			</div>
			<div class="tl-card-body">
				<div style="display: flex; flex-direction: column; gap: 16px;">
					<div style="display: flex; align-items: center; justify-content: space-between; padding: 12px; background: var(--success-light); border-radius: var(--radius-md);">
						<div style="display: flex; align-items: center; gap: 12px;">
							<div style="width: 8px; height: 8px; background: var(--success); border-radius: 50%;"></div>
							<span style="font-weight: 500; color: #065f46;">System Online</span>
						</div>
						<span style="font-size: 0.75rem; color: var(--gray);">All services operational</span>
					</div>
					<div style="font-size: 0.8125rem; color: var(--gray);">
						<strong style="color: var(--dark);">Version:</strong> MyDesk 2026.04<br>
						<strong style="color: var(--dark);">Server Time:</strong> <%= FormatDateU(Now(), True) %>
					</div>
				</div>
			</div>
		</div>

	</div>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
<%

Dim rsTMail
Dim strSql
strSql = "SELECT TMail.*, Users.Name FROM TMail INNER JOIN Users ON Users.Code = TMail.FromCode WHERE ToCode = '" & Session("Code") & "' AND Read = 0 ORDER BY [Date] DESC"
Set rsTMail = dbConn.Execute(strSql)

If Not(rsTMail.BOF And rsTMail.EOF) Then
%>
				<br>
				<b>You have unread TMail</b><br><br>
				<table width=100% cellpadding=5 cellspacing=0 border=0>
					<tr>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Message</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=150>From</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=150>Action</td>
					</tr>
<%
	Do Until rsTMail.EOF

%>
					<tr>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= FormatDateU(rsTMail("Date"), False) %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
						<b><%= rsTMail("Subject") %></b><br>
						<%= rsTMail("Message") %>
						</td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsTMail("Name") %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><a href="TMail/Reply.asp?TMailId=<%= rsTMail("TMailId") %>">Reply</a> | <a href="TMail/MarkRead_Proc.asp?TMailId=<%= rsTMail("TMailId") %>">Mark Read</a> | <a href="TMail/Del_Proc.asp?TMailId=<%= rsTMail("TMailId") %>&Portal=True">Delete</a></td>
					</tr>
<%

		rsTMail.MoveNext
	Loop
%>
				</table>
<%
End If
rsTMail.Close
Set rsTMail = Nothing

Dim rsNotices
strSql = "SELECT Noticeboard.*, Users.Name FROM Noticeboard INNER JOIN Users ON Users.Code = Noticeboard.Code WHERE Noticeboard.DateExpires >= Now() AND DateDiff('d', Noticeboard.DateEntered, Now()) <= 14 ORDER BY [DateEntered] DESC"
Set rsNotices = dbConn.Execute(strSql)

If Not(rsNotices.BOF And rsNotices.EOF) Then
%>
				<br>
				<b>You have unread Notices</b><br><br>
				<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Message</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=150>From</td>
					</tr>
<%
	Do Until rsNotices.EOF

%>
					<tr>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= FormatDateU(rsNotices("DateEntered"), False) %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
						<b><%= rsNotices("Heading") %></b><br>
						<%= rsNotices("Message") %>
						</td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsNotices("Name") %></td>
					</tr>
<%

		rsNotices.MoveNext
	Loop
%>
				</table>
<%
End If
rsNotices.Close
Set rsNotices = Nothing

Dim rsFollowUps
strSql = "SELECT Comments.* FROM Comments WHERE FromCode = '" & Session("Code") & "' AND FollowUpComplete = 0 ORDER BY Comments.[FollowUpDate]"
Set rsFollowUps = dbConn.Execute(strSql)

If Not(rsFollowUps.BOF And rsFollowUps.EOF) Then
%>
				<br>
				<b>These items need to be followed up</b><br><br>
				<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table3">
					<tr>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date Entered</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Follow Up Date</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Comment</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Action</td>
					</tr>
<%
	Do Until rsFollowUps.EOF

%>
					<tr>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= FormatDateU(rsFollowUps("FollowUpDate"), False) %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><span style="color:<%
		If DateDiff("d", CDate(rsFollowUps("FollowUpDate")), Now()) >= 14 Then
			Response.Write("Red;Font-Weight:Bold;")
		ElseIf DateDiff("d", CDate(rsFollowUps("FollowUpDate")), Now()) <= 14 Then
			Response.Write("Red")
		Else
			Response.Write("Black")
		End If		
%>;"><%= FormatDateU(rsFollowUps("FollowUpDate"), False) %></span></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsFollowUps("Comment") %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=100><a href="<%= Session("WorkingDir") %>/TableComments/Mark_FollowUpComplete_Proc.asp?CommentId=<%= rsFollowUps("CommentId") %>">Mark Complete</a></td>
					</tr>
<%

		rsFollowUps.MoveNext
	Loop
%>
				</table>
<%
End If
rsFollowUps.Close
Set rsFollowUps = Nothing
%>
			</td>
		</tr>
	</table>
	<br><br>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
