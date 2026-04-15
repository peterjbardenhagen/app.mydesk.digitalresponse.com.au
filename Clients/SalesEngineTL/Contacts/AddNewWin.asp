<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strField
Dim strContactType

strField = Trim(Request("Field"))
strContactType = Trim(Request("ContactType"))

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
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
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


	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2">Add Contact /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Contacts/AddNewWin_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="Field" value="<%= strField %>">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;" width=170>First Name</td>
									<td valign="top"><input type="text" name="FirstName" style="width:280px;" maxlength=50></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Surname</td>
									<td valign="top"><input type="text" name="Surname" style="width:280px;" maxlength=50></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Position</td>
									<td valign="top"><input type="text" name="Position" style="width:280px;" maxlength=50></td>
								</tr>
								<input type="hidden" name="CompanyId" id="CompanyId" value="142" />
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Company Name</td>
									<td valign="top"><input type="text" name="CCompany" style="width:280px;" maxlength=100 ID="Text14"></td>
								</tr>
								<tr>
									<td colspan=3><br><b>Invoice Address Details</b></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 1</td>
									<td valign="top"><input type="text" name="Address1" style="width:280px;" maxlength=50 ID="Text1"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 2</td>
									<td valign="top"><input type="text" name="Address2" style="width:280px;" maxlength=50 ID="Text2"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Suburb</td>
									<td valign="top"><input type="text" name="Suburb" style="width:280px;" maxlength=50 ID="Text3"></td>
								</tr>
								<tr>
									<td valign="top"></td>
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
									<td valign="top"><input type="text" name="State" style="width:280px;" maxlength=50 ID="Text5"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Post Code</td>
									<td valign="top"><input type="text" name="PostCode" style="width:280px;" maxlength=50 ID="Text6"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Country</td>
									<td valign="top"><input type="text" name="Country" style="width:280px;" maxlength=50 ID="Text12"></td>
								</tr>
								<tr>
									<td colspan=3><br><b>Other/Delivery Address Details</b></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 1</td>
									<td valign="top"><input type="text" name="OAddress1" style="width:280px;" maxlength=50 ID="Text7"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 2</td>
									<td valign="top"><input type="text" name="OAddress2" style="width:280px;" maxlength=50 ID="Text8"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Suburb</td>
									<td valign="top"><input type="text" name="OSuburb" style="width:280px;" maxlength=50 ID="Text9"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">State</td>
									<td valign="top">
										<select name="OStateId" ID="Select3" style="width:280px;" onChange="toggleState2()">
<%

Set rsStates = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM States ORDER BY State"
Set rsStates = dbConn.Execute(sql)

If Not(rsStates.BOF And rsStates.EOF) Then
	Do Until rsStates.EOF
		If rsStates("StateId") = 9 Then
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
									<td valign="top"><input type="text" name="OState" style="width:280px;" maxlength=50 ID="Text10"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Post Code</td>
									<td valign="top"><input type="text" name="OPostCode" style="width:280px;" maxlength=50 ID="Text11"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Country</td>
									<td valign="top"><input type="text" name="OCountry" style="width:280px;" maxlength=50 ID="Text13"></td>
								</tr>
								<tr>
									<td colspan=3><br></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Phone</td>
									<td valign="top"><input type="text" name="Phone" style="width:280px;" maxlength=50></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Fax</td>
									<td valign="top"><input type="text" name="Fax" style="width:280px;" maxlength=50></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Mobile</td>
									<td valign="top"><input type="text" name="Mobile" style="width:280px;" maxlength=50></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Email</td>
									<td valign="top"><input type="text" name="Email" style="width:280px;" maxlength=50></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Website</td>
									<td valign="top"><input type="text" name="Website" style="width:280px;" maxlength=100 value="http://"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Notes<br>I.e. Hobbies, birthdays etc</td>
									<td valign="top">
									<textarea name="Notes" id="Notes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount3',500)" onkeypress="parent.LimitText(this,500)"></textarea><br/>Characters Remaining: <input type="text" name="textcount3" size="4" value="500" readonly ID="Text4">
									</td>								
								</tr>
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="window.close();">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
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
			document.getElementById('StateTR').style.display = 'block';
		} else {
			document.Form1.State.value = '';
			document.Form1.State.readOnly = true;
			document.getElementById('StateTR').style.display = 'none';
		}
	}
	function toggleState2() {
		if(document.Form1.OStateId.value == 9) {
			document.Form1.OState.readOnly = false;
			document.getElementById('OStateTR').style.display = 'block';
		} else {
			document.Form1.OState.value = '';
			document.Form1.OState.readOnly = true;
			document.getElementById('OStateTR').style.display = 'none';
		}
	}
</script>

	</body>
</html>

<!--#include virtual="/System/ssi_dbConn_close.inc"-->
