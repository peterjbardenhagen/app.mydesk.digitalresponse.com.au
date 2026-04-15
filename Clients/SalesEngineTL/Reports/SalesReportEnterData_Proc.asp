<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim intYear, strCode, dteDate, decSalesValue, decSalesBudget, dteBegin, dteEnd

intYear = CLng(Request("Year"))
intYearConst = intYear
dteBegin = CStr("01-Jul-20" & MakePadding(Right(intYear, 2), "", 2))
dteEnd = CStr("30-Jun-20" & MakePadding(Right(intYear+1, 2), "", 2))
strCode = Trim(Request("Code"))

' Clear any old sales rep data
sql = "DELETE FROM SalesResults WHERE [Date] >= #" & dteBegin & "# AND [Date] <= #" & dteEnd & "# AND Code = '" & strCode & "'"
dbConn.Execute(sql)

i = 1
intYear = intYear + 1

Do
	If IsNumeric(Request.Form("SalesBudget" & i)) Then
		decSalesBudget = CDbl(Request.Form("SalesBudget" & i))
	Else
		decSalesBudget = 0
	End If
	dteDate = CDate("01/" & MonthName(MakePadding(i, "0", 2), True) & "/" & intYear)

	sql = "INSERT INTO SalesResults (Code, [Date], SalesBudget) VALUES ('" & strCode & "', '" & dteDate & "', " & decSalesBudget & ")"
	dbConn.Execute(sql)

	i = i + 1
	If i = 7 Then intYear = intYear + -1
	If i = 13 Then Exit Do
Loop

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("SalesReport.asp?Year=" & intYearConst & "&Code=" & strCode)

%>