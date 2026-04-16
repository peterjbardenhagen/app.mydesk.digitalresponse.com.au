<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim intDivisionId
intDivisionId = CLng(Request("DivisionId"))

If Not Request.Cookies("UserSettings")("UserTypeId") = 6 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

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
			if (emptyField(document.Form1.DivisionCode)) {
				alert("Please complete the Division Code field.");
				validFlag = false;
				document.Form1.DivisionCode.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Division)) {
				alert("Please complete the Division field.");
				validFlag = false;
				document.Form1.Division.focus();
			}}

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
			<a href="Default.asp">Divisions</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Edit Division</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Edit Division: <%= rs("Division") %></h1>
		</div>

		<div class="tl-main">
			<div class="tl-card">
				<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Divisions/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" class="tl-form">
					<input type="hidden" value="<%= rs("DivisionId") %>" name="DivisionId">
					
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Division Name <span class="tl-required">*</span></label>
							<input type="text" name="Division" id="Text1" class="tl-input" value="<%= rs("Division") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Division Code <span class="tl-required">*</span></label>
							<input type="text" name="DivisionCode" id="DivisionCode" class="tl-input" value="<%= rs("DivisionCode") %>">
						</div>
					</div>

					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">ACN</label>
							<input type="text" name="ACN" class="tl-input" value="<%= rs("ACN") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">ABN</label>
							<input type="text" name="ABN" class="tl-input" value="<%= rs("ABN") %>">
						</div>
					</div>

					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Minimum Quote Margin (%) <span class="tl-required">*</span></label>
							<div style="display: flex; align-items: center; gap: 8px;">
								<input type="number" name="MinimumMargin" class="tl-input" value="<% If rs("MinimumMargin") <> "" Then Response.Write(FormatNumber(rs("MinimumMargin"),2)) Else Response.Write("40.00") %>" step="0.01" style="width: 120px;">
								<span style="color: var(--tl-text-light);">%</span>
							</div>
							<p style="font-size: 12px; color: var(--tl-text-light); margin-top: 4px;">Approval required if margin falls below this value.</p>
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Company Logo Path</label>
							<input type="text" name="Logo" class="tl-input" value="<%= rs("Logo") %>">
						</div>
					</div>

					<div class="tl-form-group">
						<label class="tl-label">Enabled Features</label>
						<div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 16px; padding: 16px; background: #f8fafc; border-radius: 8px; border: 1px solid var(--tl-border);">
							<label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
								<input type="checkbox" name="Quotes" value="-1" style="width: 18px; height: 18px;" <% If rs("Quotes") Then Response.Write "checked" %>>
								<span>Quotes</span>
							</label>
							<label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
								<input type="checkbox" name="RFQ" value="-1" style="width: 18px; height: 18px;" <% If rs("RFQ") Then Response.Write "checked" %>>
								<span>RFQ</span>
							</label>
							<label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
								<input type="checkbox" name="Prospects" value="-1" style="width: 18px; height: 18px;" <% If rs("Prospects") Then Response.Write "checked" %>>
								<span>Prospects</span>
							</label>
							<label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
								<input type="checkbox" name="PurchaseOrders" value="-1" style="width: 18px; height: 18px;" <% If rs("PurchaseOrders") Then Response.Write "checked" %>>
								<span>Purchase Orders</span>
							</label>
							<label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
								<input type="checkbox" name="PurchaseRequests" value="-1" style="width: 18px; height: 18px;" <% If rs("PurchaseRequests") Then Response.Write "checked" %>>
								<span>Purchase Requests</span>
							</label>
							<label style="display: flex; align-items: center; gap: 8px; cursor: pointer;">
								<input type="checkbox" name="UsersAccessAll" value="-1" style="width: 18px; height: 18px;" <% If rs("UsersAccessAll") Then Response.Write "checked" %>>
								<span>Users Access All</span>
							</label>
						</div>
					</div>

					<div class="tl-form-actions" style="border-top: 1px solid var(--tl-border); padding-top: 24px; margin-top: 24px; display: flex; justify-content: flex-end; gap: 12px;">
						<button type="button" class="tl-btn" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">Cancel</button>
						<button type="submit" class="tl-btn tl-btn-primary">Save Changes</button>
					</div>
				</form>
			</div>
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