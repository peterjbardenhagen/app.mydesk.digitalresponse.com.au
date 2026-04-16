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

Dim strCode
Dim strFirstName
Dim strSurname
Dim strPosition
Dim intCompanyId
Dim strCCompany
Dim strAddress1
Dim strAddress2
Dim strSuburb
Dim strPostCode
Dim intStateId
Dim strState
Dim strCountry
Dim strOAddress1
Dim strOAddress2
Dim strOSuburb
Dim strOPostCode
Dim intOStateId
Dim strOState
Dim strOCountry
Dim strPhone
Dim strFax
Dim strEmail
Dim strWebsite
Dim strNotes
Dim sql

strCode = Request.Cookies("UserSettings")("Code")
strFirstName = Replace(Trim(Request("FirstName")), "'", "''")
strSurname = Replace(Trim(Request("Surname")), "'", "''")
strPosition = Replace(Trim(Request("Position")), "'", "''")
intCompanyId = CLng(Request("CompanyId"))
strCCompany = Replace(Trim(Request("CCompany")), "'", "''")
strAddress1 = Replace(Trim(Request("Address1")), "'", "''")
strAddress2 = Replace(Trim(Request("Address2")), "'", "''")
strSuburb = Replace(Trim(Request("Suburb")), "'", "''")
strPostCode = Replace(Trim(Request("PostCode")), "'", "''")
intStateId = CLng(Request("StateId"))
strState = Replace(Trim(Request("State")), "'", "''")
strCountry = Replace(Trim(Request("Country")), "'", "''")
strOAddress1 = Replace(Trim(Request("OAddress1")), "'", "''")
strOAddress2 = Replace(Trim(Request("OAddress2")), "'", "''")
strOSuburb = Replace(Trim(Request("OSuburb")), "'", "''")
strOPostCode = Replace(Trim(Request("OPostCode")), "'", "''")
intOStateId = CLng(Request("OStateId"))
strOState = Replace(Trim(Request("OState")), "'", "''")
strOCountry = Replace(Trim(Request("OCountry")), "'", "''")
strPhone = Replace(Trim(Request("Phone")), "'", "''")
strFax = Replace(Trim(Request("Fax")), "'", "''")
strMobile = Replace(Trim(Request("Mobile")), "'", "''")
strEmail = Replace(Trim(Request("Email")), "'", "''")
strWebsite = Replace(Trim(Request("Website")), "'", "''")
strNotes = Replace(Trim(Request("Notes")), "'", "''")

If strWebsite = "http://" Then strWebsite = ""

sql = "Insert Into Contacts " &_
		"(Code, FirstName, Surname, Position, CompanyId, CCompany, Address1, Address2, Suburb, PostCode, StateId, State, Country, OAddress1, OAddress2, OSuburb, OPostCode, OStateId, OState, OCountry, Phone, Fax, Mobile, Email, Website, Notes) " &_
		"Values ('" & strCode & "', '" & strFirstName & "', '" & strSurname & "', '" & strPosition & "', " & intCompanyId & ", '" & strCCompany & "', '" & strAddress1 & "', '" & strAddress2 & "', '" & strSuburb & "', '" & strPostCode & "', " & intStateId & ", '" & strState & "', '" & strCountry & "', '" & strOAddress1 & "', '" & strOAddress2 & "', '" & strOSuburb & "', '" & strOPostCode & "', " & intOStateId & ", '" & strOState & "', '" & strOCountry & "', '" & strPhone & "', '" & strFax & "', '" & strMobile & "', '" & strEmail & "', '" & strWebsite & "', '" & strNotes & "')"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Contact+added")

%>