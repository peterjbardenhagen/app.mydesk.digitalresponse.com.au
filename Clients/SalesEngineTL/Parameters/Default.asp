<%
Option Explicit

Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expiresabsolute = ServerToEST(Now()) - 1 
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
		<link rel="preconnect" href="https://fonts.googleapis.com">
		<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
		<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
		<!--<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>-->
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
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
			if (emptyField(document.Form1.UploadFrom)) {
				alert("Please complete the Upload From Date field.");
				validFlag = false;
				document.Form1.UploadFrom.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.MinimumValue)) {
				alert("Please complete the Minimum Value field.");
				validFlag = false;
				document.Form1.MinimumValue.focus();
			}}

		return validFlag 
		}

		</script>
	</head>
	<body bgcolor="#dddddd">
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="/Portal.asp">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>System Parameters</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">System Parameters</h1>
		</div>

		<div class="tl-main">
<%
If Request.Cookies("UserSettings")("Manager") Then
%>
			<div class="tl-card">
				<form action="Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" class="tl-form">
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Upload From Date <span class="tl-required">*</span></label>
							<div style="display: flex; align-items: center; gap: 8px;">
								<input type="text" value="<%= FormatDateU(rs("UploadFrom"), False) %>" name="UploadFrom" readonly class="tl-input" style="flex: 1;">
								<button type="button" class="tl-btn-icon" onclick="showCal('Calendar6')" title="Select Date">
									<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>
								</button>
							</div>
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Minimum Value ($) <span class="tl-required">*</span></label>
							<input type="text" name="MinimumValue" class="tl-input" value="<%= rs("MinimumValue") %>" placeholder="e.g. 100.00">
						</div>
					</div>

					<div class="tl-form-actions" style="border-top: 1px solid var(--tl-border); padding-top: 24px; margin-top: 24px; display: flex; justify-content: flex-end; gap: 12px;">
						<button type="button" class="tl-btn" onclick="document.location.href='default.asp';">Cancel</button>
						<button type="submit" class="tl-btn tl-btn-primary">Save Parameters</button>
					</div>
				</form>
			</div>
<%
End If
%>
		</div>
	</div>
	</body>
</html>
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->