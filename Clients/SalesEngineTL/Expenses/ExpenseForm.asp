<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim intDivisionId
Dim dteExpenseFormDate
Dim intMonth
Dim intYear
Dim decRunningTotal
Dim decRunningGSTTotal
Dim boolSignOff

strCode =				Trim(Request("Code"))
intDivisionId =         CInt(Request.QueryString("DivisionId")) ' Not from the form.
dteExpenseFormDate =	Request("ExpenseFormDate")
intMonth =				Month(dteExpenseFormDate)
intYear =				Year(dteExpenseFormDate)
decRunningTotal =		0.00
decRunningGSTTotal =	0.00
strReportComments =		Trim(Replace(Request("ReportComments"), "'", "''"))

If CStr(Request("SignOff")) = "True" Then boolSignOff = True Else boolSignOff = False

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

If boolSignOff Then
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ExpensesSignOffs WHERE [Month] = " & intMonth & " AND [Year] = " & intYear & " AND Code = '" & strCode & "'"
	Set rs = dbConn.Execute(sql)
	If (rs.BOF And rs.EOF) Then
		' Set sign off status
		sql = "INSERT INTO ExpensesSignOffs (Code, CodeSignedOff, Month, Year, ReportComments) VALUES ('" & strCode & "', '" & Request.Cookies("UserSettings")("Code") & "', " & intMonth & ", " & intYear & ", '" & strReportComments & "')"
		dbConn.Execute(sql)
	End If
Else
	' Check for sign off first
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ExpensesSignOffs WHERE [Month] = " & intMonth & " AND [Year] = " & intYear & " AND Code = '" & strCode & "'"
	Set rs = dbConn.Execute(sql)
	If Not(rs.BOF and rs.EOF) Then
		boolSignOff = True
	Else
		boolSignOff = False
	End If
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

If Not boolSignOff Then
%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
		<script language="javascript">
			function makeSure() {
				if(confirm('Are you sure you want to sign off this month\'s expenses.')) {
					return true;
				} else {
					return false;
				}
			}
		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" height="100%" cellpadding=0 cellspacing=0 border=0>
			<tr>
				<td align="center" valign="middle">
<%
	If strCode = Request.Cookies("UserSettings")("Code") Then
%>
				<p style="font-size:16px;">The expense form for <b><%= MonthName(intMonth) %>&nbsp;<%= intYear %></b> has not yet been signed off.</p>
				<p>To view and print your claim, you must sign off. Once signed off none of the expenses in the month can be edited, or deleted.</p>
					<table style="border:1px solid red;" cellpadding=5 cellspacing=0 border=0>
<%
		decRunningTotal = 0
		decRunningGSTTotal = 0
		' Let's find out how much money
		Set rs = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT Activity, Sum(Cost) AS TotalCost, Sum(GST) AS TotalGST FROM ExpenseForm_Expenses WHERE ExpenseForm_Expenses.Code = '" & strCode & "' AND Month([Date])=" & intMonth & " AND Year([Date])= " & intYear & " AND ExpenseForm_Expenses.Card = true"
		sql = sql & " GROUP BY Activity"
		rs.Open sql, dbConn, 0, 1
		If Not(rs.BOF And rs.EOF) Then
			Do Until rs.EOF	
				decRunningTotal = decRunningTotal + CDbl(rs("TotalCost"))
				decRunningGSTTotal = decRunningGSTTotal + CDbl(rs("TotalGST"))
				rs.MoveNext
			Loop
		End If
		rs.Close
		Set rs = Nothing

%>
						<tr>
							<td style="font-weight:bold;">Not Reimbursable Total Inc. GST:</td>
							<td style="text-align:right;"><%= FormatCurrency(decRunningTotal) %></td>
						</tr>
<%
		decRunningTotal = 0
		decRunningGSTTotal = 0
		' Let's find out how much money
		Set rs = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT Activity, Sum(Cost) AS TotalCost, Sum(GST) AS TotalGST FROM ExpenseForm_Expenses WHERE ExpenseForm_Expenses.Code = '" & strCode & "' AND Month([Date])=" & intMonth & " AND Year([Date])= " & intYear & " AND ExpenseForm_Expenses.Card = false GROUP BY Activity"
		rs.Open sql, dbConn, 0, 1
		If Not(rs.BOF And rs.EOF) Then
			Do Until rs.EOF
				decRunningTotal = decRunningTotal + CDbl(rs("TotalCost"))
				decRunningGSTTotal = decRunningGSTTotal + CDbl(rs("TotalGST"))
				rs.MoveNext
			Loop
		End If
		rs.Close
		Set rs = Nothing
