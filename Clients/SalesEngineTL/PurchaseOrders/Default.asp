<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

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
	strSort = "PurchaseOrders.Date DESC"
Else
	strSort = Trim(Request.QueryString("Sort"))
End if

If Request.Cookies("UserSettings")("Manager") Then
	strFilter_Code = Trim(Request("Filter_Code"))
	If strFilter_Code = "" Then
		strFilter_Code = "All"
		'strFilter_Code = Request.Cookies("UserSettings")("Code")
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
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Purchase Orders /></span></td>
						<td align="right"><a href="Add.asp" class="Header2">Add Purchase Order</a></td>
					</tr>
				</table>
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
				<table width=760>
					<tr>
						<td>
							<fieldset style="width:760px;">
								<legend style="font-weight:bold;">Filter</legend>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
									<form name="FormReport" id="FormReport" method="post" action="Report.asp" target="MyIFrame">
									<tr>
										<td style="font-weight:bold;">Date From</td>
										<td valign="top"><input type="input" value="<%= dteDateFrom %>" name="DateFrom" readonly ID="Input1"> <a href="javascript:showCal('Calendar3')"><img src="/Images/Calendar.gif" border=0></a></td>
										<td style="font-weight:bold;">User</td>
										<td>
											<select name="Code" ID="Select2">
											<option selected value="All">All users</option>
<%

	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If rsUsers("Name") = Request.Cookies("UserSettings")("Name") Then
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
										</td>




									</tr>
									<tr>
										<td style="font-weight:bold;">Date To</td>
										<td valign="top"><input type="input" value="<%= dteDateTo %>" name="DateTo" readonly ID="Input2"> <a href="javascript:showCal('Calendar4')"><img src="/Images/Calendar.gif" border=0></a></td>
<%

Set rsCompany = Server.CreateObject("ADODB.RecordSet")
sql = "Select DistinctRow Companies.CompanyId, Companies.Company From Contacts Inner Join Companies On Companies.CompanyId = Contacts.CompanyId Where Companies.CompanyId <> 142 And SupplierCode <> '' And (DivisionId ) Order By Companies.Company"
Set rsCompany = dbConn.Execute(sql)

%>
										<td style="font-weight:bold;">Supplier</td>
										<td valign="top">
										<select name="CompanyId" style="width:280px;" id="Select1">
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
									</tr>
									<tr>
										<td style="font-weight:bold;">Division</td>
										<td>
										<select name="DivisionId" ID="Select3">
											<option value="555" style="color:red;">Select a division</option>
<%

Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE PurchaseOrders = True AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("PurchaseOrders") & ") ORDER BY Division"
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
										<td style="font-weight:bold;">Status</td>
										<td valign="top">
										<select name="POStatusId" style="width:280px;" id="Select4">
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
										</td>
									</tr>
									<tr>
										<td colspan=2></td>
										<td></td>
										<td align="right">
										<input type="button" onclick="if(document.FormReport.DivisionId.value == 555){alert('Please select a division before generating a report.');}else{FormReport.action='Report.asp';FormReport.target='MyIFrame';this.form.submit();}" value="Generate Report" ID="Button1" NAME="Button1">
										<input type="submit" value="Pending Approval" ID="Submit1" NAME="Submit2" onclick="document.FormReport.Code.value = 'All';document.FormReport.CompanyId.value = 0;document.FormReport.POStatusId.value = 2;FormReport.action='IFrame.asp';FormReport.target='MyIFrame';">
										<input type="button" value="Filter" ID="Submit2" NAME="Submit2" onclick="document.FormReport.action='IFrame.asp';document.FormReport.target='MyIFrame';document.FormReport.submit();">
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
						<iframe id="MyIFrame" name="MyIFrame" scrolling="yes" style="width:100%;height:550px;overflow:scroll;scroll-y:auto;" src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strFilter_Code %>&Company=All&DateFrom=<%= dteDateFrom %>&DateTo=<%= dteDateTo %>&DivisionId=<%= intSelDivisionId %>" ></iframe>
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