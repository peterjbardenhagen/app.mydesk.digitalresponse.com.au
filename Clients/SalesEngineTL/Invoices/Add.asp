<%
' using Days from quoteordercontents as # Invoiced
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

intDivisionId = CInt(Request("DivisionId"))
lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim state
Sub GetState(stateId)
	If stateId = 1 Then
		state = "ACT"
	Elseif stateId = 2 Then
		state = "NSW"
	Elseif stateId = 3 Then
		state = "NT"
	Elseif stateId = 4 Then
		state = "QLD"
	Elseif stateId = 5 Then
		state = "SA"
	Elseif stateId = 6 Then
		state = "TAS"
	Elseif stateId = 7 Then
		state = "VIC"
	Elseif stateId = 8 Then
		state = "WA"
	Elseif stateId = 9 Then
		state = "Other"
	Else
		state = "Other"
	End If
End Sub

If lngQid > 0 Then
	Set rsQu = Server.CreateObject("ADODB.RecordSet")
	strSql = "SELECT Quotes.*, Contacts_WithCustomersAndSuppliers_V2.FirstName, Contacts_WithCustomersAndSuppliers_V2.Surname, Contacts_WithCustomersAndSuppliers_V2.CCompany, Contacts_WithCustomersAndSuppliers_V2.CompanyName, Contacts_WithCustomersAndSuppliers_V2.CompanyId, Contacts_WithCustomersAndSuppliers_V2.OAddress1, Contacts_WithCustomersAndSuppliers_V2.OAddress2, Contacts_WithCustomersAndSuppliers_V2.OSuburb, Contacts_WithCustomersAndSuppliers_V2.OPostCode, Contacts_WithCustomersAndSuppliers_V2.OStateId, Contacts_WithCustomersAndSuppliers_V2.OState, Contacts_WithCustomersAndSuppliers_V2.OCountry FROM Quotes INNER JOIN Contacts_WithCustomersAndSuppliers_V2 ON Quotes.ContactId = Contacts_WithCustomersAndSuppliers_V2.ContactId WHERE Qid = " & lngQid
	Set rsQu = dbConn.Execute(strSql)
	If Not(rsQu.BOF And rsQu.EOF) Then
		intDivisionId = rsQu("DivisionId")
		strCode = rsQu("Code")
		lngCompanyId = rsQu("CompanyId")
		strJobCompany = ""
		strCustomerPO = ""
		dblNettPriceTotal = CDbl(rsQu("NettPriceTotal"))
	End If
End If

%>
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

		// Check form for validation errors
		function checkForm() {

			var validFlag = true;

			if (validFlag) {
			if (emptyField(document.Form1.Code)) {
				alert("Please complete the Invoiced By field.");
				validFlag = false;
				document.Form1.Code.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.CCompany)) {
				alert("Please complete the Company field.");
				validFlag = false;
				document.Form1.CCompany.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.DivisionId)) {
				alert("Please Select an Entity.");
				validFlag = false;
				document.Form1.DivisionId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.DeliveryAddress)) {
				alert("Please enter Delivery Address.");
				validFlag = false;
				document.Form1.DeliveryAddress.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.InvoiceAddress)) {
				alert("Please enter Invoice Address.");
				validFlag = false;
				document.Form1.InvoiceAddress.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.NettPriceTotal)) {
				alert("Please complete the Total Value field.");
				validFlag = false;
				document.Form1.NettPriceTotal.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.GSTTotal)) {
				alert("Please complete the GST Total field.");
				validFlag = false;
				document.Form1.GSTTotal.focus();
			}}

			return validFlag;
		}
		</script>						
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Invoices</a> / Add Invoice</span>
				<br/><br/>
					<table width=770 cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<form method="post" name="Form1" action="Add_Proc.asp" onSubmit="return checkForm();">
						<input type="hidden" name="Qid" value="<%= lngQid %>">
						<input type="hidden" name="JobOrderId" value="<%= lngJobOrderId %>">
						<input type="hidden" name="DivisionId" value="<%= intDivisionId %>">
						<tr>
							<td style="font-weight:bold;color:red;">Invoiced By</td>
							<td>
								<select name="Code" ID="Select4">
								<option value="All">All users</option>
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If rsUsers("Code") = strCode Then
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
						
						
						<input type="hidden" name="CompanyId" id="CompanyId" value="142" />
			
