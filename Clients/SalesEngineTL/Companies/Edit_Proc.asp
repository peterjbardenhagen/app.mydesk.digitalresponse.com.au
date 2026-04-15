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
<%

Dim intCompanyId
Dim strCustomerCode
Dim strSupplierCode
Dim strCompany
Dim strVisible
Dim sql

intCompanyId = CLng(Request("CompanyId"))
strCustomerCode = Trim(Replace(Request("CustomerCode"),"'","''"))
strSupplierCode = Trim(Replace(Request("SupplierCode"),"'","''"))
strCompany = Trim(Replace(Request("Company"),"'","''"))
strVisible = 1

sql = "Update Companies Set SupplierCode = '" & strSupplierCode & "', CustomerCode = '" & strCustomerCode & "', Company = '" & strCompany & "', Visible = '" & strVisible & "' Where CompanyId = " & intCompanyId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Company+updated")

%>