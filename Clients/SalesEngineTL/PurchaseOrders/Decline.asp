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

sql = "Update PurchaseOrders Set POStatusId = 5 Where POid = " & lngPOid
dbConn.Execute(sql)

' Audit trail
sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Declined', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

' Get the value of the purchase order
sql = "Select PurchaseOrders.*, Users.Email From PurchaseOrders Inner Join Users On Users.Code = PurchaseOrders.Code Where POid = " & lngPOid
Set rsCheck = dbConn.Execute(sql)

strEmail = rsCheck("Email")

SendMail Request.Cookies("UserSettings")("Email"), strEmail, "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Declined by " & Request.Cookies("UserSettings")("Name"), "MyDesk Alert : Purchase Order #" & lngPOid & " : Declined by " & Request.Cookies("UserSettings")("Name") & PurchaseOrderDetails_ForEmail(lngPOid)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("View.asp?POid=" & lngPOid & "&Msg=Purchase+Order+declined")

%>