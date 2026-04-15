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
<!--#include virtual="/System/ssi_dates.inc"-->
<%

Dim lngCallReportId
Dim intCallReportTypeId
Dim dteDateEntered
Dim intContactId
Dim strCallPurpose
Dim strComment
Dim lngSalesProjectId
Dim sql

lngCallReportId = CLng(Request("CallReportId"))
intCallReportTypeId = CLng(Request("CallReportTypeId"))
dteDateEntered = Trim(Replace(Request("DateEntered"),"'","''"))
intContactId = Trim(Replace(Request("ContactId"),"'","''"))
strCallPurpose = Trim(Replace(Request("CallPurpose"),"'","''"))
strComment = Trim(Replace(Request("Comment"),"'","''"))
lngSalesProjectId = CLng(Request("SalesProjectId"))

sql = "Update CallReports Set CallReportTypeId = " & intCallReportTypeId & ", DateEntered = '" & dteDateEntered & "', ContactId = '" & intContactId & "', CallPurpose = '" & strCallPurpose & "', Comment = '" & strComment & "', SalesProjectId = " & lngSalesProjectId & " Where CallReportId = " & lngCallReportId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Call+Report+updated")

%>