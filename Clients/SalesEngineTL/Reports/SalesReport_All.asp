<%
Option Explicit

Response.Buffer = True
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim intYear
Dim dteBegin
Dim dteEnd
Dim strCode
Dim intDivisionId

intYear = CLng(Request("Year"))
dteBegin = CStr("01-Jul-20" & MakePadding(Right(intYear, 2), "", 2))
dteEnd = CStr("30-Jun-20" & MakePadding(Right(intYear+1, 2), "", 2))
strCode = Trim(Request("Code"))
If InStr(strCode, "Division") > 0 Then
	intDivisionId = CLng(Replace(strCode, "Division_", ""))
Else
	intDivisionId = 0
End If

'On Error Resume Next

Sub NoResults
	Response.Redirect("SalesReportGen.asp?Code=" & strCode & "&Msg=No+orders+have+been+entered+for+any+staff+of+the+selected+division.")
End Sub
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
		<link rel="stylesheet" type="text/css" href="<%= Session("WorkingDir") %>/System/<%= Session("Stylesheet") %>">
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
			if (emptyField(document.Form1.ContactId)) {
				alert("Please select a Contact.");
				validFlag = false;
				document.Form1.ContactId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Project)) {
				alert("Please complete the Project field.");
				validFlag = false;
				document.Form1.Project.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.Product)) {
				alert("Please complete the Product/Service field.");
				validFlag = false;
				document.Form1.Product.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.Value)) {
				if (isNaN(document.Form1.Value.value)) {
					alert("Please enter a valid number for the Value field.");
					validFlag = false;
					document.Form1.Value.focus();
				}
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.AmountPerMonth)) {
				if (isNaN(document.Form1.AmountPerMonth.value)) {
					alert("Please enter a valid number for the Amount Per Month field.");
					validFlag = false;
					document.Form1.AmountPerMonth.focus();
				}
			}}

			if (validFlag) {
			if (emptyField(document.Form1.PotentialOrderDate)) {
				alert("Please complete the Potential Order Date field.");
				validFlag = false;
				document.Form1.PotentialOrderDate.focus();
			}}
			return validFlag
		}

		</script>
		<style>
			body, p, td {
				font-size: 10px;
			}
			.Normal {
				font-size: 12px;
			}
			.NormalBold {
				font-weight: bold;
				font-size: 12px;
			}
			.NormalTimesBoldItalic {
				font-family: times new roman;
				font-style: italic;
				font-weight: bold;
				font-size: 12px;
			}
			.HdrTimesBoldItalic {
				font-family: times new roman;
				font-style: italic;
				font-weight: bold;
				font-size: 14px;
			}
		</style>
	</head>
	<body bgcolor="#ffffff">
<!--#include virtual="/System/ssi_Header.inc"-->
<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
	<tr>
		<td><input type="button" value="Generate another report" onclick="document.location.href='SalesReportGen.asp';" ID="Button1" NAME="Button1"> <input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Printing reports works best in landscape with minimal Margins)</td>
	</tr>
</table>
<table bgcolor="#cccccc" width="100%" cellpadding=10 cellspacing=0 border=0>
	<tr>
		<td>
<%
If intDivisionId <> 0 Then
	Set rsDI = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Divisions Where DivisionId = " & intDivisionId
	Set rsDI = dbConn.Execute(sql)
	If Not(rsDI.BOF And rsDI.EOF) Then
%>
		<span style="font-size:12px;font-weight:bold;">Division:</span> <span style="font-size:12px;"><%= rsDI("Division") %></span>
<%
	End If
Else
%>
		<span style="font-size:12px;font-weight:bold;">Division:</span> <span style="font-size:12px;">All divisions</span>
<%
End If
%>
<br><br>
<%
Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SalesReport_Names_ByDivision " & intDivisionId & ", " & intYear
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
%>
		<span style="font-size:12px;font-weight:bold;">Orders entered for:</span><br>
