<%

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

Function GetLineManagerCode(strCode)
	Dim rs
	Dim sql
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "Select LineManagerCode From Users Where Code = '" & strCode & "'"
	Set rs = dbConn.Execute(sql)
	If Not(rs.BOF And rs.EOF) Then
		GetLineManagerCode = rs("LineManagerCode")
	Else
		GetLineManagerCode = ""
	End If
End Function

Function CheckForLine(strCode, strLineManagerCode, lngId, boolQuote, boolPO)
	CheckForLine = False
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

Function GetHoursPerWeek(strCode)
	Dim intI
	Dim sqlHrs
	Dim rsHrs
	Set rsHrs = dbConn.Execute("Select HoursPerWeek From Users Where Code = '" & strCode & "'")
	If Not (rsHrs.BOF And rsHrs.EOF) Then
		intI = rsHrs("HoursPerWeek")
	Else
		intI = 38
	End If
	GetHoursPerWeek = intI
End Function

%>
