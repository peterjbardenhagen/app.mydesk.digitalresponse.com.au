<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

If Not Request.Cookies("UserSettings")("Manager") Then
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
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="/Portal.asp">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Copy Contacts</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Copy Contacts</h1>
		</div>

		<div class="tl-main">
<%
If strMsg <> "" Then
%>
						<div class="tl-alert tl-alert-info" style="margin-bottom: 20px;">
							<%= strMsg %>
						</div>
<%
End If
%>
						<div class="tl-card" style="max-width: 600px; margin: 0 auto;">
							<div class="tl-card-header">
								<h3 class="tl-card-title">Bulk Copy Contacts</h3>
							</div>
							<form action="Default_Proc.asp" method="post" name="Form1" ID="Form1" style="padding: 24px;">
								<div class="tl-form-group">
									<label class="tl-label">Copy From User</label>
									<select name="CopyFromCode" ID="Select2" class="tl-input">
										<option value="">-- Select Source User --</option>
<%
Set rsUsers = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Order By Name"
Set rsUsers = dbConn.Execute(sql)

If Not(rsUsers.BOF And rsUsers.EOF) Then
	Do Until rsUsers.EOF
%>
										<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
		rsUsers.MoveNext
	Loop
End If
rsUsers.Close
Set rsUsers = Nothing
%>
									</select>
									<p class="tl-help-text">Select the user whose contacts you want to duplicate.</p>
								</div>

								<div class="tl-form-group">
									<label class="tl-label">Copy To User</label>
									<select name="CopyToCode" ID="Select1" class="tl-input">
										<option value="">-- Select Target User --</option>
<%
Set rsUsers = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Order By Name"
Set rsUsers = dbConn.Execute(sql)

If Not(rsUsers.BOF And rsUsers.EOF) Then
	Do Until rsUsers.EOF
%>
										<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
		rsUsers.MoveNext
	Loop
End If
rsUsers.Close
Set rsUsers = Nothing
%>
									</select>
									<p class="tl-help-text">Select the user who will receive the copied contacts.</p>
								</div>

								<div class="tl-alert tl-alert-warning" style="font-size: 13px; margin: 24px 0;">
									<strong>Warning:</strong> This action will duplicate contacts. Ensure you have selected the correct source and target users.
								</div>

								<div class="tl-btn-group" style="justify-content: flex-end; margin-top: 24px;">
									<button type="button" onclick="if(confirm('Are you sure?')){history.back();}" class="tl-btn tl-btn-secondary">Cancel</button>
									<button type="submit" class="tl-btn tl-btn-primary">
										<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline></svg>
										Start Copy Process
									</button>
								</div>
							</form>
						</div>
		</div>
	</div>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->