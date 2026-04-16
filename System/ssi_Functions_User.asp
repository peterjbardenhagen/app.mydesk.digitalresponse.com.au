<%

Function CheckIfAdmin(intUserId)
	On Error Resume Next
	
	Dim rsChk, sql
	
	' Validate input
	If IsNull(intUserId) Or Not IsNumeric(intUserId) Then
		CheckIfAdmin = False
		Exit Function
	End If
	
	Set rsChk = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From UsersAccess Where Manager = 1 And UserId = " & CLng(intUserId)
	Set rsChk = dbConn.Execute(sql)
	
	If Err.Number = 0 Then
		If Not(rsChk.BOF And rsChk.EOF) Then
			CheckIfAdmin = True
		Else
			CheckIfAdmin = False
		End If
	Else
		CheckIfAdmin = False
	End If
	
	rsChk.Close
	Set rsChk = Nothing
	Err.Clear
	On Error GoTo 0
End Function

Function GetUserFullName(strCode)
	On Error Resume Next
	
	Dim rsUser, sql
	
	' Validate input
	If IsNull(strCode) Or strCode = "" Then
		GetUserFullName = "Unknown"
		Exit Function
	End If
	
	strCode = Replace(strCode, "'", "''")
	
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select [Name] From Users Where Code = '" & strCode & "'"
	Set rsUser = dbConn.Execute(sql)
	
	If Err.Number = 0 Then
		If Not(rsUser.BOF And rsUser.EOF) Then
			GetUserFullName = rsUser("Name")
		Else
			GetUserFullName = "Unknown"
		End If
	Else
		GetUserFullName = "Unknown"
	End If
	
	rsUser.Close
	Set rsUser = Nothing
	If GetUserFullName = "" Then GetUserFullName = "Unknown"
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetAccessCodesList(strCode, lngUserTypeId)
	On Error Resume Next
	
	Dim rsGetDiv, strAccessCodesList, rsUser, strCodes, cookiePrefix, divisionIdsManager
	
	' Validate inputs
	If IsNull(strCode) Or strCode = "" Then
		GetAccessCodesList = "'No Codes'"
		Exit Function
	End If
	
	If IsNull(lngUserTypeId) Or Not IsNumeric(lngUserTypeId) Then
		lngUserTypeId = 0
	Else
		lngUserTypeId = CLng(lngUserTypeId)
	End If
	
	strCode = Replace(strCode, "'", "''")
	strAccessCodesList = ""
	
	Set rsGetDiv = Server.CreateObject("ADODB.RecordSet")
	sql = "Select DivisionId From Users Where Code = '" & strCode & "'"
	Set rsGetDiv = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rsGetDiv.BOF And rsGetDiv.EOF) Then
		' Get cookie value with error handling
		cookiePrefix = ""
		On Error Resume Next
		If Not Request.Cookies("ClientSettings") Is Nothing Then
			cookiePrefix = Request.Cookies("ClientSettings")("Prefix")
		End If
		Err.Clear
		On Error GoTo 0
		
		If cookiePrefix = "TT" Then
			If IsNumeric(rsGetDiv("DivisionId")) And CInt(rsGetDiv("DivisionId")) = 1 Then
				strAccessCodesList = GetForSignalsDivision(strCode, lngUserTypeId)
			End If
		End If
	End If
	
	rsGetDiv.Close
	Set rsGetDiv = Nothing
	
	' Override - Allow users access to all records
	If strAccessCodesList = "" And (lngUserTypeId = 4 Or lngUserTypeId = 5 Or lngUserTypeId = 6 Or cookiePrefix <> "TT") Then
		' Get division IDs with error handling
		divisionIdsManager = ""
		On Error Resume Next
		If Not Request.Cookies("DivisionIdsAccess") Is Nothing Then
			divisionIdsManager = Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager")
		End If
		Err.Clear
		On Error GoTo 0
		
		If divisionIdsManager <> "" Then
			Set rsUser = Server.CreateObject("ADODB.RecordSet")
			sql = "Select * From Users Where Deleted = 0 AND DivisionId In (" & divisionIdsManager & ") Order By Code"
			Set rsUser = dbConn.Execute(sql)
			
			If Err.Number = 0 Then
				If Not(rsUser.BOF And rsUser.EOF) Then
					Do Until rsUser.EOF
						strCodes = ""
						strCodes = "'" & rsUser("Code") & "'," & strCodes
						strAccessCodesList = strAccessCodesList & "," & strCodes
						rsUser.MoveNext
					Loop
				End If
			End If
			
			rsUser.Close
			Set rsUser = Nothing
			Err.Clear
		End If
		
		strAccessCodesList = strAccessCodesList & GetKids(strCode)
	End If
	
	If strAccessCodesList = "" Then
		strAccessCodesList = GetKids(strCode)
	End If
	
	strAccessCodesList = Replace(Replace(strAccessCodesList, ",,", ", "), " ", "")
	If Left(strAccessCodesList, 1) = "," Then strAccessCodesList = Right(strAccessCodesList, Len(strAccessCodesList)-1)
	If Right(strAccessCodesList, 1) = "," Then strAccessCodesList = Left(strAccessCodesList, Len(strAccessCodesList)-1)
	If strAccessCodesList = "" Then strAccessCodesList = "'No Codes'"
	GetAccessCodesList = "'" & strCode & "', " & strAccessCodesList
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetForSignalsDivision(strCode, lngUserTypeId)
	On Error Resume Next
	
	Dim rsUser, sql, strCodes, strAccessCodesList
	
	' Validate inputs
	If IsNull(strCode) Or strCode = "" Then
		GetForSignalsDivision = ""
		Exit Function
	End If
	
	If IsNull(lngUserTypeId) Or Not IsNumeric(lngUserTypeId) Then
		lngUserTypeId = 0
	Else
		lngUserTypeId = CLng(lngUserTypeId)
	End If
	
	strAccessCodesList = ""
	
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	If lngUserTypeId = 5 Or lngUserTypeId = 6 Then
		sql = "Select * From Users"
	Else
		sql = "Select * From Users Where Deleted = 0 AND DivisionId = 1 Or (LineManagerCode = '" & Replace(strCode, "'", "''") & "' And Code <> '" & Replace(strCode, "'", "''") & "') Or UserId In (Select UserId From UsersAccess Where DivisionId = 1 And MemberOf = True)"
	End If
	Set rsUser = dbConn.Execute(sql)
	
	If Err.Number = 0 Then
		If Not(rsUser.BOF And rsUser.EOF) Then
			Do Until rsUser.EOF
				strAccessCodesList = strAccessCodesList & "," & "'" & rsUser("Code") & "',"
				rsUser.MoveNext
			Loop
		End If
	End If
	
	rsUser.Close
	Set rsUser = Nothing
	GetForSignalsDivision = strAccessCodesList
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetKids(strCode)
	On Error Resume Next
	
	Dim rsUser, sql, strCodes, strAccessCodesList, strKids, maxDepth, currentDepth
	
	' Validate input
	If IsNull(strCode) Or strCode = "" Then
		GetKids = ""
		Exit Function
	End If
	
	' Add depth limit to prevent infinite recursion
	maxDepth = 10
	currentDepth = 0
	
	strAccessCodesList = ""
	
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND LineManagerCode = '" & Replace(strCode, "'", "''") & "' And Code <> '" & Replace(strCode, "'", "''") & "' Order By Code"
	Set rsUser = dbConn.Execute(sql)
	
	If Err.Number = 0 Then
		If Not(rsUser.BOF And rsUser.EOF) Then
			Do Until rsUser.EOF
				strCodes = ""
				strKids = ""
				strCodes = "'" & rsUser("Code") & "'," & strCodes
				
				If currentDepth < maxDepth Then
					currentDepth = currentDepth + 1
					strKids = GetKids(rsUser("Code"))
					currentDepth = currentDepth - 1
				End If
				
				If strKids <> "" Then strCodes = strCodes & "," & strKids
				strAccessCodesList = strAccessCodesList & "," & strCodes
				rsUser.MoveNext
			Loop
		End If
	End If
	
	rsUser.Close
	Set rsUser = Nothing
	GetKids = strAccessCodesList
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetLineManagerCode(strCode)
	On Error Resume Next
	
	Dim rs, sql
	
	' Validate input
	If IsNull(strCode) Or strCode = "" Then
		GetLineManagerCode = ""
		Exit Function
	End If
	
	strCode = Replace(strCode, "'", "''")
	
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "Select LineManagerCode From Users Where Code = '" & strCode & "'"
	Set rs = dbConn.Execute(sql)
	
	If Err.Number = 0 Then
		If Not(rs.BOF And rs.EOF) Then
			GetLineManagerCode = rs("LineManagerCode")
		Else
			GetLineManagerCode = ""
		End If
	Else
		GetLineManagerCode = ""
	End If
	
	rs.Close
	Set rs = Nothing
	Err.Clear
	On Error GoTo 0
