<%
' Techlight MyDesk - Edit API Key

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

' Security check
If Not Request.Cookies("UserSettings")("Manager") Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

Dim isAdmin
isAdmin = (Request.Cookies("UserSettings")("UserTypeId") >= 5)

If Not isAdmin Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

Dim lngAPIKeyId
lngAPIKeyId = CLng(Request("APIKeyId"))

If lngAPIKeyId = 0 Then
	Response.Redirect("Default.asp")
End If

Dim strWorkingDir
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

' Fetch API key details
Dim rsAPIKey, sqlAPIKey
sqlAPIKey = "SELECT * FROM APIKeys WHERE APIKeyId = " & lngAPIKeyId
Set rsAPIKey = dbConn.Execute(sqlAPIKey)

If rsAPIKey.BOF And rsAPIKey.EOF Then
	rsAPIKey.Close
	Set rsAPIKey = Nothing
	MyRedirect("Default.asp?Msg=API+Key+not+found")
End If

Dim strKeyName, strDescription, intIsActive
strKeyName = rsAPIKey("KeyName")
strDescription = rsAPIKey("Description")
intIsActive = rsAPIKey("IsActive")

rsAPIKey.Close
Set rsAPIKey = Nothing
%>
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Edit API Key - Techlight MyDesk</title>
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<style>
		.form-container { max-width: 600px; margin: 0 auto; padding: 24px; }
		.page-header { margin-bottom: 24px; }
		.page-title { font-size: 24px; font-weight: 700; color: var(--tl-dark); margin-bottom: 8px; }
		.page-subtitle { font-size: 14px; color: var(--tl-text-light); }
		.tl-breadcrumb { display: flex; align-items: center; gap: 8px; font-size: 14px; margin-bottom: 16px; }
		.tl-breadcrumb a { color: var(--tl-primary); text-decoration: none; }
		.form-card { background: white; border-radius: 12px; padding: 24px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
		.form-group { margin-bottom: 20px; }
		.form-label { display: block; font-size: 14px; font-weight: 600; color: var(--tl-dark); margin-bottom: 6px; }
		.form-label span { color: #e53e3e; }
		.form-input, .form-textarea { width: 100%; padding: 10px 14px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 14px; font-family: inherit; transition: all 0.2s; }
		.form-input:focus, .form-textarea:focus { outline: none; border-color: var(--tl-primary); box-shadow: 0 0 0 3px rgba(0,168,181,0.1); }
		.form-textarea { min-height: 100px; resize: vertical; }
		.form-hint { font-size: 12px; color: var(--tl-text-muted); margin-top: 4px; }
		.form-actions { display: flex; gap: 12px; justify-content: flex-end; margin-top: 24px; }
		.tl-btn { display: inline-flex; align-items: center; gap: 8px; padding: 10px 20px; border-radius: 8px; font-weight: 600; font-size: 14px; cursor: pointer; border: none; text-decoration: none; transition: all 0.2s; }
		.tl-btn-primary { background: var(--tl-primary); color: white; }
		.tl-btn-primary:hover { background: var(--tl-primary-dark); }
		.tl-btn-secondary { background: #edf2f7; color: var(--tl-text); }
		.tl-btn-secondary:hover { background: #e2e8f0; }
		.checkbox-wrapper { display: flex; align-items: center; gap: 8px; }
		.checkbox-wrapper input[type="checkbox"] { width: 18px; height: 18px; accent-color: var(--tl-primary); }
		.checkbox-wrapper label { font-size: 14px; color: var(--tl-dark); cursor: pointer; }
		.info-box { background: #ebf8ff; border-left: 4px solid #3182ce; padding: 12px 16px; border-radius: 8px; margin-bottom: 20px; font-size: 13px; color: #2c5282; }
	</style>
</head>
<body>
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->

<div class="form-container">
	<nav class="tl-breadcrumb">
		<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="<%= strWorkingDir %>/Setup/Default.asp">Setup</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="Default.asp">API Keys</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>Edit</span>
	</nav>

	<div class="page-header">
		<h1 class="page-title">Edit API Key</h1>
		<p class="page-subtitle">Update API key details and status</p>
	</div>

	<div class="info-box">
		<strong>Note:</strong> You cannot view or edit the actual API key value for security reasons. 
		If you need a new key, please delete this one and create a new API key.
	</div>

	<div class="form-card">
		<form action="Edit_Proc.asp" method="post" id="apiKeyForm">
			<input type="hidden" name="APIKeyId" value="<%= lngAPIKeyId %>">

			<div class="form-group">
				<label class="form-label">Key Name <span>*</span></label>
				<input type="text" name="KeyName" class="form-input" value="<%= strKeyName %>" required maxlength="100">
				<p class="form-hint">A descriptive name to identify this API key</p>
			</div>

			<div class="form-group">
				<label class="form-label">Description</label>
				<textarea name="Description" class="form-textarea" placeholder="Enter a description of what this API key is used for..."><%= strDescription %></textarea>
				<p class="form-hint">Optional description for internal reference</p>
			</div>

			<div class="form-group">
				<div class="checkbox-wrapper">
					<input type="checkbox" name="IsActive" id="IsActive" value="-1" <%= intIsActive ? "checked" : "" %>>
					<label for="IsActive">Active</label>
				</div>
				<p class="form-hint">Inactive keys cannot be used for API access</p>
			</div>

			<div class="form-actions">
				<a href="Default.asp" class="tl-btn tl-btn-secondary">Cancel</a>
				<button type="submit" class="tl-btn tl-btn-primary">
					<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path><polyline points="17 21 17 13 7 13 7 21"></polyline><polyline points="7 3 7 8 15 8"></polyline></svg>
					Save Changes
				</button>
			</div>
		</form>
	</div>
</div>

<!--#include virtual="/System/ssi_dbConn_close.inc"-->

<script>
	document.getElementById('apiKeyForm').addEventListener('submit', function(e) {
		var keyName = document.querySelector('input[name="KeyName"]').value.trim();
		if (!keyName) {
			e.preventDefault();
			alert('Please enter a key name');
			return false;
		}
		return true;
	});
</script>

</body>
</html>
