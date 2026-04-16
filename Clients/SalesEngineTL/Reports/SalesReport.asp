<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"
Response.Buffer = True

Dim intYear
Dim dteBegin
Dim dteEnd
Dim strCode
Dim sql
Dim rsSP
Dim rsSR
Dim rsEX
Dim z
Dim arrSR(5,12)
Dim arrEX(1,12)
Dim aMinNettPriceR(1,12)
Dim decRunningSalesBudget
Dim decRunningSales
Dim decRunningOrders
Dim decRunningProspTotal
Dim strSalesPersonName
Dim decExpensesPerMonth
Dim decProspectsBudget
Dim boolHasSalesResults

intYear = CLng(Request("Year"))
dteBegin = CStr("01-Jul-20" & MakePadding(Right(intYear, 2), "", 2))
dteEnd = CStr("30-Jun-20" & MakePadding(Right(intYear+1, 2), "", 2))
strCode = Trim(Request("Code"))
decRunningOrders = 0

If strCode = "All" Then Response.Redirect("SalesReport_All.asp?Year=" & intYear)

'On Error Resume Next

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>MyDesk-SalesReport:<%= strCode %>:<%= Year(dteBegin) & "-" & Year(dteEnd) %></title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
        <link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
    	<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
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
			.HdrMonth, .HdrColumn {
			    background-color: #cccccc;
			    font-size: 10px;
			    font-weight: bold;
			    font-style: italic;
			    text-align: right;
			}
			.RecordRight {
			    text-align: right;
			}
		</style>
	</head>
	<body bgcolor="#ffffff">
<!--#include virtual="/System/ssi_Header.inc"-->
        <table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
	        <tr>
				<td><input type="button" value=" Back " onclick="document.location.href='Default.asp';" ID="Button1" NAME="Button1"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
	        </tr>
        </table>
<%

boolHasSalesResults = False

' Get person's details
Set rsSP = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "' Order By Name"
Set rsSP = dbConn.Execute(sql)

If Not(rsSP.BOF And rsSP.EOF) Then
	intDivisionId = rsSP("DivisionId")
	strSalesPersonName = rsSP("Name")
	decExpensesPerMonth = rsSP("ExpensesPerMonth")
	decRunningExpensesBudget = decExpensesPerMonth * 12
	decProspectsBudget = rsSP("ProspectsBudget")
	If Not decExpensesPerMonth > 0 Or Not IsNumeric(decExpensesPerMonth) Then decExpensesPerMonth = 0
	If Not decProspectsBudget > 0 Or Not IsNumeric(decProspectsBudget) Then decProspectsBudget = 0
End If

If IsObject(rsSP) Then
	rsSP.Close
	Set rsSP = Nothing
End If

' Get sales results and generate array
Set rsSR = Server.CreateObject("ADODB.RecordSet")
sql = "Select Sum(SR.SalesBudget) As SalesBudget, Sum(SBC.OrdersValue) As TotalOrdersValue, Sum(SBC.SalesValue) As TotalSalesValue From SalesResults SR Left Outer Join SalesResults_ByCustomer As SBC On SBC.Date = SR.Date And SBC.Code = SR.Code Where SR.Code = '" & strCode & "' And SR.[Date] >= #" & dteBegin & "# And SR.[Date] <= #" & dteEnd & "# GROUP BY SR.Date, SR.Code Order By SR.[Date]"
Set rsSR = dbConn.Execute(sql)

z = 1

