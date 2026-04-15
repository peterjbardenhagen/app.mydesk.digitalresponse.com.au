<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

Dim strSort
Dim strFilter_Code

If Request.Cookies("UserSettings")("Manager") Then
	strCode = Trim(Request("Code"))
	If strCode = "" Then
		strCode = "All"
	End If
Else
	strCode = Request.Cookies("UserSettings")("Code")
End If

If Request.QueryString("Sort") = "" Then
	strSort = "Quotes.Date DESC"
Else
	strSort = Trim(Request.QueryString("Sort"))
End if

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
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Sales Projects /></span></td>
						<td align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/SalesProjects/Add.asp" class="Header2">Add Sales Project</a></td>
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
									<form name="FormReport" id="FormReport" method="post" action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/SalesProjects/Report.asp" target="winResults">
									<tr>
										<td style="font-weight:bold;">Date From</td>
										<td valign="top"><input type="input" value="<%= dteDateFrom %>" name="DateFrom" readonly ID="Input1"> <a href="javascript:showCal('Calendar3')"><img src="/Images/Calendar.gif" border=0></a></td>
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
										<td style="font-weight:bold;">Date To</td>
										<td valign="top"><input type="input" value="<%= dteDateTo %>" name="DateTo" readonly ID="Input2"> <a href="javascript:showCal('Calendar4')"><img src="/Images/Calendar.gif" border=0></a></td>
<%

Set rsCompany = Server.CreateObject("ADODB.RecordSet")
sql = "Select DistinctRow Companies.CompanyId, Companies.Company AS CompanyName From Contacts Inner Join Companies On Companies.CompanyId = Contacts.CompanyId Where Companies.CompanyId <> 142 And ((Companies.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") Or Contacts.Code = '" & Request.Cookies("UserSettings")("Code") & "')) Order By Companies.Company"
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
		Response.Write "<option value=""" & rsCompany("CompanyId") & """>" & rsCompany("CompanyName") & "</option>" & vbcrlf
		rsCompany.MoveNext
	Loop
End If

rsCompany.Close
Set rsCompany = Nothing

%>
										</select>										
										</td>
									</tr>
									<tr>
										<td colspan=2></td>
										<td style="font-weight:bold;">Project</td>
										<td>
<%

	Set rsProjects = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Distinct Project From SalesProjects Inner Join Users On Users.Code = SalesProjects.Code Where Users.DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") Or SalesProjects.Code = '" & Request.Cookies("UserSettings")("Code") & "' Order By SalesProjects.Project"
	Set rsProjects = dbConn.Execute(sql)

%>
										<select name="Project" style="width:250px;" id="Select3">
											<option value="All">All projects</option>
<%

	If Not(rsProjects.BOF And rsProjects.EOF) Then
		b = ""
		Do Until rsProjects.EOF
			If Not (b = rsProjects("Project")) Then
				If rsProjects("Project") = b Then
					Response.Write "<option selected value=""" & rsProjects("Project") & """>" & rsProjects("Project") & "</option>" & vbcrlf
				Else
					Response.Write "<option value=""" & rsProjects("Project") & """>" & rsProjects("Project") & "</option>" & vbcrlf
				End If
			End If
			b = rsProjects("Project")
			rsProjects.MoveNext
		Loop
	End If

	rsProjects.Close
	Set rsProjects = Nothing

%>
										</select>
										</td>
										<td align="right">
										<input type="button" onclick="FormReport.action='Report.asp';FormReport.target='winResults';parent.SubmitForm();this.form.submit();" value="Generate Report" ID="Button1" NAME="Button1">
										<input type="submit" value="Filter" ID="Submit2" NAME="Submit2" onclick="FormReport.action='IFrame.asp';FormReport.target='MyIFrame';">
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
						<iframe width=100% height=250 name="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&Company=All&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&Project=All"></iframe>
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