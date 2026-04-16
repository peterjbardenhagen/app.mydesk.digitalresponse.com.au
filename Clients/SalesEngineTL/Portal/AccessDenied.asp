<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<link rel="preconnect" href="https://fonts.googleapis.com">
		<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
		<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<div style="display: flex; justify-content: center; align-items: center; min-height: 60vh; padding: 24px;">
			<div class="tl-card" style="max-width: 500px; width: 100%; text-align: center; padding: 48px 32px; border-top: 4px solid var(--tl-danger);">
				<div style="width: 80px; height: 80px; background: #fff5f5; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin: 0 auto 24px; color: #e53e3e;">
					<svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>
				</div>
				<h1 style="font-size: 24px; font-weight: 700; color: var(--tl-dark); margin-bottom: 12px;">Access Denied</h1>
				<p style="color: var(--tl-text-light); font-size: 16px; line-height: 1.6; margin-bottom: 32px;">
					You do not have the required permissions to access this area. If you believe this is an error, please contact your system administrator or line manager.
				</p>
				<div class="tl-btn-group" style="justify-content: center; gap: 16px;">
					<button onclick="history.back()" class="tl-btn tl-btn-secondary">
						<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg>
						Go Back
					</button>
					<a href="/Portal.asp" class="tl-btn tl-btn-primary">
						<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path><polyline points="9 22 9 12 15 12 15 22"></polyline></svg>
						Return Home
					</a>
				</div>
			</div>
		</div>
	</div>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->