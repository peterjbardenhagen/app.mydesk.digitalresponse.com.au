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
<%

lngQid = CLng(Request("Qid"))
strCode = Trim(Request("Code"))
strSenderCode = Trim(Request("SenderCode"))  ' NEW: Quote sender
If strSenderCode = "" Then strSenderCode = strCode  ' Default to owner if not specified
lngContactId = Request("ContactId")
intDivisionId = Request("DivisionId")
strQuoteNumber = Trim(Replace(Request("QuoteNumber"), "'", "''"))
intQuoteStatusId = CInt(Request("QuoteStatusId"))
strAttention = Trim(Replace(Request("Attention"), "'", "''"))
strReference = Trim(Replace(Request("Reference"), "'", "''"))
strTerms = Trim(Replace(Request("Terms"), "'", "''"))
strDelivery = Trim(Replace(Request("Delivery"), "'", "''"))
lngValidity = CLng(Request("Validity"))
strInternalNotes = Trim(Replace(Trim(Request("InternalNotes")), "'", "''"))
strCustomerNotes = Trim(Replace(Trim(Request("CustomerNotes")), "'", "''"))
decUnitCostTotal = Trim(Request("UnitCostTotal"))
decNettPriceTotal = Trim(Request("NettPriceTotal"))
decMargin = Trim(Request("RealMargin"))
intQuoteCOSId = CInt(Request("QuoteCOSId"))
strCode = Trim(Request("Code"))

If Len(lngContactId) = 0 Then lngContactId = 0
If Len(decUnitCostTotal) = 0 Then decUnitCostTotal = 0
If Len(decNettPriceTotal) = 0 Then decNettPriceTotal = 0
If Len(decMargin) = 0 Then decMargin = 0




Function FixQuotes(str)
	If Len(str) > 1 And Not(str = null) Then
		FixQuotes = Replace(str,"'","''")
	Else
		FixQuotes = str
	End If
End Function


' Quote Items
intItemLinesVal = CInt(Request("ItemLinesVal"))
intThirdPartyLinesVal = CInt(Request("ThirdPartyLinesVal"))

' Delete old quote contents
strSql = "Delete From QuoteContents Where Qid = " & lngQid
dbConn.Execute(strSql)

' Insert main quote details and generate a Quote Id
strSql = "Update Quotes Set QuoteDate = '" & ServerToEST(Now()) & "', Code = '" & strCode & "', SenderCode = '" & strSenderCode & "', ContactId = " & lngContactId & ", QuoteStatusId = " & intQuoteStatusId & ", Reference = '" & strReference & "', Terms = '" & strTerms & "', Delivery = '" & strDelivery & "', Validity = " & lngValidity & ", InternalNotes = '" & strInternalNotes & "', CustomerNotes = '" & strCustomerNotes & "', UnitCostTotal = " & decUnitCostTotal & ", NettPriceTotal = " & decNettPriceTotal & ", Margin = " & decMargin & ", QuoteCOSId = " & intQuoteCOSId & " Where Qid = " & lngQid
dbConn.Execute(strSql)

sql = "Delete From QuoteThirdPartyContents Where QuoteId = " & lngQid
dbConn.Execute(sql)

sql = "Delete From QuoteContents Where Qid = " & lngQid
dbConn.Execute(sql)

' Delete approvals
sql = "Delete From QuoteApproval Where Qid = " & lngQid
dbConn.Execute(sql)

' Items
For i = 2 To intItemLinesVal
    If IsNumeric(Request("Quantity" & i)) Or (IsNumeric(Request("Units" & i)) Or IsNumeric(Request("Days" & i))) Then
		If Request("Quantity" & i) > 0 Or (Request("Units" & i) > 0 And Request("Days" & i) > 0) Then
