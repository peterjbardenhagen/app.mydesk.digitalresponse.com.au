<% 
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg, strWorkingDir
strMsg = Trim(Request("Msg"))
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")

If strWorkingDir = "" Or IsNull(strWorkingDir) Then
	strWorkingDir = "/Clients/SalesEngineTL"
End If

If Not Request.Cookies("UserSettings")("UserTypeId") => 6 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

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
	<title>Reports - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<style>
		.reports-hub { max-width: 1400px; margin: 0 auto; padding: 24px; }
		.reports-header { margin-bottom: 32px; }
		.reports-title { font-size: 28px; font-weight: 700; color: var(--tl-dark); display: flex; align-items: center; gap: 12px; margin-bottom: 8px; }
		.reports-subtitle { font-size: 15px; color: var(--tl-text-light); }
		.reports-section { margin-bottom: 40px; }
		.reports-section-header { display: flex; align-items: center; gap: 10px; margin-bottom: 20px; padding-bottom: 12px; border-bottom: 2px solid var(--tl-border); }
		.reports-section-title { font-size: 18px; font-weight: 600; color: var(--tl-dark); }
		.reports-section-badge { background: var(--tl-primary); color: white; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 500; }
		.reports-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 16px; }
		.reports-card { background: white; border-radius: 12px; padding: 24px; border: 1px solid var(--tl-border); transition: all 0.2s ease; cursor: pointer; text-decoration: none; display: flex; flex-direction: column; gap: 16px; }
		.reports-card:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(0,0,0,0.12); border-color: var(--tl-primary-light); }
		.reports-card-header { display: flex; align-items: center; gap: 16px; }
		.reports-card-icon { width: 56px; height: 56px; border-radius: 12px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
		.reports-card-icon svg { width: 28px; height: 28px; }
		.reports-card-title { font-size: 17px; font-weight: 600; color: var(--tl-dark); }
		.reports-card-desc { font-size: 14px; color: var(--tl-text-light); line-height: 1.5; }
		.reports-card-arrow { margin-left: auto; opacity: 0; transition: opacity 0.2s; color: var(--tl-primary); }
		.reports-card:hover .reports-card-arrow { opacity: 1; }
		.reports-alert { background: linear-gradient(135deg, #fff5f5 0%, #ffffff 100%); border-left: 4px solid #e53e3e; border-radius: 8px; padding: 16px 20px; margin-bottom: 24px; }
		.reports-alert-success { background: linear-gradient(135deg, #f0fff4 0%, #ffffff 100%); border-left-color: #38a169; }
		@media (max-width: 768px) { .reports-grid { grid-template-columns: 1fr; } }
	</style>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="reports-hub">
	<!-- Header -->
	<div class="reports-header">
		<nav class="tl-breadcrumb" style="margin-bottom: 16px;">
			<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Reports</span>
		</nav>
		<h1 class="reports-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 32px; height: 32px; color: var(--tl-primary);">
				<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
				<line x1="3" y1="9" x2="21" y2="9"></line>
				<line x1="9" y1="21" x2="9" y2="9"></line>
			</svg>
			Reports & Analytics
		</h1>
		<p class="reports-subtitle">View sales reports, purchase order analytics, and business intelligence dashboards</p>
	</div>

	<% If strMsg <> "" Then %>
	<div class="reports-alert <%= InStr(strMsg, "success") > 0 Or InStr(strMsg, "Success") > 0 ? "reports-alert-success" : "" %>">
		<div style="display: flex; align-items: center; gap: 10px;">
			<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="<%= InStr(strMsg, "success") > 0 Or InStr(strMsg, "Success") > 0 ? "#38a169" : "#e53e3e" %>" stroke-width="2">
				<% If InStr(strMsg, "success") > 0 Or InStr(strMsg, "Success") > 0 Then %>
					<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline>
				<% Else %>
					<circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line>
				<% End If %>
			</svg>
			<span style="font-weight: 500; color: <%= InStr(strMsg, "success") > 0 Or InStr(strMsg, "Success") > 0 ? "#38a169" : "#e53e3e" %>;"><%= strMsg %></span>
		</div>
	</div>
	<% End If %>

	<!-- Sales Reports Section -->
	<div class="reports-section">
		<div class="reports-section-header">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 22px; height: 22px; color: var(--tl-primary);">
				<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
				<polyline points="14 2 14 8 20 8"></polyline>
				<line x1="16" y1="13" x2="8" y2="13"></line>
				<line x1="16" y1="17" x2="8" y2="17"></line>
				<polyline points="10 9 9 9 8 9"></polyline>
			</svg>
			<h2 class="reports-section-title">Sales Reports</h2>
			<span class="reports-section-badge">Revenue Analysis</span>
		</div>
		
		<div class="reports-grid">
			<!-- Sales Report Generator -->
			<a href="SalesReportGen.asp" class="reports-card" target="_top">
				<div class="reports-card-header">
					<div class="reports-card-icon" style="background: linear-gradient(135deg, #e0f7fa 0%, #b2ebf2 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#00a8b5" stroke-width="2">
							<line x1="18" y1="20" x2="18" y2="10"></line>
							<line x1="12" y1="20" x2="12" y2="4"></line>
							<line x1="6" y1="20" x2="6" y2="14"></line>
						</svg>
					</div>
					<div>
						<h3 class="reports-card-title">Sales Report</h3>
					</div>
					<svg class="reports-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="reports-card-desc">Generate detailed sales reports with filtering by date range, division, and sales representative</p>
			</a>
		</div>
	</div>

	<!-- Purchase Orders Section -->
	<div class="reports-section">
		<div class="reports-section-header">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 22px; height: 22px; color: var(--tl-primary);">
				<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
				<line x1="3" y1="6" x2="21" y2="6"></line>
				<path d="M16 10a4 4 0 0 1-8 0"></path>
			</svg>
			<h2 class="reports-section-title">Purchase Orders</h2>
			<span class="reports-section-badge">Spend Analysis</span>
		</div>
		
		<div class="reports-grid">
			<!-- Purchase Orders By Month By Division -->
			<a href="PurchaseOrders_ByMonth_ByDivision.asp" class="reports-card" target="_top">
				<div class="reports-card-header">
					<div class="reports-card-icon" style="background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#ef6c00" stroke-width="2">
							<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
							<line x1="16" y1="2" x2="16" y2="6"></line>
							<line x1="8" y1="2" x2="8" y2="6"></line>
							<line x1="3" y1="10" x2="21" y2="10"></line>
						</svg>
					</div>
					<div>
						<h3 class="reports-card-title">PO by Month & Division</h3>
					</div>
					<svg class="reports-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="reports-card-desc">Analyze purchase order trends by month and division for better spend management</p>
			</a>
		</div>
	</div>

	<!-- Additional Reports Section -->
	<div class="reports-section">
		<div class="reports-section-header">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 22px; height: 22px; color: var(--tl-primary);">
				<circle cx="12" cy="12" r="10"></circle>
				<line x1="12" y1="16" x2="12" y2="12"></line>
				<line x1="12" y1="8" x2="12.01" y2="8"></line>
			</svg>
			<h2 class="reports-section-title">Additional Reports</h2>
			<span class="reports-section-badge">Coming Soon</span>
		</div>
		
		<div class="reports-grid">
			<!-- Placeholder for future reports -->
			<div class="reports-card" style="opacity: 0.6; cursor: not-allowed;">
				<div class="reports-card-header">
					<div class="reports-card-icon" style="background: linear-gradient(135deg, #f5f5f5 0%, #e0e0e0 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#999" stroke-width="2">
							<circle cx="12" cy="12" r="1"></circle>
							<circle cx="19" cy="12" r="1"></circle>
							<circle cx="5" cy="12" r="1"></circle>
						</svg>
					</div>
					<div>
						<h3 class="reports-card-title">More Reports</h3>
					</div>
				</div>
				<p class="reports-card-desc">Additional reports and analytics will be added here in future updates</p>
			</div>
		</div>
	</div>
</div>

</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->