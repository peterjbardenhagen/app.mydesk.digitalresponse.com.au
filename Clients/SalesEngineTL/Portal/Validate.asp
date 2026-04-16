<% 
' Techlight MyDesk - Login Validation
' ===============================================================================
' Validates user credentials and sets up session
' MUST include Var.asp first to ensure WorkingDir is always set
' ===============================================================================

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<%

' HARDEN: Always ensure WorkingDir is set - fallback to hardcoded value if needed
If Session("WorkingDir") = "" Or IsNull(Session("WorkingDir")) Then
    Session("WorkingDir") = "/Clients/SalesEngineTL"
End If

' Also ensure the WorkingDir cookie is set
Response.Cookies("ClientSettings")("WorkingDir") = "/Clients/SalesEngineTL"
Response.Cookies("ClientSettings").Expires = Date() + 1000

' Set other required client settings if not already set
If Request.Cookies("ClientSettings")("Prefix") = "" Then
    Response.Cookies("ClientSettings")("Prefix") = "TL"
    Response.Cookies("ClientSettings")("State") = "AUS"
    Response.Cookies("ClientSettings").Expires = Date() + 1000
End If

Dim strUsername
Dim strPassword
Dim sql
Dim intDaysSinceLastPasswordChange
Dim strAccessCodesList
Dim strDivisionIdsVisible, strDivisionIdsManager, strDivisionIdsQuotes
Dim strDivisionIdsRFQ, strDivisionIdsPurchaseOrders, strDivisionIdsPayroll

strUsername = Trim(Replace(Request("Username"),"'","''"))
strPassword = Trim(Replace(Request("Password"),"'","''"))

' Initialize division ID strings
strDivisionIdsVisible = ""
strDivisionIdsManager = ""
strDivisionIdsQuotes = ""
strDivisionIdsRFQ = ""
strDivisionIdsPurchaseOrders = ""
strDivisionIdsPayroll = ""

Set rsCheck = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Users.*, LineManagers.Name AS LineManagerName, LineManagers.Email AS LineManagerEmail, Divisions.Division, Locations.ExpenseTypeGroupId FROM Locations INNER JOIN (Divisions INNER JOIN (Users LEFT JOIN Users AS LineManagers ON Users.LineManagerCode = LineManagers.Code) ON Divisions.DivisionId = Users.DivisionId) ON (Locations.LocationId = Users.LocationId) Where Users.[Name] = '" & strUsername & "' And Users.[PW] = '" & strPassword & "' And Users.Active = -1"
Set rsCheck = dbConn.Execute(sql)

If Not(rsCheck.BOF And rsCheck.EOF) Then
	Session("LoggedIn") = True
	Session("Code") = Trim(rsCheck("Code")) & ""
	Session("LineManagerCode") = Trim(rsCheck("LineManagerCode")) & "" & ""
	Session("Name") = Trim(rsCheck("Name")) & ""
	Session("Email") = Trim(rsCheck("Email")) & ""
	Session("Initials") = Trim(rsCheck("Initials")) & ""
	
	' Safe conversion with error handling
	On Error Resume Next
	Session("DivisionId") = CLng(rsCheck("DivisionId"))
	If Err.Number <> 0 Then Session("DivisionId") = 0
	On Error GoTo 0
	
	Session("Division") = Trim(rsCheck("Division")) & ""
	
	On Error Resume Next
	Session("UserTypeId") = CLng(rsCheck("UserTypeId"))
	If Err.Number <> 0 Then Session("UserTypeId") = 0
	On Error GoTo 0
	
	Session("LineManagerName") = rsCheck("LineManagerName") & ""
	Session("LineManagerEmail") = rsCheck("LineManagerEmail") & ""
	
	On Error Resume Next
	Session("LocationId") = CLng(rsCheck("LocationId"))
	If Err.Number <> 0 Then Session("LocationId") = 0
	On Error GoTo 0
	
	On Error Resume Next
	Session("ExpenseTypeGroupId") = CLng(rsCheck("ExpenseTypeGroupId"))
	If Err.Number <> 0 Then Session("ExpenseTypeGroupId") = 0
	On Error GoTo 0
	
	' Safe calculation for hours
	On Error Resume Next
	Session("HoursPerDay") = rsCheck("HoursPerDay")
	Session("HoursPerWeek") = rsCheck("HoursPerDay") * rsCheck("DaysPerWeek")
	If Err.Number <> 0 Then
		Session("HoursPerDay") = 8
		Session("HoursPerWeek") = 40
	End If
	On Error GoTo 0
	
	Set rsAccess = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From UsersAccess Where UserId = " & rsCheck("UserId")
	Set rsAccess = dbConn.Execute(sql)

