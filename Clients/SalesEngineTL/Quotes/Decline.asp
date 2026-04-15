<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngQid

lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Insert Into QuoteApproval (Qid, Code) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "')"
dbConn.Execute(sql)

sql = "Update Quotes Set QuoteStatusId = 11 Where Qid = " & lngQid
dbConn.Execute(sql)

' Audit trail
sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Declined', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

' Get the value of the quote
sql = "Select Quotes.*, Users.Email From Quotes Inner Join Users On Users.Code = Quotes.Code Where Qid = " & lngQid
Set rsCheck = dbConn.Execute(sql)

strEmail = rsCheck("Email")

SendMail Request.Cookies("UserSettings")("Email"), strEmail, Request.Cookies("ClientSettings")("PortalCompany") & " MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Declined by " & Request.Cookies("UserSettings")("Name"), "MyDesk Alert : Quote #" & lngQid & " : Declined by " & Request.Cookies("UserSettings")("Name") & "." & QuoteDetails_ForEmail(lngQid)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("View.asp?Qid=" & lngQid & "&Msg=Quote+declined")

%>