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

Dim intCallReportTypeId
Dim dteDateEntered
Dim dteRealDateEntered
Dim intContactId
Dim strCallPurpose
Dim strComment
Dim lngSalesProjectId
Dim sql

intCallReportTypeId = CLng(Request("CallReportTypeId"))
dteDateEntered = ServerToEST(CDate(Request("DateEntered")))
dteRealDateEntered = ServerToEST(Now())
intContactId = CLng(Request("ContactId"))
strCallPurpose = Trim(Replace(Request("CallPurpose"),"'","''"))
strComment = Trim(Replace(Request("Comment"),"'","''"))
lngSalesProjectId = CLng(Request("SalesProjectId"))

sql = "Insert Into CallReports (CallReportTypeId, Code, RealDateEntered, DateEntered, ContactId, CallPurpose, Comment, SalesProjectId) Values (" & intCallReportTypeId & ", '" & Request.Cookies("UserSettings")("Code") & "', '" & DBDate(dteRealDateEntered) & "', '" & DBDate(dteDateEntered) & "', '" & intContactId & "', '" & strCallPurpose & "', '" & strComment & "', " & lngSalesProjectId & ")"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Call+report+added")

%>