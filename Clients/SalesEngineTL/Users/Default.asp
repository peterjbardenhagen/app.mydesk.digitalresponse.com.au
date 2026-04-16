<% 
' Techlight MyDesk - Modern Users List - Hardened for Stability
On Error Resume Next

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg, strWorkingDir, userTypeId, userCode
strMsg = ""
strWorkingDir = ""
userTypeId = ""
userCode = ""

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
Dim hasAccess
hasAccess = False
On Error Resume Next
If Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("UserTypeId")) Then
		userTypeId = Request.Cookies("UserSettings")("UserTypeId")
		If IsNumeric(userTypeId) Then
			hasAccess = (CLng(userTypeId) >= 4)
		End If
	End If
End If
On Error GoTo 0

If Not hasAccess Then
	Response.Redirect("../Portal/AccessDenied.asp")
	Response.End
End If

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

' Get user code with null check
On Error Resume Next
If Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("Code")) Then
		userCode = Request.Cookies("UserSettings")("Code")
	End If
End If
If Err.Number <> 0 Or userCode = "" Then userCode = ""
On Error Resume Next

' Call GetAccessCodesList with error handling
GetAccessCodesList userCode, userTypeId
On Error GoTo 0

%>
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Users - Techlight MyDesk</title>
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
		<span>Users</span>
	</nav>

	<!-- Page Header -->
	<div class="tl-action-bar">
		<h1 class="tl-page-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
				<circle cx="12" cy="7" r="4"></circle>
			</svg>
			Manage Users
		</h1>
		<div class="tl-btn-group">
			<a href="<%= strWorkingDir %>/Users/Add.asp" class="tl-btn-primary" target="_top">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
					<line x1="12" y1="5" x2="12" y2="19"></line>
					<line x1="5" y1="12" x2="19" y2="12"></line>
				</svg>
				Add User
			</a>
		</div>
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

	<!-- Results Grid -->
	<div class="tl-grid-container">
		<table class="tl-data-table">
			<thead>
				<tr>
					<th>Name</th>
					<th>Code</th>
					<th>Division</th>
					<th>Position</th>
					<th>Active</th>
					<th>Manager</th>
					<th>User Type</th>
					<th style="text-align: right;">Actions</th>
				</tr>
			</thead>
			<tbody>
<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT U.*, D.Division, D.DivisionCode FROM Users U INNER JOIN Divisions D On D.DivisionId = U.DivisionId"
sql = sql & " WHERE U.Deleted = 0 AND U.Active = 1 AND U.Code IN (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")"
If Request.Cookies("UserSettings")("UserTypeId") <= 5 Then
	sql = sql & " AND U.UserTypeId <= " & Request.Cookies("UserSettings")("UserTypeId")
End If
sql = sql & " ORDER BY U.Name"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	Do Until rs.EOF
%>
				<tr>
					<td><a href="Edit.asp?UserId=<%= rs("UserId") %>" class="tl-link"><%= rs("Name") %></a></td>
					<td><%= rs("Code") %></td>
					<td><%= rs("DivisionCode") %></td>
					<td><%= rs("Position") %>&nbsp;</td>
					<td><% If rs("Active") Then Response.Write("Yes") Else Response.Write("No") %></td>
					<td><% If CheckIfAdmin(rs("UserId")) Then Response.Write("Yes") Else Response.Write("No") %></td>
					<td><%= rs("UserTypeId") %></td>
					<td style="text-align: right;">
						<a href="Edit.asp?UserId=<%= rs("UserId") %>" class="tl-btn tl-btn-secondary tl-btn-sm">Edit</a>
						<a href="Del_Proc.asp?Code=<%= rs("Code") %>" class="tl-btn tl-btn-secondary tl-btn-sm" onclick="return confirm('Are you sure you want to delete this user?');">Delete</a>
					</td>
				</tr>
<%
		rs.MoveNext
	Loop
Else
%>
				<tr>
					<td colspan="8" style="text-align: center; padding: 40px;">No users found.</td>
				</tr>
<%
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
			</tbody>
		</table>
	</div>
</div>
</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
