<% 

'Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strUserRole
Dim decPOApprovalLimit
Dim decPOCapExApprovalLimit
Dim decQuoteApprovalLimit
Dim sql

strUserRole = Trim(Replace(Request("UserRole"),"'","''"))
decPOApprovalLimit = CDbl(Request("POApprovalLimit"))
decPOCapExApprovalLimit = CDbl(Request("POCapExApprovalLimit"))
decQuoteApprovalLimit = CDbl(Request("QuoteApprovalLimit"))

sql = "Insert Into [UserRoles] (UserRole, POApprovalLimit, POCapExApprovalLimit, QuoteApprovalLimit) Values ('" & strUserRole & "', '" & decPOApprovalLimit & "', " & decPOCapExApprovalLimit & ", " & decQuoteApprovalLimit & ")"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=User+Role+added")

%>