<%
	Do Until rs.EOF
		strUserCodes = strUserCodes & "'" & rs("Code") & "',"
%>
		<li style="font-size:12px;"><%= rs("Name") %></li><br>
<%
		rs.MoveNext
	Loop
	If Right(strUserCodes, 1) = "," Then strUserCodes = Left(strUserCodes, Len(strUserCodes)-1)
%>
		<br>Expenses, prospects and sales data will only be visable for sales people that have orders entered.
<%
Else
	strUserCodes = "'no codes'"
%>
		<span style="font-size:12px;font-weight:bold;">No orders entered</span><br>
<%
End If
%>
		</td>
	</tr>
</table>
<%
Dim sql
Dim rsSR
Dim rsSP
Dim rsEX
Dim z
Dim arrSR(5,12)
Dim arrEX(1,12)
Dim aMinNettPriceR(1,12)
Dim decRunningSalesBudget
Dim decRunningSalesActual
Dim decRunningProspTotal
Dim strSalesPersonName
Dim decExpensesPerMonth
Dim decProspectsBudget
Dim boolHasSalesResults

boolHasSalesResults = False

Set rsSP = Server.CreateObject("ADODB.RecordSet")
sql = "Select Sum(ExpensesPerMonth) As MonthlyExpensesBudget, Sum(ProspectsBudget) As MonthlyProspectsBudget, Sum(SalesBudget) As MonthlySalesBudget From Users Where DivisionId = " & intDivisionId
Set rsSP = dbConn.Execute(sql)

If Not(rsSP.BOF And rsSP.EOF) Then
	strSalesPersonName = "All Sales People"
	decExpensesPerMonth = rsSP("MonthlyExpensesBudget")
	decProspectsBudget = rsSP("MonthlyProspectsBudget")
	decMonthlySalesBudget = rsSP("MonthlySalesBudget")
	If Not decExpensesPerMonth > 0 Or Not IsNumeric(decExpensesPerMonth) Then decExpensesPerMonth = 0
	If Not decProspectsBudget > 0 Or Not IsNumeric(decProspectsBudget) Then decProspectsBudget = 0
End If

' Get sales results and generate array
Set rsSR = Server.CreateObject("ADODB.RecordSet")
sql = "Select Sum(Value) As TotalValue, Sum(SalesBudget) As TotalSalesBudget, [Date] From SalesResults Where Code In (" & strUserCodes & ") And [Date] >= #" & dteBegin & "# And [Date] <= #" & dteEnd & "# Group By [Date] Order By [Date]"
Set rsSR = dbConn.Execute(sql)

z = 1

If Not(rsSR.BOF And rsSR.EOF) Then
	Do
		arrSR(0,z) = rsSR("TotalSalesBudget")
		arrSR(1,z) = rsSR("TotalValue")
		decRunningSalesBudget = decRunningSalesBudget + rsSR("TotalSalesBudget")
		decRunningSalesActual = decRunningSalesActual + rsSR("TotalValue")
		If z = 12 Then Exit Do
		z = z + 1
		rsSR.MoveNext
	Loop
	boolHasSalesResults = True
Else
	Call NoResults
End If

If IsObject(rsSR) Then
	rsSR.Close
	Set rsSR = Nothing
End If

' Get expenses and generate array
Set rsEX = Server.CreateObject("ADODB.RecordSet")
sql = "Select Month(ExpenseDate) As ExpMonth, Sum(CostIncGST) As ExpValue From Expenses Where Code In (" & strUserCodes & ") And [ExpenseDate] >= #" & dteBegin & "# And [ExpenseDate] <= #" & dteEnd & "# Group By Month(ExpenseDate) Order By Month(ExpenseDate)"
Set rsEX = dbConn.Execute(sql)


decRunningExpenseTotal = 0

