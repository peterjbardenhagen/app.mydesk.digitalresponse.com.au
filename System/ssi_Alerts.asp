<%

Function AlertPurchasingManager(lngDivisionId, strSubject, strMessage)
    Dim sql
    Dim rs
    sql = "Select * From Divisions Where DivisionId = " & lngDivisionId
    Set rs = dbConn.Execute(sql)
    If Not(rs.BOF And rs.EOF) Then
	    strCode = rs("PurchasingManagerCode")
    End If
    rs.Close
    Set rs = Nothing
    If InStr(Request.ServerVariables("URL"),"dev.") > 0 Then strCode = "MD0025"
    If strCode <> "" Then
	    sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
	    Set rs = dbConn.Execute(sql)
	    If Not(rs.BOF And rs.EOF) Then
		    strEmail = rs("Email")
		    SendMail "admin@mydesk.com.au", strEmail, "MyDesk Alert : " & strSubject, strMessage
	    End If
	    rs.Close
	    Set rs = Nothing
    End If
End Function

%>