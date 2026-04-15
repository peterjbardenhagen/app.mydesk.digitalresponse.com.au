<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngExpenseId
lngExpenseId = CLng(Request("ExpenseId"))

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
			if (emptyField(document.Form1.DivisionId)) {
				alert("Please select a Division.");
				validFlag = false;
				document.Form1.DivisionId.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.ExpenseDate)) {
				alert("Please select Expense Date.");
				validFlag = false;
				document.Form1.ExpenseDate.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ExpenseTypeId)) {
				alert("Please select Expense Type.");
				validFlag = false;
				document.Form1.ExpenseTypeId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Description)) {
				alert("Please complete the Description field.");
				validFlag = false;
				document.Form1.Description.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ContactId)) {
				alert("Please select a Contact.");
				validFlag = false;
				document.Form1.ContactId.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.CostIncGST.value)) {
				alert("Please enter a valid Cost Inc GST amount.");
				validFlag = false;
				document.Form1.CostIncGST.focus();
			}}

			if (validFlag) {
			if (!document.Form1.CostIncGST.value > 0) {
				alert("Please enter a valid Cost Inc GST amount.");
				validFlag = false;
				document.Form1.CostIncGST.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.GST.value)) {
				alert("Please enter a valid GST amount.");
				validFlag = false;
				document.Form1.GST.focus();
			}}

			if (validFlag) {
			if (!document.Form1.GST.value > 0) {
				alert("Please enter a valid GST amount.");
				validFlag = false;
				document.Form1.GST.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.FBTTTL.value)) {
				alert("Please enter number of Company Staff.");
				validFlag = false;
				document.Form1.FBTTTL.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.FBTNon.value)) {
				alert("Please enter number of Guests.");
				validFlag = false;
				document.Form1.FBTNon.focus();
			}}

			return validFlag
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->
<%
Dim rs
Dim sql

' *** Using user division id as we are not using the expenses division id ... yet.

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Expenses.Code AS OriginatorCode, Expenses.*, Contacts_WithCustomersAndSuppliers_V2.*, Expenses.DivisionId As ExpenseDivisionId, Users.DivisionId As UserDivisionId FROM Users INNER JOIN (Expenses INNER JOIN Contacts_WithCustomersAndSuppliers_V2 ON Expenses.ContactId = Contacts_WithCustomersAndSuppliers_V2.ContactId) ON Users.Code = Expenses.Code WHERE Expenses.Eid=" & lngExpenseId
Set rs = dbConn.Execute(sql)

If Not SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsPayroll"), rs("UserDivisionId")) Then ' If not payroll
	' Check for sign off first
	Set rsCheck = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ExpensesSignOffs WHERE [Month] = " & Month(rs("ExpenseDate")) & " AND [Year] = " & Year(rs("ExpenseDate")) & " AND Code = '" & rs("OriginatorCode") & "'"
	Set rsCheck = dbConn.Execute(sql)

	If Not(rsCheck.BOF and rsCheck.EOF) Then
%>
<script language="javascript">
	alert('Cannot update. This month has been signed off');
	document.location.href='default.asp';
</script>
<%
		Response.End
	End If
End If

Set rsUsers = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND Code = '" & rs("OriginatorCode") & "'"
Set rsUsers = dbConn.Execute(sql)

If Not(rsUsers.BOF And rsUsers.EOF) Then
	intDivisionId = rsUsers("DivisionId")
End If

rsUsers.Close
Set rsUsers = Nothing

