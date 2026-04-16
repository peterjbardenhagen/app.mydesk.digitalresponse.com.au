<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%
' Security check
If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then 
    Response.Redirect "../Portal/AccessDenied.asp"
End If

' Date range selection
dteFrom = Request("DateFrom")
dteTo = Request("DateTo")

If dteFrom = "" Then
    dteFrom = FormatDateU(DateAdd("d", -30, ServerToEST(Now())), False)
    dteTo = FormatDateU(Now(), False)
End If

Dim strWorkingDir
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
If strWorkingDir = "" Then strWorkingDir = "/Clients/SalesEngineTL"
%>
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Export to MYOB - Techlight MyDesk</title>
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
	<script language="javascript" src="/System/cal2.js"></script>
	<script language="javascript" src="/System/cal_conf2.js"></script>
</head>
<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
<div class="tl-page-container" style="max-width: 600px; margin: 40px auto; background: white; padding: 24px; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);">
	<!-- Breadcrumb -->
	<nav class="tl-breadcrumb" style="margin-bottom: 20px;">
		<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="<%= strWorkingDir %>/Invoices/Default.asp" target="_top">Invoices</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>Export</span>
	</nav>

	<h1 class="tl-page-title" style="margin-bottom: 20px;">
		<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
			<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
			<polyline points="7 10 12 15 17 10"></polyline>
			<line x1="12" y1="15" x2="12" y2="3"></line>
		</svg>
		Export Invoices to MYOB
	</h1>
	
	<p style="color: #6b7280; margin-bottom: 24px; line-height: 1.5;">Select a date range to export issued invoices to a CSV file formatted for MYOB import. Only invoices with "Issued" status will be included.</p>

	<form action="ExportToMYOB_Proc.asp" method="POST">
		<div class="tl-form-row">
			<div class="tl-form-group">
				<label class="tl-form-label">Date From</label>
				<div style="display: flex; gap: 8px;">
					<input type="text" name="DateFrom" value="<%=dteFrom%>" class="tl-form-input" readonly>
					<a href="javascript:showCal('Calendar1')" class="tl-icon-btn" title="Open Calendar">
						<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
							<line x1="16" y1="2" x2="16" y2="6"></line>
							<line x1="8" y1="2" x2="8" y2="6"></line>
							<line x1="3" y1="10" x2="21" y2="10"></line>
						</svg>
					</a>
				</div>
			</div>
			<div class="tl-form-group">
				<label class="tl-form-label">Date To</label>
				<div style="display: flex; gap: 8px;">
					<input type="text" name="DateTo" value="<%=dteTo%>" class="tl-form-input" readonly>
					<a href="javascript:showCal('Calendar2')" class="tl-icon-btn" title="Open Calendar">
						<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
							<line x1="16" y1="2" x2="16" y2="6"></line>
							<line x1="8" y1="2" x2="8" y2="6"></line>
							<line x1="3" y1="10" x2="21" y2="10"></line>
						</svg>
					</a>
				</div>
			</div>
		</div>
		<div style="margin-top: 24px; text-align: right;">
			<a href="<%= strWorkingDir %>/Invoices/Default.asp" class="tl-btn-secondary" style="margin-right: 12px; background:#f3f4f6; color:#374151; padding:10px 20px; border-radius:8px; font-size:14px; font-weight:500; text-decoration:none; display:inline-flex;">Cancel</a>
			<button type="submit" class="tl-btn-primary">Generate CSV</button>
		</div>
	</form>
</div>
</body>
</html>
