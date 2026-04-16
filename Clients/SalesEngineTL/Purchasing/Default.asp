<% 
' Techlight MyDesk - Modern Purchasing Navigation Hub - Hardened for Stability
On Error Resume Next

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg, strWorkingDir
strMsg = ""
strWorkingDir = ""

' Get message with null check
If Not IsNull(Request("Msg")) Then strMsg = Trim(Request("Msg"))

' Get working directory with fallback
On Error Resume Next
If Not Request.Cookies("ClientSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("ClientSettings")("WorkingDir")) And Request.Cookies("ClientSettings")("WorkingDir") <> "" Then
		strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
	End If
End If
If Err.Number <> 0 Or strWorkingDir = "" Then strWorkingDir = "/Clients/SalesEngineTL"
On Error GoTo 0

' Access check with null checks
Dim hasPurchaseOrdersAccess, hasRFQAccess
hasPurchaseOrdersAccess = False
hasRFQAccess = False

On Error Resume Next
If Not Request.Cookies("DivisionIdsAccess") Is Nothing Then
	If Not IsEmpty(Request.Cookies("DivisionIdsAccess")("PurchaseOrders")) Then
		hasPurchaseOrdersAccess = (Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0")
	End If
	If Not IsEmpty(Request.Cookies("DivisionIdsAccess")("RFQ")) Then
		hasRFQAccess = (Request.Cookies("DivisionIdsAccess")("RFQ") <> "0")
	End If
End If
On Error GoTo 0

If Not (hasPurchaseOrdersAccess Or hasRFQAccess) Then
	Response.Redirect("../Portal/AccessDenied.asp")
	Response.End
End If

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Purchasing - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
</head>
<body>
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->

<div class="tl-page-container">
	<!-- Breadcrumb -->
	<nav class="tl-breadcrumb">
		<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>Purchasing</span>
	</nav>

	<!-- Page Header -->
	<div class="tl-action-bar">
		<h1 class="tl-page-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
				<line x1="3" y1="6" x2="21" y2="6"></line>
				<path d="M16 10a4 4 0 0 1-8 0"></path>
			</svg>
			Purchasing
		</h1>
	</div>

<%
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

	<!-- Navigation Cards -->
	<div class="tl-grid-container" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 24px; padding: 24px;">
		<div class="tl-card" style="padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px; background: white;">
			<h3 style="margin: 0 0 16px 0; font-size: 18px; font-weight: 600; color: #1a1a1a;">
				<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="vertical-align: middle; margin-right: 8px;">
					<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
					<polyline points="14 2 14 8 20 8"></polyline>
					<line x1="16" y1="13" x2="8" y2="13"></line>
					<line x1="16" y1="17" x2="8" y2="17"></line>
				</svg>
				Request For Quote
			</h3>
			<p style="margin: 0 0 16px 0; color: #666;">Manage and track requests for quotes from suppliers.</p>
			<a href="<%= strWorkingDir %>/RFQ/" class="tl-btn-primary" target="_top" style="display: inline-block;">Go to RFQ</a>
		</div>
		<div class="tl-card" style="padding: 24px; border: 1px solid #e0e0e0; border-radius: 8px; background: white;">
			<h3 style="margin: 0 0 16px 0; font-size: 18px; font-weight: 600; color: #1a1a1a;">
				<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="vertical-align: middle; margin-right: 8px;">
					<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
					<line x1="3" y1="6" x2="21" y2="6"></line>
					<path d="M16 10a4 4 0 0 1-8 0"></path>
				</svg>
				Purchase Orders
			</h3>
			<p style="margin: 0 0 16px 0; color: #666;">Create and manage purchase orders for suppliers.</p>
			<a href="<%= strWorkingDir %>/PurchaseOrders/" class="tl-btn-primary" target="_top" style="display: inline-block;">Go to Purchase Orders</a>
		</div>
	</div>
</div>
</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->