<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

lngContactId = CLng(Request("ContactId"))
strType = Trim(Request("Type"))

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
		<script language="javascript">
			function DoSelect() {
				if(checkForm()){
					var s;
					var sState;
					var myForm = document.getElementById;
					s = myForm("Company").value + '\n';
					if(parseInt(myForm("StateId").value)==9){sState=myForm("State").value;}else{sState=myForm("StateId").options[myForm("StateId").selectedIndex].text}
					s += myForm("Address1").value + '\n';
					if(myForm("Address2").value.length>0){
						s += myForm("Address2").value + '\n';
					}
					s += myForm("Suburb").value + ' ' + sState + ' ' + myForm("Country").value + ' ' + myForm("PostCode").value + '\n';
					alert(s);
					InvoiceAddress_Select(s, myForm("Company").value, myForm("Address1").value, myForm("Address2").value, myForm("Suburb").value, myForm("StateId").value, sState, myForm("PostCode").value, myForm("Country").value);
					window.close();
				}
			}
			function DoTBA() {
                InvoiceAddress_Select('To be advised', '', 'To be advised', '', '', 9, '', '', '');
			    window.close();
			}
			function emptyField(textObj) {
				if (textObj.value.length == 0) return true;
				for (var i=0; i < textObj.value.length; i++) {
					var ch = textObj.value.charAt(i);
					if (ch != ' ' && ch != '\t') return false;
				}
				return true
			}

			// Check form for validation errors
			function checkForm() {
				var validFlag = true;

				if (validFlag) {
				if (emptyField(document.Form1.Company)) {
					alert("Please enter Company.");
					validFlag = false;
					document.Form1.Company.focus();
				}}

				if (validFlag) {
				if (emptyField(document.Form1.Address1)) {
					alert("Please enter Address 1.");
					validFlag = false;
					document.Form1.Address1.focus();
				}}

				if (validFlag) {
				if (emptyField(document.Form1.Suburb)) {
					alert("Please enter Suburb.");
					validFlag = false;
					document.Form1.Suburb.focus();
				}}

				if (validFlag) {
				if (emptyField(document.Form1.StateId)) {
					alert("Please select State.");
					validFlag = false;
					document.Form1.State.focus();
				}}

				if (validFlag) {
				if (document.Form1.StateId == 9 && emptyField(document.Form1.State)) {
					alert("Please enter State.");
					validFlag = false;
					document.Form1.State.focus();
				}}

				if (validFlag) {
				if (emptyField(document.Form1.PostCode)) {
					alert("Please enter Post Code.");
					validFlag = false;
					document.Form1.PostCode.focus();
				}}

				if (validFlag) {
				if (emptyField(document.Form1.Country)) {
					alert("Please enter Country.");
					validFlag = false;
					document.Form1.Country.focus();
				}}
				return validFlag;
			}
		</script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	</head>
	<body bgcolor="#ffffff">
<!--#include virtual="/System/ssi_Header.inc"-->
		<table width="100%" cellpadding=15 cellspacing=0 border=0>
			<tr>
				<td style="font-size:16px;" colspan=10>Invoice Address Builder</td>
			</tr>
			<form method="post" action="#" id="Form1" name="Form1">
			<tr>
				<td valign="top" colspan=3>
				<b>Select address from customer</b><br>
				<select name="ContactId" style="width:280px;" ID="Select2">
					<option value="0">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Deleted = 0 AND Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF
		If rsContacts("ContactId") = lngContactId Then
%>
					<option selected value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
		Else
%>
					<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
		End If
		rsContacts.MoveNext
	Loop
End If

If IsObject(rsContacts) Then
	rsContacts.Close
	Set rsContacts = Nothing
End If

%>
				</select><br>
<!--				<input type="button" value="Create New Contact" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId', 'Supplier');">-->
				<input type="button" value="Invoice Address" onclick="document.location.href='InvoiceAddress.asp?Type=Main&ContactId='+document.Form1.ContactId.value;">
				<input type="button" value="Other/Delivery Address" onclick="document.location.href='InvoiceAddress.asp?Type=Other&ContactId='+document.Form1.ContactId.value;">
				</td>
			</tr>
			<tr>
				<td colspan=5>
