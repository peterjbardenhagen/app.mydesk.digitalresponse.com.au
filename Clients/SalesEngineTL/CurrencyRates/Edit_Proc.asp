<% 

Option Explicit

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

Dim intCurrencyId
Dim strCurrencyName
Dim strCurrencyPrefix
Dim decCurrencyRate
Dim sql

intCurrencyId = CInt(Request("CurrencyId"))
strCurrencyName = Trim(Replace(Request("CurrencyName"),"'","''"))
strCurrencyPrefix = Trim(Replace(Request("CurrencyPrefix"),"'","''"))
decCurrencyRate = Trim(Request("CurrencyRate"))

sql = "Update [Currency] Set CurrencyName = '" & strCurrencyName & "', CurrencyPrefix = '" & strCurrencyPrefix & "', CurrencyRate = '" & decCurrencyRate & "' Where CurrencyId = " & intCurrencyId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Currency+Rate+updated")

%>