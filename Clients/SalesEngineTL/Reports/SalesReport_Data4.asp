<%@ Language=VBScript %>
<!--#include virtual="/System/ssi_functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open_dev.inc"-->
<!--#include virtual="/FusionChart/FC_Colors.asp" --><%

dteBegin = CStr(Trim(Request("Begin")))
dteEnd = CStr(Trim(Request("End")))
strCode = CStr(Trim(Request("Code")))
intDivisionId = CLng(Request("DivisionId"))
strUserCodes = Trim(Request("UserCodes"))

%><graph caption='Monthly Sales Chart MTD' numdivlines='4' lineThickness='3' showValues='0' numVDivLines='10' formatNumberScale='1' rotateNames='1' decimalPrecision='0' anchorRadius='2' anchorBgAlpha='0' numberPrefix='$' divLineAlpha='30' showAlternateHGridColor='1' yAxisMinValue='0' shadowAlpha='50' >
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
	<dataset seriesName='Actual' color='cc3333' anchorBorderColor='cc3333' anchorRadius='4'>
<%

Dim oRs, sql
Set oRs = Server.CreateObject("ADODB.Recordset")
If strCode <> "All" Then
	sql = "Select Sum(SalesValue) As TotalValue, SalesResults.SalesBudget As TotalSalesBudget From SalesResults_ByCustomer Inner Join SalesResults On SalesResults.Date = SalesResults_ByCustomer.Date And SalesResults.Code = SalesResults_ByCustomer.Code Where SalesResults_ByCustomer.Code = '" & strCode & "' And SalesResults_ByCustomer.[Date] >= #" & dteBegin & "# And SalesResults_ByCustomer.[Date] <= #" & dteEnd & "# Group By SalesResults_ByCustomer.[Date], SalesResults.SalesBudget Order By SalesResults_ByCustomer.[Date]"
Else
'	sql = "Select Sum(Value) As TotalValue, Sum(SalesResults.SalesBudget) As TotalSalesBudget From SalesResults Inner Join Users On SalesResults.Code = Users.Code Where [Date] >= #" & dteBegin & "# And [Date] <= #" & dteEnd & "# And DivisionId = " & intDivisionId & " AND Users.Code IN (" & strUserCodes & ") Group By [date] Order By [Date]"
End If
oRs.Open sql, dbConn

While not oRs.EOF
	Response.Write "		<set value='" & ors("TotalValue") & "' />" & vbCrlf
	oRs.MoveNext()
Wend

%>	</dataset>
	<dataset seriesName='Budget' color='1aa552' anchorBorderColor='1aa552' parentYAxis='S' numberPrefix='$' anchorRadius='4'>
<%

oRs.MoveFirst

While not oRs.EOF
	If Not IsNumeric(ors("TotalSalesBudget")) Then
		Response.Write "		<set value='0' />" & vbCrlf
	Else
		Response.Write "		<set value='" & ors("TotalSalesBudget") & "' />" & vbCrlf
	End If
	oRs.MoveNext()
Wend

oRs.Close()
Set oRs = nothing

%>	</dataset>
</graph>