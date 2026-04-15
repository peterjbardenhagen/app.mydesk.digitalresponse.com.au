<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

Dim lngPOid
Dim boolCapEx

lngPOid = CLng(Request("POid"))
boolCapEx = CBool(Request("HasCapEx"))

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Insert Into PurchaseOrderApproval (POid, Code) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "')"
dbConn.Execute(sql)

' Get the current users approval limits
sql = "Select UserRoles.POApprovalLimit, UserRoles.POCapExApprovalLimit From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & Request.Cookies("UserSettings")("Code") & "'"
Set rsCheck = dbConn.Execute(sql)

If boolCapEx Then
	dblPOApprovalLimit = CDbl(rsCheck("POApprovalLimit"))
Else
	dblPOApprovalLimit = CDbl(rsCheck("POCapExApprovalLimit"))
End If

' Get the value of the purchase order
sql = "Select PurchaseOrders.*, Users.Email From PurchaseOrders Inner Join Users On Users.Code = PurchaseOrders.Code Where POid = " & lngPOid
Set rsCheck = dbConn.Execute(sql)

dblPurchaseOrderValue = rsCheck("PriceIncTotal")
strEmail = rsCheck("Email")

If IsDirector(Request.Cookies("UserSettings")("Code")) Or GetPOLastLineApprover(lngPOid, boolCapEx) = Request.Cookies("UserSettings")("Name") Or dblPOApprovalLimit => dblPurchaseOrderValue Then
	sql = "Update PurchaseOrders Set POStatusId = 3 Where POid = " & lngPOid
	dbConn.Execute(sql)
	strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Approved by " & Request.Cookies("UserSettings")("Name") & ". The approval process has been completed. You may now issue the purchase order." & PurchaseOrderDetails_ForEmail(lngPOid)
Else
	strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Approved by " & Request.Cookies("UserSettings")("Name") & ". The next approver in the approval process is " & GetPONextLineApprover(lngPOid, boolCapEx) & "." & PurchaseOrderDetails_ForEmail(lngPOid)
	SendMail Request.Cookies("UserSettings")("Email"), GetPONextLineApprover_Email(lngPOid, boolCapEx), "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Waiting for your approval. Just approved by " & Request.Cookies("UserSettings")("Name"), strBodyText
End If

' Audit trail
sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Approved', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

SendMail Request.Cookies("UserSettings")("Email"), strEmail, "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Approved by " & Request.Cookies("UserSettings")("Name"), strBodyText

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("View.asp?POid=" & lngPOid & "&Msg=Purchase+Order+approved")

%>