If Not(rsSR.BOF And rsSR.EOF) Then
	Do
		If Len(rsSR("TotalOrdersValue")) = 0 Then
			arrSR(0,z) = 0
		Else
			arrSR(0,z) = rsSR("TotalOrdersValue")
		End If
		If IsNumeric(rsSR("SalesBudget")) Then
			arrSR(1,z) = rsSR("SalesBudget")
		Else
			arrSR(1,z) = 0
		End If
		If IsNumeric(rsSR("TotalSalesValue")) Then
			arrSR(2,z) = rsSR("TotalSalesValue")
		Else
			arrSR(2,z) = 0
		End If
		' Orders budget
		If IsNumeric(rsSR("SalesBudget")) Then
			arrSR(3,z) = rsSR("SalesBudget")
		Else
			arrSR(3,z) = 0
		End If
		decRunningSalesBudget = decRunningSalesBudget + rsSR("SalesBudget")
		decRunningOrdersBudget = decRunningSalesBudget + rsSR("SalesBudget") ' for now
		decRunningSales = decRunningSales + rsSR("TotalSalesValue")
		If IsNumeric(rsSR("TotalOrdersValue")) Then
			decRunningOrders = decRunningOrders + rsSR("TotalOrdersValue") ' Possible mistakes here
		End If
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
sql = "Select Code, Month(ExpenseDate) As ExpMonth, Sum(CostIncGST) As ExpValue From Expenses Where Code = '" & strCode & "' And [ExpenseDate] >= #" & dteBegin & "# And [ExpenseDate] <= #" & dteEnd & "# Group By Code, Month(ExpenseDate) Order By Month(ExpenseDate)"
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
sql = "Select Code, Month(ProspectDate) As ProspMonth, Sum(Value) As ProspValue, Sum(AmountPerMonth*NumberOfMonths) As ProspValue2 From SalesProjects Where [Code] = '" & strCode & "' And [ProspectDate] >= #" & dteBegin & "# And [ProspectDate] <= #" & dteEnd & "# Group By Code, Month(ProspectDate) Order By Month(ProspectDate)"
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
				<table width="1000" cellpadding=5 cellspacing=1 border=0>
					<tr>
						<td colspan=10 style="font-size:16px;font-weight:bold;"><%= strSalesPersonName %> Sales & Expenditure Report for <%= intYear & "/" & Right(intYear+1,2) %></td>
						<td colspan=5 style="font-size:15px;text-align:right;">
						    <table>
						        <tr>
						            <td style="font-size:14px;">Orders Budget Target YTD:</td>
						            <td style="font-size:14px;text-align:right;"><b><% If decRunningOrders > 0 And decRunningOrdersBudget > 0 Then Response.Write(FormatNumber(decRunningOrders/decRunningOrdersBudget*100, 2)) Else Response.Write FormatNumber(0,2) %>%</b></td>
						        </tr>
						        <tr>
						            <td style="font-size:14px;">Sales Budget Target YTD:</td>
						            <td style="font-size:14px;text-align:right;"><b><% If decRunningSales > 0 And decRunningSales > 0 Then Response.Write(FormatNumber(decRunningSales/decRunningSalesBudget*100, 2)) Else Response.Write FormatNumber(0,2) %>%</b></td>
						        </tr>
						    </table>
						</td>
					</tr>
					<tr>
						<td class="HdrMonth" colspan=2>&nbsp;</td>
						<td class="HdrMonth">Total</td>
<%

	Dim i
	Dim x
	i = 7
	x = 0
	Do

%>
						<td class="HdrMonth"><%= MonthName(i, True) & "'" & Right(intYear+x,2) %></td>
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
					</tr>
<!-- ORDERS -->
					<tr>
						<td colspan=14 style="font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;border-top:2px solid black;">ORDERS</td>
						<td style="border-top:1px solid black;border-top:2px solid black;">&nbsp;</td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Orders</td>
						<td style="text-align:right;"><%= FormatCurrency(decRunningOrders,0) %></td>
<%

	i = 1
	Do Until i = 13

%>
						<td align="right"><% If IsNumeric(arrSR(0,i)) Then Response.Write(FormatCurrency(arrSR(0,i),0)) Else Response.Write("$0") %></td>
<%

		i = i + 1
	Loop

%>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Budget</td>
						<td style="text-align:right;"><%= FormatCurrency(decRunningOrdersBudget,0) %></td>
<%

	i = 1
	Do Until i = 13

%>
						<td style="text-align:right;"><%= FormatCurrency(arrSR(1,i),0) %></td>
<%

		i = i + 1
	Loop

%>
					</tr>
					<tr>
						<td></td>
						<td style="border-top:1px solid black;font-weight:bold;text-align:right;">Budget %</td>
						<td style="border-top:1px solid black;text-align:right;"><% If decRunningOrders > 0 And decRunningOrdersBudget > 0 Then Response.Write FormatNumber(decRunningOrders/decRunningOrdersBudget*100, 2) Else Response.Write FormatNumber(0,2) %>%</td>
<%

	i = 1
	Do

%>
						<td style="border-top:1px solid black;text-align:right;"><% If arrSR(0,i) > 0 And arrSR(1,i) > 0 Then Response.Write FormatNumber(arrSR(0,i)/arrSR(1,i)*100,2) Else Response.Write FormatNumber(0,2) %>%</td>