'	i = 0
'	For Each Item In Split(strAccessCodesList, ",")
'		Session("strAccessCodesList")(CStr(i)) = Item
'		i = i + 1
'	Next

	Do Until rsAccess.EOF
		If rsAccess("Visible") Then strDivisionIdsVisible = strDivisionIdsVisible & rsAccess("DivisionId") & ", "
		If rsAccess("Manager") Then
			strDivisionIdsManager = strDivisionIdsManager & rsAccess("DivisionId") & ", "
			Session("Manager") = True
		End If
		If rsAccess("Quotes") Then
			strDivisionIdsQuotes = strDivisionIdsQuotes & rsAccess("DivisionId") & ", "
			Session("Quotes") = True
		End If
		If rsAccess("RFQ") Then
			strDivisionIdsRFQ = strDivisionIdsRFQ & rsAccess("DivisionId") & ", "
			Session("RFQ") = True
		End If
		If rsAccess("PurchaseOrders") Then
			strDivisionIdsPurchaseOrders = strDivisionIdsPurchaseOrders & rsAccess("DivisionId") & ", "
			Session("PurchaseOrders") = True
		End If
		If rsAccess("Payroll") Then
			strDivisionIdsPayroll = strDivisionIdsPayroll & rsAccess("DivisionId") & ", "
			Session("Payroll") = True
		End If
		rsAccess.MoveNext
	Loop
	
	rsAccess.Close
	Set rsAccess = Nothing

	If Right(strDivisionIdsVisible, 2) = ", " Then strDivisionIdsVisible = Left(strDivisionIdsVisible, Len(strDivisionIdsVisible)-2)
	If Right(strDivisionIdsManager, 2) = ", " Then strDivisionIdsManager = Left(strDivisionIdsManager, Len(strDivisionIdsManager)-2)
	If Right(strDivisionIdsQuotes, 2) = ", " Then strDivisionIdsQuotes = Left(strDivisionIdsQuotes, Len(strDivisionIdsQuotes)-2)
	If Right(strDivisionIdsRFQ, 2) = ", " Then strDivisionIdsRFQ = Left(strDivisionIdsRFQ, Len(strDivisionIdsRFQ)-2)
	If Right(strDivisionIdsPurchaseOrders, 2) = ", " Then strDivisionIdsPurchaseOrders = Left(strDivisionIdsPurchaseOrders, Len(strDivisionIdsPurchaseOrders)-2)
	If Right(strDivisionIdsPayroll, 2) = ", " Then strDivisionIdsPayroll = Left(strDivisionIdsPayroll, Len(strDivisionIdsPayroll)-2)

	If strDivisionIdsVisible = "" Then strDivisionIdsVisible = 0
	If strDivisionIdsManager = "" Then strDivisionIdsManager = 0
	If strDivisionIdsQuotes = "" Then strDivisionIdsQuotes = 0
	If strDivisionIdsRFQ = "" Then strDivisionIdsRFQ = 0
	If strDivisionIdsPurchaseOrders = "" Then strDivisionIdsPurchaseOrders = 0
	If strDivisionIdsPayroll = "" Then strDivisionIdsPayroll = 0

	Session("DivisionIdsVisible") = strDivisionIdsVisible
	Session("DivisionIdsManager") = strDivisionIdsManager
	Session("ArrDivisionIdsManager") = strDivisionIdsManager
	Session("DivisionIdsQuotes") = strDivisionIdsQuotes
	Session("DivisionIdsRFQ") = strDivisionIdsRFQ
	Session("DivisionIdsPurchaseOrders") = strDivisionIdsPurchaseOrders
	Session("DivisionIdsPayroll") = strDivisionIdsPayroll
	Session("ArrDivisionIdsPayroll") = strDivisionIdsPayroll

	Dim redirectPath
	redirectPath = "/SetCookies.asp?WorkingDir=" & Session("WorkingDir")
	Response.Redirect redirectPath
	Response.End
Else
	Session("LoggedIn") = False
	Session("Code") = ""
	Session("Name") = ""
	Session("Email") = ""
	Session("Initials") = ""
	Session("DivisionId") = 0
	Session("Division") = ""
	Session("HoursPerDay") = 0
	Session("HoursPerWeek") = 0
	Session("LineManagerName") = ""
	Session("LineManagerEmail") = ""
	Session("LocationId") = 0
	Session("ExpenseTypeGroupId") = 0

	rsCheck.Close
	Set rsCheck = Nothing	

	Response.Redirect "/?Msg=Login+failed,+incorrect+Username+and/or+Password.+Please+try+again."
	Response.End
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->