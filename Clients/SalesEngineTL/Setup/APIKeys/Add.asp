<%
' Techlight MyDesk - Add New API Key

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
isAdmin = (Request.Cookies("UserSettings")("UserTypeId") > 5)

If Not isAdmin Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

Dim strWorkingDir
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Add API Key - Techlight MyDesk</title>
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
		.key-preview { background: #f7fafc; padding: 12px; border-radius: 8px; font-family: monospace; font-size: 12px; word-break: break-all; border: 1px dashed #cbd5e0; }
		.checkbox-wrapper { display: flex; align-items: center; gap: 8px; }
		.checkbox-wrapper input[type="checkbox"] { width: 18px; height: 18px; accent-color: var(--tl-primary); }
		.checkbox-wrapper label { font-size: 14px; color: var(--tl-dark); cursor: pointer; }
	</style>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="form-container">
	<nav class="tl-breadcrumb">
		<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="<%= strWorkingDir %>/Setup/Default.asp">Setup</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="Default.asp">API Keys</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>Add New</span>
	</nav>

	<div class="page-header">
		<h1 class="page-title">Add New API Key</h1>
		<p class="page-subtitle">Create a new API key for external system integration</p>
	</div>

	<div class="form-card">
		<form action="Add_Proc.asp" method="post" id="apiKeyForm">
			<div class="form-group">
				<label class="form-label">Key Name <span>*</span></label>
				<input type="text" name="KeyName" class="form-input" placeholder="e.g., Production API Key" required maxlength="100">
				<p class="form-hint">A descriptive name to identify this API key</p>
			</div>

			<div class="form-group">
				<label class="form-label">Description</label>
				<textarea name="Description" class="form-textarea" placeholder="Enter a description of what this API key will be used for..."></textarea>
				<p class="form-hint">Optional description for internal reference</p>
			</div>

			<div class="form-group">
				<div class="checkbox-wrapper">
					<input type="checkbox" name="IsActive" id="IsActive" value="-1" checked>
					<label for="IsActive">Active</label>
				</div>
				<p class="form-hint">Inactive keys cannot be used for API access</p>
			</div>

			<div class="form-actions">
				<a href="Default.asp" class="tl-btn tl-btn-secondary">Cancel</a>
				<button type="submit" class="tl-btn tl-btn-primary">
					<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
					Create API Key
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