<%

		If i = 12 Then Exit Do

		i = i + 1
	Loop

%>
					</tr>
<!-- SALES -->
					<tr>
						<td colspan=14 style="font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;border-top:2px solid black;">SALES</td>
						<td style="border-top:1px solid black;border-top:2px solid black;">&nbsp;</td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Sales</td>
						<td style="text-align:right;"><% If IsNumeric(decRunningSales) Then Response.Write(FormatCurrency(decRunningSales,0)) Else Response.Write("$0") %></td>
<%

	i = 1
	Do Until i = 13

%>
						<td align="right"><% If IsNumeric(arrSR(2,i)) Then Response.Write(FormatCurrency(arrSR(2,i),0)) Else Response.Write("$0") %></td>
<%

		i = i + 1
	Loop

%>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Budget</td>
						<td style="text-align:right;"><%= FormatCurrency(decRunningSalesBudget,0) %></td>
<%

	i = 1
	Do Until i = 13

%>
						<td style="text-align:right;"><%= FormatCurrency(arrSR(3,i),0) %></td>
<%

		i = i + 1
	Loop

%>
					</tr>
					<tr>
						<td></td>
						<td style="border-top:1px solid black;font-weight:bold;text-align:right;">Budget %</td>
						<td style="border-top:1px solid black;text-align:right;"><% If decRunningSales > 0 And decRunningSalesBudget > 0 Then Response.Write FormatNumber(decRunningSales/decRunningSalesBudget*100, 2) Else Response.Write FormatNumber(0,2) %>%</td>
<%

	i = 1
	Do

%>
						<td style="border-top:1px solid black;text-align:right;"><% If arrSR(2,i) > 0 And arrSR(3,i) > 0 Then Response.Write FormatNumber(arrSR(2,i)/arrSR(3,i)*100,2) Else Response.Write FormatNumber(0,2) %>%</td>
<%

		If i = 12 Then Exit Do

		i = i + 1
	Loop

%>
					</tr>
<!-- ### EXPENSES -->
					<tr>
						<td colspan=14 style="border-top:2px solid black;font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;">EXPENSES</td>
						<td style="border-top:2px solid black;">&nbsp;</td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Expenses</td>
						<td style="text-align:right;"><%= FormatCurrency(decRunningExpenseTotal,0) %></td>
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
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Budget</td>
						<td style="text-align:right;"><%= FormatCurrency(decRunningExpensesBudget,0) %></td>
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
					</tr>
					<tr>
						<td></td>
						<td style="border-top:1px solid black;font-weight:bold;text-align:right;">Budget %</td>
						<td style="text-align:right;border-top:1px solid black;"><% If decRunningExpenseTotal > 0 And decExpensesPerMonth > 0 Then Response.Write FormatNumber(100*(decRunningExpenseTotal/(decExpensesPerMonth*12)),2) Else Response.Write FormatNumber(0,2) %>%</td>
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

