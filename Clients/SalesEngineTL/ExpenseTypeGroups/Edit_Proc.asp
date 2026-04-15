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

Dim intExpenseTypeGroupId
Dim strExpenseTypeGroup
Dim sql

intExpenseTypeGroupId = CInt(Request("ExpenseTypeGroupId"))
strExpenseTypeGroup = Trim(Replace(Request("ExpenseTypeGroup"),"'","''"))

sql = "Update ExpenseTypeGroups Set ExpenseTypeGroup = '" & strExpenseTypeGroup & "' Where ExpenseTypeGroupId = " & intExpenseTypeGroupId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Expense+Type+Group+updated")

%>