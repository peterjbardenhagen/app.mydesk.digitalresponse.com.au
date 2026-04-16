<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngQid
lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Update Quotes Set QuoteStatusId = 9 Where Qid = " & lngQid
dbConn.Execute(sql)

sql = "Insert Into QuoteApproval (Qid, Code) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "')"
dbConn.Execute(sql)

' Get the current users approval limits
sql = "Select UserRoles.QuoteApprovalLimit From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & Request.Cookies("UserSettings")("Code") & "'"
Set rsCheck = dbConn.Execute(sql)

dblQuoteApprovalLimit = CDbl(rsCheck("QuoteApprovalLimit"))

sql = "Select Quotes.*, Users.Email From Quotes Inner Join Users On Quotes.Code = Users.Code Where Qid = " & lngQid
Set rsCheck = dbConn.Execute(sql)

dblQuoteValue = rsCheck("NettPriceTotal")
strEmail = rsCheck("Email")

If IsDirector(Request.Cookies("UserSettings")("Code")) Or GetQuoteLastLineApprover(lngQid) = Request.Cookies("UserSettings")("Name") Or dblQuoteApprovalLimit => dblQuoteValue Then
	sql = "Update Quotes Set QuoteStatusId = 10 Where Qid = " & lngQid
	dbConn.Execute(sql)
	strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Approved by " & Request.Cookies("UserSettings")("Name") & ". The approval process has been completed. You may now issue the quote." & QuoteDetails_ForEmail(lngQid)
'	SendMail Request.Cookies("UserSettings")("Email"), strEmail, "MyDesk Alert : Quote #" & lngQid & " : Approved by " & Request.Cookies("UserSettings")("Name"), strBodyText
Else
	strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Approved by " & Request.Cookies("UserSettings")("Name") & ". The next approver in the approval process is " & GetQuoteNextLineApprover(lngQid) & "." & QuoteDetails_ForEmail(lngQid)
'	SendMail Request.Cookies("UserSettings")("Email"), GetQuoteNextLineApprover_Email(lngQid), "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Waiting for your approval. Just approved by " & Request.Cookies("UserSettings")("Name"), strBodyText
End If

' Audit trail
sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Approved', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("View.asp?Qid=" & lngQid & "&Msg=Quote+approved")

%>