<%

	Set rsCon = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Contacts Where ContactId = " & rsQu("ContactId")
	Set rsCon = dbConn.Execute(sql)
	If Not(rsCon.BOF And rsCon.EOF) Then
		strInvCompany = rsCon("CCompany")
		strInvAddress1 = rsCon("Address1")
		strInvAddress2 = rsCon("Address2")
		strInvSuburb = rsCon("Suburb")
		strInvState = rsCon("State")
		strInvCountry = rsCon("Country")
		strInvPostCode = rsCon("PostCode")
		intInvStateId = rsCon("StateId") ' StateId
		strDelCompany = rsCon("CCompany")
		strDelAddress1 = rsCon("OAddress1")
		strDelAddress2 = rsCon("OAddress2")
		strDelSuburb = rsCon("OSuburb")
		strDelState = rsCon("OState")
		strDelCountry = rsCon("OCountry")
		strDelPostCode = rsCon("OPostCode")
		intDelStateId = rsCon("OStateId") ' Del State Id
	End If
	rsCon.Close
	Set rsCon = Nothing
%>
			
						
						<tr>
							<td valign="top" style="font-weight:bold;">Customer PO#</td>
							<td valign="top"><input type="text" name="CustomerPO" style="width:100%;" tabindex=4 ID="Text1" maxlength=50 value="<% If rsQu("Poid") <> 0 Then Response.Write(rsQu("Poid")) %>"></td>
							<td valign="top" style="font-weight:bold;">Company</td>
						</tr>
						<tr>
							<td width=20% valign="top" class="TDAReq"style="font-weight:bold;">1) Get Delivery Address from Contact</td>
							<td width=30% valign="top">
							<select name="DelContactId" id="DelContactId" style="background-color:#8cd3ff;width:280px;" tabindex="1" onchange="DelContactId_Change()">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Deleted = 0 AND Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)
If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF
		If Len(rsContacts("OAddress1")) > 0 Then 'if has a delivery address
%>
								<option data-company="<%= rsContacts("CompanyName") %>" data-address1="<%= rsContacts("OAddress1") %>" data-address2="<%= rsContacts("OAddress2") %>" data-stateid="<%= rsContacts("OStateId") %>" data-suburb="<%= rsContacts("OSuburb") %>" data-country="<%= rsContacts("OCountry") %>" value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
		Else 'use invoice address insread
			If Len(rsContacts("Address1")) > 0 Then 'if has a delivery address
%>
									<option data-company="<%= rsContacts("CompanyName") %>" data-address1="<%= rsContacts("Address1") %>" data-address2="<%= rsContacts("Address2") %>" data-stateid="<%= rsContacts("StateId") %>" data-suburb="<%= rsContacts("Suburb") %>" data-country="<%= rsContacts("Country") %>"  value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
			End If		
		End If
		rsContacts.MoveNext
	Loop
End If

If IsObject(rsContacts) Then
	rsContacts.Close
	Set rsContacts = Nothing
End If

%>
							</select>
							</td>
							<td width=20% valign="top" class="TDAReq"style="font-weight:bold;">1) Get Invoice Address from Contact</td>
							<td width=30% valign="top">
							<select name="InvContactId" id="InvContactId" style="background-color:#fbf07b;width:280px;" tabindex="1" onchange="InvContactId_Change()">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Deleted = 0 AND Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Set rsContacts = dbConn.Execute(sql)
If Not(rsContacts.BOF And rsContacts.EOF) Then
	Do Until rsContacts.EOF
		If Len(rsContacts("Address1")) > 0 Then 'if has an invoice address
%>
								<option data-company="<%= rsContacts("CompanyName") %>" data-address1="<%= rsContacts("Address1") %>" data-address2="<%= rsContacts("Address2") %>" data-stateid="<%= rsContacts("StateId") %>" data-suburb="<%= rsContacts("Suburb") %>" data-country="<%= rsContacts("Country") %>"  value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
		Else ' use delivery
			If Len(rsContacts("OAddress1")) > 0 Then 'if has a delivery address