'			lngProductId = Replace(Request("ProductId" & i),"'","''")
			lngProductId = 0
			intQuantity = Replace(Request("Quantity" & i),"'","''")
			strType = Replace(Request("Type" & i),"'","''")
			intDays = Replace(Request("Days" & i),"'","''")
			intUnits = Replace(Request("Units" & i),"'","''")
			strProductCode = Replace(Request("ProductCode" & i),"'","''")
			strDescription = Replace(Request("Description" & i),"'","''")
			decUnitCost = Replace(Request("UnitCost" & i),"'","''")
			decMinNettPrice = Replace(Request("MinNettPrice" & i),"'","''")
			decNettPrice = Replace(Request("NettPrice" & i),"'","''")
			decUnitCostSubTotal = Replace(Request("UnitCostSubTotal" & i),"'","''")
			decMinExtNettPrice = Replace(Request("MinExtNettPrice" & i),"'","''")
			decExtNettPrice = Replace(Request("ExtNettPrice" & i),"'","''")
		    
			If intDays = "" Then intDays = 0
			If intUnits = "" Then intUnits = 0
		    
			If decNettPrice < decMinNettPrice Then
				boolNotApproved = True
			End If
		    
			sql = "Insert Into QuoteContents (Qid, ProductId, Quantity, Days, Units, Type, ProductCode, Description, UnitCost, MinNettPrice, NettPrice, UnitCostSubTotal, ExtNettPrice) Values (" & lngQid & ", " & lngProductId & ", " & intQuantity & ", " & intDays & ", " & intUnits & ", '" & strType & "', '" & strProductCode & "', '" & strDescription & "', " & decUnitCost & ", " & decMinNettPrice & ", " & decNettPrice & ", " & decUnitCostSubTotal & ", " & decExtNettPrice & ")"
			dbConn.Execute(sql)
		End If
    End If
Next

' Third Party Supply Items
For i = 2 To intThirdPartyLinesVal
	If Request("TP_Quantity" & i) <> "" Then
		If CInt(Request("TP_Quantity" & i)) > 0 Then
			strDescription = Replace(Request("TP_Description" & i),"'","''")
			strSupplier = Replace(Request("TP_Supplier" & i),"'","''")
			strQuoteNumber = Replace(Request("TP_QuoteNumber" & i),"'","''")
			strQuoteDate = Replace(Request("TP_QuoteDate" & i),"'","''")
			strExpiryDate = Replace(Request("TP_ExpiryDate" & i),"'","''")
			strSupplierPartNumber = Replace(Request("TP_SupplierPartNumber" & i),"'","''")
			strOurPartNumber = Replace(Request("TP_OurPartNumber" & i),"'","''")
			intQuantity = Replace(Request("TP_Quantity" & i),"'","''")
			strType = Replace(Request("TP_Type" & i),"'","''")
			decUnitCost = Replace(Request("TP_UnitCost" & i),"'","''")
			decNettPrice = Replace(Request("TP_NettPrice" & i),"'","''")
			decMargin = Replace(Request("TP_Margin" & i),"'","''")
			decExtNettPrice = Replace(Request("TP_ExtNettPrice" & i),"'","''")

			strSql = "Insert Into QuoteThirdPartyContents (QuoteId, Description, Supplier, QuoteNumber, QuoteDate, ExpiryDate, SupplierPartNumber, OurPartNumber, Quantity, Type, UnitCost, NettPrice, Margin, ExtNettPrice) Values (" & lngQid & ", '" & strDescription & "', '" & strSupplier & "', '" & strQuoteNumber & "', '" & strQuoteDate & "', '" & strExpiryDate & "', '" & strSupplierPartNumber & "', '" & strOurPartNumber & "', " & intQuantity & ", '" & strType & "', " & decUnitCost & ", " & decNettPrice & ", " & decMargin & ", " & decExtNettPrice & ")"
			dbConn.Execute(strSql)
		End If
	End If
Next

' ============================================
' BUG FIX: Server-side recalculation of quote totals
' This ensures totals are always correct regardless of client-side JavaScript
' ============================================

Dim decRecalcUnitCostTotal, decRecalcNettPriceTotal, decRecalcMargin
Dim decQCUnitCostTotal, decQCNettPriceTotal
Dim decTPUnitCostTotal, decTPNettPriceTotal

