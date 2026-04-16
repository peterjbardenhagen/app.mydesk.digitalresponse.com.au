<!--#include virtual="/System/ssi_Header_Techlight.inc"-->
<%
' --- Track User History ---
On Error Resume Next
If Request.Cookies("LoggedIn")&"" <> "" Then
    If CBool(Request.Cookies("LoggedIn")) Then
        Dim histUrl_Header, histCodel_Header
        histUrl_Header = Request.ServerVariables("URL")
        If Request.QueryString <> "" Then histUrl_Header = histUrl_Header & "?" & Request.QueryString
        histCodel_Header = Request.Cookies("UserSettings")("Code")
        
        ' Only log if not a processing page, script, or image
        If InStr(LCase(histUrl_Header), "_proc.asp") = 0 And InStr(LCase(histUrl_Header), ".inc") = 0 _
           And InStr(LCase(histUrl_Header), ".js") = 0 And InStr(LCase(histUrl_Header), ".css") = 0 _
           And InStr(LCase(histUrl_Header), ".svg") = 0 And InStr(LCase(histUrl_Header), ".png") = 0 Then
            ' Check if dbConn is available
            If IsObject(dbConn) Then
                If dbConn.State = 1 Then
                    Call SafeExecute("INSERT INTO UserHistory (UserCode, PageUrl, PageTitle) VALUES ('" & Replace(histCodel_Header, "'", "''") & "', '" & Replace(histUrl_Header, "'", "''") & "', '')")
                End If
            End If
        End If
    End If
End If
On Error GoTo 0
' -------------------------
%>
<% If strWorkingDir = "" Then strWorkingDir = "/Clients/SalesEngineTL" %>
<!-- Techlight Modern Header -->
<header class="tl-header">
	<div class="tl-header-top">
		<div class="tl-container" style="display: flex; align-items: center; justify-content: space-between; width: 100%;">
			<!-- Logo -->
			<a href="<%= strWorkingDir %>/Default.asp" target="_parent" class="tl-logo" style="text-decoration: none;" onclick="if(window.parent.location.href.indexOf('Default.asp')>-1){window.parent.location.reload();return false;}">
				<img src="/images/techlight-logo.svg" alt="Techlight" style="height: 42px; width: auto; object-fit: contain; filter: drop-shadow(0 0 10px rgba(0,200,200,0.3));" />
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
				userName = Request.Cookies("UserSettings")("Name")
				userInitials = Left(userName, 1)
'				Dim isAdmin
				isAdmin = False
				On Error Resume Next
				If Not Request.Cookies("UserSettings") Is Nothing Then
					If Not IsEmpty(Request.Cookies("UserSettings")("Admin")) Then
						Dim adminValue
						adminValue = Request.Cookies("UserSettings")("Admin")
						If IsNumeric(adminValue) Then
							isAdmin = CBool(adminValue)
						Else
							isAdmin = (LCase(adminValue) = "true")
						End If
					End If
				End If
				On Error GoTo 0
				If isAdmin Then
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
		</div>
		<% End If
		End If %>
		</div>
	</div>
	
	<!-- Navigation -->
	<% If Request.Cookies("LoggedIn")&"" <> "" Then
		If CBool(Request.Cookies("LoggedIn")) Then %>
	<nav class="tl-nav">
		<div class="tl-container">
			<ul class="tl-nav-list" style="margin: 0 auto; padding: 0; display: flex; justify-content: center; align-items: center; width: 100%;">
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
				<a href="<%= strWorkingDir %>/PurchaseOrders/" target="_top" class="tl-nav-link <%= IsActive("PurchaseOrders") %>">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
						<line x1="3" y1="6" x2="21" y2="6"></line>
						<path d="M16 10a4 4 0 0 1-8 0"></path>
					</svg>
					Purchases
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
			<li class="tl-nav-item">
				<button class="tl-nav-link" onclick="openSearchModal(event)" style="background: transparent; border: none; cursor: pointer; padding: 10px 16px; color: rgba(255,255,255,0.8); font-family: inherit;">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<circle cx="11" cy="11" r="8"></circle>
						<line x1="21" y1="21" x2="16.65" y2="16.65"></line>
					</svg>
					Search
				</button>
			</li>
		<!-- Ask AI -->
			<li class="tl-nav-item">
				<a href="#" onclick="openAskAI(); return false;" class="tl-nav-link" style="background: linear-gradient(135deg, #00c8c8 0%, #008b8b 100%); color: white; padding: 6px 14px; border-radius: 8px; font-weight: 600;">
					<svg class="tl-nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"></path>
						<circle cx="9" cy="10" r="1" fill="currentColor"></circle>
						<circle cx="15" cy="10" r="1" fill="currentColor"></circle>
					</svg>
					AI
				</a>
			</li>
			<li class="tl-nav-item tl-nav-logout" style="margin-left: auto;">
				<a href="<%= strWorkingDir %>/Portal/LogOff.asp" target="_top" class="tl-btn-logout" title="Log Out">
					<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
						<path d="M18.36 6.64a9 9 0 1 1-12.73 0"></path>
						<line x1="12" y1="2" x2="12" y2="12"></line>
					</svg>
				</a>
			</li>
		</ul>
		</div>
	</nav>
	<style>
		.tl-nav-link:hover { color: white !important; }
		button.tl-nav-link:focus, button.tl-nav-link:active { outline: none; background: rgba(255,255,255,0.1) !important; color: white !important; }
	</style>