%>
								<option data-company="<%= rsContacts("CompanyName") %>" data-address1="<%= rsContacts("OAddress1") %>" data-address2="<%= rsContacts("OAddress2") %>" data-stateid="<%= rsContacts("OStateId") %>" data-suburb="<%= rsContacts("OSuburb") %>" data-country="<%= rsContacts("OCountry") %>" value="<%= rsContacts("ContactId") %>"><%= rsContacts("CompanyName") %> - <%= rsContacts("Surname") %>, <%= rsContacts("FirstName") %></option>
<%
			End If
		End If
		rsContacts.MoveNext
	Loop
End If

If IsObject(rsContacts) Then
	rsContacts.Close
	Set rsContacts = Nothing
End If

%>
							</select>
							</td>						
						</tr>
						
						
						
						<tr>
							<td width=20% valign="top" class="TDAReq"style="font-weight:bold;">2) Delivery Company</td>
							<td width=30% valign="top"><input type="text" name="DelCompany" style="background-color:#d9f1ff;width:100%;" tabindex=4 ID="DelCompany" maxlength=50 value="<%= strDelCompany %>"></td>						
							<td width=20% valign="top" class="TDAReq"style="font-weight:bold;">2) Invoice Company</td>
							<td width=30% valign="top"><input type="text" name="InvCompany" style="background-color:#FFFDD0;width:100%;" tabindex=4 ID="InvCompany" maxlength=50 value="<%= strInvCompany %>"></td>						
						</tr>
						
						
						<tr>
							<td width=20% valign="top" class="TDAReq"style="font-weight:bold;">3) Delivery Address</td>
							<td width=30% valign="top">
							Delivery Address from contact<br/><i>text is editable:</i><br/>
							<% GetState(intDelStateId) %>
							<textarea tabindex=5 name="DelAddress" id="DelAddress" style="background-color:#d9f1ff;width:250px;height:55px;"><% If Len(strDelAddress1) > 0 Then Response.Write(strDelAddress1 & CHR(10)) End If %>
							<% If Len(strDelAddress2) > 0 Then Response.Write(strDelAddress2 & CHR(10)) End If %>
							<% If Len(strDelAddress1) > 0 And Len(strDelSuburb) > 0 Then Response.Write(strDelSuburb & " ") End If %><% If Len(strDelAddress1) > 0 And Len(state) > 0 Then Response.Write(state & " ") End If %><% If Len(strDelAddress1) > 0 And Len(strDelCountry) > 0 Then Response.Write("" & strDelCountry) End If %> <% If Len(strDelPostCode) > 0 And Len(strDelPostCode) > 0 Then Response.Write(" " & strDelPostCode) End If %></textarea>
							</td>
							<input type="hidden" name="DelAddress1" value="<%= strDelAddress1 %>">
							<input type="hidden" name="DelAddress2" value="<%= strDelAddress2 %>">
							<input type="hidden" name="DelSuburb" value="<%= strDelSuburb %>">
							<input type="hidden" name="DelStateId" value="<%= intDelStateId %>">
							<input type="hidden" name="DelState" value="<% GetState(intDelStateId) %>">
							<input type="hidden" name="DelState" value="<%= strDelState %>">
							<input type="hidden" name="DelPostCode" value="<%= strDelPostCode %>">
							<input type="hidden" name="DelCountry"  value="<%= strDelCountry %>">
							<td width=20% valign="top" class="TDAReq" style="font-weight:bold;">3) Invoice Address</td>
							<td width=30% valign="top">
							Invoice Address from contact<br/><i>text is editable:</i><br/>
							<% GetState(intInvStateId) %>
							<textarea tabindex=6 name="InvAddress" id="InvAddress" style="background-color:#FFFDD0;width:250px;height:55px;"><% If Len(strInvAddress1) > 0 Then Response.Write(strInvAddress1 & CHR(10)) End If %>
							<% If Len(strInvAddress2) > 0 Then Response.Write(strInvAddress2 & CHR(10)) End If %>
							<% If Len(strInvAddress1) > 0 And Len(strInvSuburb) > 0 Then Response.Write(strInvSuburb & " ") End If %><% If Len(strInvAddress1) > 0 And Len(state) > 0 Then Response.Write(state & " ") End If %><% If Len(strInvAddress1) > 0 And Len(strInvCountry) > 0 Then Response.Write("" & strInvCountry) End If %> <% If Len(strDelPostCode) > 0 And Len(strDelPostCode) > 0 Then Response.Write(" " & strDelPostCode) End If %></textarea>
							</td>
							<input type="hidden" name="CCompany"  value="<%= strInvCompany %>">
							<input type="hidden" name="InvAddress1"  value="<%= strInvAddress1 %>">
							<input type="hidden" name="InvAddress2" value="<%= strInvAddress2 %>">
							<input type="hidden" name="InvSuburb" value="<%= strInvSuburb %>">
							<input type="hidden" name="InvStateId" value="<%= intInvStateId %>">
							<input type="hidden" name="InvState"value="<%= strInvState %>">
							<input type="hidden" name="InvState" value="<% GetState(intInvStateId) %>">
							<input type="hidden" name="InvPostCode" value="<%= strPostCode %>">
							<input type="hidden" name="InvCountry" value="<%= strInvCountry %>">
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Attention</td>
							<td valign="top"><input type="text" name="Attention" style="width:100%;" tabindex=7 ID="Text2" maxlength=50 value="<%= rsQu("Attention") %>"></td>
							<td valign="top" style="font-weight:bold;">Account</td>
							<td valign="top"><input type="text" name="Account" style="width:100%;" tabindex=8 ID="Text5" maxlength=50></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Invoice Date</td>
							<td valign="top">Today</td>
							<td valign="top" style="font-weight:bold;">Terms</td>
							<td valign="top"><textarea tabindex=9 name="Terms" rows="3" cols="30" onkeyup="parent.TrackCount(this,'textcount999',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea2"></textarea><br/>Characters Remaining: <input type="text" name="textcount999" size="4" value="500" readonly ID="Text3"></td>
						</tr>
						<tr>
							<td valign="top" colspan=4 width=50%><span style="font-weight:bold;">Notes</span><br><small>These notes will be visible to the customer</small><br><textarea name="CustomerNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea4" tabindex=10></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="500" readonly ID="Text6"></td>
						</tr>
