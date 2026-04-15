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

Dim intDivisionId
Dim strCustomerCode
Dim strSupplierCode
Dim strCompany
Dim strVisible
Dim sql

intDivisionId = CLng(Request("DivisionId"))
strCustomerCode = Trim(Replace(Request("CustomerCode"),"'","''"))
strSupplierCode = Trim(Replace(Request("SupplierCode"),"'","''"))
strCompany = Trim(Replace(Request("Company"),"'","''"))
strVisible = 1

sql = "Insert Into Companies (DivisionId, CustomerCode, SupplierCode, Company, Visible) Values (" & intDivisionId & ", '" & strCustomerCode & "', '" & strSupplierCode & "', '" & strCompany & "', " & strVisible & ")"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Company+added")

%>