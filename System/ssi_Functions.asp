<!--#include virtual="System/Consts.asp"-->
<!--#include virtual="Timezone.asp"-->
<!--#include virtual="System/ssi_Alerts.asp"-->
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

Function CheckIfAdmin(intUserId)
	Set rsChk = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From UsersAccess Where Manager = 1 And UserId = " & intUserId
	Set rsChk = dbConn.Execute(sql)
	If Not(rsChk.BOF And rsChk.EOF) Then
		CheckIfAdmin = True
	Else
		CheckIfAdmin = False
	End If
	rsChk.Close
	Set rsChk = Nothing
End Function

Function IsNumberGreaterThanZero(decNumber)
	If IsNumeric(decNumber) Then
		If decNumber > 0 Then IsNumberGreaterThanZero = True Else IsNumberGreaterThanZero = False
	Else
		IsNumberGreaterThanZero = False
	End If
End Function

Function MyRedirect(NewURL)
    '
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
    '
End Function

Function MyRedirectWithTarget(NewURL, Target)
    '
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
    '
End Function

Function NoCacheURL()
    '
    On Error Resume Next
    '
    Randomize
    ' Randomize not needed if you use ServerToEST(Now())
    '
    NoCacheURL = "NoCache=" & Server.URLEncode(rnd)
    '
    ' or NoCacheURL = "NoCache=" &amp; Server.URLEncode(ServerToEST(Now()))
    ' per Bill
    '
End Function

Function ConvertToWebAddress(strWebsite)
	strWebsite = LCase(strWebsite)

	If strWebsite <> "" And InStr(strWebsite, ".") > 0 Then
		If Not InStr(strWebsite, "http://") > 0 And InStr(strWebsite, "www.") > 0 Then
			strWebsite = "http://" & strWebsite
		End If
		strWebsite = "<a href=""" & strWebsite & """ target=""_blank"">" & strWebsite & "</a>"
	End If
	
	ConvertToWebAddress = strWebsite
End Function

Function ConvertToEmail(strEmail)
    If strEmail <> "" Then
	    strEmail = LCase(strEmail)
	    If InStr(strEmail, ".") > 0 And InStr(strEmail, "@") > 0 Then
		    strEmail = "<a href=""mailto:" & strEmail & """>" & strEmail & "</a>"
	    End If
    End If
	ConvertToEmail = strEmail
End Function

Function ViewComments(intTableId, intItemId)
	Set rsComm = Server.CreateObject("ADODB.RecordSet")
	sqlComm = "Select * From Comments Where TableId = " & intTableId & " And ItemId = " & intItemId & " Order By [Date] Desc"
	Set rsComm = dbConn.Execute(sqlComm)

	If Not(rsComm.BOF And rsComm.EOF) Then
%>
<br />
<b>Comments</b>
<table width="100%" cellpadding="5" cellspacing="0" border="0" id="Table1">
    <tr>
        <td style="border-bottom: 1px solid black;" width="100"><b>Date</b></td>
        <td style="border-bottom: 1px solid black;"><b>Comment</b></td>
        <td style="border-bottom: 1px solid black;" width="50"><b>From</b></td>
        <td style="border-bottom: 1px solid black;" width="50"><b>To</b></td>
    </tr>
    <%

		Do Until rsComm.EOF

			FromInitals = ""
			ToInitials = ""

			Set rs2 = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 * From SalesPeople Where Code = '" & rsComm("FromCode") & "'"
			Set rs2 = dbConn.Execute(sql)
			
			If Not(rs2.BOF And rs2.EOF) Then
				FromInitials = rs2("Initials") & "&nbsp;"
			End If
			
			rs2.Close
			Set rs2 = Nothing

			Set rs3 = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 * From SalesPeople Where Code = '" & rsComm("ToCode") & "'"
			Set rs3 = dbConn.Execute(sql)
			
			If Not(rs3.BOF And rs3.EOF) Then
				ToInitials = rs3("Initials") & "&nbsp;"
			End If
			
			rs3.Close
			Set rs3 = Nothing
    %>
    <tr>
        <td style="border-bottom: 1px solid black;" width="100"><%= FormatDateU(rsComm("Date"), False) %></td>
        <td style="border-bottom: 1px solid black;"><%= rsComm("Comment") %></td>
        <td style="border-bottom: 1px solid black;" width="50"><%= FromInitials %></td>
        <td style="border-bottom: 1px solid black;" width="50"><%= ToInitials %></td>
    </tr>
    <%
			rsComm.MoveNext
		Loop
    %>
</table>
<%
	Else
%>
<p>There are currently no comments</p>
<%
	End If
	If IsObject(rsComm) Then
		rsComm.Close
		Set rsComm = Nothing
	End If
End Function

