<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

lngInvoiceId = CLng(Request("InvoiceId"))
lngDivisionId = CLng(Request("DivisionId"))
lngQid = CLng(Request("Qid"))
lngInvoiceStatusId = 1 ' Draft
strCode = Request("Code")
lngCompanyId = CLng(Request("CompanyId"))
strCCompany = Trim(Request("CCompany"))
strCustomerPO = Trim(Request("CustomerPO"))
strDelCompany = Trim(Replace(Request("DelCompany"),"'","''"))
strDelAddress1 = Trim(Replace(Request("DelAddress1"),"'","''"))
strDelAddress2 = Trim(Replace(Request("DelAddress2"),"'","''"))
strDelSuburb = Trim(Replace(Request("DelSuburb"),"'","''"))
strDelState = Trim(Replace(Request("DelState"),"'","''"))
intDelStateId = CLng(Request("DelStateId"))
strDelPostCode = Request("DelPostCode")
strDelCountry = Trim(Replace(Request("DelCountry"),"'","''"))
strInvCompany = Trim(Replace(Request("InvCompany"),"'","''"))
strInvAddress1 = Trim(Replace(Request("InvAddress1"),"'","''"))
strInvAddress2 = Trim(Replace(Request("InvAddress2"),"'","''"))
strInvSuburb = Trim(Replace(Request("InvSuburb"),"'","''"))
strInvState = Trim(Replace(Request("InvState"),"'","''"))
intInvStateId = CLng(Request("InvStateId"))
strInvPostCode = Request("InvPostCode")
strInvCountry = Trim(Replace(Request("InvCountry"),"'","''"))
strAttention = Trim(Replace(Request("Attention"),"'","''"))
strAccount = Trim(Replace(Request("Account"),"'","''"))
strTerms = Trim(Replace(Request("Terms"),"'","''"))
strCustomerNotes = Trim(Replace(Request("CustomerNotes"),"'","''"))
strInternalNotes = Trim(Replace(Request("InternalNotes"),"'","''"))

strInvAddress = Trim(Replace(Request("InvAddress"),"'","''"))
strDelAddress = Trim(Replace(Request("DelAddress"),"'","''"))

If intInvStateId = "" Or Len(Trim(intInvStateId) = 0) Then ' debug
	intInvStateId = 1
End If
If intDelStateId = "" Or Len(Trim(intDelStateId) = 0) Then ' debug
	intDelStateId = 1
End If

strSql = "Update Invoices Set DelAddress = '" & strDelAddress & "', InvAddress = '" & strInvAddress & "', Code = '" & strCode & "', InvoiceStatusId = " & lngInvoiceStatusId & ", DivisionId = " & lngDivisionId & ", Qid = " & lngQid & ", CompanyId = " & lngCompanyId & ", CCompany = '" & strCCompany & "', CustomerPO = '" & strCustomerPO & "', DelCompany = '" & strDelCompany & "', DelAddress1 = '" & strDelAddress1 & "', DelAddress2 = '" & strDelAddress2 & "', DelSuburb = '" & strDelSuburb & "', DelState = '" & strDelState & "', DelStateId = " & intDelStateId & ", DelPostCode = '" & strDelPostCode & "', DelCountry = '" & strDelCountry & "', InvCompany = '" & strCCompany & "', InvAddress1 = '" & strInvAddress1 & "', InvAddress2 = '" & strInvAddress2 & "', InvSuburb = '" & strInvSuburb & "', InvState = '" & strInvState & "', InvStateId = " & intInvStateId & ", InvPostCode = '" & strInvPostCode & "', InvCountry = '" & strInvCountry & "', Attention = '" & strAttention & "', Account = '" & strAccount & "', Terms = '" & strTerms & "', CustomerNotes = '" & strCustomerNotes & "', InternalNotes = '" & strInternalNotes & "' WHERE InvoiceId = " & lngInvoiceId
dbConn.Execute(strSql)

' Audit trail
sql = "Insert Into InvoiceAudit (InvoiceId, Code, Action, DateEntered) Values (" & lngInvoiceId & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Invoice updated', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & lngDivisionId & "&Msg=Invoice+updated")

%>