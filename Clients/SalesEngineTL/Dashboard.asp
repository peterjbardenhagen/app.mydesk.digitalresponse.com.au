<%

On Error Resume Next

Dim strMsg
strMsg = Request("Msg")

' Techlight MyDesk - Modern Dashboard
' Check authentication
If Not Session("LoggedIn") Then
	If Request.Cookies("LoggedIn") <> "" Then
		If Not CBool(Request.Cookies("LoggedIn")) Then
			Response.Redirect("DefaultFrame.asp")
		End If
	Else
		Response.Redirect("DefaultFrame.asp")
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

' Get business metrics for Directors
Dim isDirector, currentMonth, currentYear, lastYear
currentMonth = Month(Date())
currentYear = Year(Date())
lastYear = currentYear - 1

Dim userTypeId
userTypeId = Request.Cookies("UserSettings")("UserTypeID") & ""
isDirector = (userTypeId = "1")

' Initialize metrics
Dim thisMonthQuotes, thisMonthQuotesWon, thisMonthQuotesValue
Dim lastMonthQuotes, lastMonthQuotesWon, lastMonthQuotesValue
Dim thisMonthInvoices, thisMonthInvoiceValue
Dim lastMonthInvoices, lastMonthInvoiceValue
Dim ytdQuotesWon, ytdQuotesValue, ytdInvoices, ytdInvoiceValue
Dim lastYearYTDQuotesWon, lastYearYTDQuotesValue, lastYearYTDInvoices, lastYearYTDInvoiceValue
Dim pendingQuotesOver30Days, invoicesOverdue, pendingApprovalPOs

