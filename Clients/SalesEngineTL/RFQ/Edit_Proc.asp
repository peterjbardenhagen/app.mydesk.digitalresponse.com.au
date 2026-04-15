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

lngRFQid = CLng(Request("RFQid"))
strCode = Request.Cookies("UserSettings")("Code")
lngContactId = Request("ContactId")
lngDeliverToLocationId = Request("DeliverToLocationId")
lngDivisionId = Request("DivisionId")
lngRFQStatusId = 22
strTerms = Trim(Replace(Request("Terms"), "'", "''"))
strDateRequired = Trim(Request("DateRequired"))
strIntroText = Trim(Replace(Trim(Request("IntroText")), "'", "''"))
intUpdateGroup = CInt(Request("UpdateGroup"))
lngRFQGroupId = CLng(Request("RFQGroupId"))
intItemLinesVal = CInt(Request.Form("ItemLinesVal"))

If intUpdateGroup = 1 Then
	Set rsRFQ = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From RFQ Where RFQGroupId = " & lngRFQGroupId
	Set rsRFQ = dbConn.Execute(sql)
	Do Until rsRFQ.EOF
		UpdateRFQ rsRFQ("RFQid"), rsRFQ("ContactId")
		rsRFQ.MoveNext
	Loop
	rsRFQ.Close
	Set rsRFQ = Nothing
Else
	UpdateRFQ lngRFQid, lngContactId
End If

Sub UpdateRFQ(lngRFQid, lngContactId)
	Dim sql

	sql = "Update RFQ Set ContactId = " & lngContactId & ", DeliverToLocationId = " & lngDeliverToLocationId & ", DateRequired = '" & strDateRequired & "', RFQStatusId = " & lngRFQStatusId & ", Terms = '" & strTerms & "', IntroText = '" & strIntroText & "', TotalEx = 0, TotalInc = 0 Where RFQid = " & lngRFQid
	dbConn.Execute(sql)

	sql = "Delete From RFQContents Where RFQid = " & lngRFQid
	dbConn.Execute(sql)

	' Items
	For i = 1 To intItemLinesVal
		If IsNumeric(Request.Form("Quantity" & i)) And Trim(Request.Form("Item" & i)) <> "" Then
			lngQuantity = Request.Form("Quantity" & i)
			strItem = Request.Form("Item" & i)
			sql = "Insert Into RFQContents (RFQId, Quantity, Description) Values (" & lngRFQId & ", " & lngQuantity & ", '" & strItem & "')"
			dbConn.Execute(sql)
		End If
	Next
End Sub

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & lngDivisionId & "&Msg=Request+For+Quote+updated")

%>