If Request.Cookies("UserSettings")("Manager") Or rs("Code") = Request.Cookies("UserSettings")("Code") Then

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Expenses</a> / Edit Expense /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<b>*** ALL EXPENSE CLAIMS MUST BE ACCOMPANIED BY RECEIPT ***</b><br><br>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Expenses/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="ExpenseId" value="<%= rs("Eid") %>">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Division</td>
									<td valign="top">
								        <select name="DivisionId" ID="Select3" style="width:280px;">
									        <option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If CInt(rs("ExpenseDivisionId")) = CInt(rsDiv("DivisionId")) Then
			Response.Write ("									        <option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			Response.Write ("									        <option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
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
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" width=200 style="font-weight:bold;">Expense Date</td>
									<td valign="top"><input type="input" value="<%= FormatDateU(rs("ExpenseDate"), false) %>" name="ExpenseDate" readonly ID="Input2"> <a href="javascript:showCal('Calendar9')"><img src="/Images/Calendar.gif" border=0></a></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Expense Type</td>
									<td valign="top">
									<select name="ExpenseTypeId" ID="Select2" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>

<%

Set rsE = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM ExpenseTypes WHERE (Visible = 1 OR ExpenseTypeId = " & rs("ExpenseTypeId") & ") AND ExpenseTypeGroupId = " & Request.Cookies("UserSettings")("ExpenseTypeGroupId") & " ORDER BY ExpenseCode"
Set rsE = dbConn.Execute(sql)

If Not(rsE.BOF And rsE.EOF) Then
	Do Until rsE.EOF
		If rsE("ExpenseTypeId") = rs("ExpenseTypeId") Then
%>
										<option selected value="<%= rsE("ExpenseTypeId") %>"><%= rsE("ExpenseCode") %> - <%= rsE("ExpenseType") %></option>
<%
		Else
%>
										<option value="<%= rsE("ExpenseTypeId") %>"><%= rsE("ExpenseCode") %> - <%= rsE("ExpenseType") %></option>
<%
		End If
		rsE.MoveNext
	Loop
End If

If IsObject(rsE) Then
	rsE.Close
	Set rsE = Nothing
End If

%>
									</select>
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Description</td>
									<td valign="top">
									<textarea name="Description" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',500)" onkeypress="parent.LimitText(this,500)" ID="Textarea1"><%= rs("Description") %></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="<%= 500 - Len(rs("Description")) %>" readonly ID="Text6">
									</td>								
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Contact</td>
									<td valign="top">
									<select name="ContactId" ID="Select1" style="width:280px;">
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
										<option selected value="<%= rs("ContactId") %>"><%= rs("CompanyName") %> - <%= rs("Surname") %>, <%= rs("FirstName") %></option>
										<option value=""> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -</option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & rs("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then

%>
										<option value="">ORDER BY COMPANY:</option>
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

	Do Until rsContacts.EOF

%>
										<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
		rsContacts.MoveNext
	Loop
End If

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & rs("Code") & "' ORDER BY Surname, FirstName, CompanyName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then

%>
										<option value=""></option>
										<option value="">ORDER BY NAME:</option>
										<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

	Do Until rsContacts.EOF

%>
										<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %> - <%= rsContacts("CompanyName") %></option>
<%
		rsContacts.MoveNext
	Loop
End If

If IsObject(rsContacts) Then
	rsContacts.Close
	Set rsContacts = Nothing
End If

%>
									</select>
									<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId', 'Customer');">Create New Contact</a>
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Cost Inc GST (eg. 50.00)</td>
									<td valign="top">$<input type="text" size=20 name="CostIncGST" maxlength=8 value=<%= rs("CostIncGST") %> onchange="this.form.GST.value = parent.formatDecimal(this.form.CostIncGST.value/11);"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">GST (eg. 5.00)</td>
									<td valign="top">$<input type="text" size=20 name="GST" maxlength=8 ID="Text2" value=<%= rs("GST") %>></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Company Staff</td>
									<td valign="top"><input type="text" size=20 name="FBTTTL" maxlength=8 ID="Text4" value=<%= rs("FBTTTL") %>></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Guests</td>
									<td valign="top"><input type="text" size=20 name="FBTNon" maxlength=8 ID="Text5" value=<%= rs("FBTNon") %>></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>Corporate Card</b></td>
									<td valign="top">
										<input type="radio" name="TTLCorporateCard" value="-1" ID="Radio5" <% If rs("TTLCorporateCard") Then Response.Write("Checked") %>> Yes<br/>
										<input type="radio" name="TTLCorporateCard" value="0" ID="Radio6" <% If Not rs("TTLCorporateCard") Then Response.Write("Checked") %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><span style="font-weight:bold;">Comment</td>
									<td valign="top">
									<textarea name="Comment" id="Comment" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)"><%= rs("Comment") %></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="<%= 500 - Len(rs("Comment")) %>" readonly ID="Text3">
									</td>
								</tr>
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
								</tr>
								</form>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<%

End If

%>
	</body>
</html>
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