<!-- Header overrides -->
<style>
.tl-nav-link { color: #fff !important; }
.tl-nav-link:hover { color: #fff !important; background: rgba(255,255,255,0.1) !important; }
.tl-nav-item { display: flex; align-items: center; }
</style>
	<script>
		function openSearchModal(e) {
			if(e) {
                e.preventDefault();
                e.stopPropagation();
            }
			var modal = document.getElementById('searchModal');
			if(modal) {
				modal.style.display = 'flex';
				setTimeout(function() {
					var input = document.getElementById('searchModalInput');
					if(input) input.focus();
				}, 100);
			}
		}
		function closeSearchModal() {
			var modal = document.getElementById('searchModal');
			if(modal) modal.style.display = 'none';
		}
		
		document.addEventListener('keydown', function(e) {
			if (e.key === 'Escape') closeSearchModal();
		});

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
			
			var modal = document.getElementById('searchModal');
			if (e.target === modal) {
				closeSearchModal();
			}
		});

		function openAskAI() {
			window.open('<%= strWorkingDir %>/AskAI.asp', 'AskAI', 'width=450,height=600,scrollbars=yes,resizable=yes,status=no,toolbar=no,menubar=no');
		}
	</script>
	<% End If
	End If %>
</header>
<!-- Search Modal -->
<div id="searchModal" class="tl-modal" style="display: none; position: fixed; top: 0; left: 0; right: 0; bottom: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.7); z-index: 9999999; align-items: center; justify-content: center; backdrop-filter: blur(4px);">
	<div class="tl-modal-content" style="background: white; border-radius: 12px; width: 90%; max-width: 600px; box-shadow: 0 10px 50px rgba(0,0,0,0.5); overflow: hidden; transform: translateY(-10vh);">
		<div style="padding: 16px 24px; border-bottom: 1px solid #e2e8f0; display: flex; justify-content: space-between; align-items: center; background: #f8fafc;">
			<h3 style="margin: 0; font-size: 1.1rem; color: #0f172a; font-weight: 600;">Global Search</h3>
			<button onclick="closeSearchModal()" style="background: none; border: none; font-size: 1.5rem; cursor: pointer; color: #64748b; padding: 0; outline: none; line-height: 1;">&times;</button>
		</div>
		<div style="padding: 24px;">
			<form action="<%= strWorkingDir %>/GlobalSearch.asp" method="GET" target="_top" style="display: flex; gap: 12px;">
				<input type="text" name="q" id="searchModalInput" placeholder="Search ID, Name, Keyword..." class="tl-input" style="flex: 1; padding: 12px; font-size: 1.1rem; border: 2px solid #e2e8f0; border-radius: 8px;" required>
				<button type="submit" class="tl-btn-primary" style="padding: 12px 24px;">Search</button>
			</form>
			<p style="margin: 16px 0 0 0; font-size: 0.85rem; color: #64748b;">Search across Contacts, Quotes, Invoices, and Purchase Orders by keyword or reference ID.</p>
		</div>
	</div>
</div>
<!-- Spacer for fixed header -->
<div style="height: 0;"></div>