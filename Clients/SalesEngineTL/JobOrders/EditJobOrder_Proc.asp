<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

intDivisionId = Request("DivisionId")
lngJobOrderId = Request("JobOrderId")
lngCompanyId = Request("CompanyId")
strCCompany = Trim(Replace(Request("CCompany"),"'","''"))
strCustomerPO = Trim(Replace(Request("CustomerPO"),"'","''"))
strDelCompany = Trim(Replace(Request("DelCompany"),"'","''"))
strDelAddress1 = Trim(Replace(Request("DelAddress1"),"'","''"))
strDelAddress2 = Trim(Replace(Request("DelAddress2"),"'","''"))
strDelSuburb = Trim(Replace(Request("DelSuburb"),"'","''"))
strDelState = Trim(Replace(Request("DelState"),"'","''"))
intDelStateId = Request("DelStateId")
strDelPostCode = Request("DelPostCode")
strDelCountry = Trim(Replace(Request("DelCountry"),"'","''"))
strInvCompany = Trim(Replace(Request("InvCompany"),"'","''"))
strInvAddress1 = Trim(Replace(Request("InvAddress1"),"'","''"))
strInvAddress2 = Trim(Replace(Request("InvAddress2"),"'","''"))
strInvSuburb = Trim(Replace(Request("InvSuburb"),"'","''"))
strInvState = Trim(Replace(Request("InvState"),"'","''"))
intInvStateId = Request("InvStateId")
strInvPostCode = Request("InvPostCode")
strInvCountry = Trim(Replace(Request("InvCountry"),"'","''"))
strProject = Trim(Replace(Request("Project"),"'","''"))

If intDelStateId = "" Then intDelStateId = 9
If intInvStateId = "" Then intInvStateId = 9

strSql = "Update JobOrders Set CompanyId = " & lngCompanyId & ", Company = '" & strCCompany & "', CustomerPO = '" & strCustomerPO & "', DelCompany = '" & strDelCompany & "', DelAddress1 = '" & strDelAddress2 & "', DelSuburb = '" & strDelSuburb & "', DelState = '" & strDelState & "', DelStateId = " & intDelStateId & ", DelPostCode = '" & strDelPostCode & "', DelCountry = '" & strDelCountry & "', InvAddress1 = '" & strInvAddress2 & "', InvSuburb = '" & strInvSuburb & "', InvState = '" & strInvState & "', InvStateId = " & intInvStateId & ", InvPostCode = '" & strInvPostCode & "', InvCountry = '" & strInvCountry & "' Where JobOrderId = " & lngJobOrderId
dbConn.Execute(strSql)

' Send Purchasing Manager a notification
AlertPurchasingManager intDivisionId, "Updated Job Order Details. Could include delivery and invoice details. Job # " & lngJobOrderId, "New Job Order. Job # " & lngJobOrderId & " awaiting your attention. Updated by " & Request.Cookies("UserSettings")("Name")

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & intDivisionId & "&Msg=Job+Order+details+updated")

%>