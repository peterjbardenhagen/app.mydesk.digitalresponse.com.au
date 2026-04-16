<%
' Techlight MyDesk - View Newly Created API Key
' This page displays the newly generated API key to the user

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

Dim strAPIKey, strMsg
strAPIKey = Trim(Request("APIKey"))
strMsg = Trim(Request("Msg"))

If strAPIKey = "" Then
	Response.Redirect("Default.asp")
End If

Dim strWorkingDir
strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>API Key Created - Techlight MyDesk</title>
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<style>
		.container { max-width: 600px; margin: 0 auto; padding: 24px; }
		.page-header { margin-bottom: 24px; }
		.page-title { font-size: 24px; font-weight: 700; color: var(--tl-dark); margin-bottom: 8px; }
		.page-subtitle { font-size: 14px; color: var(--tl-text-light); }
		.tl-breadcrumb { display: flex; align-items: center; gap: 8px; font-size: 14px; margin-bottom: 16px; }
		.tl-breadcrumb a { color: var(--tl-primary); text-decoration: none; }
		.success-card { background: linear-gradient(135deg, #f0fff4 0%, #ffffff 100%); border: 2px solid #38a169; border-radius: 12px; padding: 32px; text-align: center; margin-bottom: 24px; }
		.success-icon { width: 64px; height: 64px; background: #38a169; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px; }
		.success-icon svg { width: 32px; height: 32px; color: white; }
		.success-title { font-size: 20px; font-weight: 700; color: #22543d; margin-bottom: 8px; }
		.success-message { color: #2f855a; font-size: 14px; }
		.key-card { background: white; border-radius: 12px; padding: 24px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); margin-bottom: 24px; }
		.key-label { font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: var(--tl-text-muted); margin-bottom: 12px; }
		.key-value { background: #1a1f2e; color: #00c4d3; padding: 16px; border-radius: 8px; font-family: monospace; font-size: 14px; word-break: break-all; text-align: center; }
		.warning-box { background: #fffaf0; border-left: 4px solid #dd6b20; padding: 16px; border-radius: 8px; margin-bottom: 24px; }
		.warning-title { font-weight: 600; color: #c05621; display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
		.warning-text { font-size: 13px; color: #7b341e; }
		.actions { display: flex; gap: 12px; justify-content: center; }
		.tl-btn { display: inline-flex; align-items: center; gap: 8px; padding: 12px 24px; border-radius: 8px; font-weight: 600; font-size: 14px; cursor: pointer; border: none; text-decoration: none; transition: all 0.2s; }
		.tl-btn-primary { background: var(--tl-primary); color: white; }
		.tl-btn-primary:hover { background: var(--tl-primary-dark); }
		.tl-btn-secondary { background: #edf2f7; color: var(--tl-text); }
		.tl-btn-secondary:hover { background: #e2e8f0; }
		.copy-btn { background: #38a169; color: white; padding: 8px 16px; border: none; border-radius: 6px; font-size: 13px; font-weight: 600; cursor: pointer; margin-top: 12px; transition: all 0.2s; }
		.copy-btn:hover { background: #2f855a; }
		.copy-btn:active { transform: scale(0.98); }
	</style>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="container">
	<nav class="tl-breadcrumb">
		<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="<%= strWorkingDir %>/Setup/Default.asp">Setup</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<a href="Default.asp">API Keys</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>View Key</span>
	</nav>

	<div class="page-header">
		<h1 class="page-title">API Key Created</h1>
		<p class="page-subtitle">Your new API key has been generated successfully</p>
	</div>

	<div class="success-card">
		<div class="success-icon">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
				<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
				<polyline points="22 4 12 14.01 9 11.01"></polyline>
			</svg>
		</div>
		<h2 class="success-title">Success!</h2>
		<p class="success-message"><%= strMsg %></p>
	</div>

	<div class="key-card">
		<div class="key-label">Your API Key (copy this now)</div>
		<div class="key-value" id="apiKey"><%= strAPIKey %></div>
		<button class="copy-btn" onclick="copyToClipboard()">
			<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="vertical-align: middle; margin-right: 4px;">
				<rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
				<path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
			</svg>
			Copy to Clipboard
		</button>
	</div>

	<div class="warning-box">
		<div class="warning-title">
			<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
				<line x1="12" y1="9" x2="12" y2="13"></line>
				<line x1="12" y1="17" x2="12.01" y2="17"></line>
			</svg>
			Important Security Notice
		</div>
		<p class="warning-text">
			<strong>This is the only time you will see this API key.</strong> Please copy it now and store it securely. 
			For security reasons, we cannot display the full key again. If you lose this key, you will need to generate a new one.
		</p>
	</div>

	<div class="actions">
		<a href="Default.asp" class="tl-btn tl-btn-primary">Back to API Keys</a>
		<a href="Add.asp" class="tl-btn tl-btn-secondary">Create Another Key</a>
	</div>
</div>

<!--#include virtual="/System/ssi_dbConn_close.inc"-->

<script>
	function copyToClipboard() {
		var keyText = document.getElementById('apiKey').textContent;
		
		if (navigator.clipboard && window.isSecureContext) {
			// Use modern clipboard API if available
			navigator.clipboard.writeText(keyText).then(function() {
				showCopySuccess();
			}).catch(function(err) {
				// Fallback
				fallbackCopyTextToClipboard(keyText);
			});
		} else {
			// Fallback for older browsers
			fallbackCopyTextToClipboard(keyText);
		}
	}
	
	function fallbackCopyTextToClipboard(text) {
		var textArea = document.createElement("textarea");
		textArea.value = text;
		textArea.style.position = "fixed";
		textArea.style.left = "-9999px";
		document.body.appendChild(textArea);
		textArea.focus();
		textArea.select();
		
		try {
			var successful = document.execCommand('copy');
			if (successful) {
				showCopySuccess();
			} else {
				alert('Could not copy automatically. Please copy the key manually.');
			}
		} catch (err) {
			alert('Could not copy automatically. Please copy the key manually.');
		}
		
		document.body.removeChild(textArea);
	}
	
	function showCopySuccess() {
		var btn = document.querySelector('.copy-btn');
		var originalText = btn.innerHTML;
		btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="vertical-align: middle; margin-right: 4px;"><polyline points="20 6 9 17 4 12"></polyline></svg> Copied!';
		btn.style.background = '#2f855a';
		
		setTimeout(function() {
			btn.innerHTML = originalText;
			btn.style.background = '#38a169';
		}, 2000);
	}
</script>

</body>
</html>