If isDirector Then
	Dim sql, rs
	
	' This month's quotes
	sql = "SELECT COUNT(*) as cnt, SUM(CASE WHEN QuoteStatusId = 2 THEN 1 ELSE 0 END) as won, SUM(NettPriceTotal) as val FROM Quotes WHERE Month(QuoteDate) = " & currentMonth & " AND Year(QuoteDate) = " & currentYear & " AND Deleted = 0"
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		thisMonthQuotes = CLng(rs("cnt"))
		thisMonthQuotesWon = CLng(rs("won"))
		thisMonthQuotesValue = CDbl(rs("val"))
	End If
	rs.Close
	
	' Last month's quotes
	sql = "SELECT COUNT(*) as cnt, SUM(CASE WHEN QuoteStatusId = 2 THEN 1 ELSE 0 END) as won, SUM(NettPriceTotal) as val FROM Quotes WHERE Month(QuoteDate) = " & IIf(currentMonth=1, 12, currentMonth-1) & " AND Year(QuoteDate) = " & IIf(currentMonth=1, currentYear-1, currentYear) & " AND Deleted = 0"
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		lastMonthQuotes = CLng(rs("cnt"))
		lastMonthQuotesWon = CLng(rs("won"))
		lastMonthQuotesValue = CDbl(rs("val"))
	End If
	rs.Close
	
	' This month's invoices
	sql = "SELECT COUNT(*) as cnt, SUM(AmountIncGST) as val FROM Invoices WHERE Month(InvoiceDate) = " & currentMonth & " AND Year(InvoiceDate) = " & currentYear
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		thisMonthInvoices = CLng(rs("cnt"))
		thisMonthInvoiceValue = CDbl(rs("val"))
	End If
	rs.Close
	
	' Last month's invoices
	sql = "SELECT COUNT(*) as cnt, SUM(AmountIncGST) as val FROM Invoices WHERE Month(InvoiceDate) = " & IIf(currentMonth=1, 12, currentMonth-1) & " AND Year(InvoiceDate) = " & IIf(currentMonth=1, currentYear-1, currentYear)
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		lastMonthInvoices = CLng(rs("cnt"))
		lastMonthInvoiceValue = CDbl(rs("val"))
	End If
	rs.Close
	
	' YTD Quotes (Won)
	sql = "SELECT COUNT(*) as cnt, SUM(NettPriceTotal) as val FROM Quotes WHERE QuoteStatusId = 2 AND Year(QuoteDate) = " & currentYear & " AND Deleted = 0"
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		ytdQuotesWon = CLng(rs("cnt"))
		ytdQuotesValue = CDbl(rs("val"))
	End If
	rs.Close
	
	' Last Year YTD Quotes (Won) - same date range last year
	sql = "SELECT COUNT(*) as cnt, SUM(NettPriceTotal) as val FROM Quotes WHERE QuoteStatusId = 2 AND QuoteDate >= DateSerial(" & lastYear & ", 1, 1) AND QuoteDate <= DateSerial(" & lastYear & ", " & currentMonth & ", " & Day(Date()) & ") AND Deleted = 0"
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		lastYearYTDQuotesWon = CLng(rs("cnt"))
		lastYearYTDQuotesValue = CDbl(rs("val"))
	End If
	rs.Close
	
	' Pending quotes over 30 days
	sql = "SELECT COUNT(*) as cnt FROM Quotes WHERE QuoteStatusId = 1 AND QuoteDate < DateAdd('d', -30, Now()) AND Deleted = 0"
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		pendingQuotesOver30Days = CLng(rs("cnt"))
	End If
	rs.Close
	
	' Overdue invoices
	sql = "SELECT COUNT(*) as cnt FROM Invoices WHERE Status = 'ISSUED' AND DueDate < Now() AND DueDate IS NOT NULL"
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		invoicesOverdue = CLng(rs("cnt"))
	End If
	rs.Close
	
	' Pending approval POs
	sql = "SELECT COUNT(*) as cnt FROM PurchaseOrders WHERE Status IN ('Draft', 'Pending')"
	Set rs = dbConn.Execute(sql)
	If Not rs.EOF Then
		pendingApprovalPOs = CLng(rs("cnt"))
	End If
	rs.Close
	
	Set rs = Nothing
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
	<link rel="icon" type="image/svg+xml" href="/favicon.svg">
	<% If isDirector Then %>
	<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>
	<% End If %>
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
				<a href="https://techlight.com.au" target="_blank" class="tl-meta-item" style="text-decoration: none; color: var(--tl-primary);">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path>
						<polyline points="15 3 21 3 21 9"></polyline>
						<line x1="10" y1="14" x2="21" y2="3"></line>
					</svg>
					Techlight Website
				</a>
				<a href="https://outlook.office365.com/mail/" target="_blank" class="tl-meta-item" style="text-decoration: none; color: var(--tl-primary);">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
						<polyline points="22,6 12,13 2,6"></polyline>
					</svg>
					Techlight Office365
				</a>
			</div>
			
			<!-- Quick Navigation Search -->
			<div class="tl-quick-search">
				<span class="tl-quick-search-label">Quick Navigation</span>
				<form action="<%= strWorkingDir %>/QuickNav.asp" method="get" target="_self" style="display: flex; align-items: center; gap: 8px; flex: 1;">
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
			<a href="<%= strWorkingDir %>/Contacts/" class="tl-action-btn" target="_self">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
					<circle cx="9" cy="7" r="4"></circle>
					<path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
					<path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
				</svg>
				<span class="tl-action-label">Contacts</span>
			</a>
			<a href="<%= strWorkingDir %>/Quotes/" class="tl-action-btn">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
					<polyline points="14 2 14 8 20 8"></polyline>
					<line x1="16" y1="13" x2="8" y2="13"></line>
					<line x1="16" y1="17" x2="8" y2="17"></line>
				</svg>
				<span class="tl-action-label">Quotes</span>
			</a>
			<a href="<%= strWorkingDir %>/Invoices/" class="tl-action-btn" target="_self">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
					<line x1="3" y1="9" x2="21" y2="9"></line>
					<line x1="9" y1="21" x2="9" y2="9"></line>
				</svg>
				<span class="tl-action-label">Invoices</span>
			</a>
			<a href="<%= strWorkingDir %>/PurchaseOrders/" class="tl-action-btn" target="_self">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
					<line x1="3" y1="6" x2="21" y2="6"></line>
					<path d="M16 10a4 4 0 0 1-8 0"></path>
				</svg>
				<span class="tl-action-label">Purchase Orders</span>
			</a>
			<a href="<%= strWorkingDir %>/Setup/" class="tl-action-btn" target="_self">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<circle cx="12" cy="12" r="3"></circle>
					<path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"></path>
				</svg>
				<span class="tl-action-label">Setup</span>
			</a>
			<a href="<%= strWorkingDir %>/Users/" class="tl-action-btn" target="_self">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
					<circle cx="12" cy="7" r="4"></circle>
				</svg>
				<span class="tl-action-label">Users</span>
			</a>
			<a href="<%= strWorkingDir %>/Portal/LogOff.asp" class="tl-action-btn" style="border-color: #e74c3c; background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%);" target="_top">
				<svg class="tl-action-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
					<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
					<polyline points="16 17 21 12 16 7"></polyline>
					<line x1="21" y1="12" x2="9" y2="12"></line>
				</svg>
				<span class="tl-action-label">Log Off</span>
			</a>
		</div>

		<% If isDirector Then %>
		<!-- Director Business Analytics -->
		<div class="tl-director-analytics" style="margin-top: 24px;">
			<!-- KPI Cards Row -->
			<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 16px; margin-bottom: 24px;">
				
				<!-- Quotes Won This Month -->
				<div class="tl-kpi-card" style="background: linear-gradient(135deg, #00a8b5 0%, #00c4d3 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(0,168,181,0.3);">
					<div style="display: flex; justify-content: space-between; align-items: start;">
						<div>
							<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">Quotes Won This Month</p>
							<h3 style="font-size: 32px; font-weight: 700; margin: 0;"><%= thisMonthQuotesWon %></h3>
							<p style="font-size: 14px; margin-top: 4px;">$<%= FormatNumber(thisMonthQuotesValue, 2) %></p>
						</div>
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
							<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
							<polyline points="14 2 14 8 20 8"></polyline>
						</svg>
					</div>
					<% If lastMonthQuotesWon > 0 Then %>
					<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
						<% If thisMonthQuotesWon >= lastMonthQuotesWon Then %>
							<span style="color: #90EE90;">&#9650; <%= FormatNumber(((thisMonthQuotesWon-lastMonthQuotesWon)/lastMonthQuotesWon)*100, 1) %>%</span> vs last month
						<% Else %>
							<span style="color: #FFB6C1;">&#9660; <%= FormatNumber(((lastMonthQuotesWon-thisMonthQuotesWon)/lastMonthQuotesWon)*100, 1) %>%</span> vs last month
						<% End If %>
					</div>
					<% End If %>
				</div>

				<!-- Invoices This Month -->
				<div class="tl-kpi-card" style="background: linear-gradient(135deg, #d4a574 0%, #e8c088 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(212,165,116,0.3);">
					<div style="display: flex; justify-content: space-between; align-items: start;">
						<div>
							<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">Invoices This Month</p>
							<h3 style="font-size: 32px; font-weight: 700; margin: 0;"><%= thisMonthInvoices %></h3>
							<p style="font-size: 14px; margin-top: 4px;">$<%= FormatNumber(thisMonthInvoiceValue, 2) %></p>
						</div>
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
							<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
							<line x1="3" y1="9" x2="21" y2="9"></line>
						</svg>
					</div>
					<% If lastMonthInvoices > 0 Then %>
					<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
						<% If thisMonthInvoices >= lastMonthInvoices Then %>
							<span style="color: #90EE90;">&#9650; <%= FormatNumber(((thisMonthInvoices-lastMonthInvoices)/lastMonthInvoices)*100, 1) %>%</span> vs last month
						<% Else %>
							<span style="color: #FFB6C1;">&#9660; <%= FormatNumber(((lastMonthInvoices-thisMonthInvoices)/lastMonthInvoices)*100, 1) %>%</span> vs last month
						<% End If %>
					</div>
					<% End If %>
				</div>

				<!-- YTD Performance -->
				<div class="tl-kpi-card" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(102,126,234,0.3);">
					<div style="display: flex; justify-content: space-between; align-items: start;">
						<div>
							<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">YTD Quotes Won</p>
							<h3 style="font-size: 32px; font-weight: 700; margin: 0;">$<%= FormatNumber(ytdQuotesValue/1000, 1) %>k</h3>
							<p style="font-size: 14px; margin-top: 4px;"><%= ytdQuotesWon %> quotes</p>
						</div>
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
							<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
						</svg>
					</div>
					<% If lastYearYTDQuotesValue > 0 Then %>
					<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
						<% If ytdQuotesValue >= lastYearYTDQuotesValue Then %>
							<span style="color: #90EE90;">&#9650; <%= FormatNumber(((ytdQuotesValue-lastYearYTDQuotesValue)/lastYearYTDQuotesValue)*100, 1) %>%</span> vs last year YTD
						<% Else %>
							<span style="color: #FFB6C1;">&#9660; <%= FormatNumber(((lastYearYTDQuotesValue-ytdQuotesValue)/lastYearYTDQuotesValue)*100, 1) %>%</span> vs last year YTD
						<% End If %>
					</div>
					<% End If %>
				</div>

				<!-- Win Rate -->
				<div class="tl-kpi-card" style="background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(17,153,142,0.3);">
					<div style="display: flex; justify-content: space-between; align-items: start;">
						<div>
							<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">Quote Win Rate</p>
							<h3 style="font-size: 32px; font-weight: 700; margin: 0;">
								<% If thisMonthQuotes > 0 Then %>
									<%= FormatNumber((thisMonthQuotesWon/thisMonthQuotes)*100, 1) %>%
								<% Else %>
									0%
								<% End If %>
							</h3>
							<p style="font-size: 14px; margin-top: 4px;"><%= thisMonthQuotesWon %> / <%= thisMonthQuotes %> quotes</p>
						</div>
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
							<polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline>
							<polyline points="17 6 23 6 23 12"></polyline>
						</svg>
					</div>
					<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
						<% If thisMonthQuotesWon >= lastMonthQuotesWon Then %>
							<span style="color: #90EE90;">&#9650;</span> Trending up
						<% Else %>
							<span style="color: #FFB6C1;">&#9660;</span> Trending down
						<% End If %>
					</div>
				</div>

			</div>

			<!-- Charts Row -->
			<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 20px; margin-bottom: 24px;">
				
				<!-- Monthly Performance Chart -->
				<div class="tl-panel" style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.08);">
					<h3 style="font-size: 16px; font-weight: 600; color: var(--tl-dark); margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
							<line x1="18" y1="20" x2="18" y2="10"></line>
							<line x1="12" y1="20" x2="12" y2="4"></line>
							<line x1="6" y1="20" x2="6" y2="14"></line>
						</svg>
						Monthly Performance vs Last Year
					</h3>
					<canvas id="monthlyChart" height="250"></canvas>
				</div>

				<!-- Revenue Breakdown Chart -->
				<div class="tl-panel" style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.08);">
					<h3 style="font-size: 16px; font-weight: 600; color: var(--tl-dark); margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
							<path d="M21.21 15.89A10 10 0 1 1 8 2.83"></path>
							<path d="M22 12A10 10 0 0 0 12 2v10z"></path>
						</svg>
						Revenue Sources This Month
					</h3>
					<canvas id="revenueChart" height="250"></canvas>
				</div>

			</div>

			<!-- Exceptions & Priority Tasks -->
			<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; margin-bottom: 24px;">
				
				<!-- Priority Alerts -->
				<div class="tl-panel" style="background: linear-gradient(135deg, #fff5f5 0%, #ffffff 100%); border-left: 4px solid #e53e3e; border-radius: 12px; padding: 20px;">
					<h3 style="font-size: 15px; font-weight: 600; color: #c53030; margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
							<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
							<line x1="12" y1="9" x2="12" y2="13"></line>
							<line x1="12" y1="17" x2="12.01" y2="17"></line>
						</svg>
						Exceptions Requiring Attention
					</h3>
					
					<% If pendingQuotesOver30Days > 0 Then %>
					<div style="display: flex; align-items: center; justify-content: space-between; padding: 12px; background: white; border-radius: 8px; margin-bottom: 8px; border: 1px solid #fed7d7;">
						<div style="display: flex; align-items: center; gap: 10px;">
							<div style="width: 32px; height: 32px; background: #feb2b2; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
								<svg viewBox="0 0 24 24" fill="none" stroke="#c53030" stroke-width="2" style="width: 16px; height: 16px;">
									<circle cx="12" cy="12" r="10"></circle>
									<polyline points="12 6 12 12 16 14"></polyline>
								</svg>
							</div>
							<div>
								<p style="font-weight: 600; color: #742a2a; font-size: 13px;"><%= pendingQuotesOver30Days %> Quotes Pending > 30 Days</p>
								<p style="font-size: 11px; color: #9b2c2c;">Require follow-up with customers</p>
							</div>
						</div>
						<a href="<%= strWorkingDir %>/Quotes/" target="_self" style="padding: 6px 12px; background: #c53030; color: white; border-radius: 6px; font-size: 12px; text-decoration: none;">View</a>
					</div>
					<% End If %>

					<% If invoicesOverdue > 0 Then %>
					<div style="display: flex; align-items: center; justify-content: space-between; padding: 12px; background: white; border-radius: 8px; margin-bottom: 8px; border: 1px solid #fed7d7;">
						<div style="display: flex; align-items: center; gap: 10px;">
							<div style="width: 32px; height: 32px; background: #feb2b2; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
								<svg viewBox="0 0 24 24" fill="none" stroke="#c53030" stroke-width="2" style="width: 16px; height: 16px;">
									<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
									<line x1="3" y1="9" x2="21" y2="9"></line>
								</svg>
							</div>
							<div>
								<p style="font-weight: 600; color: #742a2a; font-size: 13px;"><%= invoicesOverdue %> Overdue Invoices</p>
								<p style="font-size: 11px; color: #9b2c2c;">Payment collection required</p>
							</div>
						</div>
						<a href="<%= strWorkingDir %>/Invoices/" target="_self" style="padding: 6px 12px; background: #c53030; color: white; border-radius: 6px; font-size: 12px; text-decoration: none;">View</a>
					</div>
					<% End If %>

					<% If pendingApprovalPOs > 0 Then %>
					<div style="display: flex; align-items: center; justify-content: space-between; padding: 12px; background: white; border-radius: 8px; margin-bottom: 8px; border: 1px solid #fed7d7;">
						<div style="display: flex; align-items: center; gap: 10px;">
							<div style="width: 32px; height: 32px; background: #feb2b2; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
								<svg viewBox="0 0 24 24" fill="none" stroke="#c53030" stroke-width="2" style="width: 16px; height: 16px;">
									<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
								</svg>
							</div>
							<div>
								<p style="font-weight: 600; color: #742a2a; font-size: 13px;"><%= pendingApprovalPOs %> POs Pending Approval</p>
								<p style="font-size: 11px; color: #9b2c2c;">Awaiting director approval</p>
							</div>
						</div>
						<a href="<%= strWorkingDir %>/PurchaseOrders/" target="_self" style="padding: 6px 12px; background: #c53030; color: white; border-radius: 6px; font-size: 12px; text-decoration: none;">View</a>
					</div>
					<% End If %>

					<% If pendingQuotesOver30Days = 0 AND invoicesOverdue = 0 AND pendingApprovalPOs = 0 Then %>
					<div style="text-align: center; padding: 20px; color: #9b2c2c;">
						<svg viewBox="0 0 24 24" fill="none" stroke="#48bb78" stroke-width="2" style="width: 40px; height: 40px; margin-bottom: 8px;">
							<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
							<polyline points="22 4 12 14.01 9 11.01"></polyline>
						</svg>
						<p style="font-size: 14px; color: #38a169;">All caught up! No exceptions.</p>
					</div>
					<% End If %>
				</div>

				<!-- Priority Tasks -->
				<div class="tl-panel" style="background: linear-gradient(135deg, #fffaf0 0%, #ffffff 100%); border-left: 4px solid #d69e2e; border-radius: 12px; padding: 20px;">
					<h3 style="font-size: 15px; font-weight: 600; color: #975a16; margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
						<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
							<path d="M9 11l3 3L22 4"></path>
							<path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"></path>
						</svg>
						Priority Actions
					</h3>
					
					<div style="display: flex; flex-direction: column; gap: 10px;">
						<a href="<%= strWorkingDir %>/Admin/MYOBData.asp" target="_self" style="display: flex; align-items: center; gap: 10px; padding: 12px; background: white; border-radius: 8px; text-decoration: none; border: 1px solid #fbd38d; transition: all 0.2s;">
							<div style="width: 32px; height: 32px; background: #fbd38d; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
								<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
									<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
								</svg>
							</div>
							<div style="flex: 1;">
								<p style="font-weight: 600; color: #744210; font-size: 13px;">Export MYOB Data</p>
								<p style="font-size: 11px; color: #975a16;">Monthly financial export for accounting</p>
							</div>
							<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
								<polyline points="9 18 15 12 9 6"></polyline>
							</svg>
						</a>

						<a href="<%= strWorkingDir %>/Quotes/?status=pending" target="_self" style="display: flex; align-items: center; gap: 10px; padding: 12px; background: white; border-radius: 8px; text-decoration: none; border: 1px solid #fbd38d; transition: all 0.2s;">
							<div style="width: 32px; height: 32px; background: #fbd38d; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
								<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
									<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
								</svg>
							</div>
							<div style="flex: 1;">
								<p style="font-weight: 600; color: #744210; font-size: 13px;">Review Pending Quotes</p>
								<p style="font-size: 11px; color: #975a16;">Follow up on outstanding quotes</p>
							</div>
							<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
								<polyline points="9 18 15 12 9 6"></polyline>
							</svg>
						</a>

						<a href="<%= strWorkingDir %>/Admin/" target="_self" style="display: flex; align-items: center; gap: 10px; padding: 12px; background: white; border-radius: 8px; text-decoration: none; border: 1px solid #fbd38d; transition: all 0.2s;">
							<div style="width: 32px; height: 32px; background: #fbd38d; border-radius: 8px; display: flex; align-items: center; justify-content: center;">
								<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
									<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
								</svg>
							</div>
							<div style="flex: 1;">
								<p style="font-weight: 600; color: #744210; font-size: 13px;">Admin Dashboard</p>
								<p style="font-size: 11px; color: #975a16;">Financial reports and analytics</p>
							</div>
							<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
								<polyline points="9 18 15 12 9 6"></polyline>
							</svg>
						</a>
					</div>
				</div>

			</div>
		</div>
		<% End If %>

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
							<a href="<%= strWorkingDir %>/Contacts/Add.asp" class="tl-menu-link" target="_self">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Contact
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Quotes/Add.asp" class="tl-menu-link" target="_self">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Quote
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/PurchaseOrders/Add.asp" class="tl-menu-link" target="_self">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Purchase Order
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Reports/" class="tl-menu-link" target="_self">
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
						<a href="mailto:info@digitalresponse.com.au" style="display: flex; align-items: center; gap: 12px; padding: 16px; background: var(--tl-bg); border-radius: var(--tl-radius); text-decoration: none; color: var(--tl-text); transition: var(--tl-transition);">
							<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="var(--tl-primary)" stroke-width="2">
								<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
								<polyline points="22,6 12,13 2,6"></polyline>
							</svg>
							<div>
								<div style="font-weight: 600; font-size: 13px;">Email Support</div>
								<div style="font-size: 12px; color: var(--tl-text-light);">info@digitalresponse.com.au</div>
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
<% If isDirector Then %>
<script>
// Chart.js initialization for Director Dashboard
const monthlyCtx = document.getElementById('monthlyChart');
if (monthlyCtx) {
    new Chart(monthlyCtx, {
        type: 'line',
        data: {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
            datasets: [
                {
                    label: '<%= currentYear %> Quotes Won',
                    data: [<%= thisMonthQuotesWon %>, <%= lastMonthQuotesWon %>, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
                    borderColor: '#00a8b5',
                    backgroundColor: 'rgba(0, 168, 181, 0.1)',
                    fill: true,
                    tension: 0.4
                },
                {
                    label: '<%= lastYear %> Quotes Won',
                    data: [<%= lastYearYTDQuotesWon %>, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
                    borderColor: '#d4a574',
                    backgroundColor: 'rgba(212, 165, 116, 0.1)',
                    fill: true,
                    tension: 0.4
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 20
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return '$' + value.toLocaleString();
                        }
                    }
                }
            }
        }
    });
}

const revenueCtx = document.getElementById('revenueChart');
if (revenueCtx) {
    new Chart(revenueCtx, {
        type: 'doughnut',
        data: {
            labels: ['Quotes Won', 'Invoices Issued', 'Pending Quotes', 'Other'],
            datasets: [{
                data: [<%= thisMonthQuotesValue %>, <%= thisMonthInvoiceValue %>, 0, 0],
                backgroundColor: [
                    '#00a8b5',
                    '#d4a574',
                    '#667eea',
                    '#11998e'
                ],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 15
                    }
                }
            },
            cutout: '60%'
        }
    });
}
</script>
<% End If %>
</body>
</html>
