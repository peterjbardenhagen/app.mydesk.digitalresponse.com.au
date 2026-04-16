<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim intLocationId
intLocationId = CLng(Request("LocationId"))

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
			if (emptyField(document.Form1.Company)) {
				alert("Please complete the Company field.");
				validFlag = false;
				document.Form1.Company.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Address1)) {
				alert("Please complete the Address 1 field.");
				validFlag = false;
				document.Form1.Address1.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Suburb)) {
				alert("Please complete the Suburb field.");
				validFlag = false;
				document.Form1.Suburb.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.StateId)) {
				alert("Please complete the State field.");
				validFlag = false;
				document.Form1.StateId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.PostCode)) {
				alert("Please complete the Post-Code field.");
				validFlag = false;
				document.Form1.PostCode.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Country)) {
				alert("Please complete the Country field.");
				validFlag = false;
				document.Form1.Country.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POAddress1)) {
				alert("Please complete the Postal Address -  Address 1 field.");
				validFlag = false;
				document.Form1.POAddress1.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POSuburb)) {
				alert("Please complete the Postal Address -  Suburb field.");
				validFlag = false;
				document.Form1.POSuburb.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POStateId)) {
				alert("Please complete the Postal Address -  State field.");
				validFlag = false;
				document.Form1.POStateId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POPostCode)) {
				alert("Please complete the Postal Address -  Post-Code field.");
				validFlag = false;
				document.Form1.POPostCode.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POCountry)) {
				alert("Please complete the Postal Address -  Country field.");
				validFlag = false;
				document.Form1.POCountry.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Phone)) {
				alert("Please complete the Phone field.");
				validFlag = false;
				document.Form1.Phone.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Fax)) {
				alert("Please complete the Fax field.");
				validFlag = false;
				document.Form1.Fax.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Email) && !(document.Form1.Email.value.search(/^\w+((-\w+)|(\.\w+))*\@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z0-9]+$/) != -1)) {
				alert("Please ensure that you have entered a valid email address in the Email field.");
				validFlag = false;
				document.Form1.Email.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Website) && !(document.Form1.Website.value.substring(0,7) == 'http://')) {
				alert("Please ensure that you have entered a valid website beginning with http:// in the Email field.");
				validFlag = false;
				document.Form1.Website.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ExpenseTypeGroupId)) {
				alert("Please selecte Expense Type Group.");
				validFlag = false;
				document.Form1.ExpenseTypeGroupId.focus();
			}}
		return validFlag 
		}

		</script>
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->

<%
Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Locations Inner Join States On States.StateId = Locations.StateId Where LocationId = " & intLocationId
Set rs = dbConn.Execute(sql)
%>

	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="/Portal.asp">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="Default.asp">Locations</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Edit Location</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Edit Location: <%= rs("Company") %></h1>
		</div>

		<div class="tl-main">
			<div class="tl-card">
				<form action="Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" class="tl-form">
					<input type="hidden" value="<%= rs("LocationId") %>" name="LocationId">
					
					<!-- General Details -->
					<div class="tl-card-header" style="padding-left: 0;">
						<h3 class="tl-card-title">General Details</h3>
					</div>
					<div class="tl-form-group">
						<label class="tl-label">Company <span class="tl-required">*</span></label>
						<input type="text" name="Company" class="tl-input" value="<%= rs("Company") %>">
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

					<!-- Office Address -->
					<div class="tl-card-header" style="padding-left: 0; margin-top: 32px;">
						<h3 class="tl-card-title">Office Address</h3>
					</div>
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Address 1 <span class="tl-required">*</span></label>
							<input type="text" name="Address1" class="tl-input" value="<%= rs("Address1") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Address 2</label>
							<input type="text" name="Address2" class="tl-input" value="<%= rs("Address2") %>">
						</div>
					</div>
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Suburb <span class="tl-required">*</span></label>
							<input type="text" name="Suburb" class="tl-input" value="<%= rs("Suburb") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">State <span class="tl-required">*</span></label>
							<select name="StateId" class="tl-input">
