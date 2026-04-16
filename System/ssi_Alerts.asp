<%

Function AlertPurchasingManager(lngDivisionId, strSubject, strMessage)
	On Error Resume Next
	
	Dim sql, rs, strCode, strEmail, serverURL
	
	' Validate inputs
	If IsNull(lngDivisionId) Or Not IsNumeric(lngDivisionId) Then
		Exit Function
	End If
	
	If IsNull(strSubject) Or strSubject = "" Then
		strSubject = "MyDesk Alert"
	End If
	
	If IsNull(strMessage) Or strMessage = "" Then
		strMessage = ""
	End If
	
	lngDivisionId = CLng(lngDivisionId)
	
	' Check if running on dev environment
	serverURL = ""
	On Error Resume Next
	serverURL = LCase(Request.ServerVariables("URL"))
	Err.Clear
	On Error GoTo 0
	
	sql = "Select * From Divisions Where DivisionId = " & lngDivisionId
	Set rs = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rs.BOF And rs.EOF) Then
	    strCode = rs("PurchasingManagerCode")
	End If
	
	rs.Close
	Set rs = Nothing
	
	If InStr(serverURL, "dev.") > 0 Then strCode = "MD0025"
	
	If strCode <> "" Then
	    sql = "Select * From Users Where Deleted = 0 AND Code = '" & Replace(strCode, "'", "''") & "'"
	    Set rs = dbConn.Execute(sql)
	    
	    If Err.Number = 0 And Not(rs.BOF And rs.EOF) Then
		    strEmail = rs("Email")
		    If strEmail <> "" Then
			    SendMail "admin@mydesk.com.au", strEmail, "MyDesk Alert : " & strSubject, strMessage
		    End If
	    End If
	    
	    rs.Close
	    Set rs = Nothing
	End If
	
	Err.Clear
	On Error GoTo 0
End Function

%>