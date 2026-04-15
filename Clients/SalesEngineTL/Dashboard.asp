<%
' Techlight MyDesk - Modern Dashboard
' Check authentication
If Not Session("LoggedIn") Then
	If Request.Cookies("LoggedIn") <> "" Then
		If Not CBool(Request.Cookies("LoggedIn")) Then
			Response.Redirect("Default.asp")
		End If
	Else
		Response.Redirect("Default.asp")
	End If
End If

Dim strWorkingDir
If Request.Cookies("ClientSettings")("WorkingDir") <> "" Then
	strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
Else
	strWorkingDir = Session("WorkingDir")
End If

' Get user info
Dim userName, userRole, userCode
userName = Session("Name")
If userName = "" Then userName = Request.Cookies("UserSettings")("Name")
userCode = Session("Code")
If userCode = "" Then userCode = Request.Cookies("UserSettings")("Code")

If Request.Cookies("UserSettings")("Admin") Then
	userRole = "Administrator"
ElseIf Session("Manager") Then
	userRole = "Manager"
Else
	userRole = "User"
End If
%>
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Dashboard - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
</head>
<body class="tl-bg-light">
	<div class="tl-main">
		<!-- Welcome Section -->
		<div class="tl-welcome-card">
			<h1 class="tl-welcome-title">Welcome back, <%= userName %></h1>
			<p class="tl-welcome-subtitle">You have successfully logged into Techlight MyDesk. You are an <%= userRole %>.</p>
			<div class="tl-welcome-meta">
				<span class="tl-meta-item">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
						<circle cx="12" cy="7" r="4"></circle>
					</svg>
					<%= userCode %>
				</span>
				<span class="tl-meta-item">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
						<line x1="16" y1="2" x2="16" y2="6"></line>
						<line x1="8" y1="2" x2="8" y2="6"></line>
						<line x1="3" y1="10" x2="21" y2="10"></line>
					</svg>
					<%= FormatDateTime(Now(), 1) %>
				</span>
				<span class="tl-meta-item">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M12 2L2 7l10 5 10-5-10-5z"></path>
						<path d="M2 17l10 5 10-5"></path>
						<path d="M2 12l10 5 10-5"></path>
					</svg>
					Techlight MyDesk
				</span>
			</div>
			
			<!-- Quick Navigation Search -->
			<div class="tl-quick-search">
				<span class="tl-quick-search-label">Quick Navigation</span>
				<form action="<%= strWorkingDir %>/QuickNav.asp" method="get" target="_parent" style="display: flex; align-items: center; gap: 8px; flex: 1;">
					<input type="text" name="ID" class="tl-quick-search-input" placeholder="Enter ID #" required>
					<select name="Type" class="tl-quick-search-select">
						<option value="Quote">Quote</option>
						<option value="PurchaseOrder">Purchase Order</option>
						<option value="Invoice">Invoice</option>
						<option value="Contact">Contact</option>
					</select>
					<button type="submit" class="tl-btn-primary">
						<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 4px;">
							<circle cx="11" cy="11" r="8"></circle>
							<path d="m21 21-4.35-4.35"></path>
						</svg>
						Go
					</button>
				</form>
			</div>
		</div>

		<!-- Quick Actions Grid -->
		<div class="tl-quick-actions">
			<a href="<%= strWorkingDir %>/Contacts/" class="tl-action-btn" target="_parent">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
					<circle cx="9" cy="7" r="4"></circle>
					<path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
					<path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
				</svg>
				<span class="tl-action-label">Contacts</span>
			</a>
			<a href="<%= strWorkingDir %>/Quotes/" class="tl-action-btn" target="_parent">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
					<polyline points="14 2 14 8 20 8"></polyline>
					<line x1="16" y1="13" x2="8" y2="13"></line>
					<line x1="16" y1="17" x2="8" y2="17"></line>
				</svg>
				<span class="tl-action-label">Quotes</span>
			</a>
			<a href="<%= strWorkingDir %>/Invoices/" class="tl-action-btn" target="_parent">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
					<line x1="3" y1="9" x2="21" y2="9"></line>
					<line x1="9" y1="21" x2="9" y2="9"></line>
				</svg>
				<span class="tl-action-label">Invoices</span>
			</a>
			<a href="<%= strWorkingDir %>/PurchaseOrders/" class="tl-action-btn" target="_parent">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
					<line x1="3" y1="6" x2="21" y2="6"></line>
					<path d="M16 10a4 4 0 0 1-8 0"></path>
				</svg>
				<span class="tl-action-label">Purchase Orders</span>
			</a>
			<a href="<%= strWorkingDir %>/Setup/" class="tl-action-btn" target="_parent">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<circle cx="12" cy="12" r="3"></circle>
					<path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
				</svg>
				<span class="tl-action-label">Setup</span>
			</a>
			<a href="<%= strWorkingDir %>/Users/" class="tl-action-btn" target="_parent">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
					<circle cx="12" cy="7" r="4"></circle>
				</svg>
				<span class="tl-action-label">Users</span>
			</a>
		</div>

		<!-- Dashboard Grid -->
		<div class="tl-dashboard" style="margin-top: 24px;">
			<!-- Sidebar -->
			<div class="tl-sidebar">
				<!-- Quick Menu -->
				<div class="tl-panel">
					<h3 class="tl-panel-title">
						<svg class="tl-panel-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<circle cx="12" cy="12" r="10"></circle>
							<polyline points="12 6 12 12 16 14"></polyline>
						</svg>
						Quick Access
					</h3>
					<ul class="tl-menu-list">
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Contacts/Add.asp" class="tl-menu-link" target="_parent">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Contact
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Quotes/Add.asp" class="tl-menu-link" target="_parent">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Quote
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/PurchaseOrders/Add.asp" class="tl-menu-link" target="_parent">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Purchase Order
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Reports/" class="tl-menu-link" target="_parent">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<path d="M3 3v18h18"></path>
									<path d="M18.7 8l-5.1 5.2-2.8-2.7L7 14.3"></path>
								</svg>
								Reports
							</a>
						</li>
					</ul>
				</div>

				<!-- System Status -->
				<div class="tl-panel">
					<h3 class="tl-panel-title">
						<svg class="tl-panel-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<path d="M22 12h-4l-3 9L9 3l-3 9H2"></path>
						</svg>
						System Status
					</h3>
					<div style="font-size: 13px; color: var(--tl-text-light); line-height: 1.8;">
						<div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
							<span>Version:</span>
							<span style="font-weight: 600; color: var(--tl-text);">Techlight MyDesk v3.0</span>
						</div>
						<div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
							<span>Environment:</span>
							<span style="font-weight: 600; color: var(--tl-primary);">
								<% If InStr(Request.ServerVariables("SERVER_NAME"), "localhost") > 0 Then %>Local<% Else %>Production<% End If %>
							</span>
						</div>
						<div style="display: flex; justify-content: space-between;">
							<span>Status:</span>
							<span style="font-weight: 600; color: #22c55e;">Online</span>
						</div>
					</div>
				</div>
			</div>

			<!-- Main Content -->
			<div style="display: flex; flex-direction: column; gap: 24px;">
				<!-- Recent Activity Panel -->
				<div class="tl-panel">
					<h3 class="tl-panel-title">
						<svg class="tl-panel-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
						</svg>
						Activity Overview
					</h3>
					<div style="padding: 40px 20px; text-align: center; color: var(--tl-text-muted);">
						<svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" style="margin-bottom: 16px; opacity: 0.5;">
							<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
							<line x1="9" y1="9" x2="15" y2="15"></line>
							<line x1="15" y1="9" x2="9" y2="15"></line>
						</svg>
						<p style="font-size: 14px; margin: 0;">Activity feed coming soon</p>
						<p style="font-size: 12px; margin-top: 8px;">Track your recent quotes, invoices, and contacts here</p>
					</div>
				</div>

				<!-- Help & Resources -->
				<div class="tl-panel">
					<h3 class="tl-panel-title">
						<svg class="tl-panel-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<circle cx="12" cy="12" r="10"></circle>
							<path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"></path>
							<line x1="12" y1="17" x2="12.01" y2="17"></line>
						</svg>
						Help & Resources
					</h3>
					<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px;">
						<a href="mailto:support@techlight.com.au" style="display: flex; align-items: center; gap: 12px; padding: 16px; background: var(--tl-bg); border-radius: var(--tl-radius); text-decoration: none; color: var(--tl-text); transition: var(--tl-transition);">
							<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="var(--tl-primary)" stroke-width="2">
								<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
								<polyline points="22,6 12,13 2,6"></polyline>
							</svg>
							<div>
								<div style="font-weight: 600; font-size: 13px;">Email Support</div>
								<div style="font-size: 12px; color: var(--tl-text-light);">support@techlight.com.au</div>
							</div>
						</a>
						<a href="https://techlight.com.au" target="_blank" style="display: flex; align-items: center; gap: 12px; padding: 16px; background: var(--tl-bg); border-radius: var(--tl-radius); text-decoration: none; color: var(--tl-text); transition: var(--tl-transition);">
							<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="var(--tl-primary)" stroke-width="2">
								<circle cx="12" cy="12" r="10"></circle>
								<line x1="2" y1="12" x2="22" y2="12"></line>
								<path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path>
							</svg>
							<div>
								<div style="font-weight: 600; font-size: 13px;">Techlight Website</div>
								<div style="font-size: 12px; color: var(--tl-text-light);">techlight.com.au</div>
							</div>
						</a>
					</div>
				</div>
			</div>
		</div>
	</div>
</body>
</html>
