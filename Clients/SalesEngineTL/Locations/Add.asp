<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

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
			if (emptyField(document.Form1.Company)) {
				alert("Please complete the Company field.");
				validFlag = false;
				document.Form1.Company.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Address1)) {
				alert("Please complete the Address 1 field.");
				validFlag = false;
				document.Form1.Address1.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Suburb)) {
				alert("Please complete the Suburb field.");
				validFlag = false;
				document.Form1.Suburb.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.StateId)) {
				alert("Please complete the State field.");
				validFlag = false;
				document.Form1.StateId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.PostCode)) {
				alert("Please complete the Post-Code field.");
				validFlag = false;
				document.Form1.PostCode.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Country)) {
				alert("Please complete the Country field.");
				validFlag = false;
				document.Form1.Country.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POAddress1)) {
				alert("Please complete the Postal Address -  Address 1 field.");
				validFlag = false;
				document.Form1.POAddress1.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POSuburb)) {
				alert("Please complete the Postal Address -  Suburb field.");
				validFlag = false;
				document.Form1.POSuburb.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POStateId)) {
				alert("Please complete the Postal Address -  State field.");
				validFlag = false;
				document.Form1.POStateId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POPostCode)) {
				alert("Please complete the Postal Address -  Post-Code field.");
				validFlag = false;
				document.Form1.POPostCode.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.POCountry)) {
				alert("Please complete the Postal Address -  Country field.");
				validFlag = false;
				document.Form1.POCountry.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Phone)) {
				alert("Please complete the Phone field.");
				validFlag = false;
				document.Form1.Phone.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Fax)) {
				alert("Please complete the Fax field.");
				validFlag = false;
				document.Form1.Fax.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Email) && !(document.Form1.Email.value.search(/^\w+((-\w+)|(\.\w+))*\@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z0-9]+$/) != -1)) {
				alert("Please ensure that you have entered a valid email address in the Email field.");
				validFlag = false;
				document.Form1.Email.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Website) && !(document.Form1.Website.value.substring(0,7) == 'http://')) {
				alert("Please ensure that you have entered a valid website beginning with http:// in the Email field.");
				validFlag = false;
				document.Form1.Website.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ExpenseTypeGroupId)) {
				alert("Please selecte Expense Type Group.");
				validFlag = false;
				document.Form1.ExpenseTypeGroupId.focus();
			}}
		return validFlag 
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / <a href="Default.asp">Locations</a> / Add Location /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Company</td>
									<td valign="top"><input type="text" name="Company" style="width:280px;" maxlength=50 ID="Text7"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">ACN</td>
									<td valign="top"><input type="text" name="ACN" style="width:280px;" maxlength=50 ID="Text11"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">ABN</td>
									<td valign="top"><input type="text" name="ABN" style="width:280px;" maxlength=50 ID="Text10"></td>
								</tr>
								<tr>
									<td colspan=3 valign="top"><br><b>Office Address Details</b></td>
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
										<select name="StateId" ID="Select1" style="width:280px;">
											<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsStates = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM States ORDER BY State"
Set rsStates = dbConn.Execute(sql)

If Not(rsStates.BOF And rsStates.EOF) Then
	Do Until rsStates.EOF
		Response.Write ("								<option value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
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
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Post Code</td>
									<td valign="top"><input type="text" name="PostCode" style="width:280px;" maxlength=50 ID="Text4"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Country</td>
									<td valign="top"><input type="text" name="Country" style="width:280px;" maxlength=50 ID="Text12"></td>
								</tr>
								<tr>
									<td colspan=3 valign="top"><br><b>Postal Address Details</b></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Display On Statements</td>
									<td valign="top">
									<input type="radio" name="PODisplay" value="-1" checked ID="Radio1"> Yes<br/>
									<input type="radio" name="PODisplay" value="0" ID="Radio2"> No
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Address 1</td>
									<td valign="top"><input type="text" name="POAddress1" style="width:280px;" maxlength=50 ID="Text13"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Address 2</td>
									<td valign="top"><input type="text" name="POAddress2" style="width:280px;" maxlength=50 ID="Text14"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Suburb</td>
									<td valign="top"><input type="text" name="POSuburb" style="width:280px;" maxlength=50 ID="Text15"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">State</td>
									<td valign="top">
										<select name="POStateId" ID="Select2" style="width:280px;">
											<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsStates = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM States ORDER BY State"
Set rsStates = dbConn.Execute(sql)

If Not(rsStates.BOF And rsStates.EOF) Then
	Do Until rsStates.EOF
		Response.Write ("								<option value=""" & rsStates("StateId") & """>" & rsStates("State") & "</option>" & vbNewLine)
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
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Post Code</td>
									<td valign="top"><input type="text" name="POPostCode" style="width:280px;" maxlength=50 ID="Text16"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Country</td>
									<td valign="top"><input type="text" name="POCountry" style="width:280px;" maxlength=50 ID="Text17"></td>
								</tr>
								<tr>
									<td colspan=3><br></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Phone</td>
									<td valign="top"><input type="text" name="Phone" style="width:280px;" maxlength=50 ID="Text5"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Fax</td>
									<td valign="top"><input type="text" name="Fax" style="width:280px;" maxlength=50 ID="Text6"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Email</td>
									<td valign="top"><input type="text" name="Email" style="width:280px;" maxlength=50 ID="Text8"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Website</td>
									<td valign="top"><input type="text" name="Website" style="width:280px;" maxlength=50 ID="Text9"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Expense Type Group</td>
									<td valign="top">
										<select name="ExpenseTypeGroupId" ID="Select3" style="width:280px;">
											<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsEx = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM ExpenseTypeGroups ORDER BY ExpenseTypeGroup"
Set rsEx = dbConn.Execute(sql)

If Not(rsEx.BOF And rsEx.EOF) Then
	Do Until rsEx.EOF
		Response.Write ("								<option value=""" & rsEx("ExpenseTypeGroupId") & """>" & rsEx("ExpenseTypeGroup") & "</option>" & vbNewLine)
		rsEx.MoveNext
	Loop
End If

If IsObject(rsEx) Then
	rsEx.Close
	Set rsEx = Nothing
End If

%>
										</select>									
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
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->