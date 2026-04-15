<%@ Language=VBScript %>
<!--#include virtual="/System/ssi_functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open_dev.inc"-->
<!--#include virtual="/FusionChart/FC_Colors.asp" --><%

Dim strCode
Dim intYear
Dim decProspectsBudget
Dim arrValues(12)

strCode = CStr(Trim(Request("Code")))
intYear = CLng(Request("Year"))
decProspectsBudget = Trim(Request("ProspectsBudget"))
intDivisionId = CLng(Request("DivisionId"))
strUserCodes = Trim(Request("UserCodes"))

%><graph caption='Prospects Chart' showValues='0'>
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

Dim oRs
Dim sql
Set oRs = Server.CreateObject("ADODB.Recordset")
If strCode <> "All" Then
	sql = "SELECT FinancialYear.Month As FinMonth, FinancialYear.Year, Sum(SalesProjects.Value) + Sum(SalesProjects.AmountPerMonth*SalesProjects.NumberOfMonths) AS SumOfValue FROM FinancialYear LEFT JOIN SalesProjects ON Month(SalesProjects.ProspectDate) = FinancialYear.Month WHERE SalesProjects.Code = '" & strCode & "' AND ((Year(ProspectDate) = " & intYear & " AND Month(ProspectDate) In (7,8,9,10,11,12)) Or (Year(ProspectDate) = " & intYear+1 & ") AND Month(ProspectDate) In (1,2,3,4,5,6)) GROUP BY FinancialYear.Month, FinancialYear.Year"
Else
	'Ensure that year 0 is not selecting months it shouldn't be.
	'sql = "SELECT FinancialYear.Month As FinMonth, FinancialYear.Year, Sum(SalesProjects.Value) + Sum(SalesProjects.AmountPerMonth*SalesProjects.NumberOfMonths) AS SumOfValue FROM FinancialYear LEFT JOIN SalesProjects ON Month(SalesProjects.ProspectDate) = FinancialYear.Month WHERE (Year(ProspectDate) = " & intYear & " Or Year(ProspectDate) = " & intYear+1 & ") AND SalesProjects.Code IN (" & strUserCodes & ") GROUP BY FinancialYear.Month, FinancialYear.Year"
End If
oRs.Open sql, dbConn
response.Write sql
While Not oRs.EOF
	arrValues(oRs("FinMonth")) = oRs("SumOfValue")
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
	If Not(IsNumeric(decProspectsBudget)) Then
		Response.Write "		<set value='0' />" & vbCrlf
	Else
		Response.Write "		<set value='" & decProspectsBudget & "' />" & vbCrlf
	End If
	i = i + 1
Loop

%>	</dataset>
</graph>