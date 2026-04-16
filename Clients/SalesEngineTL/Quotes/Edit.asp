<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
strSql = "Select Quotes.*, Divisions.Logo From Quotes Inner Join Divisions On Divisions.DivisionId = Quotes.DivisionId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(strSql)

intDivisionId = rsQu("DivisionId")
strLogo = rsQu("Logo")

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

			if (validFlag) {
			if (document.Form1.RealMargin.value>=100) {
				alert("Please ensure Margin less than 100%.");
				validFlag = false;
				document.Form1.RealMargin.focus();
			}}

			if (validFlag) {
			if (document.Form1.RealMargin.value==0) {
				if(!confirm("Are you sure you want to quote at cost?")){
					validFlag = false;
					document.Form1.RealMargin.focus();
				}
			}}

			return validFlag;
		}


		var itemLines=1;
		var extraLines=1;
		var thirdPartyLines=1;	
		</script>
				
		<script language="javascript">
			<!--#include virtual="/Clients/SalesEngineTL/System/Global.js"-->
		</script>

		<script language="javascript">
			<!--#include virtual="/Clients/SalesEngineTL/System/Quotes.js"-->
		</script>
	</head>
	<body bgcolor="#ffffff" onload="">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<form action="Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Quotes</a> / Edit Quote /></span>
				<br/><br/>
					<table width="760" cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<input type="hidden" name="Qid" value="<%= lngQid %>" ID="Hidden1">
						<input type="hidden" name="DivisionId" value="<%= intDivisionId %>" ID="Hidden5">
						<input type="hidden" name="ItemLinesVal" value=1 ID="Hidden6">
						<input type="hidden" name="ExtraLinesVal" value=1 ID="Hidden7">
						<input type="hidden" name="ThirdPartyLinesVal" value=1 ID="Hidden8">
						<tr>
							<td valign="top" colspan=4 align="right" class="FormBtmTD"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <input type="submit" value="Save" ID="Submit1" NAME="Submit1"></td>
						</tr>
						<tr>
							<td valign="top" colspan=4><img src="<%= GetProtocol() %><%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""></td>
						</tr>
<%
If Request.Cookies("UserSettings")("UserTypeId") = 6 Then
%>
						<tr>
							<td style="font-weight:bold;">User</td>
							<td>
								<select name="Code" ID="Select4">
								<option value="All">All users</option>
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
			If rsUsers("Code") = rsQu("Code") Then
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
						<tr>
							<td valign="top" style="font-weight:bold;">Quote From</td>
							<td>
								<select name="SenderCode" ID="SenderCode" style="width:280px;">
<%
	' Get list of active users who can be senders
	Set rsSenders = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Code, Name FROM Users WHERE Active = -1 ORDER BY Name"
	Set rsSenders = dbConn.Execute(sql)
	
	Dim strCurrentSender
	strCurrentSender = rsQu("SenderCode") & ""
	If strCurrentSender = "" Then strCurrentSender = rsQu("Code")  ' Default to quote owner if no sender set
	
	If Not(rsSenders.BOF And rsSenders.EOF) Then
		Do Until rsSenders.EOF
			If rsSenders("Code") = strCurrentSender Then
%>
									<option selected value="<%= rsSenders("Code") %>"><%= rsSenders("Name") %></option>
<%
			Else
%>
									<option value="<%= rsSenders("Code") %>"><%= rsSenders("Name") %></option>
<%
			End If
			rsSenders.MoveNext
		Loop
	End If
	rsSenders.Close
	Set rsSenders = Nothing
%>
								</select>
							</td>
						</tr>
<%
Else
%>
						<input type="hidden" name="Code" value="<%= rsQu("Code") %>">
						<input type="hidden" name="SenderCode" value="<%= rsQu("SenderCode") %>">
<%
End If
%>
						<tr>
							<td valign="top" class="TDAReq" style="font-weight:bold;">Contact</td>
							<td valign="top" colspan=3>
							<select name="ContactId" style="width:280px;" ID="Select1">
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
			If CLng(rsContacts("ContactId")) = CLng(rsQu("ContactId")) Then
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
							</select>
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId', 'Customer');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Status</td>
							<td valign="top">
							<select name="QuoteStatusId" style="width:280px;" ID="QuoteStatusId">
<%

Set rsStatus = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From QuoteStatus Where QuoteStatusId Not In (3,4,5,7,10,11) Order By QuoteStatus"
Set rsStatus = dbConn.Execute(sql)

If Not(rsStatus.BOF And rsStatus.EOF) Then
	Do Until rsStatus.EOF