<!-- ### GRAPHS 1-4 -->
					<tr>
						<td colspan=20>
							<table width="100%" cellpadding=0 cellspacing=0 border=0>
								<tr>
									<td align="center" style="width:50%;text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"  codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="360" HEIGHT="315" id="FC2Column" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data1.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315" FlashVars="dataURL=SalesReport_Data1.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0" quality=high bgcolor=#FFFFFF WIDTH="360" HEIGHT="315" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>
									</td>
									<td align="center" style="width:50%;text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="360" HEIGHT="315" id="Object1" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data2.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSLine_2.swf?chartWidth=360&chartHeight=315">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSLine_2.swf" flashvars="&dataURL=SalesReport_Data2.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0" quality=high bgcolor=#FFFFFF WIDTH="360" HEIGHT="315" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>						
									</td>
								</tr>
							</table>
						</td>
					</tr>
					<tr>
						<td colspan=20>
							<table width="100%" cellpadding=0 cellspacing=0 border=0>
								<tr>
									<td align="center" style="width:50%;text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"  codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="360" HEIGHT="315" id="OBJECT3" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data4.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315" FlashVars="dataURL=SalesReport_Data4.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0" quality=high bgcolor=#FFFFFF WIDTH="360" HEIGHT="315" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>
									</td>
									<td align="center" style="width:50%;text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="360" HEIGHT="315" id="Object4" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data5.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSLine_2.swf?chartWidth=360&chartHeight=315">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSLine_2.swf" flashvars="&dataURL=SalesReport_Data5.asp*Code=<%= strCode %>*Begin=<%= dteBegin %>*End=<%= dteEnd %>*DivisionId=0" quality=high bgcolor=#FFFFFF WIDTH="360" HEIGHT="315" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>						
									</td>
								</tr>
							</table>
						</td>
					</tr>
					<tr>
						<td colspan=20>
							<table width="100%" cellpadding=0 cellspacing=0 border=0>
								<tr>
									<td align="center" style="text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"  codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="360" HEIGHT="315" id="OBJECT5" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data6.asp*Code=<%= strCode %>*Year=<%= intYear %>">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315" FlashVars="dataURL=SalesReport_Data6.asp*Code=<%= strCode %>*Year=<%= intYear %>" quality=high bgcolor=#FFFFFF WIDTH="360" HEIGHT="315" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>
									</td>
									<td align="center" style="text-align:center;">
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000" codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="360" HEIGHT="315" id="Object6" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data7.asp*Code=<%= strCode %>*Year=<%= intYear %>">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSLine_2.swf?chartWidth=360&chartHeight=315">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSLine_2.swf" flashvars="&dataURL=SalesReport_Data7.asp*Code=<%= strCode %>*Year=<%= intYear %>" quality=high bgcolor=#FFFFFF WIDTH="360" HEIGHT="315" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
									</OBJECT>						
									</td>
								</tr>
							</table>
							<span class="PageBreak">&nbsp;</span>
						</td>
					</tr>
<!-- ### PROSPECTS -->
<%

	If Not(rsPR.BOF And rsPR.EOF) Then

%>
					<tr>
						<td class="HdrMonth" colspan=2>&nbsp;</td>
						<td class="HdrMonth">Total</td>
<%

	i = 7
	x = 0
	Do

%>
						<td class="HdrMonth"><%= MonthName(i, True) & "'" & Right(intYear+x,2) %></td>
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
					</tr>
					<tr>
						<td colspan=15 style="border-top:2px solid black;font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;">PROSPECTS</td>
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Sales</td>
						<td style="text-align:right;"><%= FormatCurrency(decRunningProspTotal,0) %></td>
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
					</tr>
					<tr>
						<td></td>
						<td style="font-weight:bold;text-align:right;">Budget</td>
						<td style="text-align:right;"><%= FormatCurrency(decProspectsBudget*12,0) %></td>
<%

		i = 1
		Do Until i = 13

%>
						<td style="text-align:right;"><%= FormatCurrency(decProspectsBudget,0) %></td>
<%

			i = i + 1
		Loop

%>
					</tr>
					<tr>
						<td colspan=15 style="border-top:2px solid black;font-family:times new roman;font-size:14px;font-style:italic;font-weight:bold;">PROSPECT DETAILS</td>
					</tr>
<%

		Set rsPR = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Contacts_WithCustomersAndSuppliers_V2.CompanyName, Contacts_WithCustomersAndSuppliers_V2.FirstName, Contacts_WithCustomersAndSuppliers_V2.Surname, SalesProjects.*, [Value] As ProspValue, AmountPerMonth*NumberOfMonths As ProspValue2, Month(ProspectDate) As ProspMonth, Year(ProspectDate) As ProspYear From SalesProjects Inner Join Contacts_WithCustomersAndSuppliers_V2 On Contacts_WithCustomersAndSuppliers_V2.ContactId = SalesProjects.ContactId Where SalesProjects.Code = '" & strCode & "' And [ProspectDate] >= #" & dteBegin & "# And [ProspectDate] <= #" & dteEnd & "# Order By ProspectDate"
		Set rsPR = dbConn.Execute(sql)

%>
					<tr>
						<td colspan=15>
							<table width="100%" cellpadding=5 cellspacing=1 border=0 ID="Table1">
							    <tr>
							        <td class="HdrColumn" style="text-align:left;">Company</td>
							        <td class="HdrColumn" style="text-align:left;">Project</td>
							        <td class="HdrColumn" style="text-align:left;">Description</td>
							        <td class="HdrColumn">Value</td>
							    </tr>
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
									<td></td>
									<td></td>
									<td></td>
									<td class="Normal" align="right" style="border-top:1px solid black;text-align:right;" width=100><%= FormatCurrency(decRunningMonthTotal,0) %></td>
								</tr>
