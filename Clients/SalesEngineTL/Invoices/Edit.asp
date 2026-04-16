<%

' using Days from quoteordercontents as # Invoiced

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngInvoiceId = CInt(Request("InvoiceId"))

Dim invAddress
Dim delAddress
invAddress = ""
delAddress = ""

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsInv = Server.CreateObject("ADODB.RecordSet")
strSql = "SELECT * FROM Invoices WHERE InvoiceId = " & lngInvoiceId
Set rsInv = dbConn.Execute(strSql)

If(rsInv("Qid") <> "") Then
	Set rsQu = Server.CreateObject("ADODB.RecordSet")
	strSql = "SELECT * FROM Quotes WHERE Qid = " & rsInv("Qid")
	Set rsQu = dbConn.Execute(strSql)
End If

lngQid = rsInv("Qid")
intDivisionId = rsInv("DivisionId")
lngDivisionId = rsInv("DivisionId")
strCode = rsInv("Code")
lngCompanyId = rsInv("CompanyId")
'strJobCompany = rsInv("JobCompany")
strCustomerPO = rsInv("CustomerPO")
strDelCompany = rsInv("DelCompany")
strDelAddress1 = rsInv("DelAddress1")
strDelAddress2 = rsInv("DelAddress2")
strDelSuburb = rsInv("DelSuburb")
strDelState = rsInv("DelState")
strDelCountry = rsInv("DelCountry")
strDelPostCode = rsInv("DelPostCode")
intDelStateId = rsInv("DelStateId")
strInvCompany = rsInv("InvCompany")
strInvAddress1 = rsInv("InvAddress1")
strInvAddress2 = rsInv("InvAddress2")
strInvSuburb = rsInv("InvSuburb")
strInvState = rsInv("InvState")
strInvCountry = rsInv("InvCountry")
strInvPostCode = rsInv("InvPostCode")
intInvStateId = rsInv("InvStateId")

invAddress = rsInv("InvAddress") & ""
delAddress = rsInv("DelAddress") & ""

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
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Invoices</a> / Edit Invoice</span>
				<br/><br/>
					<table width=770 cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<form method="post" name="Form1" action="Edit_Proc.asp" onSubmit="return checkForm();">
						<input type="hidden" name="InvoiceId" value="<%= lngInvoiceId %>">
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
						<input type="hidden" name="CompanyId" value="142" />
						<tr>
							<td valign="top"><span style="font-weight:bold;color:red;">Company</span</td>
							<td valign="top"><input type="text" name="CCompany" style="width:280px;" maxlength=100 ID="Text14" value="<%= rsInv("CCompany") %>"></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Customer PO#</td>
							<td valign="top"><input type="text" name="CustomerPO" style="width:100%;" tabindex=4 ID="Text1" maxlength=50 value="<%= rsInv("CustomerPO") %>"></td>
						</tr>
						<tr>
							<td width=20% valign="top" class="TDAReq" style="font-weight:bold;color:red;">Delivery Address</td>
							<td width=30% valign="top">
							
							Delivery Company: <input type="text" name="DelCompany" style="width:280px;" maxlength=100 ID="Text14" value="<%= rsInv("DelCompany") %>"><br/>
							
							<textarea name="DelAddress" rows="4" cols="30" style="width:250px;" tabindex=7 ID="Textarea1"><%= delAddress %></textarea></td>
							<td width=20% valign="top" class="TDAReq" style="font-weight:bold;color:red;">Invoice Address</td>
							<td width=30% valign="top">
							
							Invoice Company: <input type="text" name="InvCompany" style="width:280px;" maxlength=100 ID="Text14" value="<%= rsInv("InvCompany") %>"><br/>
							
							<textarea name="InvAddress" rows="4" cols="30" style="width:250px;" tabindex=7 ID="Textarea3"><%= invAddress %></textarea>
							
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Attention</td>
							<td valign="top"><input type="text" name="Attention" style="width:100%;" tabindex=4 ID="Text2" maxlength=50 value="<%= rsInv("Attention") %>"></td>
							<td valign="top" style="font-weight:bold;">Account</td>
							<td valign="top"><input type="text" name="Account" style="width:100%;" tabindex=4 ID="Text5" maxlength=50 value="<%= rsInv("Account") %>"></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Invoice Date</td>
							<td valign="top"><%= rsInv("InvoiceDate") %></td>
							<td valign="top" style="font-weight:bold;">Terms</td>
							<td valign="top"><textarea name="Terms" rows="3" cols="30" onkeyup="parent.TrackCount(this,'textcount999',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea2"><%= rsInv("Terms") %></textarea><br/>Characters Remaining: <input type="text" name="textcount999" size="4" value="500" readonly ID="Text3"></td>
						</tr>
						<tr>
							<td valign="top" colspan=4 width=50%><span style="font-weight:bold;">Notes</span><br><small>These notes will be visible to the customer</small><br><textarea name="CustomerNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea4"><%= rsInv("CustomerNotes") %></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="500" readonly ID="Text6"></td>
						</tr>

<%
If Request.Cookies("ClientSettings")("HasInternalNotes") = "true" Then
%>
						<tr>
							<td valign="top" colspan=2 width=50%><span style="font-weight:bold;">Internal Notes</span><br><small>These notes will not be visible to the customer</small><br><textarea name="InternalNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea5"><%= rsInv("InternalNotes") %></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="500" readonly ID="Text7"></td>
						</tr>
<%
End If
%>



						<tr>
							<td colspan=2><br></td>
						</tr>
						<tr>
							<td colspan=4 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};"> <input type="submit" value="Next" ID="Submit2" NAME="Submit1"></td>
						</tr>
						</form>
					</table>
				</td>
			</tr>
		</table>
<script language="javascript">
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
		DeliveryAddress_Select_InJobOrder(s, '<%= Replace(strDelCompany,"'","''") %>', '<%= Replace(strDelAddress1,"'","''") %>', '<%= Replace(strDelAddress2,"'","''") %>', '<%= Replace(strDelSuburb,"'","''") %>', '<%= intDelStateId %>', '<%= Replace(strDelState,"'","''") %>', '<%= strDelPostCode %>', '<%= Replace(strDelCountry,"'","''") %>');
		window.close();
	}
	deliveryAddress()

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
		InvoiceAddress_Select_InJobOrder(s, '<%= Replace(strInvCompany,"'","''") %>', '<%= Replace(strInvAddress1,"'","''") %>', '<%= Replace(strInvAddress2,"'","''") %>', '<%= Replace(strInvSuburb,"'","''") %>', '<%= intInvStateId %>', '<%= Replace(strInvState,"'","''") %>', '<%= strInvPostCode %>', '<%= Replace(strInvCountry,"'","''") %>');
		window.close();
	}
	invoiceAddress()

</script>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->