' Get totals from QuoteContents (main line items)
sql = "SELECT SUM(UnitCostSubTotal) AS UnitCostTotal, SUM(ExtNettPrice) AS NettPriceTotal FROM QuoteContents WHERE Qid = " & lngQid
Set rsCalc = dbConn.Execute(sql)
If Not (rsCalc.BOF And rsCalc.EOF) Then
    decQCUnitCostTotal = CDbl(rsCalc("UnitCostTotal") & "")
    decQCNettPriceTotal = CDbl(rsCalc("NettPriceTotal") & "")
Else
    decQCUnitCostTotal = 0
    decQCNettPriceTotal = 0
End If
rsCalc.Close
Set rsCalc = Nothing

' Get totals from QuoteThirdPartyContents (third party items)
sql = "SELECT SUM(UnitCost * Quantity) AS UnitCostTotal, SUM(ExtNettPrice) AS NettPriceTotal FROM QuoteThirdPartyContents WHERE QuoteId = " & lngQid
Set rsCalc = dbConn.Execute(sql)
If Not (rsCalc.BOF And rsCalc.EOF) Then
    decTPUnitCostTotal = CDbl(rsCalc("UnitCostTotal") & "")
    decTPNettPriceTotal = CDbl(rsCalc("NettPriceTotal") & "")
Else
    decTPUnitCostTotal = 0
    decTPNettPriceTotal = 0
End If
rsCalc.Close
Set rsCalc = Nothing

' Calculate combined totals
decRecalcUnitCostTotal = decQCUnitCostTotal + decTPUnitCostTotal
decRecalcNettPriceTotal = decQCNettPriceTotal + decTPNettPriceTotal

' Calculate margin percentage
If decRecalcNettPriceTotal > 0 Then
    decRecalcMargin = ((decRecalcNettPriceTotal - decRecalcUnitCostTotal) / decRecalcNettPriceTotal) * 100
Else
    decRecalcMargin = 0
End If

' Update Quotes table with recalculated totals
sql = "UPDATE Quotes SET UnitCostTotal = " & decRecalcUnitCostTotal & ", NettPriceTotal = " & decRecalcNettPriceTotal & ", Margin = " & decRecalcMargin & " WHERE Qid = " & lngQid
dbConn.Execute(sql)

' ============================================
' END BUG FIX
' ============================================

' Audit trail
sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Updated', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

If intQuoteStatusId = 9 Then ' Pending Approval
	If GetQuoteLastLineApprover(lngQid) = Request.Cookies("UserSettings")("Name") Or GetQuoteLastLineApprover(lngQid) = "Already approved" Then
		sql = "Update Quotes Set QuoteStatusId = 10 Where Qid = " & lngQid
		dbConn.Execute(sql)

		' Audit trail
		sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Approved', '" & ServerToEST(Now()) & "')"
		dbConn.Execute(sql)

		strSql = "Select Users.Email From Users Inner Join Quotes On Quotes.Code = Users.Code Where Qid = " & lngQid
		Set rsQ = dbConn.Execute(strSql)

		strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Approved by " & Request.Cookies("UserSettings")("Name") & ". The approval process has been completed. You may now issue the quote." & QuoteDetails_ForEmail(lngQid)
		SendMail Request.Cookies("UserSettings")("Email"), rsQ("Email"), "MyDesk Alert : Quote #" & lngQid & " : Approved by " & Request.Cookies("UserSettings")("Name"), strBodyText

		rsQ.Close
		Set rsQ = Nothing
	Else
		strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Approved by " & Request.Cookies("UserSettings")("Name") & ". The next approver in the approval process is " & GetQuoteNextLineApprover(lngQid) & "." & QuoteDetails_ForEmail(lngQid)
		SendMail Request.Cookies("UserSettings")("Email"), GetQuoteNextLineApprover_Email(lngQid), "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Quote #" & lngQid & " : Waiting for your approval. Just approved by " & Request.Cookies("UserSettings")("Name"), strBodyText
	End If
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & intDivisionId & "&Msg=Quote+updated")

%>