<%
If Request.Cookies("ClientSettings")("HasInternalNotes") = "true" Then
%>
						<tr>
							<td valign="top" colspan=2 width=50%><span style="font-weight:bold;">Internal Notes</span><br><small>These notes will not be visible to the customer</small><br><textarea name="InternalNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea5" tabindex=11></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="500" readonly ID="Text7"></td>

						</tr>
<%
End If
%>						</tr>

						<tr>
							<td colspan=2><br></td>
						</tr>
<%

' Do job order contents or quote contents

Dim i
Dim dblNettPriceTotal


	' #########################################################################################
	' QUOTES

	Set rsQUC = Server.CreateObject("ADODB.RecordSet")
	strSql = "Select * From QuoteContents Where Qid = " & lngQid
	Set rsQUC = dbConn.Execute(strSql)

	i = 0

	If Not(rsQUC.BOF And rsQUC.EOF) Then
%>
						<tr>
							<td colspan=4>
								<b>Invoice these items</b>
								<table width=100% cellpadding=5>
									<tr>
										<td style="font-weight:bold;" width=25 valign="top">Qty</td>
										<td style="font-weight:bold;">Description</td>
										<td style="font-weight:bold;" width=75 valign="top">Sub-Total</td>
									</tr>
<%
		Do Until rsQUC.EOF

			If rsQUC("Days") = 0 Then ' days = number invoiced
				intOriginalQuantity = (rsQUC("Quantity"))
			Else
				intOriginalQuantity = (rsQUC("Quantity")-rsQUC("days"))
			End If

