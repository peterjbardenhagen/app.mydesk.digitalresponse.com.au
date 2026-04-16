<%
Option Explicit

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
		<link rel="preconnect" href="https://fonts.googleapis.com">
		<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
		<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
		<!--<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>-->
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
			if (emptyField(document.Form1.UserRole)) {
				alert("Please complete the User Role field.");
				validFlag = false;
				document.Form1.UserRole.focus();
			}}
			if (validFlag) {
				if (emptyField(document.Form1.POApprovalLimit)) {
					alert("Please complete the PO Approval Limit field.");
					validFlag = false;
					document.Form1.POApprovalLimit.focus();
				} else {
					if (isNaN(document.Form1.POApprovalLimit.value)) {
						alert("PO Approval Limit is not numeric. Please try again.");
						validFlag = false;
						document.Form1.POApprovalLimit.focus();
					}
				}
			}
			if (validFlag) {
				if (emptyField(document.Form1.POCapExApprovalLimit)) {
					alert("Please complete the PO Approval Cap Ex Limit field.");
					validFlag = false;
					document.Form1.POCapExApprovalLimit.focus();
				} else {
					if (isNaN(document.Form1.POCapExApprovalLimit.value)) {
						alert("PO Approval Limit is not numeric. Please try again.");
						validFlag = false;
						document.Form1.POCapExApprovalLimit.focus();
					}
				}
			}
			if (validFlag) {
				if (emptyField(document.Form1.QuoteApprovalLimit)) {
					alert("Please complete the Quote Approval Limit field.");
					validFlag = false;
					document.Form1.QuoteApprovalLimit.focus();
				} else {
					if (isNaN(document.Form1.QuoteApprovalLimit.value)) {
						alert("Quote Approval Limit is not numeric. Please try again.");
						validFlag = false;
						document.Form1.QuoteApprovalLimit.focus();
					}
				}
			}			
		return validFlag 
		}

		</script>
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="/Portal.asp">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="Default.asp">User Roles</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Add User Role</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Add User Role</h1>
		</div>

		<div class="tl-main">
			<div class="tl-card">
				<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" class="tl-form">
					<div class="tl-form-group">
						<label class="tl-label">Role Name <span class="tl-required">*</span></label>
						<input type="text" name="UserRole" class="tl-input" placeholder="e.g. Senior Manager">
					</div>

					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">PO Approval Limit ($) <span class="tl-required">*</span></label>
							<input type="text" name="POApprovalLimit" class="tl-input" placeholder="e.g. 5000.00">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">CapEx Approval Limit ($) <span class="tl-required">*</span></label>
							<input type="text" name="POCapExApprovalLimit" class="tl-input" placeholder="e.g. 10000.00">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Quote Approval Limit ($) <span class="tl-required">*</span></label>
							<input type="text" name="QuoteApprovalLimit" class="tl-input" placeholder="e.g. 20000.00">
						</div>
					</div>

					<div class="tl-form-actions" style="border-top: 1px solid var(--tl-border); padding-top: 24px; margin-top: 24px; display: flex; justify-content: flex-end; gap: 12px;">
						<button type="button" class="tl-btn" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">Cancel</button>
						<button type="submit" class="tl-btn tl-btn-primary">Create User Role</button>
					</div>
				</form>
			</div>
		</div>
	</div>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->