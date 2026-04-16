<%
' Techlight MyDesk - API Keys Management
' List, add, edit, and delete API keys for system integration

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

' Security check - only admins can access
If Not Request.Cookies("UserSettings")("Manager") Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

Dim isAdmin
isAdmin = (Request.Cookies("UserSettings")("UserTypeId") > 5)

If Not isAdmin Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

Dim strMsg, strWorkingDir
strMsg = Trim(Request("Msg"))
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
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
	<title>API Keys Management - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<style>
		.apikeys-container { max-width: 1200px; margin: 0 auto; padding: 24px; }
		.page-header { margin-bottom: 24px; }
		.page-title { font-size: 24px; font-weight: 700; color: var(--tl-dark); margin-bottom: 8px; }
		.page-subtitle { font-size: 14px; color: var(--tl-text-light); }
		.tl-breadcrumb { display: flex; align-items: center; gap: 8px; font-size: 14px; margin-bottom: 16px; }
		.tl-breadcrumb a { color: var(--tl-primary); text-decoration: none; }
		.tl-breadcrumb a:hover { text-decoration: underline; }
		.tl-breadcrumb span { color: var(--tl-text-muted); }
		.action-bar { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; }
		.tl-btn { display: inline-flex; align-items: center; gap: 8px; padding: 10px 20px; border-radius: 8px; font-weight: 600; font-size: 14px; cursor: pointer; border: none; text-decoration: none; transition: all 0.2s; }
		.tl-btn-primary { background: var(--tl-primary); color: white; }
		.tl-btn-primary:hover { background: var(--tl-primary-dark); }
		.alert { padding: 12px 16px; border-radius: 8px; margin-bottom: 20px; display: flex; align-items: center; gap: 10px; }
		.alert-success { background: #f0fff4; border-left: 4px solid #38a169; color: #22543d; }
		.alert-error { background: #fff5f5; border-left: 4px solid #e53e3e; color: #742a2a; }
		.data-table { width: 100%; background: white; border-radius: 12px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); overflow: hidden; }
		.data-table table { width: 100%; border-collapse: collapse; }
		.data-table th { background: #f7fafc; padding: 12px 16px; text-align: left; font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: 0.5px; color: var(--tl-text-light); border-bottom: 1px solid #e2e8f0; }
		.data-table td { padding: 16px; border-bottom: 1px solid #edf2f7; font-size: 14px; }
		.data-table tr:hover { background: #f7fafc; }
		.data-table tr:last-child td { border-bottom: none; }
		.key-name { font-weight: 600; color: var(--tl-dark); }
		.key-value { font-family: monospace; background: #edf2f7; padding: 4px 8px; border-radius: 4px; font-size: 12px; }
		.status-badge { display: inline-flex; align-items: center; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 600; }
		.status-active { background: #c6f6d5; color: #22543d; }
		.status-inactive { background: #fed7d7; color: #742a2a; }
		.actions { display: flex; gap: 8px; }
		.action-btn { padding: 6px 12px; border-radius: 6px; font-size: 12px; font-weight: 500; text-decoration: none; transition: all 0.2s; }
		.action-btn-edit { background: #ebf8ff; color: #3182ce; }
		.action-btn-edit:hover { background: #bee3f8; }
		.action-btn-delete { background: #fed7d7; color: #e53e3e; }
		.action-btn-delete:hover { background: #fc8181; color: white; }
		.empty-state { text-align: center; padding: 60px 20px; }
		.empty-state svg { width: 64px; height: 64px; color: var(--tl-text-muted); margin-bottom: 16px; }
		.empty-state h3 { font-size: 18px; font-weight: 600; color: var(--tl-dark); margin-bottom: 8px; }
		.empty-state p { color: var(--tl-text-light); font-size: 14px; margin-bottom: 20px; }
		@media (max-width: 768px) {
			.data-table { overflow-x: auto; }
			.data-table table { min-width: 600px; }
		}
	</style>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="apikeys-container">
	<nav class="tl-breadcrumb">
		<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="<%= strWorkingDir %>/Setup/Default.asp">Setup</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>API Keys</span>
	</nav>

	<div class="page-header">
		<h1 class="page-title">API Keys Management</h1>
		<p class="page-subtitle">Manage API keys for external system integrations and API access</p>
	</div>

<% If strMsg <> "" Then %>
	<div class="alert <%= InStr(strMsg, "deleted") > 0 Or InStr(strMsg, "success") > 0 ? "alert-success" : "alert-error" %>">
		<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
		<% If InStr(strMsg, "deleted") > 0 Or InStr(strMsg, "success") > 0 Then %>
			<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline>
		<% Else %>
			<circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line>
		<% End If %>
		</svg>
		<span><%= strMsg %></span>
	</div>
<% End If %>

	<div class="action-bar">
		<div></div>
		<a href="Add.asp" class="tl-btn tl-btn-primary">
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
			Add New API Key
		</a>
	</div>

<%
' Fetch API keys from database
Dim rsAPIKeys, sqlAPIKeys
sqlAPIKeys = "SELECT * FROM APIKeys ORDER BY DateCreated DESC"
Set rsAPIKeys = dbConn.Execute(sqlAPIKeys)

If rsAPIKeys.BOF And rsAPIKeys.EOF Then
%>
	<div class="data-table">
		<div class="empty-state">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4"></path>
			</svg>
			<h3>No API Keys Found</h3>
			<p>Create your first API key to enable external system integrations.</p>
			<a href="Add.asp" class="tl-btn tl-btn-primary">Create API Key</a>
		</div>
	</div>
<% Else %>
	<div class="data-table">
		<table>
			<thead>
				<tr>
					<th>Key Name</th>
					<th>API Key</th>
					<th>Status</th>
					<th>Created</th>
					<th>Last Used</th>
					<th>Actions</th>
				</tr>
			</thead>
			<tbody>
<%
	Do Until rsAPIKeys.EOF
		Dim keyStatus, statusClass
		If rsAPIKeys("IsActive") Then
			keyStatus = "Active"
			statusClass = "status-active"
		Else
			keyStatus = "Inactive"
			statusClass = "status-inactive"
		End If
		
		Dim lastUsed
		If IsNull(rsAPIKeys("LastUsed")) Then
			lastUsed = "Never"
		Else
			lastUsed = FormatDateU(rsAPIKeys("LastUsed"), False)
		End If
%>
				<tr>
					<td>
						<div class="key-name"><%= rsAPIKeys("KeyName") %></div>
						<% If rsAPIKeys("Description") <> "" Then %>
						<small style="color: var(--tl-text-muted);"><%= rsAPIKeys("Description") %></small>
						<% End If %>
					</td>
					<td>
						<span class="key-value"><%= Left(rsAPIKeys("APIKey"), 8) %>...</span>
					</td>
					<td>
						<span class="status-badge <%= statusClass %>"><%= keyStatus %></span>
					</td>
					<td><%= FormatDateU(rsAPIKeys("DateCreated"), False) %></td>
					<td><%= lastUsed %></td>
					<td>
						<div class="actions">
							<a href="Edit.asp?APIKeyId=<%= rsAPIKeys("APIKeyId") %>" class="action-btn action-btn-edit">Edit</a>
							<a href="Del_Proc.asp?APIKeyId=<%= rsAPIKeys("APIKeyId") %>" class="action-btn action-btn-delete" onclick="return confirm('Are you sure you want to delete this API key? This action cannot be undone.');">Delete</a>
						</div>
					</td>
				</tr>
<%
		rsAPIKeys.MoveNext
	Loop
%>
			</tbody>
		</table>
	</div>
<%
End If

rsAPIKeys.Close
Set rsAPIKeys = Nothing
%>
</div>

<!--#include virtual="/System/ssi_dbConn_close.inc"-->
</body>
</html>
