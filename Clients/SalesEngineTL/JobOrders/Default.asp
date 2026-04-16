<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strSort
Dim strFilter_Code
Dim intDivisionId

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
	If strCode = "" Then
		strCode = "All"
	End If
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

If Request.Cookies("UserSettings")("Manager") Then
	strFilter_Code = Trim(Request("Filter_Code"))
	If strFilter_Code = "" Then
		strFilter_Code = "All"
	End If
Else
	strFilter_Code = Request.Cookies("UserSettings")("Code")
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
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<center>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table3">
		<tr>
			<td>
				<br>
				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table4">
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Job Monitoring /></span></td>
					</tr>
					<tr>
						<td colspan=2>
<%

strMsg = Trim(Request("Msg"))
If strMsg <> "" Then

%>
							<br>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table5">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
<%

End If

%>
						</td>
					</tr>
				</table>
				<table width=760>
					<tr>
						<td>

							<fieldset style="width:760px;">
								<legend style="font-weight:bold;">Filter</legend>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
									<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/JobOrders/Report.asp" target="winResults">
									<tr>
										<td rowspan=2 valign="top" style="font-weight:bold;">Date Range</td>
										<td rowspan=2 valign="top">
											<table cellpadding=3 cellspacing=0 border=0>
												<tr>
													<td valign="top">From:</td><td valign="top"><input type="input" value="<%= dteDateFrom %>" name="DateFrom" readonly ID="Input1"> <a href="javascript:showCal('Calendar3')"><img src="/Images/Calendar.gif" border=0></a></td>
												</tr>
												<tr>
													<td valign="top">To:</td><td valign="top"><input type="input" value="<%= dteDateTo %>" name="DateTo" readonly ID="Input2"> <a href="javascript:showCal('Calendar4')"><img src="/Images/Calendar.gif" border=0></a></td>
												</tr>
											</table>
										</td>
										<td style="font-weight:bold;">User</td>
										<td>
											<select name="Code" ID="Select4">
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
										</td>




									</tr>
									<tr>
										<td style="font-weight:bold;">Keyword</td>
										<td><input type="text" name="Keyword" style="width:250px;"</td>
									</tr>
									<tr>
<%

Set rsCompany = Server.CreateObject("ADODB.RecordSet")
sql = "Select DistinctRow Companies.CompanyId, Companies.Company From Contacts Inner Join Companies On Companies.CompanyId = Contacts.CompanyId Where Companies.CompanyId <> 142 And (Companies.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Quotes") & ") Or Contacts.Code = '" & Request.Cookies("UserSettings")("Code") & "') Order By Companies.Company"
Set rsCompany = dbConn.Execute(sql)

%>
										<td style="font-weight:bold;">Customer</td>
										<td valign="top">
										<select name="CompanyId" style="width:250px;" id="Select1">
											<option value="0">All companies</option>
											<option value="142">Not an account</option>
											<option value="0"></option>
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
										</td>
										<td style="font-weight:bold;">Status</td>
										<td valign="top">
										<select name="JobOrderStatusCode" style="width:250px;" id="Select2">
											<option value="555">All (Active)</option>
											<option value="0">All (Active & Complete)</option>
<%

sql = "Select * From JobOrderStatus Order By JobOrderStatus"
Set rsStatus = dbConn.Execute(sql)

If Not(rsStatus.BOF And rsStatus.EOF) Then
	Do Until rsStatus.EOF
		Response.Write "<option value=""" & rsStatus("JobOrderStatusCode") & """>" & UCase(rsStatus("JobOrderStatus")) & "</option>" & vbcrlf
		rsStatus.MoveNext
	Loop
End If

rsStatus.Close
Set rsStatus = Nothing

%>
										</select>										
										</td>
									</tr>
									<tr>
										<td style="font-weight:bold;">Division</td>
										<td>
										<select name="DivisionId" ID="Select3" style="width:250px;">
											<option value="555" style="color:red;">Select a division</option>
<%

Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE Quotes = True AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Quotes") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If CLng(intDivisionId) = CLng(rsDiv("DivisionId")) Then
			intSelDivisionId = intDivisionId
			Response.Write ("								<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			If CLng(Request.Cookies("DivisionId")) = CLng(rsDiv("DivisionId")) Then
				intSelDivisionId = Request.Cookies("DivisionId")
				Response.Write ("								<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
			Else
				Response.Write ("								<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
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
										</td>
										<td colspan=2 align="right">
										<input type="button" onclick="document.location.href='../Invoices';" value="Invoices" ID="Button3" NAME="Button1">
										<input type="button" onclick="document.location.href='../Quotes';" value="Quotes" ID="Button2" NAME="Button1">
										<button type="button" class="tl-btn-primary" onclick="if(document.FormReport.DivisionId.value == 555){alert('Please select a division before generating a report.');}else{FormReport.action='Report.asp';FormReport.target='winResults';parent.SubmitForm();this.form.submit();}">
											Generate Report
										</button>
										<button type="submit" class="tl-btn-primary" onclick="FormReport.action='IFrame.asp';FormReport.target='MyIFrame';">
											Filter
										</button>
										</td>
									</tr>
								</form>
								</table>
							</fieldset>
						</td>
					</tr>
				</table>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td>
						<iframe width=100% height=250 name="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&Company=All&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&DivisionId=<%= intSelDivisionId %>&JobOrderStatusCode=555"></iframe>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</center>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->