If Not(rsEX.BOF And rsEX.EOF) Then
	Do Until rsEX.EOF
		arrEX(0,rsEX("ExpMonth")) = FormatNumber(rsEX("ExpValue"),2)
		decRunningExpenseTotal = decRunningExpenseTotal + FormatNumber(rsEX("ExpValue"),2)
		rsEX.MoveNext
	Loop
End If

If IsObject(rsEX) Then
	rsEX.Close
	Set rsEX = Nothing
End If

' Get prospects and generate array
Set rsPR = Server.CreateObject("ADODB.RecordSet")
sql = "Select Month(ProspectDate) As ProspMonth, Sum(Value) As ProspValue, Sum(AmountPerMonth*NumberOfMonths) As ProspValue2 From SalesProjects Where Code In (" & strUserCodes & ") And [ProspectDate] >= #" & dteBegin & "# And [ProspectDate] <= #" & dteEnd & "# Group By Month(ProspectDate) Order By Month(ProspectDate)"
Set rsPR = dbConn.Execute(sql)

decRunningProspTotal = 0

If Not(rsPR.BOF And rsPR.EOF) Then
	Do Until rsPR.EOF
		aMinNettPriceR(0,rsPR("ProspMonth")) = rsPR("ProspValue") + rsPR("ProspValue2")
		decRunningProspTotal = decRunningProspTotal + FormatNumber(rsPR("ProspValue"),2) + FormatNumber(rsPR("ProspValue2"),2)
		rsPR.MoveNext
	Loop
End If

If boolHasSalesResults Then
%>
	<table width="100%" cellpadding=5 cellspacing=0 border=0>
		<tr>
			<td>
				<table width="1000" cellpadding=5 cellspacing=0 border=0>
					<tr>
						<td colspan=10 style="font-size:16px;font-weight:bold;"><%= strSalesPersonName %> Sales & Expenditure Report for <%= intYear & "/" & Right(intYear+1,2) %></td>
						<td colspan=5 style="font-size:15px;text-align:right;">Budget Target YTD: <b><% If decRunningSalesActual > 0 And decRunningSalesBudget > 0 Then Response.Write(FormatNumber(decRunningSalesActual/decRunningSalesBudget*100, 2)) Else Response.Write FormatNumber(0,2) %>%</b></td>
					</tr>
					<tr>
						<td></td>
						<td></td>
<%
	Dim i
	Dim x
	i = 7
	x = 0
	Do
%>
						<td style="font-weight:bold;font-style:italic;text-align:right;"><%= MonthName(i, True) & "'" & Right(intYear+x,2) %></td>
<%
		If i = 6 Then
			Exit Do
		End If

		i = i + 1

		If i = 13 Then
			i = 1
			x = 1
		End If
	Loop
%>
						<td style="font-weight:bold;font-style:italic;border-left:1px solid black;text-align:right;">Total</td>
					</tr>
					<tr>
						<td colspan=14 style="font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;border-top:2px solid black;">ORDERS</td>
						<td style="border-top:1px solid black;border-left:1px solid black;border-top:2px solid black;">&nbsp;</td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Actual</td>
<%
	i = 1
	Do Until i = 13
%>
						<td align="right"><%= FormatCurrency(arrSR(1,i),0) %></td>
<%
		i = i + 1
	Loop
%>
						<td style="border-left:1px solid black;text-align:right;"><%= FormatCurrency(decRunningSalesActual,0) %></td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Budget</td>
<%
	i = 1
	Do Until i = 13
%>
						<td style="text-align:right;"><%= FormatCurrency(arrSR(0,i),0) %></td>
<%
		i = i + 1
	Loop
%>
						<td style="border-left:1px solid black;text-align:right;"><%= FormatCurrency(decRunningSalesBudget,0) %></td>
					</tr>
					<tr>
						<td></td>
						<td style="border-top:1px solid black;font-weight:bold;text-align:right;">Budget %</td>
<%
	i = 1
	Do
