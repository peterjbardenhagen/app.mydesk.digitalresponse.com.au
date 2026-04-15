<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

intDivisionId = CInt(Request("DivisionId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & intDivisionId
Set rsDi = dbConn.Execute(sql)

strLogo = rsDi("Logo")

rsDi.Close
Set rsDi = Nothing

%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Quotes.css">
		<script language="JavaScript">

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
			if (emptyField(document.Form1.ContactId1) && emptyField(document.Form1.ContactId2) && emptyField(document.Form1.ContactId3) && emptyField(document.Form1.ContactId4) && emptyField(document.Form1.ContactId5)) {
				alert("Please select at least 1 Contact.");
				validFlag = false;
				document.Form1.ContactId1.focus();
			}}

			if (validFlag) {
			if (document.Form1.DeliverToLocationId.selectedIndex == 0 && emptyField(document.Form1.DeliverToLocation)) {
				alert("Please enter Delivery Address.");
				validFlag = false;
				document.Form1.DeliverToLocation.focus();
			}}

			if (validFlag) {
			if (document.Form1.DeliverToLocationId.selectedIndex != 0 && !emptyField(document.Form1.DeliverToLocation)) {
				alert("Please select a Delivery Address (from the depot list) or enter a Delivery Address manually. Not both.");
				validFlag = false;
				document.Form1.DeliverToLocation.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.DateRequired)) {
				alert("Please complete Date Required.");
				validFlag = false;
				document.Form1.DateRequired.focus();
			}}
			return validFlag;
		}

		var itemLines=1;
		</script>
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/RFQ.js"></script>
	</head>
	<body bgcolor="#ffffff" onload="Items_InsertLine();">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Request For Quotes</a> / Add Request For Quote /></span>
				<br/><br/>
					<table width="760" cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
						<input type="hidden" name="DivisionId" value="<%= intDivisionId %>">
						<tr>
							<td valign="top" colspan=4 align="right" class="FormBtmTD"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <input type="submit" value="Save" ID="Submit1" NAME="Submit1"></td>
						</tr>
						<tr>
							<td valign="top" colspan=4><img src="https://<%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""></td>
						</tr>
						<tr>
							<td colspan=4>It is a requirement that you request a quote from a minimum of 3 suppliers.</td>
						</tr>
						<input type="hidden" name="ItemLinesVal" value=1 ID="Hidden1">
						<input type="hidden" name="ExtraLinesVal" value=1 ID="Hidden2">
						<tr>
							<td valign="top" colspan=4 align="right" class="FormTopTD"><img src="/Images/Spacer.gif" width=600 height=1 border=0 alt=""></td>
						</tr>
						<tr>
							<td width=150><img src="/Images/Spacer.gif" width=150  height=1 border=0 alt=""></td>
							<td width=610><img src="/Images/Spacer.gif" width=610  height=1 border=0 alt=""></td>
						</tr>
						<tr>
							<td valign="top" class="TDAReq" style="font-weight:bold;">Contact (1)</td>
							<td valign="top" colspan=3>
							<select name="ContactId1" style="width:280px;" ID="Select1">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF

%>
								<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
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
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId1', 'Supplier');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Contact (2)</td>
							<td valign="top" colspan=3>
							<select name="ContactId2" style="width:280px;" ID="Select3">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF

%>
								<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
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
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId2', 'Supplier');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Contact (3)</td>
							<td valign="top" colspan=3>
							<select name="ContactId3" style="width:280px;" ID="Select4">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF

%>
								<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
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
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId3', 'Supplier');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Contact (4)</td>
							<td valign="top" colspan=3>
							<select name="ContactId4" style="width:280px;" ID="Select5">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY Company, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF

%>
								<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
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
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId4', 'Supplier');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Contact (5)</td>
							<td valign="top" colspan=3>
							<select name="ContactId5" style="width:280px;" ID="Select6">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)

If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF

