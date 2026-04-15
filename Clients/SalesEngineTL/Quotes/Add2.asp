<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

intDivisionId = CInt(Request("DivisionId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsDi = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From Divisions Where DivisionId = " & intDivisionId
Set rsDi = dbConn.Execute(strSql)

strLogo = rsDi("Logo")

rsDi.Close
Set rsDi = Nothing

Dim boolDivisionManager
boolDivisionManager = SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), intDivisionId)

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

		var DivisionId = <%= intDivisionId %>;
		
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
			if (emptyField(document.Form1.ContactId)) {
				alert("Please select a Contact.");
				validFlag = false;
				document.Form1.ContactId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Validity)) {
				alert("Please enter Validity.");
				validFlag = false;
				document.Form1.Validity.focus();
			}}

			if (validFlag) {
			if (isNaN(document.Form1.Validity.value)) {
				alert("Please ensure Validity is valid.");
				validFlag = false;
				document.Form1.Validity.focus();
			}}
/*
			if (validFlag) {
			if (document.Form1.RealMargin.value>=100) {
				alert("Please ensure Margin less than 100%.");
				validFlag = false;
				document.Form1.RealMargin.focus();
			}}

			if (validFlag) {
			if (document.Form1.RealMargin.value==0) {
				if(!confirm("Are you sure you want to quote at 0% margin?")){
					validFlag = false;
					document.Form1.RealMargin.focus();
				}
			}}
*/

			return validFlag;
		}

		var itemLines=1;
		var extraLines=1;
		var thirdPartyLines=1;
		</script>
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>

		<script language="javascript">
			<!--#include virtual="/Clients/SalesEngineTL/System/Global.js"-->
		</script>

		<script language="javascript">
			<!--#include virtual="/Clients/SalesEngineTL/System/Quotes.js"-->
		</script>

	</head>
	<body bgcolor="#ffffff" onload="Items_InsertLine('<%= boolDivisionManager %>');">
<!-- removed ThirdParty_InsertLine(); from onload -->
<!--#include virtual="/System/ssi_Header.inc"-->
	<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Quotes/Default.asp?DivisionId=<%= intDivisionId %>" class="Header2">Quotes</a> / Add Quote /></span>
				<br/><br/>
					<table width="760" cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<input type="hidden" name="DivisionId" value="<%= intDivisionId %>">
						<input type="hidden" name="ItemLinesVal" value=1 ID="Hidden1">
						<input type="hidden" name="ExtraLinesVal" value=1 ID="Hidden2">
						<input type="hidden" name="ThirdPartyLinesVal" value=1 ID="Hidden3">
						<tr>
							<td valign="top" colspan=4 align="right" class="FormBtmTD"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <input type="submit" value="Save" ID="Submit1" NAME="Submit1"></td>
						</tr>
						<tr>
							<td valign="top" colspan=4><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""></td>
						</tr>
						<tr>
							<td valign="top" class="TDAReq" style="font-weight:bold;">Contact</td>
							<td valign="top" colspan=3>
							<select name="ContactId" style="width:280px;" ID="Select1" tabindex="1">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsContacts = Server.CreateObject("ADODB.RecordSet")
If Request.Cookies("UserSettings")("Code") = "TL0039" Then
	sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Deleted = 0 AND Code = '" & Request.Cookies("UserSettings")("Code") & "' ORDER BY CompanyName, Surname, FirstName"
Else
	sql = "SELECT * FROM Contacts_WithCustomersAndSuppliers_V2 WHERE ((Deleted = 0 AND NOT(FirstName = 'Admin') AND NOT(Surname = 'Contact') AND (Code = '" & Request.Cookies("UserSettings")("Code") & "')) OR (Deleted = 0 AND Code = 'TL0039')) ORDER BY CompanyName, Surname, FirstName"
End If
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
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId', 'Customer');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Status</td>
							<td valign="top">Draft</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Quote Date</td>
							<td valign="top">Today</td>
							<td valign="top" style="font-weight:bold;">Terms</td>
							<td valign="top"><textarea  tabindex="2" name="Terms" rows="3" cols="30" onkeyup="parent.TrackCount(this,'textcount999',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea2">F.I.S. via general road freight</textarea><br/>Characters Remaining: <input type="text" name="textcount999" size="4" value="<%= 500 - Len("F.I.S. via general road freight") %>" readonly ID="Text1"></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Project</td>
							<td valign="top"><input type="text" name="Reference" style="width:180px;" tabindex=3 ID="Text7" maxlength=50></td>
							<td valign="top" style="font-weight:bold;">Delivery</td>
							<td valign="top"><input type="text" name="Delivery" style="width:180px;" tabindex=4 ID="Text8" maxlength=50></td>
						</tr>
						<tr>
<%
If Request.Cookies("ClientSettings")("HasQuoteCOS") = "true" Then
%>
							<td valign="top" style="font-weight:bold;">Conditions of Sale</td>
							<td valign="top">
							<select name="QuoteCOSId" style="width:280px;" ID="Select2" tabindex="5">
								<option value="0"></option>
<%

	Set rsQCOS = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM QuoteCOS ORDER BY QuoteCOS"
	Set rsQCOS  = dbConn.Execute(sql)

	If Not(rsQCOS.BOF And rsQCOS.EOF) Then
		Do Until rsQCOS.EOF

%>
								<option value="<%= rsQCOS("QuoteCOSId") %>"><%= rsQCOS("QuoteCOS") %></option>
<%

			rsQCOS.MoveNext
		Loop
	End If

	If IsObject(rsQCOS) Then
		rsQCOS.Close
		Set rsQCOS = Nothing
	End If