Function GetActivityTypes(intActivityTypeId)
	If Not IsNumeric(intActivityTypeId) Then intActivityTypeId = 0
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ActivityTypes WHERE Visible = 1 OR ActivityTypeId = " & intActivityTypeId & " ORDER BY InOrder, Visible DESC, ActivityType"
	Set rs = dbConn.Execute(sql)
	s = "<option value=""0""></option>"
	If Not(rs.BOF And rs.EOF) Then
		Do Until rs.EOF
			If Not rs("Visible") Then
				s = s & "<option value=""" & rs("ActivityTypeId") & """ style=""background-color:#ebeadb;"">" & rs("ActivityType") & "</option>" & vbNewLine
				s = s & "<option value=""0"">------------------------------------------</option>" & vbNewLine
			else
				s = s & "<option value=""" & rs("ActivityTypeId") & """>" & rs("ActivityType") & "</option>" & vbNewLine
				Response.Write("this happens")
			End If
			rs.MoveNext
		Loop
	End If
	rs.Close
	Set rs = Nothing
	GetActivityTypes = s
End Function

Function GetActivityTypes(intActivityTypeId,intDayIndex,intActivityIndex)
	If Not IsNumeric(intActivityTypeId) Then intActivityTypeId = 0
	Set rs = Server.CreateObject("ADODB.RecordSet")
	If intActivityIndex = 1 Then
		sql = "SELECT * FROM ActivityTypes WHERE Visible = 1 OR ActivityTypeId = " & intActivityTypeId & " ORDER BY InOrder, Visible DESC, ActivityType"
	Else
		sql = "SELECT * FROM ActivityTypes WHERE WholeDayEvent = 0 And (Visible = 1 OR ActivityTypeId = " & intActivityTypeId & ") ORDER BY InOrder, Visible DESC, ActivityType"
	End If
	Set rs = dbConn.Execute(sql)
	s = "<option value=""0""></option>"
	If Not(rs.BOF And rs.EOF) Then
		Do Until rs.EOF
			If Not rs("Visible") Then
				s = s & "<option value=""" & rs("ActivityTypeId") & """ style=""background-color:#ebeadb;"">" & rs("ActivityType") & "</option>" & vbNewLine
				s = s & "<option value=""0"">------------------------------------------</option>" & vbNewLine
			Elseif rs("ActivityTypeId") = intActivityTypeId then
				If rs("wholeDayEvent") then
					s = s & "<option value=""" & rs("ActivityTypeId") & """ selected onclick=""javascript:blockActivities('" & intDayIndex & "','" & intActivityIndex & "')"">" & rs("ActivityType") & "</option>" & vbNewLine
				else
					s = s & "<option value=""" & rs("ActivityTypeId") & """ selected onclick=""javascript:resetActivities('" & intDayIndex & "','" & intActivityIndex & "')"">" & rs("ActivityType") & "</option>" & vbNewLine
				end if
			Elseif rs("wholeDayEvent") Then
				s = s & "<option value=""" & rs("ActivityTypeId") & """ onclick=""javascript:blockActivities('" & intDayIndex & "','" & intActivityIndex & "')"">" & rs("ActivityType") & "</option>" & vbNewLine
			else
				s = s & "<option value=""" & rs("ActivityTypeId") & """ onclick=""javascript:resetActivities('" & intDayIndex & "','" & intActivityIndex & "')"">" & rs("ActivityType") & "</option>" & vbNewLine
			End If
			rs.MoveNext
		Loop
	End If
	rs.Close
	Set rs = Nothing
	GetActivityTypes = s
End Function

Function GetActivityTypesAsArr()
	Dim arrActivityTypes(100)

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ActivityTypes"
	Set rs = dbConn.Execute(sql)

	Do Until rs.EOF
		arrActivityTypes(rs("ActivityTypeId")) = rs("ActivityType")
		rs.MoveNext
	Loop
	rs.Close
	Set rs = Nothing
	GetActivityTypesAsArr = arrActivityTypes
End Function

Function GetTimesheetStatusAsArr()
	Dim arrTimesheetStatuses(100)

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM TimesheetStatus"
	Set rs = dbConn.Execute(sql)

	Do Until rs.EOF
		arrActivityTypes(rs("TimesheetStatusId")) = rs("TimesheetStatus")
		rs.MoveNext
	Loop
	rs.Close
	Set rs = Nothing
	GetActivityTypesAsArr = arrTimesheetStatuses
End Function

Function GetTimesheetItemsAsArr(intTimesheetId)
	Dim arrTimesheetItems(6, 8, 5)

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT TI.*, A.ActivityType FROM TimesheetItems TI INNER JOIN ActivityTypes A ON A.ActivityTypeId = TI.ActivityTypeId WHERE TI.TimesheetId = " & intTimesheetId & " ORDER BY TI.Day, TI.TimesheetItemId"
	'response.Write(sql)
	'response.End
	Set rs = dbConn.Execute(sql)

	intRow = 0
	intDay = 0
	Do Until rs.EOF
		If CInt(rs("Day")) <> CInt(intDay+1) Then
			intRow = 0
			intDay = rs("Day")-1
		End If
		arrTimesheetItems(intDay, intRow, 2) = rs("StartTime")
		arrTimesheetItems(intDay, intRow, 3) = rs("FinishTime")
		arrTimesheetItems(intDay, intRow, 4) = rs("ActivityTypeId")
		arrTimesheetItems(intDay, intRow, 5) = rs("ActivityType")
		intRow = intRow + 1
		rs.MoveNext
	Loop
	rs.Close
	Set rs = Nothing
	GetTimesheetItemsAsArr = arrTimesheetItems
End Function

Function GetProjects(intDivisionId, intProjectId)
	If Not IsNumeric(intProjectId) Then intProjectId = 0
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM Projects WHERE (Visible = 1 OR ProjectId = " & intProjectId & ") AND DivisionId = " & intDivisionId & " ORDER BY Project"
	Set rs = dbConn.Execute(sql)
	s = "<option value=""0""></option>"
	If Not(rs.BOF And rs.EOF) Then
		Do Until rs.EOF
			If Not rs("Visible") Then
				s = s & "<option value=""" & rs("ProjectId") & """ style=""background-color:#ebeadb;"">" & rs("Project") & "</option>" & vbNewLine
				s = s & "<option value=""0"">------------------------------------------</option>" & vbNewLine
			Else
				s = s & "<option value=""" & rs("ProjectId") & """>" & rs("Project") & "</option>" & vbNewLine
			End If
			rs.MoveNext
		Loop
	End If
	rs.Close
	Set rs = Nothing
	GetProjects = s
End Function

Function MakePadding(intNumber, strChar, intPadding)
	Dim g
	Dim s
	s = intNumber
	g = 0
	
	Do Until g = (intPadding - Len(intNumber))
		s = CStr(strChar) & CStr(s)
		g = g + 1
	Loop
	MakePadding = s
End Function

Function GetHoursPerWeek(strCode)
	Dim intI
	Dim sqlHrs
	Dim rsHrs
	Set rsHrs = Server.CreateObject("ADODB.RecordSet")
	sqlHrs = "SELECT HoursPerDay * DaysPerWeek As [HoursPerWeek] FROM Users WHERE Code = '" & strCode & "'"
	Set rsHrs = dbConn.Execute(sqlHrs)
	intI = rsHrs("HoursPerWeek")
	rsHrs.Close
	Set rsHrs = Nothing
	GetHoursPerWeek = intI
End Function

Function GetUserLevelAccessAsArr(intCategoryId)
	Dim arrUserLevelAccess(100)
	Dim i

	Set rsArr = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM FilesCategoriesUserLevelAccess WHERE CategoryId = " & intCategoryId
	Set rsArr = dbConn.Execute(sql)

	i = 0
	Do Until rsArr.EOF
		arrUserLevelAccess(i) = rsArr("UserTypeId")
		i = i + 1
		rsArr.MoveNext
	Loop
	rsArr.Close
	Set rsArr = Nothing
	GetUserLevelAccessAsArr = arrUserLevelAccess
End Function

Function GetDivisionAccessAsArr(intCategoryId)
	Dim arrDivision(100)
	Dim i

	Set rsArr = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM FilesCategoriesDivisionAccess WHERE CategoryId = " & intCategoryId
	Set rsArr = dbConn.Execute(sql)

	i = 0
	Do Until rsArr.EOF
		arrDivision(i) = rsArr("DivisionId")
		i = i + 1
		rsArr.MoveNext
	Loop
	rsArr.Close
	Set rsArr = Nothing
	GetDivisionAccessAsArr = arrDivision
End Function

Function GetUserAccessAsArr(intCategoryId)
	If intCategoryId <> "" Then
		Dim arrUser(500)
		Dim i
	
		Set rsArr = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT * FROM FilesCategoriesUserAccess INNER JOIN Users On Users.UserId = FilesCategoriesUserAccess.UserId WHERE CategoryId = " & intCategoryId
		'response.write sql
		'response.end
		Set rsArr = dbConn.Execute(sql)

		i = 0
		Do Until rsArr.EOF
			arrUser(i) = rsArr("Code")
			i = i + 1
			rsArr.MoveNext
		Loop
		rsArr.Close
		Set rsArr = Nothing
	GetUserAccessAsArr = arrUser
	End If
End Function

Function GetFileCategoriesAsArr(intCategoryId)
	If intCategoryId <> "" Then
		Dim arrUser(500)
		Dim i
	
		Set rsArr = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT * FROM FilesCategories"
		'response.write sql
		'response.end
		Set rsArr = dbConn.Execute(sql)

		i = 0
		Do Until rsArr.EOF
			arrUser(i) = rsArr("Code")
			i = i + 1
			rsArr.MoveNext
		Loop
		rsArr.Close
		Set rsArr = Nothing
	GetFileCategoriesAsArr = arrUser
	End If
End Function

Function GetFilesCategories(intCategoryId)
	If Not IsNumeric(intCategoryId) Then intCategoryId = 0
	Set rsCat = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM FilesCategories ORDER BY Category"
	Set rsCat = dbConn.Execute(sql)
	s = "<option value=""0""></option>"
	If Not(rsCat.BOF And rsCat.EOF) Then
		Do Until rsCat.EOF
			If intCategoryId = rsCat("CategoryId") Then
				s = s & "<option selected value=""" & rsCat("CategoryId") & """>" & rsCat("Category") & "</option>" & vbNewLine
			Else
				s = s & "<option value=""" & rsCat("CategoryId") & """>" & rsCat("Category") & "</option>" & vbNewLine
			End If
			rsCat.MoveNext
		Loop
	End If
	rsCat.Close
	Set rsCat = Nothing
	GetFilesCategories = s
End Function

Function ElementInArray(arrArray, strCompare)
	Dim iLoop
	Dim bolFound
	bolFound = False

	For iLoop = LBound(arrArray) to UBound(arrArray)
		If CStr(Trim(arrArray(iLoop))) = CStr(Trim(strCompare)) then
			bolFound = True
		End If
	Next

	ElementInArray = bolFound
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

	Const cdoSendUsingPort = 2

	Set objCDO = Server.CreateObject("CDO.Message")
	Set iConf = Server.CreateObject("CDO.Configuration")

	Set Flds = iConf.Fields
	With Flds
        .Item("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserver") = "smtp.sendgrid.net"
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = 587
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpconnectiontimeout") = 60
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 1
        .Item("http://schemas.microsoft.com/cdo/configuration/sendusername") = "apikey"
        .Item("http://schemas.microsoft.com/cdo/configuration/sendpassword") = "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"
		.Item("http://schemas.microsoft.com/cdo/configuration/sendtls") = true
		.Update
	End With

	Set objCDO.Configuration = iConf

	objCDO.From = fromWho
	objCDO.To = toWho
	objCDO.Subject = Subject
	objCDO.TextBody = Body
	objCDO.Send

	Set ObjCDO = Nothing
	Set iConf = Nothing
	Set Flds = Nothing
End Sub

'**************************************
' Name: GeneratePassword
' Description:With this code, you can generate a pronounceable password. By modifying 3 constants, you can adapt this code to your language
' By: G�rard Dupont
'
' Inputs:The length of the password
'
' Returns:A string
'
' This code is copyrighted and has    ' limited warranties.Please see http://www.Planet-Source-Code.com/vb/scripts/ShowCode.asp?txtCodeId=6345&lngWId=4    'for details.    '**************************************


Private function GeneratePassword(LenPassword)
	'adapt these Const To your language
	Const strVowel = "aeiou"						'All the vowels except y
	Const strConsonant = "bcdfghjklmnprstv"			'All the consonants except q,w,x,z
	Const strDoubleConsonant = "cdfglmnprst"		'These consonants may be double	

	Dim WriteConsonant			'boolean
	Dim nbRnd					'a random number
	Dim i, tmp

	GeneratePassword = ""
	WriteConsonant = False
	Randomize
	For i = 0 To LenPassword	
	nbRnd = Rnd
		'Write a Single or a Double consonant ?
		'1.No word begin With a Double consonant
		'2.About 10% of Double consonants
		if GeneratePassword <> "" And (WriteConsonant = False) And (nbRnd < 0.10) Then
		'choose a Double consonant
			tmp = Mid(strDoubleConsonant, Int(Len(strDoubleConsonant) * Rnd + 1), 1)
			'write it
			tmp = tmp & tmp
			i = i + 1
			'next letter may be a consonant
			WriteConsonant = True
		Else
			'if the last letter is a vowel, the probability is 90% To have a consonant							
			if (WriteConsonant = False) And (nbRnd < 0.90) Then
			'single consonant
			tmp= Mid(strConsonant, Int(Len(strConsonant) * Rnd + 1), 1)
				WriteConsonant = True
				'If the last letter is a Double consonant,
				'the Next letter is necessary a vowel !
			Else
				'write it
				tmp = Mid(strVowel,Int(Len(strVowel) * Rnd + 1), 1)
				WriteConsonant = False
			End if
		End if
		'add a letter
		GeneratePassword = GeneratePassword & tmp
	Next

	'Check password length
	if Len(GeneratePassword) > LenPassword Then
		GeneratePassword = Left(GeneratePassword, LenPassword)
	End if
End Function

Function GetPONextLineApprover_Email(lngPOid, boolCapEx)
	Dim strEmail
	Dim strName
	Dim strSql
	strName = GetPONextLineApprover(lngPOid, boolCapEx)
	strSql = "Select * From Users Where Deleted = 0 AND Name = '" & Replace(strName, "'", "''") & "'"
	Set rsPONext = dbConn.Execute(strSql)
	GetPONextLineApprover_Email = rsPONext("Email")
End Function

Function GetPOLastLineManager_Email(lngPOid)
	Dim strEmail
	Dim strName
	Dim strSql
	strName = GetPOLastLineManager(lngPOid)
	strSql = "Select * From Users Where Deleted = 0 AND Name = '" & Replace(strName, "'", "''") & "'"
	Set rsPONext = dbConn.Execute(strSql)
	GetPOLastLineManager_Email = rsPONext("Email")
End Function

Function GetPONextLineApprover(lngPOid, boolCapEx)
	Dim strLastLineApproverCode
	Dim strPreviousLineApproverCode
	Dim rsUser
	Dim rs
	Dim rsPO
	Dim sql
	Dim strPONextLineApprover
	Dim decPOApprovalLimit
	Dim decPriceIncTotal
	
	strLastLineApproverCode = GetPOLastLineApprover(lngPOid, boolCapEx)

	' Get the PO
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit, UserRoles.POCapExApprovalLimit, Users.Name FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POStatusId = 2 And POId = " & lngPOid
	Set rsPO = dbConn.Execute(sql)

	If Not(rsPO.BOF And rsPO.EOF) Then
		If boolCapEx Then decPOApprovalLimit = rsPO("POCapExApprovalLimit") Else decPOApprovalLimit = rsPO("POApprovalLimit")
		decPriceIncTotal = rsPO("PriceIncTotal")
		If decPriceIncTotal <= decPOApprovalLimit Then ' PO creator can approve
			GetPONextLineApprover = "Already approved"
		Else ' Find someone who can
			' Get the most recent person to approve
			Set rs = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 Code From PurchaseOrderApproval Where POId = " & lngPOId & " Order By POApprovalId Desc"
			Set rs = dbConn.Execute(sql)
			If Not(rs.BOF And rs.EOF) Then
				strPreviousLineApproverCode = rs("Code")
				If strPreviousLineApproverCode = strLastLineApproverCode Then
					strPONextLineApprover = "Already Approved"
				Else
					'Do Until i = 1
						If Not(rs.BOF And rs.EOF) Then
							Set rsUser = Server.CreateObject("ADODB.RecordSet")
							sql = "Select * From Users Where Deleted = 0 AND Code = '" & GetLineManagerCode(strPreviousLineApproverCode) & "'"
							Set rsUser = dbConn.Execute(sql)
							If Not(rsUser.BOF And rsUser.EOF) Then
								strPONextLineApprover = rsUser("Name")
							End If
							rsUser.Close
							Set rsUser = Nothing
						End If
						rs.Close
						Set rs = Nothing
					'Loop
				End If
			Else
				Set rsPO = Server.CreateObject("ADODB.RecordSet")
				sql = "Select Code From PurchaseOrders Where POid = " & lngPOid
				Set rsPO = dbConn.Execute(sql)
				If Not(rsPO.BOF And rsPO.EOF) Then
					strPONextLineApprover = GetUserFullName(GetLineManagerCode(rsPO("Code")))
				End If
				rsPO.Close
				Set rsPO = Nothing
			End If
		End If
	End If
	If strPONextLineApprover <> "" Then
		GetPONextLineApprover = strPONextLineApprover
	Else
		strPONextLineApprover = "Unknown"
	End If
End Function

Function GetPONextLineApproverCode(lngPOid, boolCapEx)
	Dim strLastLineApproverCode
	Dim strPreviousLineApproverCode
	Dim rsUser
	Dim rs
	Dim rsPO
	Dim sql
	Dim strPONextLineApprover
	Dim decPOApprovalLimit
	Dim decPriceIncTotal
	
	strLastLineApproverCode = GetPOLastLineApproverCode(lngPOid, boolCapEx)

	' Get the PO
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit, UserRoles.POCapExApprovalLimit, Users.Name FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POStatusId = 2 And POId = " & lngPOid
	Set rsPO = dbConn.Execute(sql)

	If Not(rsPO.BOF And rsPO.EOF) Then
		If boolCapEx Then decPOApprovalLimit = rsPO("POCapExApprovalLimit") Else decPOApprovalLimit = rsPO("POApprovalLimit")
		decPriceIncTotal = rsPO("PriceIncTotal")
		If decPriceIncTotal <= decPOApprovalLimit Then ' PO creator can approve
			GetPONextLineApprover = "Already approved"
		Else ' Find someone who can
			' Get the most recent person to approve
			Set rs = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 Code From PurchaseOrderApproval Where POId = " & lngPOId & " Order By POApprovalId Desc"
			Set rs = dbConn.Execute(sql)
			If Not(rs.BOF And rs.EOF) Then
				strPreviousLineApproverCode = rs("Code")
				If strPreviousLineApproverCode = strLastLineApproverCode Then
					strPONextLineApprover = "Already Approved"
				Else
					'Do Until i = 1
						If Not(rs.BOF And rs.EOF) Then
							Set rsUser = Server.CreateObject("ADODB.RecordSet")
							sql = "Select * From Users Where Deleted = 0 AND Code = '" & GetLineManagerCode(strPreviousLineApproverCode) & "'"
							Set rsUser = dbConn.Execute(sql)
							If Not(rsUser.BOF And rsUser.EOF) Then
								strPONextLineApprover = rsUser("Code")
							End If
							rsUser.Close
							Set rsUser = Nothing
						End If
						rs.Close
						Set rs = Nothing
					'Loop
				End If
			Else
				Set rsPO = Server.CreateObject("ADODB.RecordSet")
				sql = "Select Code From PurchaseOrders Where POid = " & lngPOid
				Set rsPO = dbConn.Execute(sql)
				If Not(rsPO.BOF And rsPO.EOF) Then
					strPONextLineApprover = rsPO("Code")
				End If
				rsPO.Close
				Set rsPO = Nothing
			End If
		End If
	End If
	If strPONextLineApprover <> "" Then
		GetPONextLineApprover = strPONextLineApprover
	Else
		strPONextLineApprover = "Unknown"
	End If
End Function

Function GetPOLastLineManagers(lngPOid)
	Dim rs
	Dim sql
    Dim rspo

	Set rspo = Server.CreateObject("ADODB.RecordSet")
	sql = "select * from purchaseorders where poid = " & lngPoid
	Set rspo = dbConn.Execute(sql)

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND active = true"
	Set rs = dbConn.Execute(sql)
	If Not(rs.BOF And rs.EOF) Then
        Dim i
        i = 0
        GetPOLastLineManagers = "Unknown"
        Do Until rs.EOF
            If (GetPOLineApprover_Check(lngPOid, rs("Code"), false)) Then
                If i = 0 Then
                    GetPOLastLineManagers = rs("Email")
                Else
                    GetPOLastLineManagers = GetPOLastLineManagers + "," + rs("Email")
                End If
            End If
            rs.MoveNext
        Loop
    Else
        GetPOLastLineManagers = "Unknown"
    End If
End Function


Function GetLineManagerCode(strCode)
	Dim rs
	Dim sql
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
	Set rs = dbConn.Execute(sql)
	If Not(rs.BOF And rs.EOF) Then
		GetLineManagerCode = rs("LineManagerCode")
	Else
		GetLineManagerCode = "Unknown"
	End If
	If IsObject(rs) Then
		rs.Close
		Set rs = Nothing
	End If
End Function

Function CheckForLine(strCode, strLineManagerCode, lngId, boolQuote, boolPO)
'	Dim rs
'	Dim sql
'	Dim strCheckLineManagerCode
'	Set rs = Server.CreateObject("ADODB.RecordSet")
'	sql = "Select LineManagerCode From Users Where Code = '" & strCode & "'"
'	Set rs = dbConn.Execute(sql)
'	If Not(rs.BOF And rs.EOF) Then
'		strCheckLineManagerCode = Trim(rs("LineManagerCode"))
'		If Trim(strCheckLineManagerCode) = Trim(strLineManagerCode) Then
'			CheckForLine = True
'		Else
'			Do Until strCheckLineManagerCode = strLineManagerCode Or strCheckLineManagerCode = "Unknown"
'				strCheckLineManagerCode = Trim(GetLineManagerCode(strCheckLineManagerCode))
'			Loop
'			If strCheckLineManagerCode = "Unknown" Or strCheckLineManagerCode = "Already approved" Then
'				CheckForLine = False
'			Else
'				CheckForLine = True
'			End If
'		End If
'	Else
'		CheckForLine = False
'	End If
'	If boolQuote Then
'		sql = "Select * From QuoteApproval Where Code = '" & strLineManagerCode & "' And Qid = " & lngId
'		Set rs = dbConn.Execute(sql)
'		If Not(rs.BOF And rs.EOF) Then
'			CheckForLine = False
'		Else
'			CheckForLine = True
'		End If
'	ElseIf boolPO Then
'		sql = "Select * From PurchaseOrderApproval Where Code = '" & strLineManagerCode & "' And POid = " & lngId
'		Set rs = dbConn.Execute(sql)
'		If Not(rs.BOF And rs.EOF) Then
'			CheckForLine = False
'		Else
'			CheckForLine = True
'		End If
'	End If
'	If IsObject(rs) Then
'		rs.Close
'		Set rs = Nothing
'	End If
	CheckForLine = False
End Function

Function GetPOLastLineApprover(lngPOid, boolCapEx)
	Dim rsPO
	Dim sql
	Dim decPOApprovalLimit
	Dim decPriceIncTotal
	Dim strPOLastLineApprover

	' Get the PO
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit, UserRoles.POCapExApprovalLimit, Users.Name FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POId = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	
	If Not(rsPO.BOF And rsPO.EOF) Then
		If boolCapEx Then decPOApprovalLimit = rsPO("POCapExApprovalLimit") Else decPOApprovalLimit = rsPO("POApprovalLimit")
		decPriceIncTotal = rsPO("PriceIncTotal")
		If decPriceIncTotal <= decPOApprovalLimit Then ' PO creator can approve
			strPOLastLineApprover = "Already approved"
		Else
			strPOLastLineApprover = GetPOLastLineApprover_Find(rsPO("PriceIncTotal"), rsPO("Code"), boolCapEx)
		End If
	Else
		strPOLastLineApprover = "An error occurred"
	End If
	rsPO.Close
	Set rsPO = Nothing
	GetPOLastLineApprover = strPOLastLineApprover
End Function

Function GetPOLastLineApprover_Find(decPriceIncTotal, strCode, boolCapEx)
	Dim rsUser
	Dim rsUser2
	Dim sql
	Dim strLineManagerCode
	Dim decPOApprovalLimit
	' Get user's line manager
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & strCode & "'"
	Set rsUser = dbConn.Execute(sql)
	If Not(rsUser.BOF and rsUser.EOF) Then
		strLineManagerCode = rsUser("LineManagerCode")
		If boolCapEx Then
			decPOApprovalLimit = rsUser("POCapExApprovalLimit")
		Else
			decPOApprovalLimit = rsUser("POApprovalLimit")
		End If
		If CDbl(decPriceIncTotal) =< CDbl(decPOApprovalLimit) Then
			GetPOLastLineApprover_Find = GetUserFullName(rsUser("Code"))
			Exit Function
		Else
			GetPOLastLineApprover_Find = GetPOLastLineApprover_Find(decPriceIncTotal, rsUser("LineManagerCode"), boolCapEx)
		End If
	Else
		GetPOLastLineApprover_Find = "Unknown"
	End If
	rsUser.Close
	Set rsUser = Nothing
End Function


' Quote APPROVALS

Function GetCheckLineManager(strCode)
	sql = "Select LineManagerCode From Users Where Code = '" & strCode & "'"
	Set rs = dbConn.Execute(sql)
	If rs("LineManagerCode") <> "" Then
		GetLineManager = rs("LineManagerCode")
	Else
		GetLineManager = "End"
	End If	
	rs.Close
	Set rs = Nothing
End Function

Function GetQuoteLineApprover_Check(lngQid, strCode)
	Dim strNextLineApproverCode
	Dim strTestCode
	If IsDirector(strCode) Then
		GetQuoteLineApprover_Check = True
	Else
		strNextLineApproverCode = GetQuoteNextLineApproverCode(lngQid)
		If strCode = strNextLineApproverCode Then
			GetQuoteLineApprover_Check = True
		Else
			strTestCode = strCode
			strLineManager = strNextLineApproverCode
			Do Until strLineManager = "Unknown" Or strLineManager = strCode
				strLineManager = GetLineManagerCode(strLineManager)
				If strLineManager = strCode Then
					GetQuoteLineApprover_Check = True
				End If
			Loop
			If strLineManager = "Unknown" Then GetQuoteLineApprover_Check = False
		End If
	End If
End Function

Function IsDirector(strCode)
	Dim strDirSQL
	Dim rsDir
	IsDirector = False
	strDirSQL = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
	Set rsDir = dbConn.Execute(strDirSQL)
	If Not(rsDir.BOF And rsDir.EOF) Then
		If rsDir("UserTypeId") = 5 Or rsDir("UserTypeId") = 6 Then ' Directors and Administrators
			IsDirector = True
		End If
	End If
	rsDir.Close
	Set rsDir = Nothing
End Function

Function GetPOLineApprover_Check(lngPOid, strCode, boolCapEx)
	Dim strNextLineApproverCode
	Dim strTestCode
	If IsDirector(strCode) Then
		GetPOLineApprover_Check = True
	Else
		strNextLineApproverCode = GetPONextLineApproverCode(lngPOid, boolCapEx)
		If strCode = strNextLineApproverCode Then
			GetPOLineApprover_Check = True
		Else
			strTestCode = strCode
			strLineManager = strNextLineApproverCode
			i = 0 
			Do Until strLineManager = "Unknown" Or strLineManager = strCode Or i = 10
				strLineManager = GetLineManagerCode(strLineManager)
				If strLineManager = strCode Then
					GetPOLineApprover_Check = True
				End If
				i = i + 1
			Loop
			If strLineManager = "Unknown" Then GetPOLineApprover_Check = False
		End If
	End If
End Function

Function GetCodeByName(strName)
	Dim rsCode
	Dim sql
	Set rsCode = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Code From Users Where Name = '" & Replace(strName,"'","''") & "'"
	Set rsCode = dbConn.Execute(sql)
	If Not(rsCode.BOF And rsCode.EOF) Then
		GetCodeByName = rsCode("Code")
	End If
	rsCode.Close
	Set rsCode = Nothing
	If GetCodeByName = "" Then GetCodeByName = "Unknown"
End Function

Function GetQuoteNextLineApprover_Email(lngQid)
	Dim strEmail
	Dim strName
	Dim strSql
	strName = GetQuoteNextLineApprover(lngQid)
	strSql = "Select * From Users Where Deleted = 0 AND Name = '" & strName & "'"
	Set rsQuoteNext = dbConn.Execute(strSql)
	If Not(rsQuoteNext.BOF And rsQuoteNext.EOF) Then
		GetQuoteNextLineApprover_Email = rsQuoteNext("Email")
	Else
		GetQuoteNextLineApprover_Email = "peterb@digitalresponse.com.au"
	End If
End Function

Function GetQuoteNextLineApprover(lngQid)
	Dim strLastLineApproverCode
	Dim strPreviousLineApproverCode
	Dim rsUser
	Dim rs
	Dim rsQuote
	Dim sql
	Dim strQuoteNextLineApprover
	Dim decQuoteApprovalLimit
	Dim decNettPriceTotal
	
	strLastLineApproverCode = GetQuoteLastLineApprover(lngQid)

	' Get the Quote
	Set rsQuote = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Quotes.*, UserRoles.QuoteApprovalLimit, Users.Name FROM (Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where QuoteStatusId = 9 And Qid = " & lngQid
	Set rsQuote = dbConn.Execute(sql)

	If Not(rsQuote.BOF And rsQuote.EOF) Then
		decQuoteApprovalLimit = rsQuote("QuoteApprovalLimit")
		decNettPriceTotal = rsQuote("NettPriceTotal")

		If decNettPriceTotal <= decQuoteApprovalLimit Then ' Quote creator can approve
			GetQuoteNextLineApprover = "Already approved"
		Else ' Find someone who can
			' Get the most recent person to approve
			Set rs = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 Code From QuoteApproval Where Qid = " & lngQid & " Order By QuoteApprovalId Desc"
			Set rs = dbConn.Execute(sql)
			
			If Not(rs.BOF And rs.EOF) Then
				strPreviousLineApproverCode = rs("Code")
				If strPreviousLineApproverCode = strLastLineApproverCode Then
					strQuoteNextLineApprover = "Already Approved"
				Else
					'Do Until i = 1
						If Not(rs.BOF And rs.EOF) Then
							Set rsUser = Server.CreateObject("ADODB.RecordSet")
							sql = "Select * From Users Where Deleted = 0 AND Code = '" & GetLineManagerCode(strPreviousLineApproverCode) & "'"
							Set rsUser = dbConn.Execute(sql)
							If Not(rsUser.BOF And rsUser.EOF) Then
								strQuoteNextLineApprover = rsUser("Name")
							End If
							rsUser.Close
							Set rsUser = Nothing
						End If
						rs.Close
						Set rs = Nothing
					'Loop
				End If
			Else
				Set rsQuote = Server.CreateObject("ADODB.RecordSet")
				sql = "Select Code From Quotes Where Qid = " & lngQid
				Set rsQuote = dbConn.Execute(sql)
				If Not(rsQuote.BOF And rsQuote.EOF) Then
					strQuoteNextLineApprover = GetUserFullName(GetLineManagerCode(rsQuote("Code")))
				End If
				rsQuote.Close
				Set rsQuote = Nothing
			End If
		End If
	End If
	If strQuoteNextLineApprover <> "" Then
		GetQuoteNextLineApprover = strQuoteNextLineApprover
	Else
		strQuoteNextLineApprover = "Unknown"
	End If
End Function

Function GetQuoteNextLineApproverCode(lngQid)
	Dim strLastLineApproverCode
	Dim strPreviousLineApproverCode
	Dim rsUser
	Dim rs
	Dim rsQuote
	Dim sql
	Dim strQuoteNextLineApprover
	Dim decQuoteApprovalLimit
	Dim decNettPriceTotal
	
	strLastLineApproverCode = GetQuoteLastLineApprover(lngQid)

	' Get the Quote
	Set rsQuote = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Quotes.*, UserRoles.QuoteApprovalLimit, Users.Name FROM (Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where QuoteStatusId = 9 And Qid = " & lngQid
	Set rsQuote = dbConn.Execute(sql)

	If Not(rsQuote.BOF And rsQuote.EOF) Then
		decQuoteApprovalLimit = rsQuote("QuoteApprovalLimit")
		decNettPriceTotal = rsQuote("NettPriceTotal")

		If decNettPriceTotal <= decQuoteApprovalLimit Then ' Quote creator can approve
			GetQuoteNextLineApprover = "Already approved"
		Else ' Find someone who can
			' Get the most recent person to approve
			Set rs = Server.CreateObject("ADODB.RecordSet")
			sql = "Select Top 1 Code From QuoteApproval Where Qid = " & lngQid & " Order By QuoteApprovalId Desc"
			Set rs = dbConn.Execute(sql)
			
			If Not(rs.BOF And rs.EOF) Then
				strPreviousLineApproverCode = rs("Code")
				If strPreviousLineApproverCode = strLastLineApproverCode Then
					strQuoteNextLineApprover = "Already Approved"
				Else
					'Do Until i = 1
						If Not(rs.BOF And rs.EOF) Then
							Set rsUser = Server.CreateObject("ADODB.RecordSet")
							sql = "Select * From Users Where Deleted = 0 AND Code = '" & GetLineManagerCode(strPreviousLineApproverCode) & "'"
							Set rsUser = dbConn.Execute(sql)
							If Not(rsUser.BOF And rsUser.EOF) Then
								strQuoteNextLineApproverCode = rsUser("Code")
							End If
							rsUser.Close
							Set rsUser = Nothing
						End If
						rs.Close
						Set rs = Nothing
					'Loop
				End If
			Else
				Set rsQuote = Server.CreateObject("ADODB.RecordSet")
				sql = "Select Code From Quotes Where Qid = " & lngQid
				Set rsQuote = dbConn.Execute(sql)
				If Not(rsQuote.BOF And rsQuote.EOF) Then
					strQuoteNextLineApproverCode = GetLineManagerCode(rsQuote("Code"))
				End If
				rsQuote.Close
				Set rsQuote = Nothing
			End If
		End If
	End If
	If strQuoteNextLineApprover <> "" Then
		GetQuoteNextLineApprover = strQuoteNextLineApprover
	Else
		strQuoteNextLineApprover = "Unknown"
	End If
End Function

Function GetQuoteLastLineApprover(lngQid)
	Dim rsQuote
	Dim sql
	Dim decQuoteApprovalLimit
	Dim decNettPriceTotal
	Dim strQuoteLastLineApprover
	' Get the Quote
	Set rsQuote = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Quotes.*, UserRoles.QuoteApprovalLimit, Users.Name FROM (Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where Qid = " & lngQid
	Set rsQuote = dbConn.Execute(sql)
	If Not(rsQuote.BOF And rsQuote.EOF) Then
		decQuoteApprovalLimit = rsQuote("QuoteApprovalLimit")
		decNettPriceTotal = rsQuote("NettPriceTotal")
		If decNettPriceTotal <= decQuoteApprovalLimit Then ' Quote creator can approve
			strQuoteLastLineApprover = "Already approved"
		Else
			strQuoteLastLineApprover = GetQuoteLastLineApprover_Find(rsQuote("NettPriceTotal"), rsQuote("Code"))
		End If
	Else
		strQuoteLastLineApprover = "An error occurred"
	End If
	rsQuote.Close
	Set rsQuote = Nothing
	GetQuoteLastLineApprover = strQuoteLastLineApprover
End Function

Function GetQuoteLastLineApprover_Find(decNettPriceTotal, strCode)
	Dim rsUser
	Dim rsUser2
	Dim sql
	Dim strLineManagerCode
	Dim decQuoteApprovalLimit
	' Get user's line manager
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & strCode & "'"
	Set rsUser = dbConn.Execute(sql)
	If Not(rsUser.BOF and rsUser.EOF) Then
		strLineManagerCode = rsUser("LineManagerCode")
		decQuoteApprovalLimit = rsUser("QuoteApprovalLimit")
		If CDbl(decNettPriceTotal) =< CDbl(decQuoteApprovalLimit) Then
			GetQuoteLastLineApprover_Find = GetUserFullName(rsUser("Code"))
			Exit Function
		Else
			GetQuoteLastLineApprover_Find = GetQuoteLastLineApprover_Find(decNettPriceTotal, rsUser("LineManagerCode"))
		End If
	Else
		GetQuoteLastLineApprover_Find = "Unknown"
	End If
	rsUser.Close
	Set rsUser = Nothing
End Function

Function GetUserFullName(strCode)
	Dim rsUser
	Dim sql
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select [Name] From Users Where Code = '" & strCode & "'"
	Set rsUser = dbConn.Execute(sql)
	If Not(rsUser.BOF And rsUser.EOF) Then
		GetUserFullName = rsUser("Name")
	End If
	rsUser.Close
	Set rsUser = Nothing
	If GetUserFullName = "" Then GetUserFullName = "Unknown"
End Function

Function GetAccessCodesList(strCode, lngUserTypeId)
	Dim rsGetDiv
	Dim strAccessCodesList
	sql = "Select DivisionId From Users Where Code = '" & strCode & "'"
	Set rsGetDiv = dbConn.Execute(sql)
	If Request.Cookies("ClientSettings")("Prefix") = "TT" And CInt(rsGetDiv("DivisionId")) = 1 Then
		strAccessCodesList = GetForSignalsDivision(strCode, lngUserTypeId)
	ElseIf (lngUserTypeId = 4 Or lngUserTypeId = 5 Or lngUserTypeId = 6) Or (Request.Cookies("ClientSettings")("Prefix") <> "TT") Then ' Override - Allow users access to all records
		Dim rsUser
		Dim sql
		Dim strCodes
		Set rsUser = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From Users Where Deleted = 0 AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager") & ") Order By Code"
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
	Dim strAccessCodesList
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	If lngUserTypeId = 5 Or lngUserTypeId = 6 Then
		sql = "Select * From Users"
	Else
		sql = "Select * From Users Where Deleted = 0 AND DivisionId = 1 Or (LineManagerCode = '" & strCode & "' And Code <> '" & strCode & "') Or UserId In (Select UserId From UsersAccess Where DivisionId = 1 And MemberOf = True)"
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

Function GetKids(strCode)
	Dim rsUser
	Dim sql
	Dim strCodes
	Dim strAccessCodesList
	Dim strKids
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND LineManagerCode = '" & strCode & "' And Code <> '" & strCode & "' Order By Code"
	Set rsUser = dbConn.Execute(sql)
	If Not(rsUser.BOF And rsUser.EOF) Then
		Do Until rsUser.EOF
			strCodes = ""
			strKids = ""
			strCodes = "'" & rsUser("Code") & "'," & strCodes
			GetKids(rsUser("Code"))
			If strKids <> "" Then strCodes = strCodes & "," & strKids
			strAccessCodesList = strAccessCodesList & "," & strCodes
			rsUser.MoveNext
		Loop
	End If
	rsUser.Close
	Set rsUser = Nothing
	GetKids = strAccessCodesList
End Function

Function GetUserContactDetails(lngDivisionId, strCode)
	If lngDivisionId = 7 And strCode = "MD0029" Then
		s = "<br><br><br>" &_
			"<b>Best Regards,</b><br><br>" &_
			"<span style='font-size:14pt;'>James Hopping</span><br>" &_
			"<span style='color:#999999'>" &_
			"<span style='font-size:11pt;'>Manager<br>" &_
			"George Industries Traffic<br><br>" &_
			"t: 61 7 3271 2866<br>" &_
			"f: 61 7 3271 2152<br>" &_
			"m: 0414 921 999<br>" &_
			"e: jamesh@georgetraffic.com.au<br><br>" &_
			"</span>" &_
			"</span>" &_
			"<img src='" & GetProtocol() & Request.ServerVariables("SERVER_NAME") & Request.Cookies("ClientSettings")("WorkingDir") & "/Images/Logo_GIT2.jpg' border=0 alt='George Industries Traffic'>" &_
			"<br><br>" &_
			"<span style='color:#999999'>" &_
			"57 Campbell Ave Wacol QLD 4076<br>" &_
			"<a href='http://www.georgeindustries.com.au' target='_Blank' style='color:#999999'>www.georgeindustries.com.au</a></span>" &_
			"</span>"
	Else
		Dim rs
		Dim sql
		Dim s
		Set rs = Server.CreateObject("ADODB.RecordSet")
		If lngDivisionId <> 0 Then
			sql = "SELECT Divisions.*,Users.*,Locations.*,Users.Email AS userEmail FROM Divisions, Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId WHERE Divisions.DivisionId=" & lngDivisionId & " AND Users.Code='" & strCode & "'"
			'Response.Write sql
			'Response.End
		Else
			sql = "SELECT Users.*, Locations.Phone AS lPhone, Locations.Fax AS lFax, Divisions.* FROM Divisions INNER JOIN (Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId) ON Divisions.DivisionId = Users.DivisionId WHERE (((Users.Code)='" & strCode & "'))"
		End If
		Set rs = dbConn.Execute(sql)
		If Not(rs.BOF And rs.EOF) Then
			If rs("Phone")&"" <> "" Then strPhone = rs("Phone") Else strPhone = rs("lPhone")
			If rs("Fax")&"" <> "" Then strFax = rs("Fax") Else strFax = rs("lFax")
			s = s & "					<table cellpadding=2 cellspacing=0 border=0>" & vbcrlf
			s = s & "						<tr>" & vbcrlf
			s = s & "							<td colspan=2>" & vbcrlf
			s = s & "							Regards,<br><br>" & vbcrlf
			s = s & "							" & rs("Name") & "<br>" & vbcrlf
			s = s & "							" & rs("Position") & "<br>" & vbcrlf
			s = s & "							" &  rs("Division") & "<br><br>" & vbcrlf
			s = s & "							</td>" & vbcrlf
			s = s & "						</tr>" & vbcrlf
			s = s & "						<tr>" & vbcrlf
			s = s & "							<td width=50 style=""width:50px;""><b>Phone: </b></td>" & vbcrlf
			s = s & "							<td>" & strPhone & "</td>" & vbcrlf
			s = s & "						</tr>" & vbcrlf
			If rs("Mobile")&"" <> "" Then
				s = s & "						<tr>" & vbcrlf
				s = s & "							<td width=50 style=""width:50px;""><b>Mobile: </b></td>" & vbcrlf
				s = s & "							<td>" &  rs("Mobile") & "</td>" & vbcrlf
				s = s & "						</tr>" & vbcrlf
			End If
			s = s & "						<tr>" & vbcrlf
			s = s & "							<td width=50 style=""width:50px;""><b>Email: </b></td>" & vbcrlf
			s = s & "							<td>" & rs("userEmail") & "</td>" & vbcrlf
			s = s & "						</tr>" & vbcrlf
			s = s & "					</table>" & vbcrlf
			s = s & "					<table cellpadding=2 cellspacing=0 border=0>" & vbcrlf
			s = s & "						<tr>" & vbcrlf
			s = s & "							<td style=""font-size:12px;"" colspan=2><img src=""" & GetProtocol() & Request.ServerVariables("SERVER_NAME") & Request.Cookies("ClientSettings")("WorkingDir") & "/Images/" & Replace(rs("Logo"),".","_footer.") & """></td>" & vbcrlf
			s = s & "						</tr>" & vbcrlf
			s = s & "					</table>" & vbcrlf
		End If
	End If
	GetUserContactDetails = s
End Function

Function GetUserContactDetailsFax(lngDivisionId, strCode)
	If lngDivisionId = 7 And strCode = "MD0029" Then
		s = "<br><br><br>" &_
			"<b>Best Regards,</b><br><br>" &_
			"<span style='font-size:14pt;'>James Hopping</span><br>" &_
			"<span style='color:#999999'>" &_
			"<span style='font-size:11pt;'>Manager<br>" &_
			"George Industries Traffic<br><br>" &_
			"t: 61 7 3271 2866<br>" &_
			"f: 61 7 3271 2152<br>" &_
			"m: 0414 921 999<br>" &_
			"e: jamesh@georgetraffic.com.au<br><br>" &_
			"</span>" &_
			"</span>" &_
			"<span style='color:#999999'>" &_
			"57 Campbell Ave Wacol QLD 4076<br>" &_
			"<a href='http://www.georgeindustries.com.au' target='_Blank' style='color:#999999'>www.georgeindustries.com.au</a></span>" &_
			"</span>"
	Else
		Dim rs
		Dim sql
		Dim s
		Set rs = Server.CreateObject("ADODB.RecordSet")
		If lngDivisionId <> 0 Then
			sql = "SELECT Divisions.*,Users.*,Locations.*,Users.Email AS userEmail FROM Divisions, Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId WHERE Divisions.DivisionId=" & lngDivisionId & " AND Users.Code='" & strCode & "'"
			'sql = "SELECT * FROM Divisions, Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId WHERE Divisions.DivisionId=" & lngDivisionId & " AND Users.Code='" & strCode & "'"
		Else
			sql = "SELECT Users.*, Locations.Phone AS lPhone, Locations.Fax AS lFax, Divisions.* FROM Divisions INNER JOIN (Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId) ON Divisions.DivisionId = Users.DivisionId WHERE (((Users.Code)='" & strCode & "'))"
		End If
		Set rs = dbConn.Execute(sql)
		If Not(rs.BOF And rs.EOF) Then
			If rs("Phone")&"" <> "" Then strPhone = rs("Phone") Else strPhone = rs("lPhone")
			If rs("Fax")&"" <> "" Then strFax = rs("Fax") Else strFax = rs("lFax")
			s = s & "					<table bgcolor=""#ffffff"" cellpadding=2 cellspacing=0 border=0>" & vbcrlf
			s = s & "						<tr>" & vbcrlf
			s = s & "							<td colspan=2 style=""font-size:12px;"">" & vbcrlf
			s = s & "							<b>" & rs("Name") & "</b><br>" & vbcrlf
			s = s & "							" & rs("Position") & "<br>" & vbcrlf
			s = s & "							" &  rs("Division") & "<br><br>" & vbcrlf
			s = s & "							</td>" & vbcrlf
			s = s & "						</tr>" & vbcrlf
			If rs("Phone")&"" <> "" Then
				s = s & "						<tr>" & vbcrlf
				s = s & "							<td style=""font-size:12px;""><b>Phone:</b> " &  rs("Phone") & "</td>" & vbcrlf
				s = s & "						</tr>" & vbcrlf
			End If
			If rs("Mobile")&"" <> "" Then
				s = s & "						<tr>" & vbcrlf
				s = s & "							<td style=""font-size:12px;""><b>Mobile:</b> " &  rs("Mobile") & "</td>" & vbcrlf
				s = s & "						</tr>" & vbcrlf
			End If
			s = s & "						<tr>" & vbcrlf
			s = s & "							<td style=""font-size:12px;""><b>Email:</b> <a href=""mailto:" & rs("userEmail") & """>" & rs("userEmail") & "</a></td>" & vbcrlf
			s = s & "						</tr>" & vbcrlf
	'		s = s & "						<tr>" & vbcrlf
	'		s = s & "							<td style=""font-size:12px;""><img src=""" & GetProtocol() & Request.ServerVariables("SERVER_NAME") & "/" & Request.Cookies("ClientSettings")("WorkingDir") & "/Images/" & Replace(rs("Logo"),".gif","_footer.gif") & """></td>" & vbcrlf
	'		s = s & "						</tr>" & vbcrlf
			s = s & "					</table>" & vbcrlf
		End If
	End If
	GetUserContactDetailsFax = s