%>
									<input type="hidden" name="QuoteItemId<%= i %>" value="<%= rsQUC("QuoteItemId") %>" ID="Hidden25">
									<input type="hidden" name="JobOrderContentId<%= i %>" value="0" ID="Hidden28">
									<input type="hidden" name="Type<%= i %>" value="<%= rsQUC("Type") %>">
									<input type="hidden" name="ProductCode<%= i %>" value="<%= rsQUC("ProductCode") %>">
									<input type="hidden" name="Description<%= i %>" value="<%= rsQUC("Description") %>">
									<input type="hidden" name="NettPrice<%= i %>" value="<%= rsQUC("NettPrice") %>" ID="Hidden26">
									<input type="hidden" name="OriginalQuantity<%= i %>" value="<%= intOriginalQuantity %>" ID="Hidden27">
									<tr>
										<td width=25 valign="top"><input tabindex=13 type="text" name="Quantity<%= i %>" value="<%= intOriginalQuantity %>" id="Quantity<%= i %>" value="<%= intOriginalQuantity %>" style="width:45px;" onchange="document.Form1.SubTotal<%= i %>.value = document.Form1.Quantity<%= i %>.value * <%= rsQUC("NettPrice") %>;calculateAll()"></td>
										<td><% If Len(rsQUC("Type")) > 0 Then %>Type:</strong> <%= rsQUC("Type") %><% End If %><% If Len(rsQUC("ProductCode")) > 0 Then %> Code: <%= rsQUC("ProductCode") %>&nbsp;<% End If %> <%= rsQUC("Description") %> @ <%= FormatCurrency(rsQUC("NettPrice"),2) %></td>
										<td width=75 valign="top"><input tabindex=14 type="text" name="SubTotal<%= i %>" value="<%= Replace(FormatNumber(rsQUC("NettPrice")*intOriginalQuantity,2),",","") %>" id="SubTotal<%= i %>" value="<%= Replace(FormatNumber(rsQUC("NettPrice")*intOriginalQuantity,2),",","") %>" style="width:75px;text-align:right;" onchange="calculateAll()"></td>
									</tr>
<%
			i = i + 1
			rsQUC.MoveNext
		Loop

		rsQUC.Close
		Set rsQUC = Nothing
	End If

%>
						<input type="hidden" name="X" value="<%= i + 1 %>">
						<tr>
							<td colspan=4 align="right">
								<table>
									<tr>
										<td width=100 valign="top" style="font-weight:bold;">Sub Total ($)</td>
										<td valign="top"><input tabindex=30 readonly type="text" name="NettPriceTotal" style="width:100px;text-align:right;" tabindex=4 ID="NettPriceTotal" maxlength=50 value="0.00" style="text-align:right;"></td>
									</tr>
									<tr>
										<td width=100 valign="top" style="font-weight:bold;">GST ($)</td>
										<td valign="top"><input tabindex=31 readonly type="text" name="GSTTotal" style="width:100px;text-align:right;" tabindex=4 ID="GSTTotal" maxlength=50 value="0.00" style="text-align:right;"></td>
									</tr>
									<tr>
										<td width=100 valign="top" style="font-weight:bold;">Total Inc. GST ($)</td>
										<td valign="top"><input tabindex=32 readonly type="text" name="NettPriceTotalInc" style="width:100px;text-align:right;" tabindex=4 ID="NettPriceTotalInc" maxlength=50 value="0.00" style="text-align:right;"></td>
									</tr>
								</table>
							</td>
						</tr>
						<tr>
							<td colspan=4><br></td>
						</tr>
						<tr>
							<td colspan=4 valign="top" align="right"><input tabindex=33 type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};"> <input tabindex=34 type="submit" value="Next" ID="Submit2" NAME="Submit1"></td>
						</tr>
						</form>
					</table>
				</td>
			</tr>
		</table>
