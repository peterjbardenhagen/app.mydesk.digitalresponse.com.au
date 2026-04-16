<% 
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg, strWorkingDir
strMsg = Trim(Request("Msg"))
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")

If Not Request.Cookies("UserSettings")("Manager") Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

Dim isAdmin
isAdmin = (Request.Cookies("UserSettings")("UserTypeId") >= 5)
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
	<title>Setup - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<style>
		.setup-hub { max-width: 1400px; margin: 0 auto; padding: 24px; }
		.setup-header { margin-bottom: 32px; }
		.setup-title { font-size: 28px; font-weight: 700; color: var(--tl-dark); display: flex; align-items: center; gap: 12px; margin-bottom: 8px; }
		.setup-subtitle { font-size: 15px; color: var(--tl-text-light); }
		.setup-section { margin-bottom: 40px; }
		.setup-section-header { display: flex; align-items: center; gap: 10px; margin-bottom: 20px; padding-bottom: 12px; border-bottom: 2px solid var(--tl-border); }
		.setup-section-title { font-size: 18px; font-weight: 600; color: var(--tl-dark); }
		.setup-section-badge { background: var(--tl-primary); color: white; padding: 4px 10px; border-radius: 20px; font-size: 12px; font-weight: 500; }
		.setup-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }
		.setup-card { background: white; border-radius: 12px; padding: 20px; border: 1px solid var(--tl-border); transition: all 0.2s ease; cursor: pointer; text-decoration: none; display: flex; flex-direction: column; gap: 12px; }
		.setup-card:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(0,0,0,0.12); border-color: var(--tl-primary-light); }
		.setup-card-header { display: flex; align-items: center; gap: 12px; }
		.setup-card-icon { width: 48px; height: 48px; border-radius: 12px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
		.setup-card-icon svg { width: 24px; height: 24px; }
		.setup-card-title { font-size: 16px; font-weight: 600; color: var(--tl-dark); }
		.setup-card-desc { font-size: 13px; color: var(--tl-text-light); line-height: 1.5; }
		.setup-card-arrow { margin-left: auto; opacity: 0; transition: opacity 0.2s; color: var(--tl-primary); }
		.setup-card:hover .setup-card-arrow { opacity: 1; }
		.setup-alert { background: linear-gradient(135deg, #fff5f5 0%, #ffffff 100%); border-left: 4px solid #e53e3e; border-radius: 8px; padding: 16px 20px; margin-bottom: 24px; }
		.setup-alert-success { background: linear-gradient(135deg, #f0fff4 0%, #ffffff 100%); border-left-color: #38a169; }
		@media (max-width: 768px) { .setup-grid { grid-template-columns: 1fr; } }
	</style>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="setup-hub">
	<!-- Header -->
	<div class="setup-header">
		<nav class="tl-breadcrumb" style="margin-bottom: 16px;">
			<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Setup</span>
		</nav>
		<h1 class="setup-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 32px; height: 32px; color: var(--tl-primary);">
				<circle cx="12" cy="12" r="3"></circle>
				<path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
			</svg>
			System Setup
		</h1>
		<p class="setup-subtitle">Configure system settings, manage master data, and maintain your MyDesk environment</p>
	</div>

<% If strMsg <> "" Then %>
	<div class="setup-alert <%= InStr(strMsg, "success") > 0 Or InStr(strMsg, "Success") > 0 ? "setup-alert-success" : "" %>">
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

<% If isAdmin Then %>
	<!-- Administrator Section -->
	<div class="setup-section">
		<div class="setup-section-header">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 22px; height: 22px; color: var(--tl-primary);">
				<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
			</svg>
			<h2 class="setup-section-title">Administrator Functions</h2>
			<span class="setup-section-badge">Full Access</span>
		</div>
		
		<div class="setup-grid">
			
			<!-- Activity Types -->
			<a href="../ActivityTypes" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e0f7fa 0%, #b2ebf2 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#00a8b5" stroke-width="2">
							<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
							<line x1="16" y1="2" x2="16" y2="6"></line>
							<line x1="8" y1="2" x2="8" y2="6"></line>
							<line x1="3" y1="10" x2="21" y2="10"></line>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Activity Types</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Manage activity categories and types for time tracking and project management</p>
			</a>

			<!-- Conditions of Sale -->
			<a href="../QuoteCOS" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #f3e5f5 0%, #e1bee7 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#8e24aa" stroke-width="2">
							<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
							<polyline points="14 2 14 8 20 8"></polyline>
							<path d="M12 18v-6"></path>
							<path d="M9 15l3-3 3 3"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Conditions of Sale</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Configure terms and conditions, warranties, and legal text for customer quotes</p>
			</a>

			<!-- Copy Contacts -->
			<a href="../CopyContacts" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#43a047" stroke-width="2">
							<path d="M16 4h2a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h2"></path>
							<rect x="8" y="2" width="8" height="4" rx="1" ry="1"></rect>
							<path d="M9 14l2 2 4-4"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Copy Contacts</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Duplicate and manage contact records across divisions and territories</p>
			</a>

			<!-- Currency Rates -->
			<a href="../CurrencyRates" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#ef6c00" stroke-width="2">
							<circle cx="12" cy="12" r="10"></circle>
							<line x1="12" y1="8" x2="12" y2="12"></line>
							<line x1="12" y1="16" x2="12.01" y2="16"></line>
							<path d="M8 11h8"></path>
							<path d="M8 15h5"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Currency Rates</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Manage exchange rates for international transactions and multi-currency quotes</p>
			</a>

			<!-- Divisions -->
			<a href="../Divisions" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e3f2fd 0%, #bbdefb 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#1976d2" stroke-width="2">
							<rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
							<line x1="8" y1="21" x2="16" y2="21"></line>
							<line x1="12" y1="17" x2="12" y2="21"></line>
							<path d="M6 8h.01"></path>
							<path d="M10 8h.01"></path>
							<path d="M14 8h.01"></path>
							<path d="M18 8h.01"></path>
							<path d="M8 12h.01"></path>
							<path d="M12 12h.01"></path>
							<path d="M16 12h.01"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Divisions</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Configure business divisions, entities, and organizational structure</p>
			</a>

			<!-- Expense Types -->
			<a href="../ExpenseTypes" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #fce4ec 0%, #f8bbd9 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#c2185b" stroke-width="2">
							<circle cx="9" cy="21" r="1"></circle>
							<circle cx="20" cy="21" r="1"></circle>
							<path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Expense Types</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Define expense categories for purchase orders, invoicing, and cost tracking</p>
			</a>

			<!-- Expense Type Groups -->
			<a href="../ExpenseTypeGroups" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #f1f8e9 0%, #dcedc8 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#689f38" stroke-width="2">
							<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
							<circle cx="9" cy="7" r="4"></circle>
							<path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
							<path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Expense Type Groups</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Organize expenses into groups for reporting and budget management</p>
			</a>

			<!-- Files Categories -->
			<a href="../FilesCategories" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e0e0e0 0%, #bdbdbd 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#616161" stroke-width="2">
							<path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path>
							<line x1="12" y1="11" x2="12" y2="17"></line>
							<line x1="9" y1="14" x2="15" y2="14"></line>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Files Categories</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Create document categories for organizing uploaded files and attachments</p>
			</a>

			<!-- Import Data -->
			<a href="../ImportData" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e8eaf6 0%, #c5cae9 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#3f51b5" stroke-width="2">
							<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
							<polyline points="17 8 12 3 7 8"></polyline>
							<line x1="12" y1="3" x2="12" y2="15"></line>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Import Data</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Bulk import contacts, companies, and products from CSV or Excel files</p>
			</a>

			<!-- Locations -->
			<a href="../Locations" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e1f5fe 0%, #b3e5fc 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#0288d1" stroke-width="2">
							<path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path>
							<circle cx="12" cy="10" r="3"></circle>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Locations</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Manage office locations, warehouses, and service areas</p>
			</a>

			<!-- Maintenance -->
			<a href="Maintenance.asp" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #fff9c4 0%, #fff59d 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#f9a825" stroke-width="2">
							<circle cx="12" cy="12" r="3"></circle>
							<path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Maintenance</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Database maintenance, cleanup tools, and system optimization utilities</p>
			</a>

			<!-- Part Codes -->
			<a href="../PartCodes" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #efebe9 0%, #d7ccc8 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#5d4037" stroke-width="2">
							<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Part Codes</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Manage product codes, part numbers, and inventory SKUs</p>
			</a>

			<!-- SQL Query -->
			<a href="../SQLQuery" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #263238 0%, #37474f 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#eceff1" stroke-width="2">
							<rect x="2" y="3" width="20" height="14" rx="2" ry="2"></rect>
							<line x1="8" y1="21" x2="16" y2="21"></line>
							<line x1="12" y1="17" x2="12" y2="21"></line>
							<path d="M8 9l2 2 2-2"></path>
							<path d="M12 11V7"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">SQL Query</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Advanced database query tool for reports and data extraction</p>
			</a>

			<!-- User Roles -->
			<a href="../UserRoles" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #ffebee 0%, #ffcdd2 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#c62828" stroke-width="2">
							<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
							<circle cx="9" cy="7" r="4"></circle>
							<path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
							<path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">User Roles</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Define user permissions, access levels, and role-based security</p>
			</a>

			<!-- API Keys -->
			<a href="APIKeys/Default.asp" class="setup-card" target="_top">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#2e7d32" stroke-width="2">
							<path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">API Keys</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Manage API keys for external system integrations, webhooks, and third-party access</p>
			</a>

		</div>
	</div>
<% End If %>

	<!-- Manager Section -->
	<div class="setup-section">
		<div class="setup-section-header">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 22px; height: 22px; color: var(--tl-primary);">
				<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
			</svg>
			<h2 class="setup-section-title">Manager Functions</h2>
			<span class="setup-section-badge" style="background: #d4a574;">Management</span>
		</div>
		
		<div class="setup-grid" style="grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));">
			
			<!-- Companies -->
			<a href="../Companies" class="setup-card" target="_top" style="border-left: 4px solid #00a8b5;">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #e0f7fa 0%, #b2ebf2 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#00a8b5" stroke-width="2">
							<path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path>
							<polyline points="9 22 9 12 15 12 15 22"></polyline>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Companies</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Manage customer companies, suppliers, and business entities with contact details and billing information</p>
			</a>

			<!-- Projects -->
			<a href="../Projects" class="setup-card" target="_top" style="border-left: 4px solid #d4a574;">
				<div class="setup-card-header">
					<div class="setup-card-icon" style="background: linear-gradient(135deg, #fff3e0 0%, #ffe0b2 100%);">
						<svg viewBox="0 0 24 24" fill="none" stroke="#ef6c00" stroke-width="2">
							<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
							<polyline points="22 4 12 14.01 9 11.01"></polyline>
							<path d="M8 12l3-3 3 3"></path>
							<path d="M12 16V9"></path>
						</svg>
					</div>
					<div>
						<h3 class="setup-card-title">Projects</h3>
					</div>
					<svg class="setup-card-arrow" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 18px; height: 18px;">
						<polyline points="9 18 15 12 9 6"></polyline>
					</svg>
				</div>
				<p class="setup-card-desc">Track projects, assignments, milestones, and associated quotes, invoices, and purchase orders</p>
			</a>

		</div>
	</div>

	<!-- Quick Help -->
	<div class="setup-section" style="margin-top: 48px;">
		<div style="background: linear-gradient(135deg, #e8f4f8 0%, #f0f7fa 100%); border-radius: 12px; padding: 24px; border: 1px solid #b8d4e3;">
			<div style="display: flex; align-items: start; gap: 16px;">
				<div style="width: 48px; height: 48px; background: var(--tl-primary); border-radius: 12px; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
					<svg viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" style="width: 24px; height: 24px;">
						<circle cx="12" cy="12" r="10"></circle>
						<path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3"></path>
						<line x1="12" y1="17" x2="12.01" y2="17"></line>
					</svg>
				</div>
				<div>
					<h3 style="font-size: 16px; font-weight: 600; color: var(--tl-dark); margin-bottom: 8px;">Need Help?</h3>
					<p style="font-size: 14px; color: var(--tl-text-light); margin-bottom: 12px;">Contact Techlight support for assistance with system configuration or training.</p>
					<div style="display: flex; gap: 12px;">
						<a href="mailto:info@digitalresponse.com.au" style="display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; background: var(--tl-primary); color: white; border-radius: 6px; text-decoration: none; font-size: 13px; font-weight: 500;">
							<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 16px; height: 16px;">
								<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
								<polyline points="22,6 12,13 2,6"></polyline>
							</svg>
							Email Support
						</a>
					</div>
				</div>
			</div>
		</div>
	</div>

</div>

</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->