<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngPOid = CLng(Request("POid"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "Select PO.*, PO.DivisionId As PODivisionId, C.*, U.*, PurchaseOrderStatus.* From (Users AS U INNER JOIN (PurchaseOrders AS PO INNER JOIN Contacts_WithCustomersAndSuppliers_V2 AS C ON PO.ContactId = C.ContactId) ON U.Code = PO.Code) INNER JOIN PurchaseOrderStatus ON PO.POStatusId = PurchaseOrderStatus.POStatusId Where PO.POid = " & lngPOid
Set rsPO = dbConn.Execute(sql)

intDivisionId = CInt(rsPO("PODivisionId"))

Set rsDi = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & intDivisionId
Set rsDi = dbConn.Execute(sql)

strLogo = rsDi("Logo")

rsDi.Close
Set rsDi = Nothing

'If (rsPO("POStatusId") = 2 Or rsPO("POStatusId") = 3) And Not(Request.Cookies("UserSettings")("Name") = GetPONextLineApprover(lngPOid, rsPO("HasCapEx"))) Then
'	Response.Redirect("Default.asp?Msg=You+cannot+edit+this+Purchase+Order+as+it+has+been+approved+or+is+waiting+for+approval.+If+you+require+this+to+be+updated+please+speak+to+your+line+manager.")
'ElseIf rsPO("POStatusId") = 6 Then
'	Response.Redirect("Default.asp?Msg=You+cannot+edit+this+Purchase+Order+as+it+has+been+cancelled.")
'ElseIf rsPO("POStatusId") = 7 Then
'	Response.Redirect("Default.asp?Msg=You+cannot+edit+this+Purchase+Order+as+it+has+been+received.")
'ElseIf rsPO("POStatusId") = 4 Then
'	Response.Redirect("Default.asp?Msg=You+cannot+edit+this+Purchase+Order+as+it+has+been+issued.")
'End If

%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/PurchaseOrders.css">
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
			if (emptyField(document.Form1.ContactId)) {
				alert("Please select Contact.");
				validFlag = false;
				document.Form1.ContactId.focus();
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
			if (emptyField(document.Form1.POPaymentTypeId)) {
				alert("Please complete Payment Type.");
				validFlag = false;
				document.Form1.POPaymentTypeId.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.DateRequired)) {
				alert("Please complete Date Required.");
				validFlag = false;
				document.Form1.DateRequired.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Project)) {
				alert("Please complete Project / Job / Replacement.");
				validFlag = false;
				document.Form1.Project.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.InternalNotes)) {
				alert("Please complete Reason For Purchase (and any other Internal Notes).");
				validFlag = false;
				document.Form1.InternalNotes.focus();
			}}

//			if(validFlag) {
//				if(confirm('Would you like to set the status to PENDING APPROVAL (or APPROVED if your access permits)?', 'yes', 'no')) {
					document.Form1.POStatusId.value = '2';
//				} else {
//					document.Form1.POStatusId.value = '1';
//				}
//			}

			return validFlag;			
		}

		var itemLines=1;

<%

Dim s
Dim rsProductTypes

Set rsProductTypes = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From PurchaseOrderProductTypes Order By InOrder, POProductType"
Set rsProductTypes = dbConn.Execute(sql)

If Not(rsProductTypes.BOF And rsProductTypes.EOF) Then
	strProductTypeSel = "<select name='POProductTypeId' id='POProductTypeId' style='width:100px;'>"
	Do Until rsProductTypes.EOF
		strProductTypeSel = strProductTypeSel & "<option value=" & rsProductTypes("POProductTypeId") & ">" & rsProductTypes("POProductType")
		rsProductTypes.MoveNext
	Loop
	strProductTypeSel = strProductTypeSel & "</select>"
%>
		var sProductTypeSel;
		sProductTypeSel = "<%= strProductTypeSel %>";
<%
End If

rsProductTypes.Close
Set rsProductTypes = Nothing

Dim rsPartCodes

Set rsPartCodes = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From PartCodes Where DivisionId = " & intDivisionId & " Order By PartCode"
Set rsPartCodes = dbConn.Execute(sql)

If Not(rsPartCodes.BOF And rsPartCodes.EOF) Then
	sPartCodeSel = "<select name='PartCodeId' id='PartCodeId' style='width:100px;'>"
	Do Until rsPartCodes.EOF
		sPartCodeSel = sPartCodeSel & "<option value=" & rsPartCodes("PartCodeId") & ">" & rsPartCodes("PartCode")
		rsPartCodes.MoveNext
	Loop
	sPartCodeSel = sPartCodeSel & "</select>"