%>
						<td style="border-top:1px solid black;text-align:right;"><% If arrSR(1,i) > 0 And arrSR(0,i) > 0 Then Response.Write FormatNumber(arrSR(1,i)/arrSR(0,i)*100,2) Else Response.Write FormatNumber(0,2) %>%</td>
<%
		If i = 12 Then Exit Do
		i = i + 1
	Loop
%>
						<td style="border-top:1px solid black;border-left:1px solid black;text-align:right;"><% If decRunningSalesActual > 0 And decRunningSalesBudget > 0 Then Response.Write FormatNumber(decRunningSalesActual/decRunningSalesBudget*100, 2) Else Response.Write FormatNumber(0,2) %>%</td>
					</tr>
<!-- Expenses -->
					<tr>
						<td colspan=14 style="border-top:2px solid black;font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;">EXPENSES</td>
						<td style="border-top:2px solid black;border-left:1px solid black;">&nbsp;</td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Actual</td>
<%
	i = 1
	x = 7
	Do Until i = 13
%>
						<td style="text-align:right;"><%= FormatCurrency(arrEX(0,x),0) %></td>
<%
		x = x + 1
		If x = 13 Then x = 1
		i = i + 1
	Loop
%>
						<td style="border-left:1px solid black;text-align:right;"><%= FormatCurrency(decRunningExpenseTotal,0) %></td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Budget</td>
<%
	i = 1
	x = 7
	Do Until i = 13
%>
						<td style="text-align:right;"><%= FormatCurrency(decExpensesPerMonth,0) %></td>
<%
		x = x + 1
		If x = 13 Then x = 1
		i = i + 1
	Loop
%>
						<td style="border-left:1px solid black;text-align:right;"><%= FormatCurrency(decExpensesPerMonth,0) %></td>
					</tr>
					<tr>
						<td></td>
						<td style="border-top:1px solid black;font-weight:bold;text-align:right;">Budget %</td>
<%
	i = 1
	x = 7
	Do Until i = 13
%>
						<td style="border-top:1px solid black;text-align:right;"><% If decExpensesPerMonth > 0 Then Response.Write(FormatNumber((arrEx(0,x)/decExpensesPerMonth)*100,2)) Else Response.Write FormatNumber(0,2) End If %>%</td>
<%
		x = x + 1
		If x = 13 Then x = 1
		i = i + 1
	Loop
%>
						<td style="border-left:1px solid black;text-align:right;border-top:1px solid black;"><% If decRunningExpenseTotal > 0 And decExpensesPerMonth > 0 Then Response.Write FormatNumber(decRunningExpenseTotal/decExpensesPerMonth*12,2) Else Response.Write FormatNumber(0,2) %>%</td>
					</tr>
<!--
					<tr>
						<td></td>
						<td style="text-align:right;">Entertainment</td>
					</tr>
					<tr>
						<td></td>
						<td style="text-align:right;">Gift</td>
					</tr>
					<tr>
						<td></td>
						<td style="text-align:right;">Accommodation</td>
					</tr>
					<tr>
						<td></td>
						<td style="text-align:right;">Meeting</td>
					</tr>
					<tr>
						<td></td>
						<td style="text-align:right;">Other MV Expense</td>
					</tr>
					<tr>
						<td></td>
						<td style="text-align:right;">Parking</td>
					</tr>
