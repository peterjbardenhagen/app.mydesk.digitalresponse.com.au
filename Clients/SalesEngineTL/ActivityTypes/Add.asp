<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

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
	<title>Add Activity Type - Techlight MyDesk</title>
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css">
	<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		function checkForm() {

			var validFlag = true
			
			if (validFlag) {
			if (emptyField(document.Form1.ActivityCode)) {
				alert("Please complete the Activity Code field.");
				validFlag = false;
				document.Form1.ActivityCode.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.ActivityType)) {
				alert("Please complete the Activity Type field.");
				validFlag = false;
				document.Form1.ActivityType.focus();
			}}

		return validFlag 
		}

		</script>
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Dashboard.asp" target="_top">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="Default.asp">Activity Types</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Add Activity Type</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Add Activity Type</h1>
		</div>

		<div class="tl-main">
			<div class="tl-card">
				<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" class="tl-form">
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Activity Type <span class="tl-required">*</span></label>
							<input type="text" name="ActivityType" id="Text1" class="tl-input" placeholder="e.g. On-site Support">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Activity Code <span class="tl-required">*</span></label>
							<input type="text" name="ActivityCode" id="ActivityCode" class="tl-input" placeholder="e.g. OSS01">
						</div>
					</div>

					<div class="tl-form-group">
						<label class="tl-label">Configuration</label>
						<div class="tl-checkbox-group" style="padding: 16px; background: #f8fafc; border-radius: 8px; border: 1px solid var(--tl-border);">
							<label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
								<input type="checkbox" name="FormRequired" value="-1" style="width: 18px; height: 18px;" checked>
								<span>Form Submission Required</span>
							</label>
							<p style="font-size: 12px; color: var(--tl-text-light); margin-top: 4px; margin-left: 26px;">Specifies if a detailed activity form must be completed when using this type.</p>
						</div>
					</div>

					<div class="tl-form-actions" style="border-top: 1px solid var(--tl-border); padding-top: 24px; margin-top: 24px; display: flex; justify-content: flex-end; gap: 12px;">
						<button type="button" class="tl-btn" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">Cancel</button>
						<button type="submit" class="tl-btn tl-btn-primary">Create Activity Type</button>
					</div>
				</form>
			</div>
		</div>
	</div>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->