<!--#include virtual="/System/Consts.asp"-->
<%

' ============================================================================
' Core Functions - Hardened for Stability
' ============================================================================
' All functions include null checks, error handling, and validation
' to ensure they work reliably without errors
' ============================================================================

' Returns the appropriate protocol (http or https) based on the hostname
' Production (techlight.digitalresponse.com.au) requires HTTPS if cForceHTTPS is True
' Local/UAT environments can use HTTP or HTTPS
Function GetProtocol()
	On Error Resume Next
	
	Dim serverName, currentProtocol
	
	' Get server name with error handling
	serverName = ""
	serverName = Request.ServerVariables("SERVER_NAME")
	If Err.Number <> 0 Or IsNull(serverName) Then serverName = ""
	On Error Resume Next
	
	If serverName = "" Then serverName = "localhost"
	serverName = LCase(serverName)
	
	' Production hostname - force HTTPS only if cForceHTTPS is enabled
	If serverName = "techlight.digitalresponse.com.au" And cForceHTTPS Then
		GetProtocol = "https://"
	Else
		' Local/UAT environments or HTTPS enforcement disabled - use current protocol
		currentProtocol = Request.ServerVariables("HTTPS")
		If Err.Number <> 0 Or IsNull(currentProtocol) Then currentProtocol = "off"
		On Error Resume Next
		
		If LCase(currentProtocol) = "on" Then
			GetProtocol = "https://"
		Else
			GetProtocol = "http://"
		End If
	End If
	
	On Error GoTo 0
End Function

' Returns the full base URL with protocol and hostname
Function GetBaseURL()
	On Error Resume Next
	
	Dim serverName
	serverName = Request.ServerVariables("SERVER_NAME")
	If Err.Number <> 0 Or IsNull(serverName) Or serverName = "" Then serverName = "localhost"
	On Error Resume Next
	
	GetBaseURL = GetProtocol() & serverName
	
	On Error GoTo 0
End Function

' Returns true if running on production
Function IsProduction()
	On Error Resume Next
	
	Dim serverName
	serverName = Request.ServerVariables("SERVER_NAME")
	If Err.Number <> 0 Or IsNull(serverName) Then serverName = ""
	On Error Resume Next
	
	serverName = LCase(serverName)
	IsProduction = (serverName = "techlight.digitalresponse.com.au")
	
	On Error GoTo 0
End Function

' Returns true if running on development environment
Function IsDevelopment()
	On Error Resume Next
	
	Dim serverName
	serverName = Request.ServerVariables("SERVER_NAME")
	If Err.Number <> 0 Or IsNull(serverName) Then serverName = ""
	On Error Resume Next
	
	serverName = LCase(serverName)
	IsDevelopment = (InStr(serverName, "localhost") > 0 Or _
	                 InStr(serverName, ".local") > 0 Or _
	                 InStr(serverName, "dev") > 0)
	
	On Error GoTo 0
End Function

Function BackToList
	On Error Resume Next
	
	If Request("SortIndex") <> "" Then
		BackToList = "Default.asp?" & BackToListQS
	Else
		BackToList = "javascript:history.go(-1);"
	End If
	
	On Error GoTo 0
End Function

Function BackToListJS
	On Error Resume Next
	
	If Request("SortIndex") <> "" Then
		BackToListJS = "document.location.href='Default.asp?" & BackToListQS & "'"
	Else
		BackToListJS = "history.go(-1);"
	End If
	
	On Error GoTo 0
End Function

Function BackToListQS()
	On Error Resume Next
	
	Dim strDateFrom, strDateTo, strSortDirection, strCompany, strWholesaler, strCode
	Dim lngPage, lngSortIndex, lngPromotionId
	
	strDateFrom = Trim(Request("DateFrom"))
	strDateTo = Trim(Request("DateTo"))
	strSortDirection = Trim(Request("SortDirection"))
	strCompany = Trim(Request("Company"))
	strWholesaler = Trim(Request("Wholesaler"))
	strCode = Trim(Request("Code"))
	
	' Validate and convert numeric values with error handling
	lngPage = 0
	If IsNumeric(Request("Page")) Then lngPage = CLng(Request("Page"))
	
	lngSortIndex = 0
	If IsNumeric(Request("SortIndex")) Then lngSortIndex = CLng(Request("SortIndex"))
	
	lngPromotionId = 0
	If IsNumeric(Request("PromotionId")) Then lngPromotionId = CLng(Request("PromotionId"))
	
	BackToListQS = "DateFrom=" & Server.URLEncode(strDateFrom) & _
	               "&DateTo=" & Server.URLEncode(strDateTo) & _
	               "&Page=" & lngPage & _
	               "&SortIndex=" & lngSortIndex & _
	               "&SortDirection=" & Server.URLEncode(strSortDirection) & _
	               "&Company=" & Server.URLEncode(strCompany) & _
	               "&Wholesaler=" & Server.URLEncode(strWholesaler) & _
	               "&Code=" & Server.URLEncode(strCode) & _
	               "&PromotionId=" & lngPromotionId
	
	On Error GoTo 0
