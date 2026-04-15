<!--#include virtual="/System/Consts.asp"-->
<%

' Returns the appropriate protocol (http or https) based on the hostname
' Production (techlight.digitalresponse.com.au) requires HTTPS
' Local/UAT environments can use HTTP or HTTPS
Function GetProtocol()
	Dim serverName
	serverName = LCase(Request.ServerVariables("SERVER_NAME"))
	
	' Production hostname - must use HTTPS
	If serverName = "techlight.digitalresponse.com.au" Then
		GetProtocol = "https://"
	Else
		' Local/UAT environments - use current protocol
		Dim currentProtocol
		currentProtocol = Request.ServerVariables("HTTPS")
		If currentProtocol = "on" Then
			GetProtocol = "https://"
		Else
			GetProtocol = "http://"
		End If
	End If
End Function

' Returns the full base URL with protocol and hostname
Function GetBaseURL()
	GetBaseURL = GetProtocol() & Request.ServerVariables("SERVER_NAME")
End Function

' Returns true if running on production
Function IsProduction()
	Dim serverName
	serverName = LCase(Request.ServerVariables("SERVER_NAME"))
	IsProduction = (serverName = "techlight.digitalresponse.com.au")
End Function

' Returns true if running on development environment
Function IsDevelopment()
	Dim serverName
	serverName = LCase(Request.ServerVariables("SERVER_NAME"))
	IsDevelopment = (InStr(serverName, "localhost") > 0 Or InStr(serverName, "dev") > 0)
End Function

Function BackToList
	If Request("SortIndex") <> "" Then
		BackToList = "Default.asp?" & BackToListQS
	Else
		BackToList = "javascript:history.go(-1);"
	End If
End Function

Function BackToListJS
	If Request("SortIndex") <> "" Then
		BackToListJS = "document.location.href='Default.asp?" & BackToListQS & "'"
	Else
		BackToListJS = "history.go(-1);"
	End If
End Function

Function BackToListQS()
	strDateFrom = Trim(Request("DateFrom"))
	strDateTo = Trim(Request("DateTo"))
	lngPage = CLng(Request("Page"))
	lngSortIndex = CLng(Request("SortIndex"))
	strSortDirection = Trim(Request("SortDirection"))
	strCompany = Trim(Request("Company"))
	strWholesaler = Trim(Request("Wholesaler"))
	strCode = Trim(Request("Code"))
	lngPromotionId = Request("PromotionId")
	If lngPromotionId = "" Then lngPromotionId = 0
	BackToListQS = "DateFrom= " & strDateFrom & "&DateTo=" & strDateTo & "&Page=" & lngPage & "&SortIndex=" & lngSortIndex & "&SortDirection=" & strSortDirection & "&Company=" & strCompany & "&Wholesaler=" & strWholesaler & "&Code="& strCode & "&PromotionId=" & lngPromotionId
End Function

Function MyRedirect(NewURL)
    NewURL = Replace(NewURL, "&&", "&")
    If Not IsEmpty(NewURL & "") Then
        Dim QuestionMarkX
        QuestionMarkX = Instr(NewURL, "?")
        If QuestionMarkX = 0 Then
			Response.Redirect NewURL & "?" & NoCacheURL()
			Response.End
        Else
			Response.Redirect NewURL & "&" & NoCacheURL()
			Response.End
        End If
    End If
End Function

Function MyRedirectWithTarget(NewURL, Target)
    If Not IsEmpty(NewURL & "") Then
        Dim QuestionMarkX
        QuestionMarkX = Instr(NewURL, "?")
        If QuestionMarkX = 0 Then
			Response.Redirect NewURL & "?" & NoCacheURL() & Target
			Response.End
        Else
			Response.Redirect NewURL & "&" & NoCacheURL() & Target
			Response.End
        End If
    End If
End Function

Function NoCacheURL()
    On Error Resume Next
    NoCacheURL = "NoCache=" & Timer()
    On Error GoTo 0
End Function

Function ConvertToWebAddress(strWebsite)
	strWebsite = LCase(strWebsite)

	If strWebsite <> "" And InStr(strWebsite, ".") > 0 Then
		If InStr(strWebsite, "http://") = 0 And InStr(strWebsite, "https://") = 0 Then
			strWebsite = "http://" & strWebsite
		End If
	End If
	ConvertToWebAddress = strWebsite
End Function

Function ConvertToEmail(strEmail)
    If strEmail <> "" Then
	    strEmail = LCase(strEmail)
	    If InStr(strEmail, ".") > 0 And InStr(strEmail, "@") > 0 Then
			strEmail = "<a href='mailto:" & strEmail & "'>" & strEmail & "</a>"
	    End If
	End If
	ConvertToEmail = strEmail
End Function

Function IsNumberGreaterThanZero(decNumber)
	If IsNumeric(decNumber) Then
		If decNumber > 0 Then IsNumberGreaterThanZero = True Else IsNumberGreaterThanZero = False
	Else
		IsNumberGreaterThanZero = False
	End If
End Function

Function MakePadding(intNumber, strChar, intPadding)
	Dim g
	Dim s
	s = intNumber
	If Len(s) < intPadding Then
		For g = 1 To intPadding - Len(s)
			s = strChar & s
		Next
	End If
	MakePadding = s
End Function

Function GetErrorCode(strS)
	If InStr(strS, "The record cannot be deleted or changed because table") > 0 Or Instr(strS, "[Microsoft][ODBC Microsoft Access Driver]") > 0 And InStr(strS, "includes related records") > 0 Then 
		GetErrorCode = 1
	Else
		GetErrorCode = 0
	End If
End Function

Sub SendMail(fromWho, toWho, Subject, Body)
	Dim objCDO
	Dim iConf
	Dim Flds
	Set objCDO = Server.CreateObject("CDO.Message")
	Set iConf = Server.CreateObject("CDO.Configuration")
	Set Flds = iConf.Fields
	
	Flds("http://schemas.microsoft.com/cdo/configuration/smtpserver") = Session("SMTPServer")
	Flds("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
	Flds.Update
	
	Set objCDO.Configuration = iConf
	objCDO.From = fromWho
	objCDO.To = toWho
	objCDO.Subject = Subject
	objCDO.HTMLBody = Body
	objCDO.Send
	
	Set objCDO = Nothing
	Set iConf = Nothing
	Set Flds = Nothing
End Sub

Sub SetWorkingDir(strUrl)
	Dim strPath
	Dim intSalesEngine
	strPath = Request.ServerVariables("Url")
	intSalesEngine = InStr(strPath, "SalesEngine")
	strPath = Mid(strPath, 1, intSalesEngine+12)
	Session("WorkingDir") = strPath
	Session("State") = "NA"
	Session("Prefix") = Right(strPath,2)
	Response.Cookies("ClientSettings")("WorkingDir") = strPath
	Response.Cookies("ClientSettings")("State") = "NA"
	Response.Cookies("ClientSettings")("Prefix") = Right(strPath,2)
	Response.Cookies("ClientSettings").Expires = Date() + 1
End Sub

%>