-->
					<tr>
						<td colspan=14>
							<table width="100%" cellpadding=0 cellspacing=0 border=0>
								<tr>
									<td align="center" style="text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"  codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="400" HEIGHT="350" id="FC2Column" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data1.asp*Code=All*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=<%= intDivisionId %>*UserCodes=<%= strUserCodes %>">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=400&chartHeight=350">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=400&chartHeight=350" FlashVars="dataURL=SalesReport_Data1.asp*Code=All*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=<%= intDivisionId %>*UserCodes=<%= strUserCodes %>" quality=high bgcolor=#FFFFFF WIDTH="400" HEIGHT="350" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>
									</td>
									<td align="center" style="text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="400" HEIGHT="350" id="Object1" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data2.asp*Code=All*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=<%= intDivisionId %>*UserCodes=<%= strUserCodes %>">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSLine_2.swf?chartWidth=400&chartHeight=350">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSLine_2.swf" flashvars="&dataURL=SalesReport_Data2.asp*Code=All*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=<%= intDivisionId %>*UserCodes=<%= strUserCodes %>" quality=high bgcolor=#FFFFFF WIDTH="400" HEIGHT="350" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>						
									</td>
								</tr>
							</table>
						</td>
					</tr>
<!-- Prospects -->
<%
	If Not(rsPR.BOF And rsPR.EOF) Then
%>
					<tr>
						<td colspan=14 style="border-top:2px solid black;font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;">PROSPECTS</td>
						<td style="border-top:2px solid black;border-left:1px solid black;">&nbsp;</td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Actual</td>
<%
		i = 1
		x = 7
		Do Until i = 13
%>
						<td style="text-align:right;"><%= FormatCurrency(aMinNettPriceR(0,x),0) %></td>
<%
			i = i + 1
			x = x + 1
			If x = 13 Then x = 1
		Loop
%>
						<td style="border-left:1px solid black;text-align:right;"><%= FormatCurrency(decRunningProspTotal,0) %></td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Budget</td>
<%
		i = 1
		Do Until i = 13
%>
						<td style="text-align:right;"><%= FormatCurrency(decProspectsBudget,0) %></td>
<%
			i = i + 1
		Loop
%>
						<td style="border-left:1px solid black;text-align:right;"><%= FormatCurrency(decProspectsBudget*12,0) %></td>
					</tr>
					<tr>
						<td colspan=15 style="border-top:2px solid black;font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;">PROSPECT DETAILS</td>
					</tr>
<%
		Set rsPR = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Contacts_WithCustomersAndSuppliers.Company, Contacts_WithCustomersAndSuppliers.FirstName, Contacts_WithCustomersAndSuppliers.Surname, SalesProjects.*, [Value] As ProspValue, AmountPerMonth*NumberOfMonths As ProspValue2, Month(ProspectDate) As ProspMonth, Year(ProspectDate) As ProspYear From SalesProjects Inner Join Contacts_WithCustomersAndSuppliers On Contacts_WithCustomersAndSuppliers.ContactId = SalesProjects.ContactId Where SalesProjects.Code In (" & strUserCodes & ") And [ProspectDate] >= #" & dteBegin & "# And [ProspectDate] <= #" & dteEnd & "# Order By ProspectDate"
		Set rsPR = dbConn.Execute(sql)
%>
					<tr>
						<td colspan=15>
							<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
<%
		Dim intCurrMonth
		Dim intCurrYear
		Dim decCuMinNettPricerospValue
		Dim decRunningMonthTotal
		Dim k
		Dim g

		k = 0
		decRunningMonthTotal = CDbl(0)
		intCurrMonth = 1
		intCurrYear = 5

		Do Until rsPR.EOF
			decCuMinNettPricerospValue = 0
			If rsPR("ProspValue") > 0 Then
				decCuMinNettPricerospValue = decCuMinNettPricerospValue + FormatNumber(rsPR("ProspValue"),2)
			End If
			If rsPR("ProspValue2") > 0 Then
				decCuMinNettPricerospValue = decCuMinNettPricerospValue + FormatNumber(rsPR("ProspValue2"),2)
			End If
			
			If k = 0 Then
				intCurrMonth = rsPR("ProspMonth")
				intCurrYear = rsPR("ProspYear")
			End If

			If CLng(intCurrMonth) <> CLng(rsPR("ProspMonth")) Or k = 0 Then	
				intCurrMonth = rsPR("ProspMonth")
				intCurrYear = rsPR("ProspYear")

				If k <> 0 Then