%>
							</select>
							</td>
<%
Else
%>
							<td></td>
							<td></td>
<%
End If
%>
							<td valign="top" style="font-weight:bold;" class="TDAReq">Validity</td>
							<td valign="top"><input type="text" name="Validity" style="width:120px;" value=30 tabindex=6 ID="Text6" maxlength=3> days</td>
						</tr>

						<tr>
							<td valign="top" colspan=2 width=50%><span style="font-weight:bold;">Notes</span><br><small>These notes will be visible to the customer</small><br><textarea name="CustomerNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',1500)" onkeypress="parent.LimitText(this,1500)" style="width:100%;" ID="Textarea1" tabindex="7"></textarea><br/>Characters Remaining: <input type="text" name="textcount2" tabindex="8" size="4" value="1500" readonly ID="Text5"></td>
							<%
							If Request.Cookies("ClientSettings")("HasInternalNotes") = "true" Then
							%>
							<td valign="top" colspan=2 width=50%><span style="font-weight:bold;">Internal Notes</span><br><small>These notes will not be visible to the customer</small><br><textarea name="InternalNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',1500)" onkeypress="parent.LimitText(this,1500)" style="width:100%;"  tabindex="9"></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="1500" readonly ID="Text3"  tabindex="10"></td>
							<%
							End If
							%>
						</tr>
					</table>
					<table width=760 cellpadding=5 cellspacing=0 border=0 ID="Table5">
						<tr>
							<td valign="top" colspan=4>
								<a name="Anchor_QuoteItems">
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table6">
									<tr>
										<td colspan=2><p style="font-style:italic;">All prices are ex. GST.</p></td>
									</tr>
									<tr>
										<td class="Quote_TD1"><b>Items</b></td>
										<td class="Quote_TD1" align="right"><input type="button" value="Insert Item Line" onclick="Items_InsertLine();" ID="Button2" NAME="Button2" tabindex="11"></td>
									</tr>
									<tr>
										<td valign="top" colspan=2>
											<table width='99%' cellpadding='3' cellspacing='0' ID="Table7">
												<tr>
													<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=90 height=1 border=0 alt=""><br>Quantity</td>
													<!--<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>Item</td>-->
													<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>Type</td>
													<td width=250 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=250 height=1 border=0 alt=""><br>Description</td>
													<td width=220 class="Quote_TD2" valign="top" align='right'><img src="/Images/Spacer.gif" width=220 height=1 border=0 alt=""><br>Prices</td>
<%

If boolDivisionManager Then

%>
													<td width=40 class="Quote_TD2" valign="top" align='right'><img src="/Images/Spacer.gif" width=40 height=1 border=0 alt=""><br>Margin</td>
<%

Else

%>
													<td width=40 class="Quote_TD2" valign="top" align='right'><img src="/Images/Spacer.gif" width=40 height=1 border=0 alt=""></td>
<%

End If

%>
													<td width=20 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""><br>&nbsp;</td>
												</tr>
											</table>
											<div id="QuoteItems">
											</div>
										</td>
									</tr>
								</table>
								<br>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1" style="display:none;">
									<tr>
										<td class="Quote_TD1"><b>Third Party Supply</b></td>
										<td class="Quote_TD1" align="right"><input type="button" value="Insert Third Party Supply Line" onclick="ThirdParty_InsertLine();" ID="Button4" NAME="Button4"></td>
									</tr>
									<tr>
										<td colspan=2 valign="top">
											<div id="thirdPartyLines">
											</div>
										</td>
									</tr>
								</table>
								<br/>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table9">
									<tr>
										<td>
											<div align="right">
											<table cellpadding='3' cellspacing='0' ID="Table11">
												<tr>
													<td colspan=7 style="text-align:right;">
														<table cellpadding=0 cellspacing=0 border=0 ID="Table12">
															<tr>
																<td width=170 align="right"><b>Total Cost Ex. GST ($)</b></td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td><input type="text" name="UnitCostTotal" id="UnitCostTotal" class="Quote_Sub_Total_Field" value="0.00" readonly></td>
															</tr>
															<tr>
																<td colspan=3><img src="/Images/Spacer.gif" width=50 height=10 border=0></td>
															</tr>
															<tr>
																<td width=170 align="right"><b>Nett Price Total Ex. GST ($)</b></td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td><input type="text" name="NettPriceTotal" id="NettPriceTotal" class="Quote_Sub_Total_Field" value="0.00" readonly></td>
															</tr>
															<tr>
																<td colspan=3><img src="/Images/Spacer.gif" width=50 height=10 border=0></td>
															</tr>
															<tr>
																<td width=170 align="right"><b>Nett Price Total Inc. GST ($)</b></td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td><input type="text" name="NettPriceTotalInc" id="NettPriceTotalInc" class="Quote_Total_Field" value="0.00" readonly></td>
															</tr>
															<tr>
																<td colspan=3><img src="/Images/Spacer.gif" width=50 height=25 border=0></td>
															</tr>
															<tr>
																<td width=170 style="font-weight:bold;" align="right">Margin (%)</td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td valign="top"><input type="text" name="RealMargin" id="RealMargin" value="0.00" class="Quote_Total_Field" readonly></td>
															</tr>
														</table>
													</td>
												</tr>
											</table>
											</div>
										</td>
									</tr>
								</table>
								<br><br>
							</td>
						</tr>
						<tr>
							<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">&nbsp;<input type="submit" value="Save" ID="Submit2" NAME="Submit1"></td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
	</form>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
