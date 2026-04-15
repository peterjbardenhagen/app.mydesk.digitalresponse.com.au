<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngContactId
lngContactId = CLng(Request("ContactId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>SalesEngine</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
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
			if (emptyField(document.Form1.FirstName)) {
				alert("Please complete the First Name field.");
				validFlag = false;
				document.Form1.FirstName.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Surname)) {
				alert("Please complete the Surname field.");
				validFlag = false;
				document.Form1.Surname.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.CompanyId)) {
				alert("Please complete the Customer/Supplier field.");
				validFlag = false;
				document.Form1.CompanyId.focus();
			}}

			if (validFlag) {
			if (document.Form1.CompanyId.value == 142 && emptyField(document.Form1.CCompany)) {
				alert("Please complete the Customer/Supplier Company field.");
				validFlag = false;
				document.Form1.CCompany.focus();
			}}

			if (validFlag) {
			if (document.Form1.CompanyId.value != 142 && !emptyField(document.Form1.CCompany)) {
				alert("Please do not enter a Customer/Supplier Company, if you have selected a Customer/Supplier.");
				validFlag = false;
				document.Form1.CompanyId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Phone) && emptyField(document.Form1.Mobile)) {
				alert("Please complete the Phone or Mobile field.");
				validFlag = false;
				document.Form1.Phone.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Email) && !(document.Form1.Email.value.search(/^\w+((-\w+)|(\.\w+))*\@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z0-9]+$/) != -1)) {
				alert("Please ensure that you have entered a valid email address in the Email field.");
				validFlag = false;
				document.Form1.Email.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Website) && !(document.Form1.Website.value.substring(0,7) == 'http://')) {
				alert("Please ensure that you have entered a valid website beginning with http:// in the Website field.");
				validFlag = false;
				document.Form1.Website.focus();
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

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select C.*, Users.DivisionId, Users.UserId From Contacts_WithCustomersAndSuppliers_V2 C Inner Join Users On Users.Code = C.Code Where C.ContactId = " & lngContactId
Set rs = dbConn.Execute(sql)

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Contacts</a> / Edit Contact /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Contacts/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="ContactId" value="<%= rs("ContactId") %>" ID="Hidden1">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">First Name</td>
									<td valign="top"><input type="text" name="FirstName" style="width:280px;" maxlength=50 ID="Text1" value="<%= rs("FirstName") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Surname</td>
									<td valign="top"><input type="text" name="Surname" style="width:280px;" maxlength=50 ID="Text2" value="<%= rs("Surname") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Position</td>
									<td valign="top"><input type="text" name="Position" style="width:280px;" maxlength=50 ID="Text3" value="<%= rs("Position") %>"></td>
								</tr>
								<input type="hidden" value="142" id="CompanyId" name="CompanyId" />
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Company</td>
									<td valign="top"><input type="text" name="CCompany" style="width:280px;" maxlength=100 ID="Text14" value="<%= rs("CCompany") %>"></td>
								</tr>
								<tr>
									<td colspan=3><br><b>Invoice Address Details</b></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 1</td>
									<td valign="top"><input type="text" name="Address1" style="width:280px;" maxlength=50 ID="Text5" value="<%= rs("Address1") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 2</td>
									<td valign="top"><input type="text" name="Address2" style="width:280px;" maxlength=50 ID="Text6" value="<%= rs("Address2") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Suburb</td>
									<td valign="top"><input type="text" name="Suburb" style="width:280px;" maxlength=50 ID="Text7" value="<%= rs("Suburb") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">State</td>
									<td valign="top">
										<select name="StateId" ID="Select1" style="width:280px;" onChange="toggleState()">
<%

Set rsStates = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM States ORDER BY State"
Set rsStates = dbConn.Execute(sql)

If Not(rsStates.BOF And rsStates.EOF) Then
	Do Until rsStates.EOF
		If CLng(rsStates("StateId")) = CLng(rs("StateId")) Then
			Response.Write ("								<option selected value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
		End If
		rsStates.MoveNext
	Loop
End If

If IsObject(rsStates) Then
	rsStates.Close
	Set rsStates = Nothing
End If

%>
										</select>									
									</td>
								</tr>
								<tr id="StateTR">
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">If other, enter State</td>
									<td valign="top"><input type="text" name="State" style="width:280px;" maxlength=50 ID="Text21"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Post Code</td>
									<td valign="top"><input type="text" name="PostCode" style="width:280px;" maxlength=50 ID="Text8" value="<%= rs("PostCode") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Country</td>
									<td valign="top"><input type="text" name="Country" style="width:280px;" maxlength=50 ID="Text19" value="<%= rs("Country") %>"></td>
								</tr>
								<tr>
									<td colspan=3><br><b>Delivery (or other) Address Details</b></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 1</td>
									<td valign="top"><input type="text" name="OAddress1" style="width:280px;" maxlength=50 ID="Text15" value="<%= rs("OAddress1") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 2</td>
									<td valign="top"><input type="text" name="OAddress2" style="width:280px;" maxlength=50 ID="Text16" value="<%= rs("OAddress2") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Suburb</td>
									<td valign="top"><input type="text" name="OSuburb" style="width:280px;" maxlength=50 ID="Text17" value="<%= rs("OSuburb") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">State</td>
									<td valign="top">
										<select name="OStateId" ID="Select3" style="width:280px;" onChange="toggleState2()">
<%

Set rsStates = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM States ORDER BY State"
Set rsStates = dbConn.Execute(sql)

If Not(rsStates.BOF And rsStates.EOF) Then
	Do Until rsStates.EOF
		If CLng(rsStates("StateId")) = CLng(rs("OStateId")) Then
			Response.Write ("								<option selected value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
		End If
		rsStates.MoveNext
	Loop
End If

If IsObject(rsStates) Then
	rsStates.Close
	Set rsStates = Nothing
End If

%>
										</select>									
									</td>
								</tr>
								<tr id="OStateTR">
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">If other, enter State</td>
									<td valign="top"><input type="text" name="OState" style="width:280px;" maxlength=50 ID="Text22"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Post Code</td>
									<td valign="top"><input type="text" name="OPostCode" style="width:280px;" maxlength=50 ID="Text18" value="<%= rs("OPostCode") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Country</td>
									<td valign="top"><input type="text" name="OCountry" style="width:280px;" maxlength=50 ID="Text20" value="<%= rs("OCountry") %>"></td>
								</tr>
								<tr>
									<td colspan=3><br></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Phone</td>
									<td valign="top"><input type="text" name="Phone" style="width:280px;" maxlength=50 ID="Text9" value="<%= rs("Phone") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Mobile</td>
									<td valign="top"><input type="text" name="Mobile" style="width:280px;" maxlength=50 ID="Text11" value="<%= rs("Mobile") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Email</td>
									<td valign="top"><input type="text" name="Email" style="width:280px;" maxlength=50 ID="Text12" value="<%= rs("Email") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Website</td>
									<td valign="top"><input type="text" name="Website" style="width:280px;" maxlength=100 ID="Text13" value="<%= rs("Website") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Notes<br>I.e. Hobbies, birthdays etc</td>
									<td valign="top">
									<textarea name="Notes" id="Notes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount3',500)" onkeypress="parent.LimitText(this,500)"><%= rs("Notes") %></textarea><br/>Characters Remaining: <input type="text" name="textcount3" size="4" value="<% If Len(rs("Notes")) > 0 Then Response.Write 500-Len(rs("Notes")) Else Response.Write 500 %>" readonly ID="Text4">
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
<script language="javascript">
	function toggleState() {
		if(document.Form1.StateId.value == 9) {
			document.Form1.State.readOnly = false;
			document.getElementById('StateTR').style.display = 'table-row';
		} else {
			document.Form1.State.value = '';
			document.Form1.State.readOnly = true;
			document.getElementById('StateTR').style.display = 'none';
		}
	}
	function toggleState2() {
		if(document.Form1.OStateId.value == 9) {
			document.Form1.OState.readOnly = false;
			document.getElementById('OStateTR').style.display = 'table-row';
		} else {
			document.Form1.OState.value = '';
			document.Form1.OState.readOnly = true;
			document.getElementById('OStateTR').style.display = 'none';
		}
	}
	toggleState();
	toggleState2();
</script>
	</body>
</html>
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