'		If rsStatus("QuoteStatusId") <> 1 Or (rsStatus("QuoteStatusId") = 1 And Not(rsQu("QuoteStatusId") = 1)) Then
			If (CLng(rsStatus("QuoteStatusId")) = CLng(rsQu("QuoteStatusId")) Or (rsQu("QuoteStatusId") = 1 And rsStatus("QuoteStatusId") = 3)) Then
%>
								<option selected value="<%= rsStatus("QuoteStatusId") %>"><%= rsStatus("QuoteStatus") %></option>
<%
			Else
%>
								<option value="<%= rsStatus("QuoteStatusId") %>"><%= rsStatus("QuoteStatus") %></option>
<%
			End If
'		End If
		rsStatus.MoveNext
	Loop
End If

If IsObject(rsStatus) Then
	rsStatus.Close
	Set rsStatus = Nothing
End If

%>
							</select>
<%

If rsQu("QuoteStatusId") = 2 Or rsQu("QuoteStatusId") = 7 Or rsQu("QuoteStatusId") = 9 Or rsQu("QuoteStatusId") = 10 Or rsQu("QuoteStatusId") = 11 Then

%>
<script language="javascript">//document.getElementById("QuoteStatusId").value = 1;</script>
<%

End If

strTerms = rsQu("Terms")

%>
							</td>
							<td valign="top" style="font-weight:bold;">Terms</td>
							<td valign="top"><textarea name="Terms" rows="3" cols="30" onkeyup="parent.TrackCount(this,'textcount999',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" ID="Textarea2"><%= strTerms %></textarea><br/>Characters Remaining: <input type="text" name="textcount999" size="4" value="<% If Len(strTerms) > 0 Then Response.Write 500-Len(strTerms) Else Response.Write 500 %>" readonly ID="Text8"></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Quote Date</td>
							<td valign="top">Today</td>
							<td valign="top" style="font-weight:bold;">Delivery</td>
							<td valign="top"><input type="text" name="Delivery" style="width:180px;" tabindex=11 value="<%= rsQu("Delivery") %>" ID="Text6" maxlength=50></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Project</td>
							<td valign="top"><input type="text" name="Reference" style="width:180px;" tabindex=4 value="<%= rsQu("Reference") %>" maxlength=50></td>
							<td valign="top" style="font-weight:bold;" class="TDAReq">Validity</td>
							<td valign="top"><input type="text" name="Validity" style="width:120px;" tabindex=10 value="<%= rsQu("Validity") %>" ID="Text7" maxlength=3> days</td>
						</tr>
						<tr>
<%
If Request.Cookies("ClientSettings")("HasQuoteCOS") = "true" Then
%>
							<td valign="top" style="font-weight:bold;">Conditions of Sale</td>
							<td valign="top">
							<select name="QuoteCOSId" style="width:280px;" ID="Select3">
								<option value="0"></option>
<%

Set rsQCOS = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM QuoteCOS ORDER BY QuoteCOS"
Set rsQCOS  = dbConn.Execute(sql)

If Not(rsQCOS.BOF And rsQCOS.EOF) Then
	Do Until rsQCOS.EOF
		If rsQCOS("QuoteCOSId") = rsQu("QuoteCOSId") Then
%>
								<option selected value="<%= rsQCOS("QuoteCOSId") %>"><%= rsQCOS("QuoteCOS") %></option>
<%
		Else
%>
								<option value="<%= rsQCOS("QuoteCOSId") %>"><%= rsQCOS("QuoteCOS") %></option>
<%
		End If
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
						</tr>
<%

Dim strCustomerNotes
Dim strInternalNotes

strCustomerNotes = rsQu("CustomerNotes")&""
strInternalNotes = rsQu("InternalNotes")&""