<%

					decRunningMonthTotal = 0
				End If

%>
								<tr>
									<td class="Normal" style="font-family:times new roman;font-size:14px;font-weight:bold;font-style:italic;"><%= MonthName(intCurrMonth, False) & " " & intCurrYear %></td>
								</tr>
<%
			End If

%>
								<tr>
									<td class="Normal" width=300 valign="top"><%= UCase(rsPR("CompanyName")) %></td>
									<td class="Normal" width=300 valign="top"><%= UCase(rsPR("Project")) %></td>
									<td class="Normal" width=390 valign="top"><% If Len(rsPR("Comment")) > 0 Then Response.Write UCase(rsPR("Comment")) Else Response.Write "NO DESCRIPTION ENTERED" %></td>
									<td class="Normal" align="right" width=100 valign="top"><%= FormatCurrency(decCuMinNettPricerospValue,0) %></td>
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
									<OBJECT classid="clsid:D27CDB6E-AE6D-11cf-96B8-444553540000"  codebase="http://download.macromedia.com/pub/shockwave/cabs/flash/swflash.cab#version=6,0,0,0" WIDTH="360" HEIGHT="315" id="Object2" ALIGN="" VIEWASTEXT>
									<PARAM NAME="FlashVars" value="&dataURL=SalesReport_Data3.asp*Code=<%= strCode %>*Year=<%= intYear %>*ProspectsBudget=<%= decProspectsBudget %>">
									<PARAM NAME=movie VALUE="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315">
									<PARAM NAME=quality VALUE=high>
									<PARAM NAME=bgcolor VALUE=#FFFFFF>
									<EMBED src="/FusionChart/FC_2_3_MSColumnLine_DY_3D[1].swf?chartWidth=360&chartHeight=315" FlashVars="dataURL=SalesReport_Data3.asp*Code=<%= strCode %>*Year=<%= intYear %>*ProspectsBudget=<%= decProspectsBudget %>" quality=high bgcolor=#FFFFFF WIDTH="360" HEIGHT="315" NAME="FC2Column" ALIGN="" TYPE="application/x-shockwave-flash" PLUGINSPAGE="http://www.macromedia.com/go/getflashplayer"></EMBED>
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
<%
	Response.Flush

	YearlyFigures True
	YearlyFigures False
	
	Sub YearlyFigures(boolNonAccount)
		' Get contacts
		Set rsCON = Server.CreateObject("ADODB.RecordSet")
		If boolNonAccount Then
            sql = "SELECT CompanyId, Company As CompanyName FROM Companies WHERE CompanyId = 142"
		Else
            sql = "SELECT DISTINCT CompanyId, CompanyName FROM Contacts_WithCustomersAndSuppliers_V2 WHERE Code = '" & strCode & "' AND CompanyId <> 142 GROUP BY CompanyName, CompanyId ORDER BY CompanyName"
        End If
		Set rsCON = dbConn.Execute(sql)
		
		Dim z
		Dim y
		z = 0
		y = 0
		
		Do Until rsCON.EOF
			strCompany = rsCON("CompanyName")
			lngCompanyId = rsCON("CompanyId")

%>
							<div id="ClientAnalysis<%= z %>">
							<table width="100%" cellpadding=3 cellspacing=0 border=0>
								<tr>
									<td class="Normal" style="font-weight:bold;"><%= UCase(strCompany) %></td>
								</tr>
								<tr>
								    <td>
								        <table style="width:100%;" cellpadding="5" cellspacing="1" border="0">
                                            <tr>
                                                <td width=80 class="HdrMonth" style="text-align:left;">Type</td>
                                                <td width=70 class="HdrMonth" style="text-align:right;">Total</td>
<%

	Dim i
	Dim x
	i = 7
	x = 0
	Do

%>
                                                <td width=70 class="HdrMonth"><%= MonthName(i, True) & "'" & Right(intYear+x,2) %></td>
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
                                            </tr>
<%
            ' ### Get Orders
            Set rs = Server.CreateObject("ADODB.RecordSet")
            sql = "Select * From SalesReport_Orders_Crosstab Where Code = '" & strCode & "' And CompanyId = " & lngCompanyId
            Set rs = dbConn.Execute(sql)
            
            If Not(rs.BOF And rs.EOF) Then
                y = y + 1
