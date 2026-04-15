<% 

'Response.AddHeader "Pragma", "No-Store"
'Response.AddHeader "cache-control", "no-store, private, must-revalidate"
'Response.Expires = -1
'Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
'Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Function GetAccessCodesList(strCode, lngUserTypeId)
	sql = "Select DivisionId From Users Where Code = '" & strCode & "'"
	Set rsGetDiv = dbConn.Execute(sql)

	Dim strAccessCodesList
	If Request.Cookies("ClientSettings")("Prefix") = "TT" And CInt(rsGetDiv("DivisionId")) = 1 Then
		strAccessCodesList = GetForSignalsDivision(strCode, lngUserTypeId)
	ElseIf (lngUserTypeId = 4 Or lngUserTypeId = 5 Or lngUserTypeId = 6) Or (Request.Cookies("ClientSettings")("Prefix") <> "TT") Then ' Override - Allow users access to all records
		Dim rsUser
		Dim sql
		Dim strCodes
		Set rsUser = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From Users Where Deleted = 0 AND Active = 1 AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager") & ") Order By Code"
		Set rsUser = dbConn.Execute(sql)
		If Not(rsUser.BOF And rsUser.EOF) Then
			Do Until rsUser.EOF
				strCodes = ""
				strCodes = "'" & rsUser("Code") & "'," & strCodes
				strAccessCodesList = strAccessCodesList & "," & strCodes
				rsUser.MoveNext
			Loop
		End If
		rsUser.Close
		Set rsUser = Nothing
		strAccessCodesList = strAccessCodesList & GetKids(strCode)
	Else
		strAccessCodesList = GetKids(strCode)
	End If
	strAccessCodesList = Replace(Replace(strAccessCodesList, ",,", ", "), " ", "")
	If Left(strAccessCodesList, 1) = "," Then strAccessCodesList = Right(strAccessCodesList, Len(strAccessCodesList)-1)
	If Right(strAccessCodesList, 1) = "," Then strAccessCodesList = Left(strAccessCodesList, Len(strAccessCodesList)-1)
	If strAccessCodesList = "" Then strAccessCodesList = "'No Codes'"
	GetAccessCodesList = "'" & strCode & "', " & strAccessCodesList
End Function

Function GetForSignalsDivision(strCode, lngUserTypeId)
	Dim rsUser
	Dim sql
	Dim strCodes
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	If lngUserTypeId = 5 Or lngUserTypeId = 6 Then
		sql = "Select * From Users"
	Else
		sql = "Select * From Users Where Deleted = 0 AND Active = 1 AND DivisionId = 1 Or (LineManagerCode = '" & strCode & "' And Code <> '" & strCode & "') Or UserId In (Select UserId From UsersAccess Where DivisionId = 1 And MemberOf = True)"
	End If
	Set rsUser = dbConn.Execute(sql)
	If Not(rsUser.BOF And rsUser.EOF) Then
		Do Until rsUser.EOF
			strAccessCodesList = strAccessCodesList & "," & "'" & rsUser("Code") & "',"
			rsUser.MoveNext
		Loop
	End If
	rsUser.Close
	Set rsUser = Nothing
	GetForSignalsDivision = strAccessCodesList
End Function



Dim strMsg

strMsg = Trim(Request("Msg"))

' Set cookies...

Response.Cookies("DivisionId") = Session("DivisionId")
Response.Cookies("DivisionId").Expires = Date() + 7

Response.Cookies("LoggedIn") = True
Response.Cookies("LoggedIn").Expires = Date() + 7

Response.Cookies("UserSettings")("Admin") = Session("Admin") ' For PL
Response.Cookies("UserSettings")("Code") = Session("Code")
Response.Cookies("UserSettings")("UserTypeId") = Session("UserTypeId")
Response.Cookies("UserSettings")("LocationId") = Session("LocationId")
Response.Cookies("UserSettings")("Name") = Session("Name")
Response.Cookies("UserSettings")("Initials") = Session("Initials")
Response.Cookies("UserSettings")("Email") = Session("Email")
If Session("Manager") Then
	Response.Cookies("UserSettings")("Manager") = True 