End Function

Function MyRedirect(NewURL)
	On Error Resume Next
	
	Dim QuestionMarkX
	
	' Validate input
	If IsNull(NewURL) Or NewURL = "" Then
		Exit Function
	End If
	
	NewURL = Replace(NewURL, "&&", "&")
	
	If Not IsEmpty(NewURL & "") Then
		QuestionMarkX = Instr(NewURL, "?")
		If QuestionMarkX = 0 Then
			Response.Redirect NewURL & "?" & NoCacheURL()
			Response.End
		Else
			Response.Redirect NewURL & "&" & NoCacheURL()
			Response.End
		End If
	End If
	
	On Error GoTo 0
End Function

Function MyRedirectWithTarget(NewURL, Target)
	On Error Resume Next
	
	Dim QuestionMarkX
	
	' Validate inputs
	If IsNull(NewURL) Or NewURL = "" Then
		Exit Function
	End If
	
	If IsNull(Target) Or Target = "" Then
		Target = ""
	Else
		Target = "&" & Target
	End If
	
	If Not IsEmpty(NewURL & "") Then
		QuestionMarkX = Instr(NewURL, "?")
		If QuestionMarkX = 0 Then
			Response.Redirect NewURL & "?" & NoCacheURL() & Target
			Response.End
		Else
			Response.Redirect NewURL & "&" & NoCacheURL() & Target
			Response.End
		End If
	End If
	
	On Error GoTo 0
End Function

Function NoCacheURL()
	On Error Resume Next
	NoCacheURL = "NoCache=" & Timer()
	On Error GoTo 0
End Function

Function ConvertToWebAddress(strWebsite)
	On Error Resume Next
	
	' Validate input
	If IsNull(strWebsite) Then strWebsite = ""
	strWebsite = CStr(strWebsite)
	strWebsite = LCase(strWebsite)
	
	If strWebsite <> "" And InStr(strWebsite, ".") > 0 Then
		If InStr(strWebsite, "http://") = 0 And InStr(strWebsite, "https://") = 0 Then
			strWebsite = "http://" & strWebsite
		End If
	End If
	
	ConvertToWebAddress = strWebsite
	
	On Error GoTo 0
End Function

Function ConvertToEmail(strEmail)
	On Error Resume Next
	
	' Validate input
	If IsNull(strEmail) Then strEmail = ""
	strEmail = CStr(strEmail)
	
	If strEmail <> "" Then
		strEmail = LCase(strEmail)
		If InStr(strEmail, ".") > 0 And InStr(strEmail, "@") > 0 Then
			strEmail = "<a href='mailto:" & strEmail & "'>" & strEmail & "</a>"
		End If
	End If
	
	ConvertToEmail = strEmail
	
	On Error GoTo 0
End Function

Function IsNumberGreaterThanZero(decNumber)
	On Error Resume Next
	
	If IsNumeric(decNumber) Then
		If CLng(decNumber) > 0 Then
			IsNumberGreaterThanZero = True
		Else
			IsNumberGreaterThanZero = False
		End If
	Else
		IsNumberGreaterThanZero = False
	End If
	
	On Error GoTo 0
End Function

Function MakePadding(intNumber, strChar, intPadding)
	On Error Resume Next
	
	Dim g, s
	
	' Validate inputs
	If IsNull(intNumber) Then intNumber = 0
	If IsNull(strChar) Then strChar = "0"
	If IsNull(intPadding) Then intPadding = 2
	If Not IsNumeric(intPadding) Then intPadding = 2
	
	s = CStr(intNumber)
	If Len(s) < CLng(intPadding) Then
		For g = 1 To CLng(intPadding) - Len(s)
			s = strChar & s
		Next
	End If
	
	MakePadding = s
	
	On Error GoTo 0
