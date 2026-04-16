<%@ Language="VBScript" %>
<%
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1

Dim strWorkingDir
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
If strWorkingDir = "" Then strWorkingDir = "/Clients/SalesEngineTL"

If Not Request.Cookies("UserSettings")("LoggedIn") Then
	Response.Redirect("/Default.asp")
End If

Dim strQuery
strQuery = Trim(Request.QueryString("q"))
%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Global Search - Techlight MyDesk</title>
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<style>
		.search-container { max-width: 1000px; margin: 40px auto; padding: 0 24px; }
		.search-header { margin-bottom: 24px; }
		.search-title { font-size: 24px; font-weight: 700; color: var(--dark); margin-bottom: 8px; }
		.search-subtitle { font-size: 14px; color: var(--gray); margin-bottom: 24px; }
		.search-results { display: flex; flex-direction: column; gap: 16px; }
		.search-item {
			background: var(--bg-light); border: 1px solid var(--border); border-radius: var(--radius-md);
			padding: 16px; display: flex; align-items: start; gap: 16px; text-decoration: none;
			transition: all var(--transition-fast);
		}
		.search-item:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); border-color: var(--primary); }
		.search-item-icon {
			width: 40px; height: 40px; border-radius: 8px; display: flex; align-items: center; justify-content: center; flex-shrink: 0;
			background: var(--bg-dark); color: var(--primary);
		}
		.search-item-content { flex: 1; }
		.search-item-title { font-size: 16px; font-weight: 600; color: var(--primary-dark); margin-bottom: 4px; }
		.search-item-desc { font-size: 13px; color: var(--dark); margin-bottom: 6px; }
		.search-item-meta { font-size: 12px; color: var(--gray); display: flex; gap: 12px; }
		.badge { padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 600; background: var(--info-light); color: var(--info); }
		.empty-state { text-align: center; padding: 48px; background: var(--bg-light); border-radius: var(--radius-lg); border: 1px dashed var(--border); }
	</style>
</head>
<body>
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->

<div class="search-container">
	<div class="search-header">
		<h1 class="search-title">Global Search Results</h1>
		<p class="search-subtitle">Searching for "<%= Server.HTMLEncode(strQuery) %>" across all modules</p>
	</div>

	<div class="search-results">
<%
If strQuery <> "" Then
	Dim hasResults
	hasResults = False
	Dim safeQuery
	safeQuery = Replace(strQuery, "'", "''")
	
	' Search Companies
	Dim rsCo
	Set rsCo = Server.CreateObject("ADODB.RecordSet")
	rsCo.Open "SELECT CompanyId, Company, Type, Phone FROM Companies WHERE Company LIKE '%" & safeQuery & "%' ORDER BY Company", dbConn
	Do While Not rsCo.EOF
		hasResults = True
%>
		<a href="<%= strWorkingDir %>/Companies/View.asp?CompanyId=<%= rsCo("CompanyId") %>" class="search-item" target="_parent">
			<div class="search-item-icon">
				<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"></path></svg>
			</div>
			<div class="search-item-content">
				<div class="search-item-title"><%= rsCo("Company") %></div>
				<div class="search-item-desc">Company</div>
				<div class="search-item-meta">
					<span class="badge" style="background:#e0f2fe;color:#0284c7;">Company ID: <%= rsCo("CompanyId") %></span>
					<% If rsCo("Type") <> "" Then %><span>Type: <%= rsCo("Type") %></span><% End If %>
					<% If rsCo("Phone") <> "" Then %><span>Phone: <%= rsCo("Phone") %></span><% End If %>
				</div>
			</div>
		</a>
<%
		rsCo.MoveNext
	Loop
	rsCo.Close
	Set rsCo = Nothing

	' Search Contacts
	Dim rsCt
	Set rsCt = Server.CreateObject("ADODB.RecordSet")
	rsCt.Open "SELECT ContactId, Name, Email, Phone, Company FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Name LIKE '%" & safeQuery & "%' OR Email LIKE '%" & safeQuery & "%' ORDER BY Name", dbConn
	Do While Not rsCt.EOF
		hasResults = True
%>
		<a href="<%= strWorkingDir %>/Contacts/View.asp?ContactId=<%= rsCt("ContactId") %>" class="search-item" target="_parent">
			<div class="search-item-icon">
				<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="7" r="4"></circle><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path></svg>
			</div>
			<div class="search-item-content">
				<div class="search-item-title"><%= rsCt("Name") %></div>
				<div class="search-item-desc"><%= rsCt("Company") %></div>
				<div class="search-item-meta">
					<span class="badge" style="background:#dcfce7;color:#166534;">Contact ID: <%= rsCt("ContactId") %></span>
					<% If rsCt("Email") <> "" Then %><span><%= rsCt("Email") %></span><% End If %>
					<% If rsCt("Phone") <> "" Then %><span><%= rsCt("Phone") %></span><% End If %>
				</div>
			</div>
		</a>