Else
	Response.Cookies("UserSettings")("Manager") = False
End If
Response.Cookies("UserSettings")("Division") = Session("Division")
Response.Cookies("UserSettings")("ExpenseTypeGroupId") = Session("ExpenseTypeGroupId")
Response.Cookies("UserSettings")("LineManagerCode") = Session("LineManagerCode")
Response.Cookies("UserSettings")("LineManagerName") = Session("LineManagerName")
Response.Cookies("UserSettings")("LineManagerEmail") = Session("LineManagerEmail")
Response.Cookies("UserSettings")("HoursPerDay") = Session("HoursPerDay")
Response.Cookies("UserSettings")("HoursPerWeek") = Session("HoursPerWeek")
Response.Cookies("UserSettings").Expires = Date() + 7

Response.Cookies("DivisionIdsAccess")("Manager") = Session("DivisionIdsManager")
Response.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager") = Session("ArrDivisionIdsManager")
Response.Cookies("DivisionIdsAccess")("Visible") = Session("DivisionIdsVisible")
Response.Cookies("DivisionIdsAccess")("Quotes") = Session("DivisionIdsQuotes")
Response.Cookies("DivisionIdsAccess")("RFQ") = Session("DivisionIdsRFQ")
Response.Cookies("DivisionIdsAccess")("PurchaseOrders") = Session("DivisionIdsPurchaseOrders")
Response.Cookies("DivisionIdsAccess")("Payroll") = Session("DivisionIdsPayroll")
Response.Cookies("DivisionIdsAccess")("ArrDivisionIdsPayroll") = Session("ArrDivisionIdsPayroll")
Response.Cookies("DivisionIdsAccess").Expires = Date() + 7

Response.Cookies("SMTPServer") = "localhost"
Response.Cookies("SMTPServer").Expires = Date() + 7

'Features
Response.Cookies("ClientSettings")("HasQuoteCOS") = "false"
Response.Cookies("ClientSettings")("HasInternalNotes") = "false"

Response.Cookies("ClientSettings")("Stylesheet") = Session("Stylesheet")
Response.Cookies("ClientSettings")("StylesheetHome") = Session("StylesheetHome")
Response.Cookies("ClientSettings")("MainBgColor") = Session("MainBgColor")
Response.Cookies("ClientSettings")("BgColor2") = Session("BgColor2")
Response.Cookies("ClientSettings")("BgColor3") = Session("BgColor3")
Response.Cookies("ClientSettings")("Prefix") = Session("Prefix")
Response.Cookies("ClientSettings")("PortalCompany") = Session("PortalCompany")
Response.Cookies("ClientSettings")("State") = Session("State")
Response.Cookies("ClientSettings")("HomeColor1") = Session("HomeColor1")
Response.Cookies("ClientSettings")("WorkingDir") = Session("WorkingDir")
Response.Cookies("SMTPServer").Expires = Date() + 7

If Session("Prefix") <> "PL" Then
'	Response.Cookies("AccessCodesList") = GetAccessCodesList(Session("Code"), Session("UserTypeId"))
'	Response.Cookies("AccessCodesList").Expires = Date() + 1	
'	Response.Write(GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")))
'response.end
End If

' Just in case...
' Due to timeout complaints

For Each Item In Request.Cookies
	Response.Cookies(Item).Expires = Date() + 7
'	Response.Cookies(Item) = ""
'	Response.Write(Request.Cookies(Item).Name & "<BR>")
'	Response.Write(Request.Cookies(Item) & "<BR>")
'	Response.Write(Request.Cookies(Item).Expires & "<BR>")
Next

dim x,y
for each x in Request.Cookies
  'Response.Write("<p>")
  if Request.Cookies(x).HasKeys then
    for each y in Request.Cookies(x)
      'Response.Write(x & ":" & y & "=" & Request.Cookies(x)(y))
      'Response.Write("<br />")
    next
  else
    'Response.Write(x & "=" & Request.Cookies(x) & "<br />")
  end if
  'Response.Write "</p>"
next

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<script>parent.window.location.href='<%= Session("WorkingDir") %>?Msg=<%= Msg %>';</script>

