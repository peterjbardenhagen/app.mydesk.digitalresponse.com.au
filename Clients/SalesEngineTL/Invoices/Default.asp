<% 
' Techlight MyDesk - Modern Invoices List
Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strSort, strFilter_Code, intDivisionId, strCode

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
	If strCode = "" Then strCode = "All"
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

If Request.Cookies("UserSettings")("Manager") Then
	strFilter_Code = Trim(Request("Filter_Code"))
	If strFilter_Code = "" Then strFilter_Code = "All"
Else
	strFilter_Code = "All"
End If

dteDateFrom = FormatDateU(DateAdd("M", -3, ServerToEST(Now())), False)
dteDateTo = FormatDateU(DateAdd("D", 1, ServerToEST(Now())), False)
intDivisionId = Request("DivisionId")

If IsNumeric(intDivisionId) Then
	intDivisionId = CInt(intDivisionId)
Else
	intDivisionId = Request.Cookies("DivisionId")
End If

intSelDivisionId = 555
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
	<title>Invoices - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css">
	<script language="javascript" src="/System/cal2.js"></script>
	<script language="javascript" src="/System/cal_conf2.js"></script>
</head>
<body>
<!--#include virtual="/System/ssi_Header.inc"-->

<div class="tl-page-container">
	<!-- Breadcrumb -->
	<nav class="tl-breadcrumb">
		<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Dashboard.asp" target="_top">Home</a>
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
		<span>Invoices</span>
	</nav>

	<!-- Page Header -->
	<div class="tl-action-bar">
		<h1 class="tl-page-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
				<line x1="3" y1="9" x2="21" y2="9"></line>
				<line x1="9" y1="21" x2="9" y2="9"></line>
			</svg>
			Invoices
		</h1>
		<div class="tl-btn-group">
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Invoices/Add.asp" class="tl-btn-primary" target="_top">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
					<line x1="12" y1="5" x2="12" y2="19"></line>
					<line x1="5" y1="12" x2="19" y2="12"></line>
				</svg>
				New Invoice
			</a>
		</div>
	</div>

<%
strMsg = Trim(Request("Msg"))
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

	<!-- Compact Filter Panel -->
	<div class="tl-filter-compact">
		<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Invoices/IFrame.asp" target="MyIFrame">
			<div class="tl-filter-compact-inner">
				<div class="tl-filter-field tl-filter-field-narrow">
					<label>Date From</label>
					<div style="display: flex; gap: 4px;">
						<input type="text" value="<%= dteDateFrom %>" name="DateFrom" class="tl-form-input" readonly style="flex: 1;">
						<a href="javascript:showCal('Calendar3')" class="tl-icon-btn" title="Calendar" style="padding: 6px;">
							<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
								<line x1="16" y1="2" x2="16" y2="6"></line>
								<line x1="8" y1="2" x2="8" y2="6"></line>
								<line x1="3" y1="10" x2="21" y2="10"></line>
							</svg>
						</a>
					</div>
				</div>
				<div class="tl-filter-field tl-filter-field-narrow">
					<label>Date To</label>
					<div style="display: flex; gap: 4px;">
						<input type="text" value="<%= dteDateTo %>" name="DateTo" class="tl-form-input" readonly style="flex: 1;">
						<a href="javascript:showCal('Calendar4')" class="tl-icon-btn" title="Calendar" style="padding: 6px;">
							<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
								<line x1="16" y1="2" x2="16" y2="6"></line>
								<line x1="8" y1="2" x2="8" y2="6"></line>
								<line x1="3" y1="10" x2="21" y2="10"></line>
							</svg>
						</a>
					</div>
				</div>
				<div class="tl-filter-field">
					<label>User</label>
					<select name="Code" class="tl-form-select">
						<option value="All">All users</option>
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If rsUsers("Code") = strFilter_Code Then
%>
						<option selected value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
			Else
%>
						<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
			End If	
			rsUsers.MoveNext
		Loop
	End If

	rsUsers.Close
	Set rsUsers = Nothing
%>
					</select>
				</div>
				<div class="tl-filter-field">
					<label>Customer</label>
