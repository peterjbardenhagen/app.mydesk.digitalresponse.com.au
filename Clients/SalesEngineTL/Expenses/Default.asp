<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

Dim strMsg
Dim strCompany
Dim strFilter_Code
Dim dteDateFrom
Dim dteDateTo

strMsg = Trim(Request("Msg"))
strCompany = Trim(Request.Form("Company"))

If Request.Cookies("UserSettings")("Manager") Then
	strFilter_Code = Trim(Request("Filter_Code"))
	If strFilter_Code = "" Then
		strFilter_Code = Request.Cookies("UserSettings")("Code")
	End If
Else
	strFilter_Code = Request.Cookies("UserSettings")("Code")
End If

dteDateFrom = FormatDateU(DateAdd("M", -3, ServerToEST(Now())), False)
dteDateTo = FormatDateU(DateAdd("D", 1, ServerToEST(Now())), False)

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
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
		<script language="javascript">
		function expenseForm() {
			if(document.FormReport.Code.value=='All') {
				alert('You can only generate expense reports for each individual person.');
			} else {
				document.FormReport.action='ExpenseForm.asp';
				document.FormReport.target='winResults';
				parent.SubmitForm();
				document.FormReport.submit()
			}
		}
		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->
	<center>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0>
		<tr>
			<td>
				<br>
				<table width="100%" cellpadding=0 cellspacing=0 border=0>
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Expenses /></span></td>
						<td align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Expenses/Add.asp" class="Header2">Add Expense</a></td>
					</tr>
				</table>
<%

If strMsg <> "" Then

%>
				<br>
				<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table4">
					<tr>
						<td><span style="color:red;"><%= strMsg %></span></td>
					</tr>
				</table>
<%

End If

%>
				<table width=760>
					<tr>
						<td>
							<fieldset style="width:760px;">
								<legend style="font-weight:bold;">Filter</legend>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
									<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Expenses/Report.asp" target="winResults">
									<tr>
										<td style="font-weight:bold;">Month</td>
										<td>
											<select name="ExpenseFormDate" ID="Select4">
<%
	dteDateStart = CDate("01-Feb-2005")
	dteDateEnd = CDate("01-" & MonthName(Month(ServerToEST(Now())), False) & "-" & Year(ServerToEST(Now())))
	dteDate = dteDateEnd
	
	Do Until dteDate =< dteDateStart
%>
											<option value="<%= dteDate %>"><%= MonthName(Month(dteDate), False) & " " & Year(dteDate) %></option>
<%
		dteDate = DateAdd("M", -1, dteDate)
	Loop
	
%>
											</select>
										</td>
										<td style="font-weight:bold;">User</td>
										<td>
											<select name="Code" ID="Select2">
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
<%

Set rsCompany = Server.CreateObject("ADODB.RecordSet")
sql = "Select DistinctRow Companies.CompanyId, Companies.Company From Contacts Inner Join Companies On Companies.CompanyId = Contacts.CompanyId Where Companies.CompanyId <> 142 And (Companies.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") Or Contacts.Code = '" & Request.Cookies("UserSettings")("Code") & "') Order By Companies.Company"
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
										<td colspan=2 align="right">
										<input type="button" onclick="expenseForm();" value="Expense Form"> 
										<input type="submit" value="Filter" onclick="FormReport.action='IFrame.asp';FormReport.target='MyIFrame';">
										</td>
									</tr>
								</form>
								</table>
							</fieldset>
						</center>
						</td>
					</tr>
				</table>

				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td>
						<iframe name="MyIFrame" width=100% height=280 src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Expenses/IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&Company=All&ExpenseFormDate=<%= CDate("01-" & MonthName(Month(ServerToEST(Now())), False) & "-" & Year(ServerToEST(Now()))) %>"></iframe>
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