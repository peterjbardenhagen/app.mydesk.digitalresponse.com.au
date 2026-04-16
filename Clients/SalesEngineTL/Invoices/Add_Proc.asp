<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

lngDivisionId = CLng(Request("DivisionId"))
lngQid = CLng(Request("Qid"))
lngInvoiceStatusId = 1 ' Draft
strCode = Request("Code")
lngCompanyId = CLng(Request("CompanyId"))
strCustomerPO = Trim(Request("CustomerPO"))

'strDelAddress1 = Trim(Replace(Request("DelAddress1"),"'","''"))
'strDelAddress2 = Trim(Replace(Request("DelAddress2"),"'","''"))
'strDelSuburb = Trim(Replace(Request("DelSuburb"),"'","''"))
'strDelState = Trim(Replace(Request("DelState"),"'","''"))
'intDelStateId = Trim(Request("DelStateId"))
'strDelPostCode = Request("DelPostCode")
'strDelCountry = Trim(Replace(Request("DelCountry"),"'","''"))
'strInvAddress1 = Trim(Replace(Request("InvAddress1"),"'","''"))
'strInvAddress2 = Trim(Replace(Request("InvAddress2"),"'","''"))
'strInvSuburb = Trim(Replace(Request("InvSuburb"),"'","''"))
'strInvState = Trim(Replace(Request("InvState"),"'","''"))
'intInvStateId = Trim(Request("InvStateId"))
'strInvPostCode = Request("InvPostCode")
'strInvCountry = Trim(Replace(Request("InvCountry"),"'","''"))

strDelAddress = Trim(Replace(Request("DelAddress"),"'","''"))
strInvAddress = Trim(Replace(Request("InvAddress"),"'","''"))

strDelCompany = Trim(Replace(Request("DelCompany"),"'","''"))
strInvCompany = Trim(Replace(Request("InvCompany"),"'","''"))
strCCompany = strInvCompany

strAttention = Trim(Replace(Request("Attention"),"'","''"))
strAccount = Trim(Replace(Request("Account"),"'","''"))
strTerms = Trim(Replace(Request("Terms"),"'","''"))
strCustomerNotes = Trim(Replace(Request("CustomerNotes"),"'","''"))
strInternalNotes = Trim(Replace(Request("InternalNotes"),"'","''"))
dblTotalValue = CDbl(Request("NettPriceTotal"))
dblTotalGST = CDbl(Request("GSTTotal"))
dblNettPriceTotalInc = CDbl(Request("NettPriceTotalInc"))
intX = CInt(Request("X")) ' Number of rows


Response.Write "intDelStateId" & intDelStateId & "<br>"
Response.Write "intInvStateId" & intInvStateId

' Insert main invoice details and generate an invoice
strSql = "Insert Into Invoices (Code, InvoiceStatusId, InvoiceDate, DivisionId, Qid, CompanyId, CCompany, CustomerPO, DelCompany, DelAddress1, DelAddress2, DelSuburb, DelState, DelStateId, DelPostCode, DelCountry, InvCompany, InvAddress1, InvAddress2, InvSuburb, InvState, InvStateId, InvPostCode, InvCountry, Attention, Account, Terms, CustomerNotes, InternalNotes, NettPriceTotal, GSTTotal, DelAddress, InvAddress) " &_
			" Values ('" & strCode & "', " & lngInvoiceStatusId & ", '" & ServerToEST(Now()) & "', " & lngDivisionId & ", " & lngQid & ", " & lngCompanyId & ", '" & strCCompany & "', '" & strCustomerPO & "', '" & strDelCompany & "', '', '', '', '', 0, '', '', '" & strInvCompany & "', '', '', '', '', 0, '', '', '" & strAttention & "', '" & strAccount & "', '" & strTerms & "', '" & strCustomerNotes & "', '" & strInternalNotes & "', " & dblTotalValue & ", " & dblTotalGST & ", '" & strDelAddress & "', '" & strInvAddress & "')"
dbConn.Execute(strSql)

Set rsNew = Server.CreateObject("ADODB.RecordSet")
strSql = "Select @@IDENTITY As InvoiceId"
Set rsNew = dbConn.Execute(strSql)

lngInvoiceId = rsNew("InvoiceId")

rsNew.Close
Set rsNew = Nothing

' Invoice Items
For i = 0 To intX
    If IsNumeric(Request.Form("Quantity" & i)) Then
		If Request.Form("Quantity" & i) > 0 Then
'			If Request.Form("Type" & i) = "Standard" Then
			lngQuoteItemId = Replace(Request.Form("QuoteItemId" & i),"'","''")
			intQuantity = Replace(Request.Form("Quantity" & i),"'","''")
			intOriginalQuantity = Replace(Request.Form("OriginalQuantity" & i),"'","''")
			intDays = Replace(Request.Form("Days" & i),"'","''")
			intUnits = Replace(Request.Form("Units" & i),"'","''")
			strType = Replace(Request.Form("Type" & i),"'","''")
			strProductCode = Replace(Request.Form("ProductCode" & i),"'","''")
			strDescription = Replace(Request.Form("Description" & i),"'","''")
			decNettPrice = Replace(Request.Form("NettPrice" & i),"'","''")
			decSubTotal = Replace(Request.Form("SubTotal" & i),"'","''")
			
			If Len(strType) > 0 Then
				strDescription = "Type: " & strType & " " & strDescription
			End If

			If intDays = "" Then intDays = 0
			If intUnits = "" Then intUnits = 0

			intOrdered = intQuantity
			intBackOrder = 0
			intUnits = intQuantity

			' update quotecontents
			sql = "Update quotecontents set days = days + " + Request.Form("Quantity" & i) + " where quoteitemid = " & lngQuoteItemID
			dbConn.Execute(sql)
		
			sql = "Insert Into InvoiceContents (InvoiceId, Quantity, BackOrder, Ordered, Units, Days, ProductCode, Description, NettPrice, ExtNettPrice) " & _
					"Values (" & lngInvoiceId & ", " & intQuantity & ", " & intBackOrder & ", " & intOrdered & ", " & intUnits & ", " & intDays & ", '" & strProductCode & "', '" & strDescription & "', " & decNettPrice & ", " & decSubTotal & ")"
			dbConn.Execute(sql)
		End If
    End If
Next

' Audit trail
sql = "Insert Into InvoiceAudit (InvoiceId, Code, Action, DateEntered) Values (" & lngInvoiceId & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Invoice created', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & lngDivisionId & "&Msg=Invoice+added")

%>