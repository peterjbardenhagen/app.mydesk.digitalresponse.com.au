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

Dim strCompany
Dim strACN
Dim strABN
Dim strAddress1
Dim strAddress2
Dim strSuburb
Dim strState
Dim strPostCode
Dim strCountry
Dim strPODisplay
Dim strPOAddress1
Dim strPOAddress2
Dim strPOSuburb
Dim strPOState
Dim strPOPostCode
Dim strPOCountry
Dim strPhone
Dim strFax
Dim strEmail
Dim strWebsite
Dim lngExpenseTypeGroupId
Dim sql

strCompany = Trim(Replace(Request("Company"),"'","''"))
strACN = Trim(Replace(Request("ACN"),"'","''"))
strABN = Trim(Replace(Request("ABN"),"'","''"))
strAddress1 = Trim(Replace(Request("Address1"),"'","''"))
strAddress2 = Trim(Replace(Request("Address2"),"'","''"))
strSuburb = Trim(Replace(Request("Suburb"),"'","''"))
intStateId = CInt(Request("StateId"))
strPostCode = Trim(Request("PostCode"))
strCountry = Trim(Replace(Request("Country"),"'","''"))
strPODisplay = Request("PODisplay")
strPOAddress1 = Trim(Replace(Request("POAddress1"),"'","''"))
strPOAddress2 = Trim(Replace(Request("POAddress2"),"'","''"))
strPOSuburb = Trim(Replace(Request("POSuburb"),"'","''"))
intPOStateId = CInt(Request("POStateId"))
strPOPostCode = Trim(Request("POPostCode"))
strPOCountry = Trim(Replace(Request("POCountry"),"'","''"))
strPhone = Trim(Replace(Request("Phone"),"'","''"))
strFax = Trim(Replace(Request("Fax"),"'","''"))
strEmail = Trim(Replace(Request("Email"),"'","''"))
lngExpenseTypeGroupId = CLng(Request("ExpenseTypeGroupId"))

sql = "Insert Into Locations (Company, ACN, ABN, Address1, Address2, Suburb, StateId, PostCode, Country, PODisplay, POAddress1, POAddress2, POSuburb, POStateId, POPostCode, POCountry, Phone, Fax, Email, Website, ExpenseTypeGroupId) Values ('" & strCompany & "', '" & strACN & "', '" & strABN & "', '" & strAddress1 & "', '" & strAddress2 & "', '" & strSuburb & "', " & intStateId & ", '" & strPostCode & "', '" & strCountry & "', " & strPODisplay & ", '" & strPOAddress1 & "', '" & strPOAddress2 & "', '" & strPOSuburb & "', " & intPOStateId & ", '" & strPOPostCode & "', '" & strPOCountry & "', '" & strPhone & "', '" & strFax & "', '" & strEmail & "', '" & strWebsite & "', " & lngExpenseTypeGroupId & ")"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Location+added")

%>