<%
		rsCt.MoveNext
	Loop
	rsCt.Close
	Set rsCt = Nothing
	
	' Search Quotes
	If IsNumeric(strQuery) Then
		Dim rsQq
		Set rsQq = Server.CreateObject("ADODB.RecordSet")
		rsQq.Open "SELECT Qid, Project, CompanyName, QuoteStatus, QuoteDate FROM Quotes_WithCustomersAndSuppliers WHERE Qid = " & CLng(strQuery), dbConn
		If Not rsQq.EOF Then
			hasResults = True
%>
		<a href="<%= strWorkingDir %>/Quotes/View.asp?Qid=<%= rsQq("Qid") %>" class="search-item" target="_parent">
			<div class="search-item-icon">
				<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path><polyline points="14 2 14 8 20 8"></polyline></svg>
			</div>
			<div class="search-item-content">
				<div class="search-item-title"><%= rsQq("Project") %></div>
				<div class="search-item-desc"><%= rsQq("CompanyName") %></div>
				<div class="search-item-meta">
					<span class="badge" style="background:#fef08a;color:#854d0e;">Quote: <%= rsQq("Qid") %></span>
					<span>Status: <%= rsQq("QuoteStatus") %></span>
					<span>Date: <%= FormatDateU(rsQq("QuoteDate"), False) %></span>
				</div>
			</div>
		</a>
<%
		End If
		rsQq.Close
		Set rsQq = Nothing
		
		' Search Invoices
		Dim rsInv
		Set rsInv = Server.CreateObject("ADODB.RecordSet")
		rsInv.Open "SELECT Invoices.InvoiceId, Invoices.Qid, Invoices.InvCompany, InvoiceStatus.InvoiceStatus FROM Invoices LEFT JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId WHERE InvoiceId = " & CLng(strQuery) & " OR Qid = " & CLng(strQuery), dbConn
		If Not rsInv.EOF Then
			hasResults = True
%>
		<a href="<%= strWorkingDir %>/Invoices/View.asp?InvoiceId=<%= rsInv("InvoiceId") %>" class="search-item" target="_parent">
			<div class="search-item-icon">
				<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect><line x1="3" y1="9" x2="21" y2="9"></line></svg>
			</div>
			<div class="search-item-content">
				<div class="search-item-title">Invoice #<%= rsInv("InvoiceId") %></div>
				<div class="search-item-desc"><%= rsInv("InvCompany") %></div>
				<div class="search-item-meta">
					<span class="badge" style="background:#fbcfe8;color:#9d174d;">Invoice: <%= rsInv("InvoiceId") %></span>
					<% If rsInv("Qid") > 0 Then %><span>Quote: <%= rsInv("Qid") %></span><% End If %>
					<span>Status: <%= rsInv("InvoiceStatus") %></span>
				</div>
			</div>
		</a>
<%
		End If
		rsInv.Close
		Set rsInv = Nothing
		
		' Search Purchase Orders
		Dim rsPo
		Set rsPo = Server.CreateObject("ADODB.RecordSet")
		rsPo.Open "SELECT PO.POid, PO.PODate, Contacts_WithCustomersAndSuppliers_V2.CompanyName, PurchaseOrderStatus.POStatus FROM PurchaseOrders AS PO LEFT JOIN PurchaseOrderStatus ON PO.POStatusId = PurchaseOrderStatus.POStatusId LEFT JOIN Contacts_WithCustomersAndSuppliers_V2 ON PO.ContactId = Contacts_WithCustomersAndSuppliers_V2.ContactId WHERE PO.POid = " & CLng(strQuery), dbConn
		If Not rsPo.EOF Then
			hasResults = True
%>
		<a href="<%= strWorkingDir %>/PurchaseOrders/View.asp?POid=<%= rsPo("POid") %>" class="search-item" target="_parent">
			<div class="search-item-icon">
				<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path><line x1="3" y1="6" x2="21" y2="6"></line></svg>
			</div>
			<div class="search-item-content">
				<div class="search-item-title">Purchase Order #<%= rsPo("POid") %></div>
				<div class="search-item-desc"><%= rsPo("CompanyName") %></div>
				<div class="search-item-meta">
					<span class="badge" style="background:#e9d5ff;color:#6b21a8;">PO: <%= rsPo("POid") %></span>
					<span>Status: <%= rsPo("POStatus") %></span>
					<span>Date: <%= FormatDateU(rsPo("PODate"), False) %></span>
				</div>
			</div>
		</a>
<%
		End If
		rsPo.Close
		Set rsPo = Nothing
	End If

	If Not hasResults Then
%>
		<div class="empty-state">
			<svg viewBox="0 0 24 24" width="48" height="48" fill="none" stroke="currentColor" stroke-width="1.5" style="color:var(--gray-light);margin-bottom:16px;">
				<circle cx="11" cy="11" r="8"></circle>
				<line x1="21" y1="21" x2="16.65" y2="16.65"></line>
			</svg>
			<h3 style="font-size:18px;color:var(--dark);margin-bottom:8px;">No results found</h3>
			<p style="color:var(--gray);font-size:14px;">We couldn't find anything matching "<%= Server.HTMLEncode(strQuery) %>". Try a different ID or keyword.</p>
		</div>
<%
	End If
Else
%>
		<p>Please enter a search term.</p>
<%
End If
%>
	</div>
</div>

<!--#include virtual="/System/ssi_dbConn_close.inc"-->
</body>
</html>
