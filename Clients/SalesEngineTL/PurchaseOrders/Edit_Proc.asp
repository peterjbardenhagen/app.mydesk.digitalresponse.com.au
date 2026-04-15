<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

lngPOid = CLng(Request("POid"))
lngRFQid = CLng(Request("RFQid"))
lngQid = CLng(Request("Qid"))
strCode = Request.Cookies("UserSettings")("Code")
lngDivisionId = CLng(Request("DivisionId"))
strProject = Trim(Replace(Trim(Request("Project")), "'", "''"))
lngContactId = Request("ContactId")
lngDivisionId = Request("DivisionId")
boolGST = CBool(Request("GST"))
lngPOStatusId = Request("POStatusId")
lngPOPaymentTypeId = Request("POPaymentTypeId")
strTerms = Replace(Trim(Request("Terms")), "'", "''")
strDateRequired = Request("DateRequired")
lngDeliverToLocationId = Request("DeliverToLocationId")
strDeliverToLocation = Trim(Replace(Request("DeliverToLocation"),"'","''"))
strIntroText = Trim(Replace(Trim(Request("IntroText")), "'", "''"))
strInternalNotes = Trim(Replace(Trim(Request("InternalNotes")), "'", "''"))
strPriceExTotal = Trim(Request("PriceExTotal"))
strPriceGSTTotal = Trim(Request("PriceGSTTotal"))
strPriceIncTotal = Trim(Request("PriceIncTotal"))

intItemLinesVal = CInt(Request.Form("ItemLinesVal"))

If Not boolGST Then
	strPriceGSTTotal = "0"
	strPriceIncTotal = strPriceExTotal
End If

If lngContactId <> "" Then UpdateFor lngPOid, lngContactId

' Delete approvals
sql = "Delete From PurchaseOrderApproval Where POid = " & lngPOid
dbConn.Execute(sql)

Sub UpdateFor(lngPOid, lngContactId)
	sql = "Update PurchaseOrders Set PODate = '" & ServerToEST(Now()) & "', Project = '" & strProject & "', ContactId = " & lngContactId & ", POStatusId = " & lngPOStatusId & ", GST = " & boolGST & ", POPaymentTypeId = " & lngPOPaymentTypeId & ", Terms = '" & strTerms & "', DateRequired = '" & strDateRequired & "', DeliverToLocationId = " & lngDeliverToLocationId & ", DeliverToLocation = '" & strDeliverToLocation & "', IntroText = '" & strIntroText & "', InternalNotes = '" & strInternalNotes & "', PriceExTotal = " & strPriceExTotal & ", PriceGSTTotal = " & strPriceGSTTotal & ", PriceIncTotal = " & strPriceIncTotal & ", RFQid = " & lngRFQid & ", Qid = " & lngQid & " Where POid = " & lngPOid
	dbConn.Execute(sql)

	sql = "Delete * From PurchaseOrderContents Where POid = " & lngPOid
	dbConn.Execute(sql)

	' Items
	For i = 2 To intItemLinesVal
		If IsNumeric(Request.Form("Quantity" & i)) And Trim(Request.Form("Item" & i)) <> "" Then
			lngQuantity = Request.Form("Quantity" & i)
			strItem = Replace(Request.Form("Item" & i),"'","''")
			strPriceEx = Request.Form("PriceEx" & i)
			strPriceExSubTotal = Request.Form("PriceExSubTotal" & i)
			lngPOProductTypeId = Request.Form("POProductTypeId" & i)
			intPartCodeId = Request.Form("PartCodeId" & i)

			' Check if Cap Ex. or not
			sql = "Select CapEx From PurchaseOrderProductTypes Where POProductTypeId = " & lngPOProductTypeId
			Set rsCapEx = dbConn.Execute(sql)
			If rsCapEx("CapEx") Then boolCapEx = True

			sql = "Insert Into PurchaseOrderContents (POid, PartCodeId, Quantity, Description, PriceEx, PriceExSubTotal, POProductTypeId) Values (" & lngPOid & ", " & intPartCodeId & ", " & lngQuantity & ", '" & strItem & "', " & strPriceEx & ", " & strPriceExSubTotal & ", " & lngPOProductTypeId & ")"
			dbConn.Execute(sql)
		End If
	Next
	If boolCapEx Then
		sql = "Update PurchaseOrders Set HasCapEx = true Where POid = " & lngPOid
		dbConn.Execute(sql)
	End If
	' Audit trail
	sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Updated', '" & ServerToEST(Now()) & "')"
	dbConn.Execute(sql)

	If GetPOLastLineApprover(lngPOid, boolCapEx) = "Already approved"  Then
		sql = "Insert Into PurchaseOrderApproval (POid, Code) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "')"
		dbConn.Execute(sql)

		sql = "Update PurchaseOrders Set POStatusId = 3 Where POid = " & lngPOid
		dbConn.Execute(sql)

		' Audit trail
		sql = "Insert Into PurchaseOrderAudit (POid, Code, Action, DateEntered) Values (" & lngPOid & ", '" & Request.Cookies("UserSettings")("Code") & "', 'Approved', '" & ServerToEST(Now()) & "')"
		dbConn.Execute(sql)
	Else
		If lngPOStatusId = 2 Then
			' Approval Process
			strBodyText = "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Approved by " & Request.Cookies("UserSettings")("Name") & ". The next approver in the approval process is " & GetPONextLineApprover(lngPOid, boolCapEx) & "." & PurchaseOrderDetails_ForEmail(lngPOid)
			SendMail Request.Cookies("UserSettings")("Email"), GetPONextLineApprover_Email(lngPOid, boolCapEx), "MyDesk " & Request.Cookies("ClientSettings")("PortalCompany") & " Alert : Purchase Order #" & lngPOid & " : Waiting for your approval. Just approved by " & Request.Cookies("UserSettings")("Name"), strBodyText
		End If
	End If
End Sub

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Purchase+Order+updated&DivisionId=" & lngDivisionId)

%>