%>
								<tr>
									<td colspan=2></td>
									<td class="Normal" align="right" style="border-top:1px solid black;text-align:right;" width=100><%= FormatCurrency(decRunningMonthTotal,0) %></td>
								</tr>
<%
					decRunningMonthTotal = 0
				End If
%>
								<tr>
									<td class="Normal" style="font-family:times new roman;font-size:14px;font-style:italic;"><%= MonthName(intCurrMonth, False) & " " & intCurrYear %></td>
								</tr>
<%
			End If
%>
								<tr>
									<td class="Normal" width=300 valign="top"><%= rsPR("Project") %></td>
									<td class="Normal" width=390 valign="top"><% If Len(rsPR("Comment")) > 0 Then Response.Write rsPR("Comment") Else Response.Write "No description entered" %></td>
									<td class="Normal" align="right" width=100 valign="top"><%= FormatCurrency(decCuMinNettPricerospValue,0) %></td>
									<td class="Normal" width=300 valign="top"><%= rsPR("Company") %></td>
								</tr>
<%
			decRunningMonthTotal = decRunningMonthTotal + decCuMinNettPricerospValue

			k = 2
			rsPR.MoveNext
		Loop
%>
								<tr>
									<td></td>
									<td></td>
									<td class="Normal" align="right" style="border-top:1px solid black;text-align:right;" width=100><%= FormatCurrency(decRunningMonthTotal,0) %></td>
								</tr>
							</table>
						</td>
					</tr>
					<tr>
						<td colspan=15>
							<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table2">
								<tr>
									<td align="center" style="text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"  codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="400" HEIGHT="350" id="Object2" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data3.asp*Code=All*Year=<%= intYear %>*ProspectsBudget=<%= decProspectsBudget %>*DivisionId=<%= intDivisionId %>*UserCodes=<%= strUserCodes %>">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=400&chartHeight=350">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=400&chartHeight=350" FlashVars="dataURL=SalesReport_Data3.asp*Code=All*Year=<%= intYear %>*ProspectsBudget=<%= decProspectsBudget %>*DivisionId=<%= intDivisionId %>*UserCodes=<%= strUserCodes %>" quality=high bgcolor=#FFFFFF WIDTH="400" HEIGHT="350" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>
									</td>
								</tr>
							</table>
						</td>
					</tr>
<%
	End If

	rsPR.Close
	Set rsPR = Nothing
%>
<!-- Client Analysis -->
					<tr>
						<td colspan=15 style="border-top:2px solid black;font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;">CLIENT ANALYSIS</td>
					</tr>
					<tr>
						<td colspan=15>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table4">
								<tr>
									<td class="HdrTimesBoldItalic" width=640>Client</td>
									<td class="HdrTimesBoldItalic" width=150 style="text-align:right;">Orders Value</td>
									<td class="HdrTimesBoldItalic" width=150 style="text-align:right;">Prospect Value</td>
									<td class="HdrTimesBoldItalic" width=150 style="text-align:right;">Calls</td>
									<td class="HdrTimesBoldItalic" width=150 style="text-align:right;">Expenditure</td>
								</tr>