End Function

Function SearchArray(arrArray, strFind)
	If Not IsArray(arrArray) Then
		arrArray = Split(arrArray, ",")
	End If

	Dim i
	Dim boolFind
	i = 0
	boolFind = False
	If Not IsEmpty(arrArray) Then
		For i = 0 To UBound(arrArray)
			If CStr(Trim(arrArray(i))) = CStr(Trim(strFind&"")) Then
				boolFind = True
				Exit For
			End If
		Next
	End If
	SearchArray = boolFind
End Function

Sub SetWorkingDir(strUrl)
	strPath = Request.ServerVariables("Url")
	intSalesEngine = InStr(strPath, "SalesEngine")
	strPath = Mid(strPath, 1, intSalesEngine+12)
	Session("WorkingDir") = strPath
	Session("State") = "NA"
	Session("Prefix") = Right(strPath,2)
	Response.Cookies("WorkingDir") = strPath
	Response.Cookies("WorkingDir").Expires = Date() + 1
	Response.Cookies("ClientSettings")("State") = "NA"
	Response.Cookies("ClientSettings")("Prefix") = Right(strPath,2)
	Response.Cookies("ClientSettings").Expires = Date() + 1
End Sub

Function DisplayLocationAddress(strAddress1, strAddress2, strSuburb, strState, strPostCode, strCountry, boolPODisplay, strPOAddress1, strPOAddress2, strPOSuburb, strPOState, strPOPostCode, strPOCountry)
	Dim strA
	If strState = "Other" Then
		strState = ""
	End If
	If strPOState = "Other" Then
		strPOState = ""
	End If
    If Trim(rsLoc("Address1")) <> "" Then
	    strA = rsLoc("Address1") & "<br>" & vbclrf
	    If strAddress2 <> "" Then
		    strA = strA & strAddress2 & "<br>"
	    End If
	    strA = strA & strSuburb & ", " & strState & " " & strPostCode & "<br>" & vbcrlf
	    strA = strA & strCountry & vbcrlf
		strA = strA & "<br><br>" & vbcrlf
    End If
	If boolPODisplay Then
		strA = strA & strPOAddress1 & "<br>"
		If strPOAddress2 <> "" Then
			strA = strA & strPOAddress2 & "<br>"
		End If
		strA = strA & strPOSuburb & ", " & strPOState & " " & strPOPostCode & "<br>"
		strA = strA & strPOCountry
	End If
	DisplayLocationAddress = strA
