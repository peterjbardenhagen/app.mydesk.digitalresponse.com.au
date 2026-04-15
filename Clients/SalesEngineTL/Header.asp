<%
Dim strWorkingDir

If Request.Cookies("LoggedIn")&"" <> "" Then
	strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
Else
	strWorkingDir = Session("WorkingDir")
End If

' Get current page for active nav highlighting
Dim currentPage
currentPage = LCase(Request.ServerVariables("SCRIPT_NAME"))

Function IsActive(pageName)
	If InStr(currentPage, LCase(pageName)) > 0 Then
		IsActive = "active"
	Else
		IsActive = ""
	End If
End Function
%>
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>MyDesk - Techlight CRM</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Modern.css">
	<script language="javascript" src="<%= strWorkingDir %>/System/Global.js"></script>
</head>
<body>

<!-- Techlight Modern Header -->
<header class="tl-header">
	<div class="tl-header-top">
		<!-- Logo -->
		<a href="<%= strWorkingDir %>/PortalFrame.asp" target="_parent" class="tl-logo" style="text-decoration: none;">
			<svg viewBox="0 0 340 80" xmlns="http://www.w3.org/2000/svg" style="width: 180px; height: 42px;">
				<defs>
					<filter id="cyanGlow" x="-50%" y="-50%" width="200%" height="200%">
						<feGaussianBlur stdDeviation="2" result="blur"/>
						<feMerge>
							<feMergeNode in="blur"/>
							<feMergeNode in="SourceGraphic"/>
						</feMerge>
					</filter>
					<linearGradient id="logoGradient" x1="0%" y1="0%" x2="100%" y2="0%">
						<stop offset="0%" style="stop-color:#00e0e0"/>
						<stop offset="100%" style="stop-color:#00c8c8"/>
					</linearGradient>
				</defs>
				<g transform="translate(40,40)">
					<circle cx="0" cy="0" r="34" fill="none" stroke="#00c0c0" stroke-width="1.5" opacity="0.35" filter="url(#cyanGlow)"/>
					<circle cx="0" cy="0" r="30" fill="none" stroke="#ffffff" stroke-width="3" opacity="0.95"/>
					<circle cx="0" cy="0" r="20" fill="none" stroke="url(#logoGradient)" stroke-width="2.5" opacity="0.9"/>
					<circle cx="0" cy="0" r="12" fill="#08121a" stroke="#00c8c8" stroke-width="2" opacity="1"/>
					<circle cx="0" cy="0" r="5" fill="#00e0e0" opacity="0.95"/>
				</g>
				<text x="92" y="52" font-family="Arial Black, Arial Bold, Arial, sans-serif" font-weight="900" font-size="32" letter-spacing="-0.5" fill="#ffffff">Techlight</text>
			</svg>
			<div class="tl-logo-text">
				<span class="tl-logo-product">MyDesk CRM</span>
			</div>
		</a>
		
		<!-- User Info -->
		<% If Request.Cookies("LoggedIn")&"" <> "" Then
			If CBool(Request.Cookies("LoggedIn")) Then %>
		<div class="tl-header-right">
			<div class="tl-user-info">
				<span class="tl-user-name"><%= Request.Cookies("UserSettings")("Name") %></span>
				<span class="tl-user-meta">
					<% If Request.Cookies("UserSettings")("LineManagerName") <> "" Then %>
						Manager: <%= Request.Cookies("UserSettings")("LineManagerName") %>
						<a href="mailto:<%= Request.Cookies("UserSettings")("LineManagerEmail") %>">Email</a>
					<% End If %>
				</span>
			</div>
		</div>
		<% End If
		End If %>
	</div>
	
	<!-- Navigation -->
	<% If Request.Cookies("LoggedIn")&"" <> "" Then
		If CBool(Request.Cookies("LoggedIn")) Then %>
	<nav class="tl-nav">
		<ul class="tl-nav-list">
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/PortalFrame.asp" target="_parent" class="tl-nav-link <%= IsActive("PortalFrame") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
						<polyline points="9 22 9 12 15 12 15 22"></polyline>
					</svg>
					Home
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Contacts/" target="_parent" class="tl-nav-link <%= IsActive("contacts") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
						<circle cx="9" cy="7" r="4"></circle>
						<path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
						<path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
					</svg>
					Contacts
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Quotes/" target="_parent" class="tl-nav-link <%= IsActive("quotes") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
						<polyline points="14 2 14 8 20 8"></polyline>
						<line x1="16" y1="13" x2="8" y2="13"></line>
						<line x1="16" y1="17" x2="8" y2="17"></line>
						<polyline points="10 9 9 9 8 9"></polyline>
					</svg>
					Quotes
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Invoices/" target="_parent" class="tl-nav-link <%= IsActive("invoices") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
						<line x1="3" y1="9" x2="21" y2="9"></line>
						<line x1="9" y1="21" x2="9" y2="9"></line>
					</svg>
					Invoices
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Purchasing/" target="_parent" class="tl-nav-link <%= IsActive("purchasing") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
						<line x1="3" y1="6" x2="21" y2="6"></line>
						<path d="M16 10a4 4 0 0 1-8 0"></path>
					</svg>
					Purchasing
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Setup/" target="_parent" class="tl-nav-link <%= IsActive("setup") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<circle cx="12" cy="12" r="3"></circle>
						<path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
					</svg>
					Setup
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Users/" target="_parent" class="tl-nav-link <%= IsActive("users") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
						<circle cx="12" cy="7" r="4"></circle>
					</svg>
					Users
				</a>
			</li>
			<li class="tl-nav-item tl-nav-logout">
				<a href="<%= strWorkingDir %>/Portal/LogOff.asp" target="_parent" class="tl-btn-logout">
					<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
						<polyline points="16 17 21 12 16 7"></polyline>
						<line x1="21" y1="12" x2="9" y2="12"></line>
					</svg>
					Log Out
				</a>
			</li>
		</ul>
	</nav>
	<% End If
	End If %>
</header>

<!-- Spacer for fixed header -->
<div style="height: 0;"></div>

</body>
</html>