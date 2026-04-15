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

Dim intUserRoleId
Dim strUserRole
Dim decPOApprovalLimit
Dim decPOCapExApprovalLimit
Dim decQuoteApprovalLimit
Dim sql

intUserRoleId = CInt(Request("UserRoleId"))
strUserRole = Trim(Replace(Request("UserRole"),"'","''"))
decPOApprovalLimit = CDbl(Request("POApprovalLimit"))
decPOCapExApprovalLimit = CDbl(Request("POCapExApprovalLimit"))
decQuoteApprovalLimit = CDbl(Request("QuoteApprovalLimit"))

sql = "Update [UserRoles] Set UserRole = '" & strUserRole & "', POApprovalLimit = " & decPOApprovalLimit & ", POCapExApprovalLimit = " & decPOCapExApprovalLimit & ", QuoteApprovalLimit = " & decQuoteApprovalLimit & " Where UserRoleId = " & intUserRoleId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=User+Role+updated")

%>