End Function

Function IsLineManagerOf(strCode, strLineManagerCode)
	Dim sql
	Dim rsILM
	sql = "Select LineManagerCode From Users Where Code = '" & strCode & "'"
	Set rsILM = dbConn.Execute(sql)
	If Not(rsILM.BOF And rsILM.EOF) Then
		If rsILM("LineManagerCode") = strLineManagerCode Then
			IsLineManagerOf = True
			Exit Function
		ElseIf rsILM("LineManagerCode") = "" Then
			IsLineManagerOf = False
			Exit Function
		Else ' Try again
			IsLineManagerOf = IsLineManagerOf(rsILM("LineManagerCode"), strLineManagerCode)
		End If
	End If
	IsLineManager = False
End Function

Function QuoteDetails_ForEmail(lngQid)
	Dim sql
	Dim rsQuote
	Dim strQuoteDetails
	Dim strCompany
	sql = "Select * From Quotes Inner Join Contacts_WithCustomersAndSuppliers_V2 On Contacts_WithCustomersAndSuppliers_V2.ContactId = Quotes.ContactId Where Qid = " & lngQid
	Set rsQuote = dbConn.Execute(sql)
	If Not (rsQuote.BOF And rsQuote.EOF) Then
		strCompany = rsQuote("CompanyName")
		If strCompany = "" Then strCompany = "Non account customer"
		strQuoteDetails = VbCrLf & VbCrLf
		strQuoteDetails = strQuoteDetails & "All prices are ex. GST" & VbCrLf & VbCrLf
		strQuoteDetails = strQuoteDetails & "Customer: " & strCompany & VbCrLf
		strQuoteDetails = strQuoteDetails & "Project: " & rsQuote("Reference") & VbCrLf
		strQuoteDetails = strQuoteDetails & "Total Value: " & FormatCurrency(rsQuote("NettPriceTotal"),2) & VbCrLf
		sql = "Select * From QuoteContents Where Qid = " & lngQid & " Order By QuoteItemId"
		Set rsQuoteC = dbConn.Execute(sql)
		If Not(rsQuoteC.BOF And rsQuoteC.EOF) Then
			strQuoteDetails = strQuoteDetails & VbCrLf & "Quote Items:" & VbCrLf
			Do Until rsQuoteC.EOF
				If rsQuoteC("Units") <> 0 Then
					strQuoteDetails = strQuoteDetails & Replace(rsQuoteC("Units") & " units x " & rsQuoteC("Days") & " days x " & rsQuoteC("Description") & " @ " & FormatCurrency(rsQuoteC("NettPrice"),2), vbcrlf, "") & VbCrLf 
				Else
					strQuoteDetails = strQuoteDetails & Replace(rsQuoteC("Quantity") & " x " & rsQuoteC("Description") & " @ " & FormatCurrency(rsQuoteC("NettPrice"),2), vbcrlf, "") & VbCrLf
				End If
				rsQuoteC.MoveNext
			Loop
		End If
		rsQuoteC.Close
		Set rsQuoteC = Nothing
	End If
	rsQuote.Close
	Set rsQuote = Nothing
	QuoteDetails_ForEmail = strQuoteDetails