End Function

Function GetErrorCode(strS)
	On Error Resume Next
	
	' Validate input
	If IsNull(strS) Then strS = ""
	strS = CStr(strS)
	
	If InStr(strS, "The record cannot be deleted or changed because table") > 0 Or _
	   Instr(strS, "[Microsoft][ODBC Microsoft Access Driver]") > 0 And _
	   InStr(strS, "includes related records") > 0 Then 
		GetErrorCode = 1
	Else
		GetErrorCode = 0
	End If
	
	On Error GoTo 0
End Function

Sub SendMail(fromWho, toWho, Subject, Body)
	On Error Resume Next
	
	Dim objCDO, iConf, Flds
	Dim strSMTPServer
	
	' Validate inputs
	If IsNull(fromWho) Or fromWho = "" Then Exit Sub
	If IsNull(toWho) Or toWho = "" Then Exit Sub
	If IsNull(Subject) Then Subject = ""
	If IsNull(Body) Then Body = ""
	
	' Get SMTP server from session with fallback
	strSMTPServer = ""
	If Not IsEmpty(Session("SMTPServer")) Then strSMTPServer = Session("SMTPServer")
	If strSMTPServer = "" Then strSMTPServer = "localhost"
	
	' Create CDO objects with error handling
	Set objCDO = Server.CreateObject("CDO.Message")
	If Err.Number <> 0 Then
		Exit Sub
	End If
	
	Set iConf = Server.CreateObject("CDO.Configuration")
	If Err.Number <> 0 Then
		Set objCDO = Nothing
		Exit Sub
	End If
	
	Set Flds = iConf.Fields
	
	Flds("http://schemas.microsoft.com/cdo/configuration/smtpserver") = strSMTPServer
	Flds("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
	Flds.Update
	
	Set objCDO.Configuration = iConf
	objCDO.From = fromWho
	objCDO.To = toWho
	objCDO.Subject = Subject
	objCDO.HTMLBody = Body
	
	' Send email with error handling
	objCDO.Send
	
	' Cleanup
	Set objCDO = Nothing
	Set iConf = Nothing
	Set Flds = Nothing
	
	On Error GoTo 0
End Sub

Sub SetWorkingDir(strUrl)
	On Error Resume Next
	
	Dim strPath, intSalesEngine
	
	' Get URL path with error handling
	strPath = Request.ServerVariables("Url")
	If Err.Number <> 0 Or IsNull(strPath) Then strPath = ""
	On Error Resume Next
	
	intSalesEngine = InStr(strPath, "SalesEngine")
	
	If intSalesEngine > 0 Then
		strPath = Mid(strPath, 1, intSalesEngine + 12)
	Else
		strPath = "/Clients/SalesEngineTL"
	End If
	
	' Set session variables
	Session("WorkingDir") = strPath
	Session("State") = "NA"
	Session("Prefix") = Right(strPath, 2)
	
	' Set cookies with error handling
	On Error Resume Next
	Response.Cookies("ClientSettings")("WorkingDir") = strPath
	Response.Cookies("ClientSettings")("State") = "NA"
	Response.Cookies("ClientSettings")("Prefix") = Right(strPath, 2)
	Response.Cookies("ClientSettings").Expires = Date() + 1
	
	On Error GoTo 0
End Sub

Function SearchArray(arrArray, strFind)
	On Error Resume Next
	
	Dim i, boolFind
	
	' Validate inputs
	If IsNull(arrArray) Then
		SearchArray = False
		Exit Function
	End If
	
	If IsNull(strFind) Then strFind = ""
	
	' Convert to array if string
	If Not IsArray(arrArray) Then
		If arrArray <> "" Then
			arrArray = Split(arrArray, ",")
		Else
			SearchArray = False
			Exit Function
		End If
	End If

	i = 0
	boolFind = False
	
	If Not IsEmpty(arrArray) Then
		For i = 0 To UBound(arrArray)
			If CStr(Trim(arrArray(i))) = CStr(Trim(strFind & "")) Then
				boolFind = True
				Exit For
			End If
		Next
	End If
	
	SearchArray = boolFind
	
	On Error GoTo 0
End Function

%>