%>
						<tr>
							<td style="font-weight:bold;">Reimbursable Total Inc. GST:</td>
							<td style="text-align:right;"><%= FormatCurrency(decRunningTotal) %></td>
						</tr>
					</table>
				<form method="post" action="ExpenseForm.asp?ExpenseFormDate=<%= FormatDateU(dteExpenseFormDate, False) %>&Code=<%= strCode %>&SignOff=True">
					<b>Report Comments:</b><br>
					<textarea name="ReportComments" rows=7 cols=80></textarea><br><br>
					<input type="submit" value="Sign Off" onclick="return makeSure();">
				</form>
<%
	Else
%>
				<p>The expense form for <%= MonthName(intMonth) %>&nbsp;<%= intYear %> has not yet been signed off.</p>
				<p>Each staff member has to sign off his/her own expense forms before it can be viewed by other staff members.</p>
				<p>Click <a href="javascript:window.close()">here</a> to close this window.</p>

<%
	End If
%>
				</td>
			</tr>
		</table>	
	</body>
</html>
<%
Else
%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
		<style>
			HR {
				text-align: left;
			}
		</style>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
            <form method="get" name="FormFilter">
			<tr>
				<td>
				<input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> 
                &nbsp;&nbsp;&nbsp;<b>Filter:</b>&nbsp;<select name="DivisionId" ID="Select3" style="width:280px;" onchange="document.location.href='<%= Request.ServerVariables("URL") %>?ExpenseFormDate=<%= Request("ExpenseFormDate") %>&Code=<%= Request("Code") %>&SignOff=True&DivisionId='+document.FormFilter.DivisionId.value;">
                    <option value="0">ALL DIVISIONS</option>
