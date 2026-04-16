<% 
' Techlight MyDesk - Modern Purchase Orders List - Hardened for Stability
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
	If Not IsEmpty(Request.Cookies("DivisionIdsAccess")("PurchaseOrders")) Then
		hasAccess = (Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0")
	End If
End If
On Error GoTo 0

If Not hasAccess Then
	Response.Redirect("../Portal/AccessDenied.asp")
	Response.End
End If

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

' Get sort order with null check
strSort = ""
On Error Resume Next
If Request.QueryString("Sort") = "" Or IsNull(Request.QueryString("Sort")) Then
	strSort = "PurchaseOrders.Date DESC"
Else
	strSort = Trim(Request.QueryString("Sort"))
End If
If Err.Number <> 0 Then strSort = "PurchaseOrders.Date DESC"
On Error GoTo 0

' Get filter code with null checks
strFilter_Code = ""
On Error Resume Next
If Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("Manager")) Then
		If CBool(Request.Cookies("UserSettings")("Manager")) Then
			strFilter_Code = Trim(Request("Filter_Code"))
			If strFilter_Code = "" Then
				If Not IsEmpty(Request.Cookies("UserSettings")("Code")) Then
					strFilter_Code = Request.Cookies("UserSettings")("Code")
				End If
			End If
		Else
			If Not IsEmpty(Request.Cookies("UserSettings")("Code")) Then
				strFilter_Code = Request.Cookies("UserSettings")("Code")
			End If
		End If
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
	<title>Purchase Orders - Techlight MyDesk</title>
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
		<span>Purchase Orders</span>
	</nav>

	<!-- Page Header -->
	<div class="tl-action-bar">
		<h1 class="tl-page-title">
			<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
				<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
				<line x1="3" y1="6" x2="21" y2="6"></line>
				<path d="M16 10a4 4 0 0 1-8 0"></path>
			</svg>
			Purchase Orders
		</h1>
		<div class="tl-btn-group">
			<a href="<%= strWorkingDir %>/PurchaseOrders/Add.asp" class="tl-btn-primary" target="_top">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="display: inline-block; vertical-align: middle; margin-right: 6px;">
					<line x1="12" y1="5" x2="12" y2="19"></line>
					<line x1="5" y1="12" x2="19" y2="12"></line>
				</svg>
				New Purchase Order
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
			Filter Purchase Orders
		</div>
		<form name="FormReport" id="FormReport" method="post" action="<%= strWorkingDir %>/PurchaseOrders/IFrame.asp" target="MyIFrame">
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
					<label class="tl-form-label">Supplier</label>
<%
Set rsCompany = Server.CreateObject("ADODB.RecordSet")
sql = "Select DistinctRow Companies.CompanyId, Companies.Company From Contacts Inner Join Companies On Companies.CompanyId = Contacts.CompanyId Where Companies.CompanyId <> 142 And SupplierCode <> '' And (DivisionId ) Order By Companies.Company"
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
					<label class="tl-form-label">Division</label>
					<select name="DivisionId" class="tl-form-select">
						<option value="555" style="color:red;">Select a division</option>
<%
Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE PurchaseOrders = True AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("PurchaseOrders") & ") ORDER BY Division"
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
					<select name="POStatusId" class="tl-form-select">
						<option value="555">All (Active)</option>
						<option value="0" selected>All (Active & Complete)</option>
<%
sql = "Select * From PurchaseOrderStatus Order By POStatus"
Set rsStatus = dbConn.Execute(sql)

If Not(rsStatus.BOF And rsStatus.EOF) Then
	Do Until rsStatus.EOF
		Response.Write "<option value=""" & rsStatus("POStatusId") & """>" & rsStatus("POStatus") & "</option>" & vbcrlf
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
					<button type="button" class="tl-btn-secondary" onclick="if(document.FormReport.DivisionId.value == 555){alert('Please select a division before generating a report.');}else{FormReport.action='Report.asp';FormReport.target='MyIFrame';this.form.submit();}">
						Generate Report
					</button>
					<button type="button" class="tl-btn-secondary" onclick="document.FormReport.Code.value = 'All';document.FormReport.CompanyId.value = 0;document.FormReport.POStatusId.value = 2;FormReport.action='IFrame.asp';FormReport.target='MyIFrame';this.form.submit();">
						Pending Approval
					</button>
				</div>
			</div>
		</form>
	</div>
	<!-- Results Grid -->
	<div class="tl-grid-container">
		<iframe scrolling="yes" style="width:100%;height:600px;border:none;" name="MyIFrame" id="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&Company=All&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&DivisionId=<%= intSelDivisionId %>"></iframe>
	</div>
</div>
</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->