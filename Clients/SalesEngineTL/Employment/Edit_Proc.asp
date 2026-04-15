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
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim lngEmploymentId
Dim intDivisionId
Dim dteDateExpires
Dim strTitle
Dim strDetails
Dim sql

lngEmploymentId = CLng(Request("EmploymentId"))
intDivisionId = CLng(Request("DivisionId"))
dteDateExpires = DBDate(Request("DateExpires"))
strTitle = Trim(Replace(Request("Title"),"'","''"))
strDetails = Trim(Replace(Request("Details"),"'","''"))

sql = "Update Employment Set DivisionId = " & intDivisionId & ", DateExpires = '" & dteDateExpires & "', Title = '" & strTitle & "', Details = '" & strDetails & "' Where EmploymentId = " & lngEmploymentId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Employment+Opportunity+updated")

%>