<script language="javascript">
	function calculateAll() {
		var i = 0.00;
		var x = <%= i %>;
		var total = 0;
		document.getElementById("SubTotal"+i).value = formatDecimal(parseFloat(document.getElementById("SubTotal"+i).value));
		while (i<(x)) {
			total = parseFloat(total) + parseFloat(document.getElementById("SubTotal"+i).value);
			i++;
		}
		document.getElementById("NettPriceTotal").value = formatDecimal(total);
		document.getElementById("GSTTotal").value = formatDecimal(total/10);
		document.getElementById("NettPriceTotalInc").value = formatDecimal(parseFloat(document.getElementById("NettPriceTotal").value) + parseFloat(document.getElementById("GSTTotal").value));
	}
	calculateAll();

	function deliveryAddress() {
		var s;
		var sState;
		if('<%= strDelAddress1 %>' == 'To be advised') {
		    s = 'To be advised';
		} else {
		    s = '<%= Replace(strDelCompany,"'","''") %>' + '\n';
		    sState = '<%= Replace(strDelState,"'","''") %>';
		    s += '<%= Replace(strDelAddress1,"'","''") %>' + '\n';
		    if(<%= Len(Replace(strDelAddress2,"'","''")) %>>0){
			    s += '<%= Replace(strDelAddress2,"'","''") %>' + '\n';
		    }
		    s += '<%= Replace(strDelSuburb,"'","''") %>' + ' ' + sState + ' ' + '<%= Replace(strDelCountry,"'","''") %>' + ' ' + '<%= strDelPostCode %>' + '\n';
        }
		return s;
	}
	deliveryAddress()
	
	
	function GetState(stateId) {
		var s = "";
		if (stateId == "1") {
			s = "ACT";
		}
		if (stateId == "2") {
			s = "NSW";
		}
		if (stateId == "3") {
			s = "NT";
		}
		if (stateId == "4") {
			s = "QLD";
		}
		if(stateId=="5"){s="SA";}
		if(stateId=="6"){s="TAS";}
		if(stateId=="7"){s="VIC";}
		if(stateId=="8"){s="WA";}
		if(stateId=="9" || stateId==""){s="Other";}
		return s;
	}

	function invoiceAddress() {
		var s;
		var sState;
		if('<%= strInvAddress1 %>' == 'To be advised') {
		    s = 'To be advised';
		} else {
		    s = '<%= Replace(strInvCompany,"'","''") %>' + '\n';
		    sState = '<%= Replace(strInvState,"'","''") %>';
		    s += '<%= Replace(strInvAddress1,"'","''") %>' + '\n';
		    if(<%= Len(Replace(strInvAddress2,"'","''")) %>>0){
			    s += '<%= Replace(strInvAddress2,"'","''") %>' + '\n';
		    }
		    s += '<%= Replace(strInvSuburb,"'","''") %>' + ' ' + sState + ' ' + '<%= Replace(strInvCountry,"'","''") %>' + ' ' + '<%= strInvPostCode %>' + '\n';
        }
		return s;
	}
	invoiceAddress()

	function DelContactId_Change() {
		try {
			// For Invoice Address Change
			var sel = document.getElementById('DelContactId');
			if(sel.selectedIndex > 0){
				var selected = sel.options[sel.selectedIndex];
				var company = selected.getAttribute('data-company');
				var address1 = selected.getAttribute('data-address1');
				var address2 = selected.getAttribute('data-address2');
				var suburb = selected.getAttribute('data-suburb');
				var stateId = selected.getAttribute('data-stateId');
				var postcode = selected.getAttribute('data-postcode');
				var country = selected.getAttribute('data-country');
				var s;
				var sState;
				if(address1 == 'To be advised') {
					s = 'To be advised';
				} else {
					//s = '<%= Replace(strInvCompany,"'","''") %>' + '\n';
					sState = GetState(stateId);
					s = address1 + '\n';
					if(!address2===""){
						s = s + address2 + '\n';
					}
					if(country==null || country=="null"){
						country="";
					}
					if(postcode==null || postcode=="null"){
						postcode="";
					}
					s = s + suburb + ' ' + sState + ' ' + country + ' ' + postcode + '\n';
				}
				var delCompany = document.getElementById("DelCompany");
				delCompany.value = company;

				var delAddress = document.getElementById("DelAddress");
				delAddress.value = s;
			}
		} catch(ex) {
			alert(ex);
		}
	}

	function InvContactId_Change() {
		try {
			// For Invoice Address Change
			var sel = document.getElementById('InvContactId');
			if(sel.selectedIndex > 0){
				var selected = sel.options[sel.selectedIndex];
				var company = selected.getAttribute('data-company');
				var address1 = selected.getAttribute('data-address1');
				var address2 = selected.getAttribute('data-address2');
				var suburb = selected.getAttribute('data-suburb');
				var stateId = selected.getAttribute('data-stateId');
				var postcode = selected.getAttribute('data-postcode');
				var country = selected.getAttribute('data-country');
				var s;
				var sState;
				if(address1 == 'To be advised') {
					s = 'To be advised';
				} else {
					//s = '<%= Replace(strInvCompany,"'","''") %>' + '\n';
					sState = GetState(stateId);
					s = address1 + '\n';
					if(!address2===""){
						s = s + address2 + '\n';
					}
					if(country==null || country=="null"){
						country="";
					}
					if(postcode==null || postcode=="null"){
						postcode="";
					}
					s = s + suburb + ' ' + sState + ' ' + country + ' ' + postcode + '\n';
				}
				var invCompany = document.getElementById("InvCompany");
				invCompany.value = company;

				var invAddress = document.getElementById("InvAddress");
				invAddress.value = s;
			}
		} catch(ex) {
			alert(ex);
		}
	}


</script>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->