%>
                                            <tr>
                                                <td width=100>Orders</td>
                                                <td class="RecordRight"><% If rs("Total Of OrdersValue") <> "" Then Response.Write FormatCurrency(CLng(rs("Total Of OrdersValue")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jul") <> "" Then Response.Write FormatCurrency(CLng(rs("Jul")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Aug") <> "" Then Response.Write FormatCurrency(CLng(rs("Aug")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Sep") <> "" Then Response.Write FormatCurrency(CLng(rs("Sep")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Oct") <> "" Then Response.Write FormatCurrency(CLng(rs("Oct")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Nov") <> "" Then Response.Write FormatCurrency(CLng(rs("Nov")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Dec") <> "" Then Response.Write FormatCurrency(CLng(rs("Dec")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jan") <> "" Then Response.Write FormatCurrency(CLng(rs("Jan")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Feb") <> "" Then Response.Write FormatCurrency(CLng(rs("Feb")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Mar") <> "" Then Response.Write FormatCurrency(CLng(rs("Mar")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Apr") <> "" Then Response.Write FormatCurrency(CLng(rs("Apr")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("May") <> "" Then Response.Write FormatCurrency(CLng(rs("May")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jun") <> "" Then Response.Write FormatCurrency(CLng(rs("Jun")), 0) Else Response.Write "$0" %></td>
                                            </tr>
<%
            End If

            ' ### Get Sales
            Set rs = Server.CreateObject("ADODB.RecordSet")
            sql = "Select * From SalesReport_Sales_Crosstab Where Code = '" & strCode & "' And CompanyId = " & lngCompanyId
            Set rs = dbConn.Execute(sql)
            
            If Not(rs.BOF And rs.EOF) Then
                y = y + 1
%>
                                            <tr>
                                                <td width=100>Sales</td>
                                                <td class="RecordRight"><% If rs("Total Of SalesValue") <> "" Then Response.Write FormatCurrency(CLng(rs("Total Of SalesValue")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jul") <> "" Then Response.Write FormatCurrency(CLng(rs("Jul")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Aug") <> "" Then Response.Write FormatCurrency(CLng(rs("Aug")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Sep") <> "" Then Response.Write FormatCurrency(CLng(rs("Sep")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Oct") <> "" Then Response.Write FormatCurrency(CLng(rs("Oct")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Nov") <> "" Then Response.Write FormatCurrency(CLng(rs("Nov")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Dec") <> "" Then Response.Write FormatCurrency(CLng(rs("Dec")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jan") <> "" Then Response.Write FormatCurrency(CLng(rs("Jan")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Feb") <> "" Then Response.Write FormatCurrency(CLng(rs("Feb")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Mar") <> "" Then Response.Write FormatCurrency(CLng(rs("Mar")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Apr") <> "" Then Response.Write FormatCurrency(CLng(rs("Apr")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("May") <> "" Then Response.Write FormatCurrency(CLng(rs("May")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jun") <> "" Then Response.Write FormatCurrency(CLng(rs("Jun")), 0) Else Response.Write "$0" %></td>
                                            </tr>
<%
            End If

            ' ### Get Prospects
            Set rs = Server.CreateObject("ADODB.RecordSet")
            sql = "Select * From SalesReport_Prospects_Crosstab Where Code = '" & strCode & "' And CompanyId = " & lngCompanyId
            Set rs = dbConn.Execute(sql)
            
            If Not(rs.BOF And rs.EOF) Then
                y = y + 1
%>
                                           <tr>
                                                <td width=100>Prospects</td>
                                                <td class="RecordRight"><% If rs("Total Of ProspectsTotal") <> "" Then Response.Write FormatCurrency(CLng(rs("Total Of ProspectsTotal")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jul") <> "" Then Response.Write FormatCurrency(CLng(rs("Jul")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Aug") <> "" Then Response.Write FormatCurrency(CLng(rs("Aug")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Sep") <> "" Then Response.Write FormatCurrency(CLng(rs("Sep")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Oct") <> "" Then Response.Write FormatCurrency(CLng(rs("Oct")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Nov") <> "" Then Response.Write FormatCurrency(CLng(rs("Nov")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Dec") <> "" Then Response.Write FormatCurrency(CLng(rs("Dec")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jan") <> "" Then Response.Write FormatCurrency(CLng(rs("Jan")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Feb") <> "" Then Response.Write FormatCurrency(CLng(rs("Feb")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Mar") <> "" Then Response.Write FormatCurrency(CLng(rs("Mar")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Apr") <> "" Then Response.Write FormatCurrency(CLng(rs("Apr")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("May") <> "" Then Response.Write FormatCurrency(CLng(rs("May")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jun") <> "" Then Response.Write FormatCurrency(CLng(rs("Jun")), 0) Else Response.Write "$0" %></td>
                                            </tr>
<%
            End If

            ' ### Get Expenses
            Set rs = Server.CreateObject("ADODB.RecordSet")
            sql = "Select * From SalesReport_Expenses_Crosstab Where Code = '" & strCode & "' And CompanyId = " & lngCompanyId
            Set rs = dbConn.Execute(sql)
            
            If Not(rs.BOF And rs.EOF) Then
                y = y + 1
%>
                                           <tr>
                                                <td width=100>Expenses</td>
                                                <td class="RecordRight"><% If rs("Total Of ExpensesTotal") <> "" Then Response.Write FormatCurrency(CLng(rs("Total Of ExpensesTotal")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jul") <> "" Then Response.Write FormatCurrency(CLng(rs("Jul")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Aug") <> "" Then Response.Write FormatCurrency(CLng(rs("Aug")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Sep") <> "" Then Response.Write FormatCurrency(CLng(rs("Sep")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Oct") <> "" Then Response.Write FormatCurrency(CLng(rs("Oct")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Nov") <> "" Then Response.Write FormatCurrency(CLng(rs("Nov")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Dec") <> "" Then Response.Write FormatCurrency(CLng(rs("Dec")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jan") <> "" Then Response.Write FormatCurrency(CLng(rs("Jan")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Feb") <> "" Then Response.Write FormatCurrency(CLng(rs("Feb")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Mar") <> "" Then Response.Write FormatCurrency(CLng(rs("Mar")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Apr") <> "" Then Response.Write FormatCurrency(CLng(rs("Apr")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("May") <> "" Then Response.Write FormatCurrency(CLng(rs("May")), 0) Else Response.Write "$0" %></td>
                                                <td class="RecordRight"><% If rs("Jun") <> "" Then Response.Write FormatCurrency(CLng(rs("Jun")), 0) Else Response.Write "$0" %></td>
                                            </tr>
<%
            End If

            ' ### Get Calls
            Set rs = Server.CreateObject("ADODB.RecordSet")
            sql = "Select * From SalesReport_Calls_Crosstab Where Code = '" & strCode & "' And CompanyId = " & lngCompanyId
            Set rs = dbConn.Execute(sql)
            
            If Not(rs.BOF And rs.EOF) Then
                y = y + 1
%>
                                            <tr>
                                                <td width=100>Calls</td>
                                                <td class="RecordRight"><% If rs("Total Of Calls") <> "" Then Response.Write CLng(rs("Total Of Calls")) Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Jul") <> "" Then Response.Write rs("Jul") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Aug") <> "" Then Response.Write rs("Aug") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Sep") <> "" Then Response.Write rs("Sep") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Oct") <> "" Then Response.Write rs("Oct") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Nov") <> "" Then Response.Write rs("Nov") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Dec") <> "" Then Response.Write rs("Dec") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Jan") <> "" Then Response.Write rs("Jan") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Feb") <> "" Then Response.Write rs("Feb") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Mar") <> "" Then Response.Write rs("Mar") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Apr") <> "" Then Response.Write rs("Apr") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("May") <> "" Then Response.Write rs("May") Else Response.Write "0" %></td>
                                                <td class="RecordRight"><% If rs("Jun") <> "" Then Response.Write rs("Jun") Else Response.Write "0" %></td>
                                            </tr>
<%
            End If
%>
								        </table>
								    </td>
								</tr>
                                <tr>
                                    <td colspan=20><hr /></td>
                                </tr>
							</table>
							</div>
<%			

            If y = 0 Then
%>
                            <script language="javascript">
                                document.getElementById("ClientAnalysis<%= z %>").style.display = 'none';
                            </script>
<%
            Else
                y = 0
            End If

            z = z + 1
			Response.Flush
			rsCON.MoveNext
		Loop
		
		rsCON.Close
		Set rsCON = Nothing
	End Sub
%>
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
<%
	
Sub NoResults
	'Response.Redirect("SalesReportError.asp?Code=" & strCode & "&Year=" & intYear)
End Sub

%>