End Function

Function CheckForLine(strCode, strLineManagerCode, lngId, boolQuote, boolPO)
	CheckForLine = False
End Function

Function IsLineManagerOf(strCode, strLineManagerCode)
	On Error Resume Next
	
	Dim sql, rsILM, maxDepth, currentDepth
	
	' Validate input
	If IsNull(strCode) Or strCode = "" Then
		IsLineManagerOf = False
		Exit Function
	End If
	
	If IsNull(strLineManagerCode) Or strLineManagerCode = "" Then
		IsLineManagerOf = False
		Exit Function
	End If
	
	' Add depth limit to prevent infinite recursion
	maxDepth = 10
	currentDepth = 0
	
	sql = "Select LineManagerCode From Users Where Code = '" & Replace(strCode, "'", "''") & "'"
	Set rsILM = dbConn.Execute(sql)
	
	If Err.Number = 0 Then
		If Not(rsILM.BOF And rsILM.EOF) Then
			If rsILM("LineManagerCode") = strLineManagerCode Then
				IsLineManagerOf = True
			ElseIf rsILM("LineManagerCode") = "" Then
				IsLineManagerOf = False
			ElseIf currentDepth < maxDepth Then
				currentDepth = currentDepth + 1
				IsLineManagerOf = IsLineManagerOf(rsILM("LineManagerCode"), strLineManagerCode)
			Else
				IsLineManagerOf = False
			End If
		Else
			IsLineManagerOf = False
		End If
	Else
		IsLineManagerOf = False
	End If
	
	rsILM.Close
	Set rsILM = Nothing
	Err.Clear
	On Error GoTo 0
End Function

Function GetHoursPerWeek(strCode)
	On Error Resume Next
	
	Dim intI, sqlHrs, rsHrs
	
	' Validate input
	If IsNull(strCode) Or strCode = "" Then
		GetHoursPerWeek = 38
		Exit Function
	End If
	
	strCode = Replace(strCode, "'", "''")
	
	Set rsHrs = dbConn.Execute("Select HoursPerWeek From Users Where Code = '" & strCode & "'")
	
	If Err.Number = 0 Then
		If Not (rsHrs.BOF And rsHrs.EOF) Then
			If IsNumeric(rsHrs("HoursPerWeek")) Then
				intI = CLng(rsHrs("HoursPerWeek"))
			Else
				intI = 38
			End If
		Else
			intI = 38
		End If
	Else
		intI = 38
	End If
	
	rsHrs.Close
	Set rsHrs = Nothing
	GetHoursPerWeek = intI
	
	Err.Clear
	On Error GoTo 0
End Function

%>
