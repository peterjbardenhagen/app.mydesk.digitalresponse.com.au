<%@ Language=VBScript %>
<!--#include virtual="/System/ssi_functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/FusionChart/FC_Colors.asp" --><%

Dim strCode
Dim intYear
Dim decExpensesBudget
Dim arrValues(12)

strCode = CStr(Trim(Request("Code")))
intYear = CLng(Request("Year"))
decExpensesBudget = Trim(Request("ExpensesBudget"))
intDivisionId = CLng(Request("DivisionId"))
strUserCodes = Trim(Request("UserCodes"))

%><graph caption='Monthly Expenses Chart MTD' showValues='0'>
	<categories>
		<category Name='Jul' />
		<category Name='Aug' />
		<category Name='Sep' />
		<category Name='Oct' />
		<category Name='Nov' />
		<category Name='Dec' />
		<category Name='Jan' />
		<category Name='Feb' />
		<category Name='Mar' />
		<category Name='Apr' />
		<category Name='May' />
		<category Name='Jun' />
	</categories>
	<dataset seriesName='Actual' color='cc3333' anchorBorderColor='cc3333'>
<%

Dim uRs, sql, lngExpensesPerMonth
Set uRs = Server.CreateObject("ADODB.Recordset")
sql = "Select ExpensesPerMonth From Users Where Code = '" & strCode & "'"
Set uRs = dbConn.Execute(sql)

If Not(uRs.BOF And uRs.EOF) Then
    lngExpensesPerMonth = uRs("ExpensesPerMonth")
End If

uRs.Close
Set uRs = Nothing

Dim oRs
Set oRs = Server.CreateObject("ADODB.Recordset")
' For single rep
sql = "SELECT FinancialYear.Month As FinMonth, FinancialYear.Year, Sum(CostIncGST) As TotalValue FROM FinancialYear LEFT JOIN Expenses ON Month(Expenses.ExpenseDate) = FinancialYear.Month WHERE Expenses.Code = '" & strCode & "' AND ((Year(Expenses.ExpenseDate) = " & intYear & " AND Month(Expenses.ExpenseDate) In (7,8,9,10,11,12)) Or (Year(Expenses.ExpenseDate) = " & intYear+1 & ") AND Month(Expenses.ExpenseDate) In (1,2,3,4,5,6)) GROUP BY FinancialYear.Month, FinancialYear.Year"
oRs.Open sql, dbConn
While Not oRs.EOF
	arrValues(oRs("FinMonth")) = oRs("TotalValue")
	oRs.MoveNext()
Wend

Dim i
i = 7

Do
	If arrValues(i) <> "" Then
		Response.Write "		<set value='" & arrValues(i) & "' />" & vbCrlf
	Else
		Response.Write "		<set value='0'/>" & vbCrlf
	End If

	If i = 6 Then
		Exit Do
	End If

	i = i + 1

	If i = 13 Then
		i = 1
	End If
Loop

%>	</dataset>
	<dataset seriesName='Budget' color='1aa552' anchorBorderColor='1aa552' parentYAxis='S'>
<%

i = 0
Do Until i = 12
	If Not(IsNumeric(lngExpensesBudget)) Then
		Response.Write "		<set value='0' />" & vbCrlf
	Else
		Response.Write "		<set value='" & lngExpensesPerMonth & "' />" & vbCrlf
	End If
	i = i + 1
Loop

%>	</dataset>
</graph>