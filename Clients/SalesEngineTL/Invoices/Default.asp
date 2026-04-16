<% 
' Techlight MyDesk - Modern Invoices List - Hardened for Stability
On Error Resume Next

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

' Access check with null checks
Dim hasAccess
hasAccess = False
On Error Resume Next
If Not Request.Cookies("DivisionIdsAccess") Is Nothing Then
	If Not IsEmpty(Request.Cookies("DivisionIdsAccess")("Invoices")) Then
		hasAccess = (Request.Cookies("DivisionIdsAccess")("Invoices") <> "0")
	End If
End If
On Error GoTo 0

'If Not hasAccess Then
'	Response.Redirect("../Portal/AccessDenied.asp")
'	Response.End
'End If

Dim strSort, strFilter_Code, intDivisionId, strCode, dteDateFrom, dteDateTo, intSelDivisionId
Dim strWorkingDir

' Get working directory with fallback
strWorkingDir = ""
On Error Resume Next
If Not Request.Cookies("ClientSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("ClientSettings")("WorkingDir")) And Request.Cookies("ClientSettings")("WorkingDir") <> "" Then
		strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
	End If
End If
If Err.Number <> 0 Or strWorkingDir = "" Then strWorkingDir = "/Clients/SalesEngineTL"
On Error GoTo 0

' Get user code with null checks
strCode = ""
On Error Resume Next
If Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("Manager")) Then
		If CBool(Request.Cookies("UserSettings")("Manager")) Then
			strCode = Trim(Request("Code"))
			If strCode = "" Then strCode = "All"
		Else
			If Not IsEmpty(Request.Cookies("UserSettings")("Code")) Then
				strCode = Request.Cookies("UserSettings")("Code")
			End If
		End If
	End If
End If
If Err.Number <> 0 Or strCode = "" Then strCode = "All"
On Error GoTo 0

' Get filter code with null checks
strFilter_Code = ""
On Error Resume Next
If Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("Manager")) Then
		If CBool(Request.Cookies("UserSettings")("Manager")) Then
			strFilter_Code = Trim(Request("Filter_Code"))
			If strFilter_Code = "" Then strFilter_Code = "All"
		Else
			strFilter_Code = "All"
		End If
	Else
		strFilter_Code = "All"
	End If
End If
If Err.Number <> 0 Or strFilter_Code = "" Then strFilter_Code = "All"
On Error GoTo 0

' Date calculations with error handling
On Error Resume Next
dteDateFrom = FormatDateU(DateAdd("M", -3, ServerToEST(Now())), False)
If Err.Number <> 0 Then dteDateFrom = FormatDateU(Now(), False)
On Error Resume Next

dteDateTo = FormatDateU(DateAdd("D", 1, ServerToEST(Now())), False)
If Err.Number <> 0 Then dteDateTo = FormatDateU(Now(), False)
On Error GoTo 0

' Division ID with validation
intDivisionId = 0
On Error Resume Next
intDivisionId = Request("DivisionId")
If IsNumeric(intDivisionId) Then
	intDivisionId = CInt(intDivisionId)
Else
	If Not Request.Cookies("ClientSettings") Is Nothing Then
		If Not IsEmpty(Request.Cookies("DivisionId")) And IsNumeric(Request.Cookies("DivisionId")) Then
			intDivisionId = CInt(Request.Cookies("DivisionId"))
		End If
	End If
End If
If Err.Number <> 0 Then intDivisionId = 0
On Error GoTo 0

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
	<link rel="stylesheet" type="text/css" href="<%= strWorkingDir %>/System/Style_Techlight.css">
	<script language="javascript" src="/System/cal2.js"></script>
	<script language="javascript" src="/System/cal_conf2.js"></script>
</head>
<body>
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->