<%
If lngContactId = 0 Then
%>
					<table cellpadding=2 cellspacing=0 border=0>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Company</td>
							<td valign="top"><input type="text" name="Company" style="width:280px;" maxlength=50 ID="Text20"></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Address 1</td>
							<td valign="top"><input type="text" name="Address1" style="width:280px;" maxlength=50 ID="Text1"></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Address 2</td>
							<td valign="top"><input type="text" name="Address2" style="width:280px;" maxlength=50 ID="Text2"></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Suburb</td>
							<td valign="top"><input type="text" name="Suburb" style="width:280px;" maxlength=50 ID="Text3"></td>
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
		If rsStates("StateId") = 9 Then
			Response.Write ("									<option selected value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
		Else
			Response.Write ("									<option value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
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
							<td valign="top"><input type="text" name="State" style="width:280px;" maxlength=50 ID="Text4"></td>
						</tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Post Code</td>
							<td valign="top"><input type="text" name="PostCode" style="width:280px;" maxlength=50 ID="Text5"></td>
						</tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Country</td>
							<td valign="top"><input type="text" name="Country" style="width:280px;" maxlength=50 value="Australia"></td>
						</tr>
					</table>
<%
Else
	sql = "Select * From Contacts_WithCustomersAndSuppliers_V2 Where Deleted = 0 AND ContactId = " & lngContactId
	Set rs = dbConn.Execute(sql)
	If strType = "Main" Then
%>
					<table cellpadding=2 cellspacing=0 border=0 ID="Table2">
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Company</td>
							<td valign="top"><input type="text" name="Company" style="width:280px;" maxlength=50 ID="Text18" value="<%= rs("CompanyName") %>"></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Address 1</td>
							<td valign="top"><input type="text" name="Address1" style="width:280px;" maxlength=50 ID="Text6" value="<%= rs("Address1") %>"></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Address 2</td>
							<td valign="top"><input type="text" name="Address2" style="width:280px;" maxlength=50 ID="Text7" value="<%= rs("Address2") %>"></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Suburb</td>
							<td valign="top"><input type="text" name="Suburb" style="width:280px;" maxlength=50 ID="Text8" value="<%= rs("Suburb") %>"></td>
						</tr>
						<tr id="StateTR">
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">State</td>
							<td valign="top">
								<select name="StateId" ID="Select3" style="width:280px;">
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
							<td valign="top"><input type="text" name="State" style="width:280px;" maxlength=50 ID="Text16"></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Post Code</td>
							<td valign="top"><input type="text" name="PostCode" style="width:280px;" maxlength=50 ID="Text10" value="<%= rs("PostCode") %>"></td>
						</tr>
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Country</td>
							<td valign="top"><input type="text" name="Country" style="width:280px;" maxlength=50 ID="Text19" value="<%= rs("Country") %>"></td>
						</tr>
					</table>
<%
	Else
%>
					<table cellpadding=2 cellspacing=0 border=0 ID="Table1">
						<tr>
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">Company</td>
							<td valign="top"><input type="text" name="Company" style="width:280px;" maxlength=50 ID="Text9" value="<%= rs("CompanyName") %>"></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Address 1</td>
							<td valign="top"><input type="text" name="Address1" style="width:280px;" maxlength=50 ID="Text11" value="<%= rs("OAddress1") %>"></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Address 2</td>
							<td valign="top"><input type="text" name="Address2" style="width:280px;" maxlength=50 ID="Text12" value="<%= rs("OAddress2") %>"></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Suburb</td>
							<td valign="top"><input type="text" name="Suburb" style="width:280px;" maxlength=50 ID="Text13" value="<%= rs("OSuburb") %>"></td>
						</tr>
						<tr id="StateTR">
							<td valign="top" class="Req">*</td>
							<td valign="top" style="font-weight:bold;">State</td>
							<td valign="top">
								<select name="StateId" ID="Select4" style="width:280px;">
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
						<tr id="StateTR">
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">If other, enter State</td>
							<td valign="top"><input type="text" name="State" style="width:280px;" maxlength=50 ID="Text17"></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Post Code</td>
							<td valign="top"><input type="text" name="PostCode" style="width:280px;" maxlength=50 ID="Text14" value="<%= rs("OPostCode") %>"></td>
						</tr>
						<tr>
							<td valign="top"></td>
							<td valign="top" style="font-weight:bold;">Country</td>
							<td valign="top"><input type="text" name="Country" style="width:280px;" maxlength=50 ID="Text15" value="<%= rs("OCountry") %>"></td>
						</tr>
					</table>
<%
	End If
End If
%>
				</td>
			</tr>
			<tr>
				<td colspan=2 align="right"><input type="button" value="Make TBA" onclick="DoTBA();" /> <input type="Button" value="Select" onclick="DoSelect();"></td>
			</tr>
			</form>
		</table>
	</body>
	</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->