<%

    Set rsDiv = Server.CreateObject("ADODB.RecordSet")
    sql = "SELECT * FROM Divisions ORDER BY Division"
    Set rsDiv = dbConn.Execute(sql)

    If Not(rsDiv.BOF And rsDiv.EOF) Then
	    Do Until rsDiv.EOF
	        If intDivisionId = CInt(rsDiv("DivisionId")) And Not(intDivisionId = 0) Then
    		    Response.Write ("                    <option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
            Else
    		    Response.Write ("                    <option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
            End If
		    rsDiv.MoveNext
	    Loop
    End If

    If IsObject(rsDiv) Then
	    rsDiv.Close
	    Set rsDiv = Nothing
    End If

%>
                </select>&nbsp;&nbsp;&nbsp;
				<% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
			</tr>
			</form>
		</table>
		<br>
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sqlUsers = "SELECT Name, ExpenseTypeGroupId FROM Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId WHERE Code = '" & strCode & "'"
	Set rsUsers = dbConn.Execute(sqlUsers)

	strName = rsUsers("Name")
	intExpenseTypeGroupId = rsUsers("ExpenseTypeGroupId")

	rsUsers.Close
	Set rsUsers = Nothing
%>
		<table width=950 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top" class="TimesHeader">
				Expense Form for <%= MonthName(intMonth, False) %>&nbsp;<%= intYear %>&nbsp;:&nbsp;
<%
    If intDivisionId = 0 Then
        Response.Write("For All Divisions")
    Else
        Dim rsDiv2
        rsDiv2 = Server.CreateObject("ADODB.RecordSet")
        sql = "Select * From Divisions Where DivisionId = " & intDivisionId
        Set rsDiv2 = dbConn.Execute(sql)
        Response.Write("For " & rsDiv2("Division"))
        rsDiv2.Close
        Set rsDiv2 = Nothing
    End If
%>
                </span>
				<td valign="top" class="TimesHeader" align="right"><%= strName %></td>
			</tr>
			<tr>
				<td colspan=2 class="TimesItalicBold">Printed on <%= FormatDateTime(ServerToEST(Now()), 1) %></td>
			</tr>
		</table>
<%
	Set rsSignOff = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT ReportComments FROM ExpensesSignOffs WHERE Code = '" & strCode & "' AND [Month] = " & intMonth & " AND [Year] = " & intYear
	Set rsSignOff = dbConn.Execute(sql)

	If Not(rsSignOff.BOF And rsSignOff.EOF) Then
		strReportComments = rsSignOff("ReportComments")
		If Len(strReportComments) > 0 Then
%>
		<br>
		<table width="950" cellpadding=3 cellspacing=0 border=0 ID="Table7">
			<tr>
				<td><span class="TimesItalicBold">Report Comments</span></td>
			</tr>
			<tr>
				<td><%= strReportComments %></td>
			</tr>
		</table>
<%
		End If
	End If

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ExpenseForm WHERE Code = '" & strCode & "' AND Month([Date]) = " & intMonth & " AND Year([Date]) = " & intYear
	If intDivisionId <> 0 Then sql = sql & " AND DivisionId = " & intDivisionId
	Set rs = dbConn.Execute(sql)

	If Not(rs.BOF And rs.EOF) Then
%>
		<br>
		<table width="950" cellpadding=3 cellspacing=0 border=0>
			<tr>
				<th style="text-align:left;" class="TimesItalicBold" nowrap>Date</td>
				<th style="text-align:left;" class="TimesItalicBold" nowrap>Activity</td>
				<th style="text-align:left;" class="TimesItalicBold" nowrap>Customer</td>
				<th style="text-align:right;" class="TimesItalicBold" nowrap>Cost</td>
				<th style="text-align:right;" class="TimesItalicBold" nowrap>GST</td>
				<th style="text-align:center;" class="TimesItalicBold" nowrap>Cash</td>
				<th style="text-align:left;" class="TimesItalicBold" width=300>Details</td>
			</tr>
<%
		decRunningTotal = 0
		decRunningGSTTotal = 0
		Do Until rs.EOF
			If IsNumeric(rs("Cost")) Then decRunningTotal = decRunningTotal + CDbl(rs("Cost"))
			If IsNumeric(rs("GST")) Then decRunningGSTTotal = decRunningGSTTotal + CDbl(rs("GST"))
%>
			<tr>
				<td style="vertical-align:top;text-align:left;" nowrap><%= FormatDateU(rs("Date"), False) %></td>
				<td style="vertical-align:top;text-align:left;" nowrap><%= rs("Activity") %></td>
				<td style="vertical-align:top;text-align:left;" nowrap><%= rs("Contact") %></td>
<%
			If rs("Type") = 2 Then
%>
				<td style="vertical-align:top;text-align:right;" nowrap><% If IsNumeric(rs("Cost")) Then Response.Write(FormatCurrency(rs("Cost"), 2)) %></td>
				<td style="vertical-align:top;text-align:right;" nowrap><% If IsNumeric(rs("GST")) Then Response.Write(FormatCurrency(rs("GST"), 2)) %></td>
				<td style="vertical-align:top;text-align:center;" nowrap><%= Replace(Replace(rs("Card"), -1, "No"), 0, "Yes") %></td>
<%
			Else
%>
				<td style="vertical-align:top;text-align:right;" nowrap>&nbsp;</td>
				<td style="vertical-align:top;text-align:right;" nowrap>&nbsp;</td>
				<td style="vertical-align:top;text-align:right;" nowrap>&nbsp;</td>
<%
			End If
%>
				<td style="vertical-align:top;text-align:left;" width=300><%= UCase(rs("Details")) %></td>
			</tr>
<%
			rs.MoveNext
		Loop
%>
			<tr>
				<td></td>
				<td></td>
				<td></td>
				<td align="right" style="border-top:1px solid black;"><%= FormatCurrency(decRunningTotal,2) %></td>
				<td align="right" style="border-top:1px solid black;"><%= FormatCurrency(decRunningGSTTotal,2) %></td>
			</tr>
		</table>
		<br>
		<hr width=950>
<%
	Else
		Response.Write("<br><table cellpadding=3 cellspacing=0 border=0><tr><td>There are no activities for this month.</td></tr></table>")
	End If

	If IsObject(rs) Then
		rs.Close
		Set rs = Nothing
	End If
%>
		<table width="950" cellpadding=5 cellspacing=0 border=0>
			<tr>
<%
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Activity, Sum(Cost) AS TotalCost, Sum(GST) AS TotalGST FROM ExpenseForm_Expenses WHERE ExpenseForm_Expenses.Code = '" & strCode & "' AND Month([Date])=" & intMonth & " AND Year([Date])= " & intYear & " AND ExpenseForm_Expenses.Card = true"
	If intDivisionId <> 0 Then sql = sql & " AND DivisionId = " & intDivisionId
	sql = sql & " GROUP BY Activity"
	rs.Open sql, dbConn, 0, 1 

	If Not(rs.BOF And rs.EOF) Then
%>
				<td valign="top" width="50%">
					<table width="100%" cellpadding=3 cellspacing=0 border=0>
						<tr>
							<td colspan=3 class="TimesItalicBold">Expense Summary AMEX</td>
						</tr>
						<tr>
							<td class="TimesItalicBold">Activity</td>
							<td class="TimesItalicBold" style="text-align:right;width:90px;">Total Inc. GST</td>
							<td class="TimesItalicBold" style="text-align:right;width:90px;">GST</td>
						</tr>
<%
		decRunningTotal = 0
		decRunningGSTTotal = 0
		Do Until rs.EOF
			decRunningTotal = decRunningTotal + CDbl(rs("TotalCost"))
			decRunningGSTTotal = decRunningGSTTotal + CDbl(rs("TotalGST"))
%>
						<tr>
							<td><%= rs("Activity") %></td>
							<td style="text-align:right;" style="text-align:right;width:90px;"><%= FormatCurrency(rs("TotalCost"), 2) %></td>
							<td style="text-align:right;" style="text-align:right;width:90px;"><%= FormatCurrency(rs("TotalGST"), 2) %></td>
						</tr>
<%
			rs.MoveNext
		Loop
%>
						<tr>
							<td></td>
							<td style="text-align:right;border-top:1px solid black;"><%= FormatCurrency(decRunningTotal) %></td>
							<td style="text-align:right;border-top:1px solid black;"><%= FormatCurrency(decRunningGSTTotal) %></td>
						</tr>
					</table>
				</td>
<%
	End If

	If IsObject(rs) Then
		rs.Close
		Set rs = Nothing
	End If

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Activity, Sum(Cost) AS TotalCost, Sum(GST) AS TotalGST FROM ExpenseForm_Expenses WHERE ExpenseForm_Expenses.Code = '" & strCode & "' AND Month([Date])=" & intMonth & " AND Year([Date])= " & intYear & " AND ExpenseForm_Expenses.Card = false"
	If intDivisionId <> 0 Then sql = sql & " AND DivisionId = " & intDivisionId
	sql = sql & " GROUP BY Activity"
	rs.Open sql, dbConn, 0, 1 

	If Not(rs.BOF And rs.EOF) Then
%>
				<td valign="top" width="50%">
					<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table2">
						<tr>
							<td colspan=3 class="TimesItalicBold">Expense Summary Reimbursable</td>
						</tr>
						<tr>
							<td class="TimesItalicBold">Activity</td>
							<td class="TimesItalicBold" style="text-align:right;width:90px;">Total Inc. GST</td>
							<td class="TimesItalicBold" style="text-align:right;width:90px;">GST</td>
						</tr>
<%
		decRunningTotal = 0
		decRunningGSTTotal = 0
		Do Until rs.EOF
			decRunningTotal = decRunningTotal + CDbl(rs("TotalCost"))
			decRunningGSTTotal = decRunningGSTTotal + CDbl(rs("TotalGST"))
%>
						<tr>
							<td><%= rs("Activity") %></td>
							<td style="text-align:right;"><%= FormatCurrency(rs("TotalCost"), 2) %></td>
							<td style="text-align:right;"><%= FormatCurrency(rs("TotalGST"), 2) %></td>
						</tr>
<%
			rs.MoveNext
		Loop
%>
						<tr>
							<td></td>
							<td style="text-align:right;border-top:1px solid black;"><%= FormatCurrency(decRunningTotal) %></td>
							<td style="text-align:right;border-top:1px solid black;"><%= FormatCurrency(decRunningGSTTotal) %></td>
						</tr>
					</table>
				</td>
<%
	End If

	If IsObject(rs) Then
		rs.Close
		Set rs = Nothing
	End If
%>
			</tr>
		</table>
<%
    If intDivisionId = 0 Then
	    Set rs = Server.CreateObject("ADODB.RecordSet")
	    sql = "ExpenseForm_Prospects_Open '" & strCode & "'"
	    rs.Open sql, dbConn, 0, 1 

	    If Not(rs.BOF And rs.EOF) Then
%>
		<br>
		<hr width=950>
		<table width="950" cellpadding=5 cellspacing=0 border=0 ID="Table4">
			<tr>
				<td class="TimesItalicBold" colspan=10>Current Prospect Details</td>
			</tr>
			<tr>
				<td class="TimesItalicBold" width=100 nowrap>Entered</td>
				<td class="TimesItalicBold" width=100>Project Name</td>
				<td class="TimesItalicBold" width=150>Customer</td>
				<td class="TimesItalicBold" width=150>Contact</td>
				<td class="TimesItalicBold" width=80 align="right">Value</td>
				<td class="TimesItalicBold">Required Notes</td>
			</tr>
<%
		    Do Until rs.EOF
%>
			<tr>
				<td valign="top"><%= FormatDateU(rs("Date"), False) %></td>
				<td valign="top"><%= rs("Project") %></td>
				<td valign="top"><%= rs("Company") %></td>
				<td valign="top"><%= rs("Contact") %></td>
				<td valign="top" align="right"><%

			    If rs("OneOffSalesProject") Then
				    Response.Write(FormatCurrency(rs("Value"),2))
			    Else
				    Response.Write(FormatCurrency(rs("AmountPerMonth")*rs("NumberOfMonths"),2))
			    End If

%></td>
				<td valign="top"><%= rs("Comment") %></td>
<%
			    rs.MoveNext
		    Loop
%>
			</tr>
		</table>
<%
	    End If

	    If IsObject(rs) Then
		    rs.Close
		    Set rs = Nothing
	    End If

	    Set rs = Server.CreateObject("ADODB.RecordSet")
	    sql = "ExpenseForm_Prospects_Open_Summary '" & strCode & "'"
	    rs.Open sql, dbConn, 0, 1 

	    If Not(rs.BOF And rs.EOF) Then
%>
		<br>
		<hr width=950>
		<table width="500" cellpadding=5 cellspacing=0 border=0 ID="Table5">
			<tr>
				<td class="TimesItalicBold">Current Prospect Summary</td>
			</tr>
			<tr>
				<td class="TimesItalicBold">Customer</td>
				<td class="TimesItalicBold" style="text-align:right;width:150px;">Value</td>
			</tr>
<%
		    Do Until rs.EOF
%>
			<tr>
				<td><%= rs("Company") %></td>
				<td style="text-align:right;"><%= FormatCurrency(rs("TotalValue"), 2) %></td>
			</tr>
<%
			    rs.MoveNext
		    Loop
%>
		</table>
<%
	    End If

	    If IsObject(rs) Then
		    rs.Close
		    Set rs = Nothing
	    End If

	    Set rs = Server.CreateObject("ADODB.RecordSet")
	    sql = "ExpenseForm_CallReports_Summary '" & strCode & "', " & intMonth & ", " & intYear
	    rs.Open sql, dbConn, 0, 1 

	    If Not(rs.BOF And rs.EOF) Then
%>
		<br>
		<hr width=950>
		<table width="500" cellpadding=5 cellspacing=0 border=0 ID="Table6">
			<tr>
				<td class="TimesItalicBold">Calls Summary</td>
			</tr>
			<tr>
				<td class="TimesItalicBold">Customer</td>
				<td class="TimesItalicBold" style="text-align:right;width:150px;">Contacts</td>
			</tr>
<%
		    Do Until rs.EOF
%>
			<tr>
				<td><%= rs("Company") %></td>
				<td style="text-align:right;"><%= rs("TotalCalls") %></td>
			</tr>
<%
			    rs.MoveNext
		    Loop
%>
		</table>
<%
	    End If

	    If IsObject(rs) Then
		    rs.Close
		    Set rs = Nothing
	    End If
    End If
%>
		<br />
		<table width=950 cellpadding=0 cellspacing=0 border=0>
			<tr>
				<td colspan=2>
					<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table8">
						<tr>
							<td valign="top" width="50%" align="center">
								<table cellpadding=10 cellspacing=0 border=0>
									<tr>
										<td valign="top">Submitted by</td>
									</tr>
									<tr>
										<td>______________________________________________________</td>
									</tr>
									<tr>
										<td><%= strName %></td>
									</tr>
								</table>
							</td>
							<td valign="top" width="50%" align="center">
								<table cellpadding=10 cellspacing=0 border=0 ID="Table9">
									<tr>
										<td valign="top">Approved by</td>
									</tr>
									<tr>
										<td>______________________________________________________</td>
									</tr>
									<tr>
										<td>Name _________________________________________________</td>
									</tr>
								</table>
							</td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
	</body>
</html>
<%
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->