End Function

Function PurchaseOrderDetails_ForEmail(lngPOid)
	Dim sql
	Dim rsPO
	Dim strPODetails
	Dim strCompany
	sql = "Select * From PurchaseOrders Inner Join Contacts_WithCustomersAndSuppliers_V2 On Contacts_WithCustomersAndSuppliers_V2.ContactId = PurchaseOrders.ContactId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	If Not (rsPO.BOF And rsPO.EOF) Then
		strCompany = rsPO("CompanyName")
		If strCompany = "" Then strCompany = "Non account supplier"
		strPODetails = VbCrLf & VbCrLf
		strPODetails = strPODetails & "All prices are ex. GST" & VbCrLf & VbCrLf
		strPODetails = strPODetails & "Supplier: " & strCompany & VbCrLf
		strPODetails = strPODetails & "Project: " & rsPO("Project") & VbCrLf
		strPODetails = strPODetails & "Total Value: " & FormatCurrency(rsPO("PriceExTotal"),2) & VbCrLf
		sql = "Select * From PurchaseOrderContents Where POid = " & lngPOid & " Order By POItemId"
		Set rsPOC = dbConn.Execute(sql)
		If Not(rsPOC.BOF And rsPOC.EOF) Then
			strPODetails = strPODetails & VbCrLf & "PO Items:" & VbCrLf
			Do Until rsPOC.EOF
				strPODetails = strPODetails & Replace(rsPOC("Quantity") & " x " & rsPOC("Description") & " @ " & FormatCurrency(rsPOC("PriceEx"),2), vbcrlf, "") & VbCrLf 
				rsPOC.MoveNext
			Loop
		End If
		rsPOC.Close
		Set rsPOC = Nothing
	End If
	rsPO.Close
	Set rsPO = Nothing
	PurchaseOrderDetails_ForEmail = strPODetails
