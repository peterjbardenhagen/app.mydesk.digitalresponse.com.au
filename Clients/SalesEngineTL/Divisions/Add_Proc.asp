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

strDivisionCode = Trim(Replace(Request("DivisionCode"),"'","''"))
strDivision = Trim(Replace(Request("Division"),"'","''"))
strABN = Trim(Replace(Request("ABN"),"'","''"))
strACN = Trim(Replace(Request("ACN"),"'","''"))
strQuotes = Trim(Request("Quotes"))
strRFQ = Trim(Request("RFQ"))
strProspects = Trim(Request("Prospects"))
strPurchaseOrders = Trim(Request("PurchaseOrders"))
strPurchaseRequests = Trim(Request("PurchaseRequests"))
strLogo = Trim(Replace(Request("Logo"),"'","''"))
strUsersAccessAll = Trim(Request("UsersAccessAll"))
dblMinimumMargin = CDbl(Request("MinimumMargin"))

'patch
sql = "Select Max(DivisionId) As MaxDivisionId From Divisions"
Set rsMaxLoc = dbConn.Execute(sql)

sql = "Insert Into Divisions (DivisionId, DivisionCode, Division, ACN, ABN, Quotes, RFQ, Prospects, PurchaseOrders, PurchaseRequests, UsersAccessAll, Logo, Visible, MinimumMargin) " &_
      "Values (" & rsMaxLoc("MaxDivisionId")+1 & ", '" & strDivisionCode & "', '" & strDivision & "', '" & strABN & "', '" & strACN & "', " & strQuotes & ", " & strRFQ & ", " & strProspects & ", " & strPurchaseOrders & ", '" & strPurchaseRequests & "', '" & strUsersAccessAll & "', '" & strLogo & "', 1, " & dblMinimumMargin & ")"
dbConn.Execute(sql)

rsMaxLoc.Close
Set rsMaxLoc = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Division+added")

%>