<div class="tl-page-container">
	<!-- Breadcrumb -->
	<nav class="tl-breadcrumb">
		<a href="<%= strWorkingDir %>/Dashboard.asp" target="_top">Home</a>
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
			<a href="<%= strWorkingDir %>/Invoices/Add.asp" class="tl-btn-primary" target="_top">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
					<line x1="12" y1="5" x2="12" y2="19"></line>
					<line x1="5" y1="12" x2="19" y2="12"></line>
				</svg>
				New Invoice
			</a>
			<a href="<%= strWorkingDir %>/Invoices/ExportToMYOB.asp" class="tl-btn-secondary" style="background:#f3f4f6; color:#374151; padding:10px 20px; border-radius:8px; font-size:14px; font-weight:500; text-decoration:none; display:inline-flex; align-items:center;" target="_top">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
					<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
					<polyline points="7 10 12 15 17 10"></polyline>
					<line x1="12" y1="15" x2="12" y2="3"></line>
				</svg>
				Export to MYOB
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

	<!-- Filter Panel -->
	<div class="tl-filter-panel">
		<div class="tl-filter-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<polygon points="22 3 2 3 10 12.46 10 19 14 21 14 12.46 22 3"></polygon>
			</svg>
			Filter Invoices
		</div>
		<form name="FormReport" id="FormReport" method="post" action="<%= strWorkingDir %>/Invoices/IFrame.asp" target="MyIFrame">
			<div class="tl-form-row">
				<div class="tl-form-group">
					<label class="tl-form-label">Date From</label>
					<div style="display: flex; gap: 8px;">
						<input type="text" value="<%= dteDateFrom %>" name="DateFrom" class="tl-form-input" readonly style="flex: 1;">
						<a href="javascript:showCal('Calendar3')" class="tl-icon-btn" title="Open Calendar">
							<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
								<line x1="16" y1="2" x2="16" y2="6"></line>
								<line x1="8" y1="2" x2="8" y2="6"></line>
								<line x1="3" y1="10" x2="21" y2="10"></line>
							</svg>
						</a>
					</div>
				</div>
				<div class="tl-form-group">
					<label class="tl-form-label">Date To</label>
					<div style="display: flex; gap: 8px;">
						<input type="text" value="<%= dteDateTo %>" name="DateTo" class="tl-form-input" readonly style="flex: 1;">
						<a href="javascript:showCal('Calendar4')" class="tl-icon-btn" title="Open Calendar">
							<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
								<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
								<line x1="16" y1="2" x2="16" y2="6"></line>
								<line x1="8" y1="2" x2="8" y2="6"></line>
								<line x1="3" y1="10" x2="21" y2="10"></line>
							</svg>
						</a>
					</div>
				</div>
				<div class="tl-form-group">
					<label class="tl-form-label">User</label>
					<select name="Code" class="tl-form-select">
						<option selected value="All">All users</option>
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If rsUsers("Code") = strFilter_Code Then
%>
						<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
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
			</div>
			<div class="tl-form-row">
				<div class="tl-form-group">
					<label class="tl-form-label">Customer</label>
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
				<div class="tl-form-group">
					<label class="tl-form-label">Entity</label>
					<select name="DivisionId" class="tl-form-select">
						<option value="555" style="color:red;">Select an Entity</option>
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
				<div class="tl-form-group">
					<label class="tl-form-label">Status</label>
					<select name="InvoicestatusId" class="tl-form-select">
						<option value="555">All (Active)</option>
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
					</select>
				</div>
			</div>
			<div class="tl-form-row">
				<div class="tl-form-group" style="display: flex; align-items: flex-end; gap: 8px;">
					<button type="submit" class="tl-btn-primary" onclick="FormReport.action='IFrame.asp';FormReport.target='MyIFrame';">
						Filter
					</button>
					<button type="button" class="tl-btn-primary" onclick="if(document.FormReport.DivisionId.value == 555){alert('Please select an entity before generating a report.');}else{FormReport.action='Report.asp';FormReport.target='MyIFrame';this.form.submit();}">
						Generate Report
					</button>
				</div>
			</div>
		</form>
	</div>

	<!-- Results Grid -->
	<div class="tl-grid-container">
		<iframe style="width:100%;height:600px;border:none;" scrolling="yes" name="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&Company=All&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&DivisionId=<%= intSelDivisionId %>&InvoicestatusId=0"></iframe>
	</div>
</div>

</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->