End Function

        Function GetNodePath(categoryId)
        Dim s
        Dim n
        Dim i
        Dim category
        i = 0
        Set rscu = Server.CreateObject("ADODB.RecordSet")
        sql = "Select Category from FilesCategories Where CategoryId = " & categoryId
        Set rscu = dbConn.Execute(sql)
        If Not (rscu.BOF and rscu.EOF) Then
            category = rscu("Category")
        End If
        n = "start"
        Do Until n = "" Or i = 3
            Set rsfc = Server.CreateObject("ADODB.RecordSet")
            sql = "Select ParentCategoryId from FilesCategories Where CategoryId = " & categoryId
            Set rsfc = dbConn.Execute(sql)
            If Not (rsfc.BOF And rsfc.EOF) Then
                n = GetParentCategory(rsfc("ParentCategoryId"))
                s = n & "/" & s
                categoryId = rsfc("ParentCategoryId")
            End If
            i = i + 1
        Loop
        s = s & category
        If Left(s,1) <> "/" Then s = "/" & s
        GetNodePath = s
    End Function

    Function GetParentCategory(parentCategoryId)
        If parentCategoryId <> "" Then
        Set rsnp = Server.CreateObject("ADODB.RecordSet")
        sql = "Select Category from FilesCategories Where CategoryId = " & parentCategoryId
        Set rsnp = dbConn.Execute(sql)
        If Not(rsnp.BOF And rsnp.EOF) Then
            GetParentCategory = rsnp("Category")
        Else
            GetParentCategory = ""
        End If
        rsnp.Close
        Set rsnp = Nothing
        End If
    End Function

 Function IconImage(WhatFile)
	Dim s
    If InStr(LCase(WhatFile),".pdf") > 0 Then
        s = s & "/images/icons/acrobat.gif"
    ElseIf InStr(LCase(WhatFile),".mdb") > 0 Then
        s = s & "/images/icons/access.gif"
    ElseIf InStr(LCase(WhatFile),".xls") > 0 Or InStr(LCase(WhatFile),".xlsx") > 0 Then
        s = s & "/images/icons/excel.gif"
    ElseIf InStr(LCase(WhatFile),".doc") > 0 Or InStr(LCase(WhatFile),".docx") > 0 Then
        s = s & "/images/icons/word.gif"
    ElseIf InStr(LCase(WhatFile),".swf") > 0 Then
        s = s & "/images/icons/flash.gif"
    ElseIf InStr(LCase(WhatFile),".rtf") > 0 Then
        s = s & "/images/icons/rtf.gif"
    ElseIf InStr(LCase(WhatFile),".txt") > 0 Then
        s = s & "/images/icons/text.gif"
    ElseIf InStr(LCase(WhatFile),".zip") > 0 Then
        s = s & "/images/icons/zip.gif"
    ElseIf InStr(LCase(WhatFile),".mpp") > 0 Then
        s = s & "/images/icons/project.gif"
    ElseIf InStr(LCase(WhatFile),".jpg") > 0 Or InStr(LCase(WhatFile),".tiff") > 0 Or InStr(LCase(WhatFile),".jpeg") > 0 Or InStr(LCase(WhatFile),".gif") > 0 Then
        s = s & "/images/icons/image.gif"
    Else
        s = s & "/images/icons/text.gif"
    End If
    IconImage = s
  end function

%>