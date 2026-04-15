<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim intExpenseTypeId
Dim strExpenseCode
Dim strFBTExpenseCode
Dim strExpenseType
Dim strVisible
Dim sql

intExpenseTypeId = CLng(Request("ExpenseTypeId"))
strExpenseCode = Trim(Replace(Request("ExpenseCode"),"'","''"))
strFBTExpenseCode = Trim(Replace(Request("FBTExpenseCode"),"'","''"))
strExpenseType = Trim(Replace(Request("ExpenseType"),"'","''"))
strVisible = 1

sql = "Update ExpenseTypes Set ExpenseCode = '" & strExpenseCode & "', FBTExpenseCode = '" & strFBTExpenseCode & "', ExpenseType = '" & strExpenseType & "', Visible = " & strVisible & " Where ExpenseTypeId = " & intExpenseTypeId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Expense+Type+updated")

%>