<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

' Increase Invoice # to 1,000

i = 0
For i = 0 To i = 999
	sql = "SELECT TOP 1 InvoiceId FROM Invoices ORDER BY InvoiceId DESC"
	Set rs = dbConn.Execute(sql)
	If (rs("InvoiceId") => 999) Then
		Exit For
	End If
	sql = "INSERT INTO Invoices Set Qid = 12345"
	dbConn.Execute(sql)
Next

sql = "DELETE FROM Invoices WHERE Qid = 12345"
dbConn.Execute(sql) ' Clean up

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Upgrade+complete")

%>