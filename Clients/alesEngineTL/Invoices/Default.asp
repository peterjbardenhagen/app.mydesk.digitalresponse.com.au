<% 
' Techlight MyDesk - Modern Invoices List
Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strSort, strFilter_Code, intDivisionId, strCode

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
	If strCode = "" Then strCode = "All"
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

If Request.Cookies("UserSettings")("Manager") Then
	strFilter_Code = Trim(Request("Filter_Code"))
	If strFilter_Code = "" Then strFilter_Code = "All"
Else
	strFilter_Code = "All"
End If

dteDateFrom = FormatDateU(DateAdd("M", -3, ServerToEST(Now())), False)
dteDateTo = FormatDateU(DateAdd("D", 1, ServerToEST(Now())), False)
intDivisionId = Request("DivisionId")

If IsNumeric(intDivisionId) Then
	intDivisionId = CInt(intDivisionId)
Else
	intDivisionId = Request.Cookies("DivisionId")
End If

intSelDivisionId = 555
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
	<title>Invoices - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css">
	<script language="javascript" src="/System/cal2.js"></script>
	<script language="javascript" src="/System/cal_conf2.js"></script>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="tl-page-container">
	<!-- Breadcrumb -->
	<nav class="tl-breadcrumb">
		<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Dashboard.asp" target="_parent">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>Invoices</span>
	</nav>

	<!-- Page Header -->
	<div class="tl-action-bar">
		<h1 class="tl-page-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
				<line x1="3" y1="9" x2="21" y2="9"></line>
				<line x1="9" y1="21" x2="9" y2="9"></line>
			</svg>
			Invoices
		</h1>
		<div class="tl-btn-group">
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Invoices/Add.asp" class="tl-btn-primary" target="_parent">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
					<line x1="12" y1="5" x2="12" y2="19"></line>
					<line x1="5" y1="12" x2="19" y2="12"></line>
				</svg>
				New Invoice
			</a>
		</div>
	</div>

<%
strMsg = Trim(Request("Msg"))
If strMsg <> "" Then
%>
	<div class="tl-alert tl-alert-success">
		<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
			<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
			<polyline points="22 4 12 14.01 9 11.01"></polyline>
		</svg>
		<%= strMsg %>
	</div>
<%
End If
%>

	<!-- Compact Filter Panel -->
	<div class="tl-filter-compact">
		<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/SalesProjects/Report.asp" target="MyIFrame">
			<div class="tl-filter-compact-inner">
				<div class="tl-filter-field tl-filter-field-narrow">
					<label>Date From</label>
					<div style="display: flex; gap: 4px;">
						<input type="text" value="<%= dteDateFrom %>" name="DateFrom" class="tl-form-input" readonly style="flex: 1;">
						<a href="javascript:showCal('Calendar3')" class="tl-icon-btn" title="Calendar" style="padding: 6px;">
							<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
								<line x1="16" y1="2" x2="16" y2="6"></line>
								<line x1="8" y1="2" x2="8" y2="6"></line>
								<line x1="3" y1="10" x2="21" y2="10"></line>
							</svg>
						</a>
					</div>
				</div>
				<div class="tl-filter-field tl-filter-field-narrow">
					<label>Date To</label>
					<div style="display: flex; gap: 4px;">
						<input type="text" value="<%= dteDateTo %>" name="DateTo" class="tl-form-input" readonly style="flex: 1;">
						<a href="javascript:showCal('Calendar4')" class="tl-icon-btn" title="Calendar" style="padding: 6px;">
							<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
								<line x1="16" y1="2" x2="16" y2="6"></line>
								<line x1="8" y1="2" x2="8" y2="6"></line>
								<line x1="3" y1="10" x2="21" y2="10"></line>
							</svg>
						</a>
					</div>
				</div>
				<div class="tl-filter-field">
					<label>User</label>
					<select name="Code" class="tl-form-select">
						<option value="All">All users</option>
						<option value="<%= Request.Cookies("UserSettings")("Code") %>" selected><%= Request.Cookies("UserSettings")("Name") %></option>
					</select>
				</div>
				<div class="tl-filter-field">
					<label>Customer</label>
					<select name="CompanyId" class="tl-form-select">
						<option value="0">All companies</option>
					</select>
				</div>
				<div class="tl-filter-field">
					<label>Status</label>
					<select name="Status" class="tl-form-select">
						<option value="">All (Active & Complete)</option>
						<option value="ISSUED">Issued</option>
						<option value="PAID">Paid</option>
						<option value="OVERDUE">Overdue</option>
					</select>
				</div>
				<div class="tl-filter-actions">
					<button type="button" onclick="document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Quotes/'" class="tl-filter-btn tl-filter-btn-secondary">
						Quotes
					</button>
					<button type="button" onclick="if(document.FormReport.DivisionId.value == 555){alert('Please select a division');}else{FormReport.action='Report.asp';FormReport.target='MyIFrame';FormReport.submit();}" class="tl-filter-btn tl-filter-btn-secondary">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
							<polyline points="7 10 12 15 17 10"></polyline>
							<line x1="12" y1="15" x2="12" y2="3"></line>
						</svg>
						Report
					</button>
					<button type="submit" onclick="FormReport.action='IFrame.asp';FormReport.target='MyIFrame';" class="tl-filter-btn tl-filter-btn-primary">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<circle cx="11" cy="11" r="8"></circle>
							<line x1="21" y1="21" x2="16.65" y2="16.65"></line>
						</svg>
						Filter
					</button>
				</div>
			</div>
		</form>
	</div>

	<!-- Results -->
	<div class="tl-panel" style="padding: 0; overflow: hidden;">
		<iframe src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strCode %>&Company=All&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&DivisionId=<%= intSelDivisionId %>" 
				name="MyIFrame" id="MyIFrame" 
				style="width:100%;height:550px;border:none;"></iframe>
	</div>
</div>

</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