%>
		var sPartCodeSel;
		sPartCodeSel = "<%= sPartCodeSel %>";
<%
End If

rsPartCodes.Close
Set rsPartCodes = Nothing

%>

		</script>
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>

		<script language="javascript">
			<!--#include virtual="/Clients/SalesEngineTL/System/Global.js"-->
		</script>

		<script language="javascript">
			<!--#include virtual="/Clients/SalesEngineTL/System/PurchaseOrders.js"-->
		</script>
	</head>
	<body bgcolor="#ffffff" onload="Switch_GST(<%= LCase(rsPO("GST")) %>);">
<!--#include virtual="/System/ssi_Header.inc"-->
	<form action="Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Purchase Orders</a> / Edit Purchase Order /></span>
				<br/><br/>
					<table width="760" cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<input type="hidden" name="POid" value="<%= lngPOid %>" ID="Hidden1">
						<input type="hidden" name="DivisionId" value="<%= rsPO("DivisionId") %>" ID="Hidden4">
						<tr>
							<td valign="top" colspan=4 align="right" class="FormBtmTD"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <input type="submit" value="Save" ID="Submit1" NAME="Submit1"></td>
						</tr>
						<tr>
							<td valign="top" colspan=4><img src="https://<%= Request.ServerVariables("SERVER_NAME") %><%= Request.Cookies("ClientSettings")("WorkingDir") %>/images/<%= strLogo %>" border=0 alt=""></td>
						</tr>
						<input type="hidden" name="ItemLinesVal" value=1 ID="Hidden2">
						<input type="hidden" name="ExtraLinesVal" value=1 ID="Hidden3">
						<tr>
							<td valign="top" style="font-weight:bold;">Relate to Quote</td>
							<td valign="top">
								<select style="width:280px;">
									<option value=0>Not related to a quote</option>
<%

Set rsQuotes = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From Quotes Where Quotes.DivisionId = " & intDivisionId & " Order By Qid"
Set rsQuotes = dbConn.Execute(strSql)