<%
Set rsCompany = Server.CreateObject("ADODB.RecordSet")
sql = "Select DistinctRow Companies.CompanyId, Companies.Company From Contacts Inner Join Companies On Companies.CompanyId = Contacts.CompanyId Where Companies.CompanyId <> 142 And (Companies.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Quotes") & ") Or Contacts.Code = '" & Request.Cookies("UserSettings")("Code") & "') Order By Companies.Company"
Set rsCompany = dbConn.Execute(sql)
%>
					<select name="CompanyId" class="tl-form-select">
						<option value="0">All companies</option>
						<option value="142">Not an account</option>
<%
If Not(rsCompany.BOF And rsCompany.EOF) Then
	Do Until rsCompany.EOF
		Response.Write "<option value=""" & rsCompany("CompanyId") & """>" & rsCompany("Company") & "</option>" & vbcrlf
		rsCompany.MoveNext
	Loop
End If

rsCompany.Close
Set rsCompany = Nothing
%>
					</select>
				</div>
				<div class="tl-filter-field">
					<label>Entity</label>
					<select name="DivisionId" class="tl-form-select">
						<option value="555">Select an Entity</option>
<%
Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE Quotes = True AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Quotes") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If CLng(intDivisionId) = CLng(rsDiv("DivisionId")) Then
			intSelDivisionId = intDivisionId
			Response.Write ("						<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			If CLng(Request.Cookies("DivisionId")) = CLng(rsDiv("DivisionId")) Then
				intSelDivisionId = Request.Cookies("DivisionId")
				Response.Write ("						<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
			Else
				Response.Write ("						<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
			End If
		End If
		rsDiv.MoveNext
	Loop
End If

If IsObject(rsDiv) Then
	rsDiv.Close
	Set rsDiv = Nothing
End If
%>
					</select>
				</div>
				<div class="tl-filter-field">
					<label>Status</label>
					<select name="InvoicestatusId" class="tl-form-select">
						<option value="0">All (Active & Complete)</option>
<%
sql = "Select * From Invoicestatus Order By Invoicestatus"
Set rsStatus = dbConn.Execute(sql)

If Not(rsStatus.BOF And rsStatus.EOF) Then
	Do Until rsStatus.EOF
		Response.Write "<option value=""" & rsStatus("InvoicestatusId") & """>" & rsStatus("Invoicestatus") & "</option>" & vbcrlf
		rsStatus.MoveNext
	Loop
End If

rsStatus.Close
Set rsStatus = Nothing
%>
						<option value="555">All (Active)</option>
					</select>
				</div>
				<div class="tl-filter-actions">
					<button type="button" onclick="goToQuotes()" class="tl-btn-secondary">
						<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
							<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
							<polyline points="14 2 14 8 20 8"></polyline>
						</svg>
						Quotes
					</button>
					<button type="button" onclick="generateReport()" class="tl-btn-secondary">
						<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
							<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
							<polyline points="14 2 14 8 20 8"></polyline>
							<line x1="16" y1="13" x2="8" y2="13"></line>
							<line x1="16" y1="17" x2="8" y2="17"></line>
						</svg>
						Generate Report
					</button>
					<button type="submit" class="tl-btn-primary">
						<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
							<circle cx="11" cy="11" r="8"></circle>
							<line x1="21" y1="21" x2="16.65" y2="16.65"></line>
						</svg>
						Search
					</button>
				</div>
			</div>
		</form>
	</div>

	<script>
		function goToQuotes() {
			var workingDir = '<%= Request.Cookies("ClientSettings")("WorkingDir") %>';
			document.location.href = workingDir + '/Quotes/';
		}
		
		function generateReport() {
			if(document.FormReport.DivisionId.value == 555) {
				alert('Please Select an Entity before generating a report.');
			} else {
				FormReport.action = 'Report.asp';
				FormReport.target = 'MyIFrame';
				FormReport.submit();
			}
		}
	</script>

	<!-- Results Grid -->
	<div class="tl-grid-container">
		<iframe style="width:100%;height:600px;border:none;" scrolling="yes" name="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&Company=All&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&DivisionId=<%= intSelDivisionId %>&InvoicestatusId=0"></iframe>
	</div>
</div>

</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->