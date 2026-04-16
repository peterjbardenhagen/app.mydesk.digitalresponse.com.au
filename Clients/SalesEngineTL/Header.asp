<%
Dim strWorkingDir

If Request.Cookies("LoggedIn")&"" <> "" Then
	strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
Else
	strWorkingDir = Session("WorkingDir")
End If

' Fallback to ensure WorkingDir is never empty
If strWorkingDir = "" Or IsNull(strWorkingDir) Then
	strWorkingDir = "/Clients/SalesEngineTL"
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

Function IsDirector()
	Dim userTypeId
	userTypeId = Request.Cookies("UserSettings")("UserTypeID") & ""
	' UserTypeID 1 = Director, 2 = Manager, 3 = Standard User
	IsDirector = (userTypeId = "1")
End Function
%>
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<script language="javascript" src="<%= strWorkingDir %>/System/Global.js"></script>
</head>
<body>

<!-- Techlight Modern Header -->
<header class="tl-header">
	<div class="tl-header-top">
		<!-- Logo -->
		<a href="<%= strWorkingDir %>/Default.asp" target="_top" class="tl-logo" style="text-decoration: none;" onclick="if(window.top.location.href.indexOf('Default.asp')>-1){window.top.location.reload();return false;}">
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
				<span class="tl-logo-brand">Techlight</span>
				<span class="tl-logo-product">MyDesk</span>
			</div>
		</a>
		
		<!-- Decorative Elements -->
		<div class="tl-header-decor">
			<svg class="tl-decor-svg" viewBox="0 0 400 100" preserveAspectRatio="none">
				<defs>
					<linearGradient id="decorGrad1" x1="0%" y1="0%" x2="100%" y2="100%">
						<stop offset="0%" style="stop-color:#00a8b5;stop-opacity:0.3"/>
						<stop offset="100%" style="stop-color:#d4a574;stop-opacity:0.2"/>
					</linearGradient>
					<linearGradient id="decorGrad2" x1="0%" y1="100%" x2="100%" y2="0%">
						<stop offset="0%" style="stop-color:#00c4d3;stop-opacity:0.15"/>
						<stop offset="100%" style="stop-color:#00a8b5;stop-opacity:0.1"/>
					</linearGradient>
				</defs>
				<!-- Flowing curves -->
				<path d="M0,80 Q100,20 200,60 T400,40" stroke="url(#decorGrad1)" stroke-width="2" fill="none" opacity="0.6"/>
				<path d="M0,60 Q80,90 160,50 T320,70 T400,30" stroke="url(#decorGrad2)" stroke-width="1.5" fill="none" opacity="0.4"/>
				<path d="M50,100 Q150,40 250,80 T400,60" stroke="#00a8b5" stroke-width="1" fill="none" opacity="0.3"/>
				<!-- Decorative circles -->
				<circle cx="320" cy="25" r="15" fill="url(#decorGrad1)" opacity="0.3"/>
				<circle cx="360" cy="70" r="8" fill="#d4a574" opacity="0.25"/>
				<circle cx="280" cy="85" r="5" fill="#00c4d3" opacity="0.4"/>
			</svg>
		</div>
		
		<!-- User Info -->
		<% If Request.Cookies("LoggedIn")&"" <> "" Then
			If CBool(Request.Cookies("LoggedIn")) Then 
				Dim userInitials, userName, userRole
				userName = Request.Cookies("UserSettings")("Name")
				userInitials = Left(userName, 1)
				If Request.Cookies("UserSettings")("Admin") Then
					userRole = "Administrator"
				Else
					userRole = "User"
				End If
		%>
		<div class="tl-user-panel">
			<div class="tl-user-info">
				<span class="tl-user-name"><%= userName %></span>
				<span class="tl-user-role"><%= userRole %></span>
			</div>
			<div class="tl-user-avatar"><%= userInitials %></div>
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
				<a href="<%= strWorkingDir %>/Dashboard.asp" target="MainFrame" class="tl-nav-link <%= IsActive("Dashboard") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
						<polyline points="9 22 9 12 15 12 15 22"></polyline>
					</svg>
					Home
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Contacts/" target="_top" class="tl-nav-link <%= IsActive("contacts") %>">
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
				<a href="<%= strWorkingDir %>/Quotes/" target="_top" class="tl-nav-link <%= IsActive("quotes") %>">
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
				<a href="<%= strWorkingDir %>/Invoices/" target="_top" class="tl-nav-link <%= IsActive("invoices") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
						<line x1="3" y1="9" x2="21" y2="9"></line>
						<line x1="9" y1="21" x2="9" y2="9"></line>
					</svg>
					Invoices
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Purchasing/" target="_top" class="tl-nav-link <%= IsActive("purchasing") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
						<line x1="3" y1="6" x2="21" y2="6"></line>
						<path d="M16 10a4 4 0 0 1-8 0"></path>
					</svg>
					Purchasing
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Setup/" target="_top" class="tl-nav-link <%= IsActive("setup") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<circle cx="12" cy="12" r="3"></circle>
						<path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
					</svg>
					Setup
				</a>
			</li>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Users/" target="_top" class="tl-nav-link <%= IsActive("users") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
						<circle cx="12" cy="7" r="4"></circle>
					</svg>
					Users
				</a>
			</li>
			<% If IsDirector() Then %>
			<li class="tl-nav-item">
				<a href="<%= strWorkingDir %>/Admin/" target="_top" class="tl-nav-link <%= IsActive("admin") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
					</svg>
					Admin
				</a>
			</li>
			<% End If %>
			<li class="tl-nav-item tl-nav-dropdown">
				<button class="tl-nav-link tl-dropdown-toggle" onclick="toggleDropdown(event)">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<line x1="3" y1="12" x2="21" y2="12"></line>
						<line x1="3" y1="6" x2="21" y2="6"></line>
						<line x1="3" y1="18" x2="21" y2="18"></line>
					</svg>
					Quick Nav
					<svg class="tl-dropdown-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<polyline points="6 9 12 15 18 9"></polyline>
					</svg>
				</button>
				<div class="tl-dropdown-menu">
					<a href="<%= strWorkingDir %>/Dashboard.asp" target="MainFrame" class="tl-dropdown-item">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>
						Dashboard
					</a>
					<a href="<%= strWorkingDir %>/Contacts/" target="MainFrame" class="tl-dropdown-item">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg>
						Contacts
					</a>
					<a href="<%= strWorkingDir %>/Quotes/" target="MainFrame" class="tl-dropdown-item">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline></svg>
						Quotes
					</a>
					<a href="<%= strWorkingDir %>/Invoices/" target="MainFrame" class="tl-dropdown-item">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><line x1="3" y1="9" x2="21" y2="9"></line></svg>
						Invoices
					</a>
					<a href="<%= strWorkingDir %>/PurchaseOrders/" target="MainFrame" class="tl-dropdown-item">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path><line x1="3" y1="6" x2="21" y2="6"></line></svg>
						Purchase Orders
					</a>
			</li>
			<li class="tl-nav-item tl-nav-logout">
				<a href="<%= strWorkingDir %>/Portal/LogOff.asp" target="_top" class="tl-btn-logout">
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
	<script>
		function toggleDropdown(e) {
			e.preventDefault();
			e.stopPropagation();
			var dropdown = e.currentTarget.parentElement;
			dropdown.classList.toggle('active');
		}
		
		document.addEventListener('click', function(e) {
			var dropdowns = document.querySelectorAll('.tl-nav-dropdown');
			dropdowns.forEach(function(dropdown) {
				if (!dropdown.contains(e.target)) {
					dropdown.classList.remove('active');
				}
			});
		});
	</script>
	<% End If
	End If %>
</header>

<!-- Spacer for fixed header -->
<div style="height: 0;"></div>

</body>
</html>