Do Until rsQuotes.EOF
	If CLng(rsQuotes("Qid")) = lngQid Then
		Response.Write "									<option selected value=""" & rsQuotes("Qid") & """>#" & rsQuotes("Qid") & " Reference: " & rsQuotes("Reference") & "</option>"
	Else
		Response.Write "									<option value=""" & rsQuotes("Qid") & """>#" & rsQuotes("Qid") & " Reference: " & rsQuotes("Reference") & "</option>"
	End If
	rsQuotes.MoveNext
Loop

%>
								</select>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Relate to RFQ</td>
							<td valign="top">
								<select style="width:280px;" ID="Select4" NAME="Select3">
									<option value=0>Not related to an RFQ</option>
<%

Set rsRFQs = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From RFQ Where RFQ.DivisionId = " & intDivisionId & " Order By RFQid"
Set rsRFQs = dbConn.Execute(strSql)

Do Until rsRFQs.EOF
	If CLng(rsRFQs("RFQid")) = lngRFQid Then
		Response.Write "									<option selected value=""" & rsRFQs("RFQid") & """>#" & rsRFQs("RFQid") & "</option>"
	Else
		Response.Write "									<option value=""" & rsRFQs("RFQid") & """>#" & rsRFQs("RFQid") & "</option>"
	End If
	rsRFQs.MoveNext
Loop

%>
								</select>
							</td>
						</tr>
						<tr>
							<td valign="top" class="TDAReq" style="font-weight:bold;">Contact</td>
							<td valign="top" colspan=3>
							<select name="ContactId" ID="Select1" style="width:280px;">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
								<option selected value="<%= rsPO("ContactId") %>"><%= rsPO("CompanyName") %> - <%= rsPO("Surname") %>, <%= rsPO("FirstName") %></option>
								<option value=""> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -</option>

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
							<a href="#" onclick="CreateNewContact('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', 'ContactId', 'Supplier');">Create New Contact</a>
							</td>
						</tr>
						<tr>
							<td valign="top" class="TDAReq" style="font-weight:bold;">Delivery Address</td>
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
		If CInt(rsLocations("LocationId")) = CInt(rsPO("DeliverToLocationId")) Then
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
							<td valign="top"><textarea name="DeliverToLocation" rows="4" cols="30" style="width:100%;" tabindex=7 ID="Textarea4"><%= rsPO("DeliverToLocation") %></textarea><br/>Characters Remaining: <input type="text" name="textcount8" size="4" value="<% If Len(rsPO("DeliverToLocation")) > 0 Then Response.Write 500-Len(rsPO("DeliverToLocation")) Else Response.Write 500 %>" readonly ID="Text4"></td>
						</tr>
						<tr>
							<td valign="top"><span style="font-weight:bold;">GST applicable</span><br>Not applicable on all international, optional for domestic.</td>
							<td valign="top"><input type="radio" name="GST" value="true" ID="Radio1" <% If rsPO("GST") Then Response.Write("checked") End If %> onclick="Switch_GST(true)">&nbsp;Yes<br><input type="radio" name="GST" value="false" onclick="Switch_GST(false)" <% If Not rsPO("GST") Then Response.Write("checked") End If %> ID="Radio1">&nbsp;No</td>
						</tr>
						<tr height=30>
							<td valign="top" style="font-weight:bold;">Status</td>
							<td valign="top">
<%

If rsPO("POStatusId") =< 2 Then

%>
							<select name="POStatusId" style="width:280px;" ID="Select7">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

	Set rsPOStatus = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From PurchaseOrderStatus Where POStatusId < 3 Order By InOrder"
	Set rsPOStatus = dbConn.Execute(sql)

	If Not(rsPOStatus.BOF And rsPOStatus.EOF) Then
		Do Until rsPOStatus.EOF
			If (CLng(rsPOStatus("POStatusId")) = CLng(rsPO("POStatusId"))) Then
%>
								<option selected value="<%= rsPOStatus("POStatusId") %>"><%= rsPOStatus("POStatus") %></option>
<%
			Else
%>
								<option value="<%= rsPOStatus("POStatusId") %>"><%= rsPOStatus("POStatus") %></option>
<%
			End If
			rsPOStatus.MoveNext
		Loop
	End If

	If IsObject(rsPOStatus) Then
		rsPOStatus.Close
		Set rsPOStatus = Nothing
	End If

%>
							</select>
<%

Else

%>
						<%= rsPO("POStatus") %>
						<input type="hidden" name="POStatusId" value="<%= rsPO("POStatusId") %>" ID="Hidden5">
<%

End If

%>
							</td>
						</tr>
						<tr height=30>
							<td valign="top" style="font-weight:bold;">P.O. Date</td>
							<td valign="top"><%= FormatDateU(rsPO("PODate"),False) %></td>
						</tr>
						<tr height=30>
							<td valign="top" style="font-weight:bold;">Payment Type</td>
							<td valign="top">
							<select name="POPaymentTypeId" style="width:280px;" tabindex=5 ID="Select5">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsP = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From PurchaseOrderPaymentTypes Order By POPaymentType"
Set rsP = dbConn.Execute(sql)

Do Until rsP.EOF
	If CLng(rsP("POPaymentTypeId")) = CLng(rsPO("POPaymentTypeId")) Then
%>
								<option selected value="<%= rsP("POPaymentTypeId") %>"><%= rsP("POPaymentType") %></option>
<%
	Else
%>
								<option value="<%= rsP("POPaymentTypeId") %>"><%= rsP("POPaymentType") %></option>
<%
	End If
	rsP.MoveNext
Loop

rsP.Close
Set rsP = Nothing

%>
							</select>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Terms</td>
							<td valign="top"><textarea name="Terms" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount3',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" tabindex=7 ID="Textarea3"><%= rsPO("Terms") %></textarea><br/>Characters Remaining: <input type="text" name="textcount3" size="4" value="<% If Len(rsPO("Terms")) > 0 Then Response.Write 500-Len(rsPO("Terms")) Else Response.Write 500 %>" readonly ID="Text2"></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;" class="TDAReq">Date Required</td>
							<td valign="top"><input type="input" value="<%= FormatDateU(rsPO("DateRequired"), False) %>" name="DateRequired" readonly ID="DateRequired" tabindex=8> <a href="javascript:showCal('Calendar18')"><img src="/Images/Calendar.gif" border=0></a></td>
						</tr>
<%

Dim strIntroText
Dim strInternalNotes

strIntroText = rsPO("IntroText")&""
strInternalNotes = rsPO("InternalNotes")&""

%>
						<tr>
							<td valign="top" colspan=2 width="50%"><span style="font-weight:bold;">Notes</span><br><small>These notes will be visible to the supplier</small><br><textarea name="IntroText" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount2',1500)" onkeypress="parent.LimitText(this,1500)" ID="Textarea1" style="width:100%;"><%= strIntroText %></textarea><br/>Characters Remaining: <input type="text" name="textcount2" size="4" value="<% If Len(strIntroText) > 0 Then Response.Write 1500-Len(strIntroText) Else Response.Write 1500 %>" readonly ID="Text5"></td>
							<td valign="top" colspan=2 width="50%" class="TDAReq"><strong>Project / Job / Replacement</strong><br /><input type="text" name="Project" style="width:280px;" tabindex=0 value="<%= rsPO("Project") %>" ID="Text1"  maxlength=50><br /><br /><span style="font-weight:bold;">Reason For Purchase (any any other Internal Notes)</span><br>These notes <b>will not be</b> visible to the supplier<br><textarea name="InternalNotes" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',1500)" onkeypress="parent.LimitText(this,1500)" style="width:100%;" tabindex=10><%= strInternalNotes %></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="<% If Len(strInternalNotes) > 0 Then Response.Write 1500-Len(strInternalNotes) Else Response.Write 1500 %>" readonly ID="Text6"></td>
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
											<table width='700' cellpadding='3' cellspacing='0' ID="Table7">
												<tr>
													<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>Quantity</td>
													<td width=355 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=355 height=1 border=0 alt=""><br>Item</td>
													<td width=80 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=80 height=1 border=0 alt=""><br>Unit Price Ex ($)</td>
													<td width=80 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=80 height=1 border=0 alt=""><br>Sub-Total Ex ($)</td>
													<td width=40 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=40 height=1 border=0 alt=""><br>&nbsp;</td>
												</tr>
											</table>
											<div id="PurchaseOrdersItems">
											</div>
										</td>
									</tr>
								</table>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table9">
									<tr>
										<td>
											<div align="right">
											<table cellpadding='3' cellspacing='0' ID="Table11">
												<tr>
													<td colspan=7 style="text-align:right;">
														<table cellpadding=0 cellspacing=0 border=0 ID="Table12">
															<tr>
																<td width=150 align="right"><b>Price Total Ex. GST</b></td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td>$<input type="text" name="PriceExTotal" id="PriceExTotal" class="Quote_Sub_Total_Field" value="0.00" readonly></td>
															</tr>
															<tr id="GST0">
																<td colspan=3><img src="/Images/Spacer.gif" width=50 height=10 border=0></td>
															</tr>
															<tr id="GST1">
																<td width=150 align="right"><b>GST</b></td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td>$<input type="text" name="PriceGSTTotal" id="PriceGSTTotal" class="Quote_Sub_Total_Field" value="0.00" readonly></td>
															</tr>
															<tr id="GST2">
																<td colspan=3><img src="/Images/Spacer.gif" width=50 height=10 border=0></td>
															</tr>
															<tr id="GST3">
																<td width=150 align="right"><b>Price Total Inc. GST</b></td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td>$<input type="text" name="PriceIncTotal" id="PriceIncTotal" class="Quote_Total_Field" value="0.00" readonly></td>
															</tr>
														</table>
													</td>
												</tr>
											</table>
											</div>
										</td>
									</tr>
								</table>
								<br>
							</td>
						</tr>
						<tr>
							<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button3" NAME="Button3"> <input type="submit" value="Save" ID="Submit2" NAME="Submit1"></td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
		</form>
	</body>
</html>
<script language="javascript">
<%

Set rsPOC = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From PurchaseOrderContents Where POid = " & lngPOid
Set rsPOC = dbConn.Execute(sql)

Do Until rsPOC.EOF

%>
	Items_InsertLine_WithData('<%= rsPOC("Quantity") %>', '<%= Replace(Replace(Replace(rsPOC("Description")&"","'","`"),vbcrlf,"\n"),CHR(13),"") %>', '<%= rsPOC("PriceEx") %>', '<%= rsPOC("PriceExSubTotal") %>', <%= rsPOC("POProductTypeId") %>, <%= rsPOC("PartCodeId") %>);
<%

	rsPOC.MoveNext
Loop

rsPOC.Close
Set rsPOC = Nothing

%>
	Items_InsertLine(true);
	console.log('testing');
	Items_CalcTotal();		
	CalcAll();
	
</script>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->