<%
Set rsStates = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM States ORDER BY State"
Set rsStates = dbConn.Execute(sql)
Do Until rsStates.EOF
	If CInt(rsStates("StateId")) = CInt(rs("StateId")) Then
		Response.Write ("								<option selected value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
	Else
		Response.Write ("								<option value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
	End If
	rsStates.MoveNext
Loop
rsStates.Close
Set rsStates = Nothing
%>
							</select>
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Post Code <span class="tl-required">*</span></label>
							<input type="text" name="PostCode" class="tl-input" value="<%= rs("PostCode") %>">
						</div>
					</div>
					<div class="tl-form-group">
						<label class="tl-label">Country <span class="tl-required">*</span></label>
						<input type="text" name="Country" class="tl-input" value="<%= rs("Country") %>">
					</div>

					<!-- Postal Address -->
					<div class="tl-card-header" style="padding-left: 0; margin-top: 32px; display: flex; justify-content: space-between; align-items: center;">
						<h3 class="tl-card-title">Postal Address</h3>
						<div style="display: flex; align-items: center; gap: 12px; font-size: 14px;">
							<span>Display on Statements?</span>
							<label style="display: flex; align-items: center; gap: 4px; cursor: pointer;">
								<input type="radio" name="PODisplay" value="-1" <% If rs("PODisplay") Then Response.Write "Checked" %>> Yes
							</label>
							<label style="display: flex; align-items: center; gap: 4px; cursor: pointer;">
								<input type="radio" name="PODisplay" value="0" <% If Not rs("PODisplay") Then Response.Write "Checked" %>> No
							</label>
						</div>
					</div>
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">PO Address 1 <span class="tl-required">*</span></label>
							<input type="text" name="POAddress1" class="tl-input" value="<%= rs("POAddress1") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">PO Address 2</label>
							<input type="text" name="POAddress2" class="tl-input" value="<%= rs("POAddress2") %>">
						</div>
					</div>
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">PO Suburb <span class="tl-required">*</span></label>
							<input type="text" name="POSuburb" class="tl-input" value="<%= rs("POSuburb") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">PO State <span class="tl-required">*</span></label>
							<select name="POStateId" class="tl-input">
<%
Set rsStates = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM States ORDER BY State"
Set rsStates = dbConn.Execute(sql)
Do Until rsStates.EOF
	If CInt(rsStates("StateId")) = CInt(rs("POStateId")) Then
		Response.Write ("								<option selected value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
	Else
		Response.Write ("								<option value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
	End If
	rsStates.MoveNext
Loop
rsStates.Close
Set rsStates = Nothing
%>
							</select>
						</div>
						<div class="tl-form-group">
							<label class="tl-label">PO Post Code <span class="tl-required">*</span></label>
							<input type="text" name="POPostCode" class="tl-input" value="<%= rs("POPostCode") %>">
						</div>
					</div>
					<div class="tl-form-group">
						<label class="tl-label">PO Country <span class="tl-required">*</span></label>
						<input type="text" name="POCountry" class="tl-input" value="<%= rs("POCountry") %>">
					</div>

					<!-- Contact & Other -->
					<div class="tl-card-header" style="padding-left: 0; margin-top: 32px;">
						<h3 class="tl-card-title">Contact & Classification</h3>
					</div>
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Phone <span class="tl-required">*</span></label>
							<input type="text" name="Phone" class="tl-input" value="<%= rs("Phone") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Fax <span class="tl-required">*</span></label>
							<input type="text" name="Fax" class="tl-input" value="<%= rs("Fax") %>">
						</div>
					</div>
					<div class="tl-form-row">
						<div class="tl-form-group">
							<label class="tl-label">Email</label>
							<input type="email" name="Email" class="tl-input" value="<%= rs("Email") %>">
						</div>
						<div class="tl-form-group">
							<label class="tl-label">Website</label>
							<input type="text" name="Website" class="tl-input" value="<%= rs("Website") %>">
						</div>
					</div>
					<div class="tl-form-group">
						<label class="tl-label">Expense Type Group <span class="tl-required">*</span></label>
						<select name="ExpenseTypeGroupId" class="tl-input">
<%
Set rsEx = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM ExpenseTypeGroups ORDER BY ExpenseTypeGroup"
Set rsEx = dbConn.Execute(sql)
Do Until rsEx.EOF
	If rs("ExpenseTypeGroupId") = rsEx("ExpenseTypeGroupId") Then
		Response.Write ("								<option selected value=""" & rsEx("ExpenseTypeGroupId") & """>" & rsEx("ExpenseTypeGroup") & "</option>" & vbNewLine)
	Else
		Response.Write ("								<option value=""" & rsEx("ExpenseTypeGroupId") & """>" & rsEx("ExpenseTypeGroup") & "</option>" & vbNewLine)
	End If
	rsEx.MoveNext
Loop
rsEx.Close
Set rsEx = Nothing
%>
						</select>
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