%>
						<tr>
							<td valign="top" colspan=2 width=50%><span style="font-weight:bold;">Notes</span><br><small>These notes will be visible to the customer</small><br><textarea name="CustomerNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',1500)" onkeypress="parent.LimitText(this,1500)" ID="Textarea4" style="width:100%;"><%= strCustomerNotes %></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="<% If Len(strCustomerNotes) > 0 Then Response.Write 1500-Len(strCustomerNotes) Else Response.Write 1500 %>" readonly ID="Text5"></td>
							<%
							If Request.Cookies("ClientSettings")("HasInternalNotes") = "true" Then
							%>
							<td valign="top" colspan=2 width=50%><span style="font-weight:bold;">Internal Notes</span><br><small>These notes will not be visible to the customer</small><br><textarea name="InternalNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',1500)" onkeypress="parent.LimitText(this,1500)" ID="Textarea3" style="width:100%;"><%= strInternalNotes %></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="<% If Len(strInternalNotes) > 0 Then Response.Write 1500-Len(strInternalNotes) Else Response.Write 1500 %>" readonly ID="Text2"></td>
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
										<td valign="top" colspan=4>
											<a name="Anchor_QuoteItems">
											<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
												<tr>
													<td colspan=2><p style="font-style:italic;">All prices are ex. GST.</p></td>
												</tr>
												<tr>
													<td class="Quote_TD1"><b>Items</b></td>
													<td class="Quote_TD1" align="right"><input type="button" value="Insert Item Line" onclick="Items_InsertLine();" ID="Button3" NAME="Button2"></td>
												</tr>
												<tr>
													<td valign="top" colspan=2>
														<table width='760' cellpadding='3' cellspacing='0' ID="Table7">
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
											<div style="display:none;">
											<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
												<tr>
													<td class="Quote_TD1"><b>Third Party Supply</b></td>
													<td class="Quote_TD1" align="right"><input type="button" value="Insert Third Party Supply Line" onclick="ThirdParty_InsertLine();" ID="Button6" NAME="Button4"></td>
												</tr>
												<tr>
													<td colspan=2 valign="top">
														<div id="thirdPartyLines">
														</div>
													</td>
												</tr>
											</table>
											<br>
											</div>
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
																			<td valign="top"><input type="text" name="RealMargin" value="0.00" class="Quote_Total_Field" readonly ID="RealMargin"></td>
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
								</table>
							</td>
						</tr>
						<tr>
							<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button4" NAME="Button4"> <input type="submit" value="Save" ID="Submit2" NAME="Submit1" onclick="Quotes_CalcAll()"></td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
		<script language="javascript">
//			document.Form1.Margin.value = '<%= rsQu("Margin") %>';
<%

i = 2

Set rsQc = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From QuoteContents Where Qid = " & lngQid
Set rsQc = dbConn.Execute(sql)

Do Until rsQc.EOF
	If rsQc("Quantity") = 0 And (rsQc("Days") > 0 And rsQc("Units") > 0) Then
		boolPerUnitPerDay = True
	Else
		boolPerUnitPerDay = False
	End If
%>
			console.log('Add quote lines');
			Items_InsertLine_WithData(<%= i %>, <%= rsQc("Quantity") %>, <%= rsQc("Days") %>, <%= rsQc("Units") %>, '<%= Replace(rsQc("Type")&"","'","`") %>', '<%= Replace(Replace(rsQc("ProductCode")&"","'","`"),vbcrlf,"\n") %>', '<%= Replace(Replace(rsQc("Description")&"","'","`"),vbcrlf,"\n") %>', <%= rsQc("ProductId") %>, <%= rsQc("UnitCost") %>, <%= rsQc("MinNettPrice") %>, <%= rsQc("NettPrice") %>, <%= rsQc("UnitCostSubTotal") %>, 0, <%= rsQc("ExtNettPrice") %>, '<%= boolDivisionManager %>', '<%= boolPerUnitPerDay %>')
<%
		i = i + 1
	rsQc.MoveNext
Loop

rsQc.Close
Set rsQc = Nothing

' THIRD PARTY LINES

i = 2

Set rsQc = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From QuoteThirdPartyContents Where QuoteId = " & lngQid
Set rsQc = dbConn.Execute(strSql)

Do Until rsQc.EOF

%>
			ThirdParty_InsertLineWithData(<%= i %>, '<%= Replace(Replace(rsQc("Description")&"","'","`"),vbcrlf,"\n") %>', '<%= Replace(rsQc("Supplier")&"","'","`") %>', '<%= Replace(rsQc("QuoteNumber")&"","'","`") %>', '<%= Replace(rsQc("QuoteDate")&"","'","`") %>', '<%= Replace(rsQc("ExpiryDate")&"","'","`") %>', '<%= Replace(rsQc("SupplierPartNumber")&"","'","`") %>', '<%= Replace(rsQc("OurPartNumber")&"","'","`") %>', <%= Replace(rsQc("Quantity")&"","'","`") %>, '<%= Replace(rsQc("Type")&"","'","`") %>', <%= rsQc("UnitCost") %>, <%= rsQc("NettPrice") %>, <%= rsQc("Margin") %>, <%= rsQc("TotalCost") %>, <%= rsQc("NettPrice") %>)
<%
		i = i + 1
	rsQc.MoveNext
Loop

rsQc.Close
Set rsQc = Nothing

%>
			PostRender();
		</script>
		</form>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
