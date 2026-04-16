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
			if (emptyField(document.Form1.CompanyId)) {
				alert("Please complete the Customer Account field.");
				validFlag = false;
				document.Form1.CompanyId.focus();
			}}

			if (validFlag) {
			if (document.Form1.CompanyId.value == 142 && emptyField(document.Form1.CCompany)) {
				alert("Please complete the Customer Company field.");
				validFlag = false;
				document.Form1.CCompany.focus();
			}}

			if (validFlag) {
			if (document.Form1.CompanyId.value != 142 && !emptyField(document.Form1.CCompany)) {
				alert("Please do not enter a Customer Company, if you have selected a Customer.");
				validFlag = false;
				document.Form1.CompanyId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.CustomerPO)) {
				alert("Please enter Customer Purchase Order #.");
				validFlag = false;
				document.Form1.CustomerPO.focus();
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

		var itemLines=1;
		var extraLines=1;
		var thirdPartyLines=1;
		</script>
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/JobOrders.js"></script>
	</head>
	<body bgcolor="#ffffff">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/JobOrders/Default.asp?DivisionId=<%= intDivisionId %>" class="Header2">Job Monitoring</a> / Add Job Order /></span>
				<br/><br/>
					<table width="760" cellpadding=5 cellspacing=0 border=0 ID="Table2">
						<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
						<input type="hidden" name="Qid" value="<%= lngQid %>" ID="Hidden4">
						<input type="hidden" name="DivisionId" value="<%= intDivisionId %>">
						<input type="hidden" name="ItemLinesVal" value=1 ID="Hidden1">
						<input type="hidden" name="ExtraLinesVal" value=1 ID="Hidden2">
						<input type="hidden" name="ThirdPartyLinesVal" value=1 ID="Hidden3">
						<tr>
							<td colspan=10>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 bgcolor="#555555" ID="Table3">
									<tr>
										<td valign="top" colspan=3 align="right" class="FormBtmTD"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button6" NAME="Button1"> <input type="submit" value="Save" ID="Submit1" NAME="Submit1"></td>
									</tr>
								</table>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Status</td>
							<td valign="top">Draft</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;">Date Accepted</td>
							<td valign="top">Today</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;color:red;">Customer</td>
							<td valign="top" colspan=3>
								<table ID="Table8">
									<tr>
										<td valign="top" style="font-weight:bold;color:red;">Account</td>
										<td valign="top">
										<select name="CompanyId" style="width:350px;" ID="Select2" onChange="toggleCompanyName()">
											<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
											<option value="142">Not an account</option>
<%
' Customers
Set rsCU = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Companies WHERE DivisionId IN (" & Request.Cookies("DivisionIdsAccess")("Visible") & ") AND Companies.CompanyId <> 142 AND Companies.Company <> '' AND Companies.CustomerCode <> '' ORDER BY Company"
Set rsCU = dbConn.Execute(sql)

If Not(rsCU.BOF And rsCU.EOF) Then
%>
												<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -</option>
												<option value="">Customers (listed below)</option>
												<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -</option>
<%
	Do Until rsCU.EOF
%>
												<option value="<%= rsCU("CompanyId") %>"><%= rsCU("Company") & " (Cust# " & rsCU("CustomerCode") & ")" %></option>
<%
		rsCU.MoveNext
	Loop
%>
												<option value=""></option>
												<option value=""></option>
												<option value=""></option>
												<option value=""></option>
												<option value=""></option>
												<option value=""></option>
												<option value=""></option>
												<option value=""></option>
<%
End If
If IsObject(rsCU) Then
	rsCU.Close
	Set rsCU = Nothing
End If
%>
											</select>
										</td>
									</tr>
									<tr id="CCompanyTR">
										<td valign="top"><span style="font-weight:bold;color:red;">Company</span><br><small style="color:red;">Where not an account.</small></td>
										<td valign="top"><input type="text" name="CCompany" style="width:280px;" maxlength=100 ID="Text14"></td>
									</tr>
									<tr>
										<td><img src="/Images/Spacer.gif" width=150 height=1 border=0 alt=""></td>
										<td></td>
									</tr>
								</table>
							</td>					
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;color:red;">Customer PO#</td>
							<td valign="top"><input type="text" name="CustomerPO" style="width:100%;" tabindex=4 ID="Text1" maxlength=50></td>
							<td valign="top" style="font-weight:bold;">Project</td>
							<td valign="top"><input type="text" name="Project" style="width:100%;" tabindex=4 ID="Text7" maxlength=50 value="<%= rsQu("Reference") %>"></td>
						</tr>
						<tr>
							<td width=20% valign="top" class="TDAReq" style="font-weight:bold;">Delivery Address</td>
							<td width=30% valign="top"><textarea name="DeliveryAddress" rows="4" cols="30" style="width:100%;" tabindex=7></textarea></td>
							<input type="hidden" name="DelCompany" value="" ID="Hidden12">
							<input type="hidden" name="DelAddress1" value="">
							<input type="hidden" name="DelAddress2" value="">
							<input type="hidden" name="DelSuburb" value="">
							<input type="hidden" name="DelStateId" value="">
							<input type="hidden" name="DelState" value="">
							<input type="hidden" name="DelPostCode" value="">
							<input type="hidden" name="DelCountry" value="">
							<td width=20% valign="top" class="TDAReq" style="font-weight:bold;">Invoice Address</td>
							<td width=30% valign="top"><textarea name="InvoiceAddress" rows="4" cols="30" style="width:100%;" tabindex=7 ID="Textarea3"></textarea></td>
							<input type="hidden" name="InvCompany" value="" ID="Hidden13">
							<input type="hidden" name="InvAddress1" value="" ID="Hidden5">
							<input type="hidden" name="InvAddress2" value="" ID="Hidden6">
							<input type="hidden" name="InvSuburb" value="" ID="Hidden7">
							<input type="hidden" name="InvStateId" value="" ID="Hidden8">
							<input type="hidden" name="InvState" value="" ID="Hidden9">
							<input type="hidden" name="InvPostCode" value="" ID="Hidden10">
							<input type="hidden" name="InvCountry" value="" ID="Hidden11">
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
										<td class="Quote_TD1" align="right"><input type="button" value="Insert Item Line" onclick="Items_InsertLine();" ID="Button2" NAME="Button2"></td>
									</tr>
									<tr>
										<td valign="top" colspan=2>
											<table width='700' cellpadding='3' cellspacing='0' ID="Table7">
												<tr>
													<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=90 height=1 border=0 alt=""><br>Quantity</td>
													<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>Item</td>
													<td width=50 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>Type</td>
													<td width=250 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=250 height=1 border=0 alt=""><br>Description</td>
													<td width=220 class="Quote_TD2" valign="top" align='right'><img src="/Images/Spacer.gif" width=220 height=1 border=0 alt=""><br>Prices</td>
													<td width=40 class="Quote_TD2" valign="top" align='right'><img src="/Images/Spacer.gif" width=40 height=1 border=0 alt=""></td>
													<td width=20 class="Quote_TD2" valign="top"><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""><br>&nbsp;</td>
												</tr>
											</table>
											<div id="QuoteItems">
											</div>
										</td>
									</tr>
								</table>
								<br>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
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
								<br>
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
																<td><input type="text" name="NettPriceTotal" id="Text2" class="Quote_Sub_Total_Field" value="0.00" readonly></td>
															</tr>
															<tr>
																<td colspan=3><img src="/Images/Spacer.gif" width=50 height=10 border=0></td>
															</tr>
															<tr>
																<td width=170 align="right"><b>Nett Price Total Inc. GST ($)</b></td>
																<td width=5><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
																<td><input type="text" name="NettPriceTotalInc" id="NettPriceTotalInc" class="Quote_Total_Field" value="0.00" readonly></td>
															</tr>
															<input type="hidden" name="RealMargin" value="0.00">
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
							<td colspan=10>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 bgcolor="#666666">
									<tr>
										<td valign="top" colspan=3 align="right" class="FormBtmTD"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button5" NAME="Button1"> <input type="submit" value="Save" ID="Submit2" NAME="Submit1"></td>
									</tr>
								</table>
							</td>
						</tr>
						</form>
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
		</script>
		<script language="javascript">
			function toggleCompanyName() {
				if(document.Form1.CompanyId.value == 142 || document.Form1.CompanyId.value == '') {
					document.Form1.CCompany.readOnly = false;
					document.getElementById('CCompanyTR').style.display = 'block';
				} else {
					document.Form1.CCompany.value = '';
					document.Form1.CCompany.readOnly = true;
					document.getElementById('CCompanyTR').style.display = 'none';
				}
			}
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
