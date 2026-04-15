<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("UserSettings")("UserTypeId") = 6 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

intDivisionId = CLng(Request("DivisionId"))
strDivisionCode = Trim(Replace(Request("DivisionCode"),"'","''"))
strDivision = Trim(Replace(Request("Division"),"'","''"))
strABN = Trim(Replace(Request("ABN"),"'","''"))
strACN = Trim(Replace(Request("ACN"),"'","''"))
strQuotes = Trim(Request("Quotes"))
strRFQ = Trim(Request("RFQ"))
strProspects = Trim(Request("Prospects"))
strPurchaseOrders = Trim(Request("PurchaseOrders"))
strPurchaseRequests = Trim(Request("PurchaseRequests"))
strUsersAccessAll = Trim(Request("UsersAccessAll"))
strLogo = Trim(Replace(Request("Logo"),"'","''"))
strVisible = 1
dblMinimumMargin = CDbl(Request("MinimumMargin"))

sql = "Update Divisions Set DivisionCode = '" & strDivisionCode & "', Division = '" & strDivision & "', ACN = '" & strACN & "', ABN = '" & strABN & "', Visible = " & strVisible & ", Quotes = " & strQuotes & ", RFQ = " & strRFQ & ", Prospects = " & strProspects & ", PurchaseOrders = " & strPurchaseOrders & ", PurchaseRequests = '" & strPurchaseRequests & "', UsersAccessAll = '" & strUsersAccessAll & "', Logo = '" & strLogo & "', MinimumMargin = " & dblMinimumMargin & " Where DivisionId = " & intDivisionId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Division+updated")

%>