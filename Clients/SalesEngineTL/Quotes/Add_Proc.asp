<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

strCode = Request.Cookies("UserSettings")("Code")
lngContactId = Request("ContactId")
intDivisionId = Request("DivisionId")
strQuoteNumber = Trim(Replace(Request("QuoteNumber"), "'", "''"))
intQuoteStatusId = 1
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

If Len(lngContactId) = 0 Then lngContactId = 0
If Len(decUnitCostTotal) = 0 Then decUnitCostTotal = 0
If Len(decNettPriceTotal) = 0 Then decNettPriceTotal = 0
If Len(decMargin) = 0 Then decMargin = 0

intItemLinesVal = CInt(Request("ItemLinesVal"))
intThirdPartyLinesVal = CInt(Request("ThirdPartyLinesVal"))

' Insert main quote details and generate a Quote Id
strSql = "Insert Into Quotes (QuoteDate, Code, ContactId, DivisionId, QuoteNumber, QuoteStatusId, Reference, Terms, Delivery, Validity, InternalNotes, CustomerNotes, UnitCostTotal, NettPriceTotal, Margin, QuoteCOSId) Values ('" & ServerToEST(Now()) & "', '" & strCode & "', " & lngContactId & ", " & intDivisionId & ", '" & strQuoteNumber & "', " & intQuoteStatusId & ", '" & strReference & "', '" & strTerms & "', '" & strDelivery & "', " & lngValidity & ", '" & strInternalNotes & "', '" & strCustomerNotes & "', " & decUnitCostTotal & ", " & decNettPriceTotal & ", " & decMargin & ", '" & intQuoteCOSId & "')"
dbConn.Execute(strSql)

Set rsNew = Server.CreateObject("ADODB.RecordSet")
strSql = "Select @@IDENTITY As Qid"
Set rsNew = dbConn.Execute(strSql)

lngQid = rsNew("Qid")

rsNew.Close
Set rsNew = Nothing

' Items
For i = 2 To intItemLinesVal	
    If IsNumeric(Request("Quantity" & i)) Or (IsNumeric(Request("Units" & i)) Or IsNumeric(Request("Days" & i))) Then
		If Request("Quantity" & i) > 0 Or (Request("Units" & i) > 0 And Request("Days" & i) > 0) Then
			lngProductId = Replace(Request("ProductId" & i),"'","''")
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
	        
			sql = "Insert Into QuoteContents (QID, ProductId, Quantity, Type, Days, Units, ProductCode, Description, UnitCost, MinNettPrice, NettPrice, UnitCostSubTotal, ExtNettPrice) Values (" & lngQid & ", " & lngProductId & ", " & intQuantity & ", '" & strType & "', " & intDays & ", " & intUnits & ", '" & strProductCode & "', '" & strDescription & "', " & decUnitCost & ", " & decMinNettPrice & ", " & decNettPrice & ", " & decUnitCostSubTotal & ", " & decExtNettPrice & ")"
			dbConn.Execute(sql)
			
		End If
    End If
Next

' Third Party Supply Items
For i = 2 To intThirdPartyLinesVal
	If Request("TP_Quantity" & i) <> "" Then
		If Request("TP_Quantity" & i) > 0 Then
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

' Audit trail
sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Created', '" & ServerToEST(Now()) & "')"
dbConn.Execute(sql)

' Set approved if can approve own quotes
Set rsQu = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From Quotes Where Qid = " & lngQid
Set rsQu = dbConn.Execute(strSql)

If (rsQu("QuoteStatusId") = 1 Or rsQu("QuoteStatusId") = 9) And GetQuoteLineApprover_Check(lngQid,Request.Cookies("UserSettings")("Code")) Or CheckForLine(rsQu("Code"),Request.Cookies("UserSettings")("Code"), lngQid, True, False) Then
	sql = "Update Quotes Set QuoteStatusId = 2 Where Qid = " & lngQid
	dbConn.Execute(sql)
	
	' Audit trail
	sql = "Insert Into QuoteAudit (Qid, Code, Action, DateEntered) Values (" & lngQid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Auto-Approved', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & intDivisionId & "&Msg=Quote+added")

%>