%>
								<option value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
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
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId5', 'Supplier');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" class="TDAReq" style="font-weight:bold;">Delivery Address depot</td>
							<td valign="top" colspan=3>
							<select name="DeliverToLocationId" style="width:280px;" ID="Select2" tabindex=4>
<%

Set rsLocations = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Locations.*, States.State FROM Locations INNER JOIN States ON States.StateId = Locations.StateId ORDER BY Company, Suburb, States.State"
Set rsLocations = dbConn.Execute(sql)

If Not(rsLocations.BOF And rsLocations.EOF) Then
	Dim strLocation
	Do Until rsLocations.EOF
		If InStr(rsLocations("Company"), "***") > 0 Then
			strLocation = rsLocations("Company")
		Else
			strLocation = rsLocations("Company") & " - " & rsLocations("Suburb") & ", " & rsLocations("State")
		End If
		If CInt(rsLocations("LocationId")) = lngLocationId Then
%>
								<option selected value="<%= rsLocations("LocationId") %>"><%= strLocation %></option>
<%
		Else
%>
								<option value="<%= rsLocations("LocationId") %>"><%= strLocation %></option>
<%
		End If
		rsLocations.MoveNext
	Loop
End If

If IsObject(rsLocations) Then
	rsLocations.Close
	Set rsLocations = Nothing
End If

%>
							</select>
							</td>
						</tr>
						<tr>
							<td valign="top" class="TDAReq" style="font-weight:bold;">Or Delivery Address</td>
							<td valign="top"><textarea name="DeliverToLocation" rows="4" cols="30" onkeyup="parent.TrackCount(this,'textcount8',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" tabindex=7 ID="Textarea3"></textarea><br/>Characters Remaining: <input type="text" name="textcount8" size="4" value="500" readonly ID="Text1"></td>
						</tr>
						<tr height=30>
							<td valign="top" style="font-weight:bold;">Status</td>
							<td valign="top">Draft</td>
						</tr>
						<tr height=30>
							<td valign="top" style="font-weight:bold;">RFQ Date</td>
							<td valign="top">Today</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Terms</td>
							<td valign="top"><textarea name="Terms" rows="3" cols="30" onkeyup="parent.TrackCount(this,'textcount55',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea2"></textarea><br/>Characters Remaining: <input type="text" name="textcount55" size="4" value="500" readonly ID="Text2"></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;" class="TDAReq">Date Required</td>
							<td valign="top"><input type="input" value="" name="DateRequired" readonly ID="DateRequired"> <a href="javascript:showCal('Calendar18')"><img src="/Images/Calendar.gif" border=0></a></td>
						</tr>
						<tr>
							<td valign="top" colspan=2><span style="font-weight:bold;">Notes</span><br>These notes <strong>will be</strong> visible to the supplier<br><textarea name="IntroText" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea1"></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="500" readonly ID="Text5"></td>
						</tr>
					</table>
					<table width=760 cellpadding=5 cellspacing=0 border=0 ID="Table5">
						<tr>
							<td valign="top" colspan=4>
								<a name="Anchor_QuoteItems">
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table6">
									<tr>
										<td class="Quote_TD1"><b>Items</b></td>
										<td class="Quote_TD1" align="right"><input type="button" value="Insert Item Line" onclick="Items_InsertLine();" ID="Button2" NAME="Button2"></td>
									</tr>
									<tr>
										<td valign="top" colspan=2>
											<table width='100%' cellpadding='3' cellspacing='0' ID="Table7">
												<tr>
													<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>Quantity</td>
													<td width=610 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=180 height=1 border=0 alt=""><br>Item</td>
													<td width=40 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=60 height=1 border=0 alt=""><br>&nbsp;</td>
												</tr>
											</table>
											<div id="RFQItems">
											</div>
										</td>
									</tr>
								</table>
								<br>
							</td>
						</tr>
						<tr>
							<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};"> <input type="submit" value="Save" ID="Submit2" NAME="Submit1"></td>
						</tr>
						</form>
					</table>
				</td>
			</tr>
		</table>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