<%
	Response.Flush

	' Get contacts
	Set rsCON = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT CompanyId, Company FROM Contacts_AndCustomers ORDER BY Company"
	Set rsCON = dbConn.Execute(sql)
	
	Do Until rsCON.EOF
		strCompany = rsCON("Company")&""
		If rsCON("CompanyId") <> 0 Then
			' Get orders
			Set rsOR = Server.CreateObject("ADODB.RecordSet")
			sql = "SELECT [Value] AS OrdersValue FROM SalesResults_ByCustomer WHERE Code IN (" & strUserCodes & ") AND CustomerId = " & rsCon("CompanyId") & " AND Year([Date]) = " & intYear
			Set rsOR = dbConn.Execute(sql)

			decOrdersValue = 0

			If Not(rsOR.BOF And rsOR.EOF) Then
				If rsOR("OrdersValue") > 0 Then
					decOrdersValue = FormatNumber(rsOR("OrdersValue"),2)
				End If
			End If

			If IsObject(rsOR) Then
				rsOR.Close
				Set rsOR = Nothing
			End If
		Else
			decOrdersValue = 0
		End If
			
		' Get prospects
		Set rsPR = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT Sum(Value) AS ProspValue1, Sum(AmountPerMonth*NumberOfMonths) As ProspValue2 FROM SalesProjects WHERE ContactId IN (SELECT ContactId FROM Contacts_WithCustomersAndSuppliers WHERE Code IN (" & strUserCodes & ") AND Company = '" & Replace(strCompany, "'", "''") & "') AND [ProspectDate] >= #" & dteBegin & "# And [ProspectDate] <= #" & dteEnd & "#"
		Set rsPR = dbConn.Execute(sql)

		decProspValue = 0
		
		If rsPR("ProspValue1") > 0 Then
			decProspValue = decProspValue + FormatNumber(rsPR("ProspValue1"),2)
		End If
		If rsPR("ProspValue2") > 0 Then
			decProspValue = decProspValue + FormatNumber(rsPR("ProspValue2"),2)
		End If

		If IsObject(rsPR) Then
			rsPR.Close
			Set rsPR = Nothing
		End If

		' Get calls
		Set rsCR = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT Count(*) AS Calls FROM CallReports WHERE ContactId IN (SELECT ContactId FROM Contacts_WithCustomersAndSuppliers WHERE Code IN (" & strUserCodes & ") AND Company = '" & Replace(strCompany, "'", "''") & "') AND [DateEntered] >= #" & dteBegin & "# And [DateEntered] <= #" & dteEnd & "#"
		Set rsCR = dbConn.Execute(sql)
		
		If rsCR("Calls") > 0 Then
			intCalls = rsCR("Calls")
		Else
			intCalls = 0
		End If
		
		If IsObject(rsCR) Then
			rsCR.Close
			Set rsCR = Nothing
		End If

		' Get expenses
		Set rsEX = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT Sum(CostIncGST) AS Expenditure FROM Expenses WHERE ContactId IN (SELECT ContactId FROM Contacts_WithCustomersAndSuppliers WHERE Code IN (" & strUserCodes & ") AND Company = '" & Replace(strCompany, "'", "''") & "') AND [ExpenseDate] >= #" & dteBegin & "# And [ExpenseDate] <= #" & dteEnd & "#"
		Set rsEX = dbConn.Execute(sql)
		
		If rsEX("Expenditure") <> "" Then
			decExpenditure = CDbl(rsEX("Expenditure"))
		Else
			decExpenditure = 0
		End If

		If IsObject(rsEX) Then
			rsEX.Close
			Set rsEX = Nothing
		End If
		
		If (decOrdersValue > 0 Or decProspValue > 0 Or intCalls > 0 Or decExpenditure > 0) Then
%>
								<tr>
									<td class="Normal"><%= rsCON("Company") %></td>
									<td class="Normal" style="text-align:right;"><%= FormatCurrency(decOrdersValue, 0) %></td>
									<td class="Normal" style="text-align:right;"><%= FormatCurrency(decProspValue, 0) %></td>
									<td class="Normal" style="text-align:right;"><%= intCalls %></td>
									<td class="Normal" style="text-align:right;"><%= FormatCurrency(decExpenditure, 0) %></td>
								</tr>
<%
		End If
		Response.Flush
		rsCON.MoveNext
	Loop
	
	rsCON.Close
	Set rsCON = Nothing
%>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<%
Else
	Call NoResults
End If

If err.number > 0 Then
	Call NoResults
End If
%>
	</body>
</html>
<%
If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->