<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("RFQ") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

strCode = Request.Cookies("UserSettings")("Code")
lngContactId1 = Request("ContactId1")
lngContactId2 = Request("ContactId2")
lngContactId3 = Request("ContactId3")
lngContactId4 = Request("ContactId4")
lngContactId5 = Request("ContactId5")
lngDeliverToLocationId = Request("DeliverToLocationId")
strDeliverToLocation = Trim(Replace(Request("DeliverToLocation"),"'","''"))
lngDivisionId = Request("DivisionId")
lngRFQStatusId = 22
strTerms = Trim(Replace(Request("Terms"), "'", "''"))
strDateRequired = Trim(Request("DateRequired"))
strIntroText = Trim(Replace(Trim(Request("IntroText")), "'", "''"))

intItemLinesVal = CInt(Request.Form("ItemLinesVal"))

Dim RFQGroupId

Set rsQ = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT MAX(RFQGroupId)+1 AS MaxRFQGroupId FROM RFQ"
Set rsQ = dbConn.Execute(sql)

If IsNull(rsQ("MaxRFQGroupId")) Then
	lngRFQGroupId = 0
Else
	lngRFQGroupId = CLng(rsQ("MaxRFQGroupId"))
End If

If lngContactId1 <> "" Then InsertFor lngContactId1, lngRFQGroupId
If lngContactId2 <> "" Then InsertFor lngContactId2, lngRFQGroupId
If lngContactId3 <> "" Then InsertFor lngContactId3, lngRFQGroupId
If lngContactId4 <> "" Then InsertFor lngContactId4, lngRFQGroupId
If lngContactId5 <> "" Then InsertFor lngContactId5, lngRFQGroupId

Sub InsertFor(lngContactId, lngRFQGroupId)
	Dim rsQ
	Dim rsNew
	Dim sql
	Dim lngQuantity
	
	sql = "Insert Into RFQ (RFQGroupId, RFQDate, Code, ContactId, DivisionId, DeliverToLocationId, DeliverToLocation, DateRequired, RFQStatusId, Terms, IntroText, Password) Values (" & lngRFQGroupId & ", '" & ServerToEST(Now()) & "', '" & strCode & "', " & lngContactId & ", " & lngDivisionId & ", " & lngDeliverToLocationId & ", '" & strDeliverToLocation & "', '" & strDateRequired & "', " & lngRFQStatusId & ", '" & strTerms & "', '" & strIntroText & "', '" & GeneratePassword(5) & "')"
	dbConn.Execute(sql)

	Set rsNew = Server.CreateObject("ADODB.RecordSet")
	sql = "Select @@IDENTITY As RFQId"
	Set rsNew = dbConn.Execute(sql)

	lngRFQId = rsNew("RFQId")

	rsNew.Close
	Set rsNew = Nothing

	' Items
	For i = 2 To intItemLinesVal
		If IsNumeric(Request.Form("Quantity" & i)) And Trim(Request.Form("Item" & i)) <> "" Then
			lngQuantity = Request.Form("Quantity" & i)
			strItem = Replace(Request.Form("Item" & i),"'","''")
			sql = "Insert Into RFQContents (RFQId, Quantity, Description) Values (" & lngRFQId & ", " & lngQuantity & ", '" & strItem & "')"
			dbConn.Execute(sql)
		End If
	Next
End Sub

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & lngDivisionId & "&Msg=Request+For+Quote+added")

%>