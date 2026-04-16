<%

Function ViewComments(intTableId, intItemId)
	On Error Resume Next
	
	Dim rsComm, sqlComm
	
	' Validate inputs
	If IsNull(intTableId) Or Not IsNumeric(intTableId) Then
		ViewComments = "No comments"
		Exit Function
	End If
	
	If IsNull(intItemId) Or Not IsNumeric(intItemId) Then
		ViewComments = "No comments"
		Exit Function
	End If
	
	intTableId = CLng(intTableId)
	intItemId = CLng(intItemId)
	
	Set rsComm = Server.CreateObject("ADODB.RecordSet")
	sqlComm = "Select * From Comments Where TableId = " & intTableId & " And ItemId = " & intItemId & " Order By [Date] Desc"
	Set rsComm = dbConn.Execute(sqlComm)
	
	If Err.Number = 0 And Not(rsComm.BOF And rsComm.EOF) Then
		ViewComments = ViewComments & "<table width='100%' class='CommentTable'>" & vbcrlf
		Do Until rsComm.EOF
			ViewComments = ViewComments & "<tr class='CommentTableHeader'>" & vbcrlf
			ViewComments = ViewComments & "<td width='100'>" & rsComm("Date") & "</td>" & vbcrlf
			ViewComments = ViewComments & "<td width='100'>" & rsComm("Name") & "</td>" & vbcrlf
			ViewComments = ViewComments & "</tr>" & vbcrlf
			ViewComments = ViewComments & "<tr class='CommentTableRow'>" & vbcrlf
			ViewComments = ViewComments & "<td colspan='2'>" & Replace(rsComm("Comment"), vbcrlf, "<br>") & "</td>" & vbcrlf
			ViewComments = ViewComments & "</tr>" & vbcrlf
			rsComm.MoveNext
		Loop
		ViewComments = ViewComments & "</table>" & vbcrlf
	Else
		ViewComments = "No comments"
	End If
	
	rsComm.Close
	Set rsComm = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetUserContactDetails(lngDivisionId, strCode)
	On Error Resume Next
	
	Dim rs, sql, s, strPhone, strFax, workingDir
	
	' Validate inputs
	If IsNull(strCode) Or strCode = "" Then
		GetUserContactDetails = ""
		Exit Function
	End If
	
	If IsNull(lngDivisionId) Or Not IsNumeric(lngDivisionId) Then
		lngDivisionId = 0
	Else
		lngDivisionId = CLng(lngDivisionId)
	End If
	
	' Get working directory from constant (was previously cookie/session)
	workingDir = TL_WORKING_DIR
	
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
			"<img src='" & GetProtocol() & Request.ServerVariables("SERVER_NAME") & workingDir & "/Images/Logo_GIT2.jpg' border=0 alt='George Industries Traffic'>" &_
			"<br><br>" &_
			"<span style='color:#999999'>" &_
			"57 Campbell Ave Wacol QLD 4076<br>" &_
			"<a href='http://www.georgeindustries.com.au' target='_Blank' style='color:#999999'>www.georgeindustries.com.au</a></span>" &_
			"</span>"
	Else
		s = ""
		Set rs = Server.CreateObject("ADODB.RecordSet")
		
		If lngDivisionId <> 0 Then
			sql = "SELECT Divisions.*,Users.*,Locations.*,Users.Email AS userEmail FROM Divisions, Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId WHERE Divisions.DivisionId=" & lngDivisionId & " AND Users.Code='" & Replace(strCode, "'", "''") & "'"
		Else
			sql = "SELECT Users.*, Locations.Phone AS lPhone, Locations.Fax AS lFax, Divisions.* FROM Divisions INNER JOIN (Users INNER JOIN Locations ON Users.LocationId = Locations.LocationId) ON Divisions.DivisionId = Users.DivisionId WHERE (((Users.Code)='" & Replace(strCode, "'", "''") & "'))"
		End If
		
		Set rs = dbConn.Execute(sql)
		
		If Err.Number = 0 And Not(rs.BOF And rs.EOF) Then
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
			s = s & "							<td style=""font-size:12px;"" colspan=2><img src=""" & GetProtocol() & Request.ServerVariables("SERVER_NAME") & workingDir & "/Images/" & Replace(rs("Logo"),".","_footer.") & """></td>" & vbcrlf
		End If
		
		rs.Close
		Set rs = Nothing
	End If
	
	GetUserContactDetails = s
	
	Err.Clear
	On Error GoTo 0
End Function

Function DisplayLocationAddress(strAddress1, strAddress2, strSuburb, strState, strPostCode, strCountry, boolPODisplay, strPOAddress1, strPOAddress2, strPOSuburb, strPOState, strPOPostCode, strPOCountry)
	On Error Resume Next
	
	Dim strA
	
	' Handle null values
	If IsNull(strState) Then strState = ""
	If IsNull(strPOState) Then strPOState = ""
	
	If strState = "Other" Then
		strState = ""
	End If
	If strPOState = "Other" Then
		strPOState = ""
	End If
	
	strA = ""
	
    If Not IsNull(strAddress1) And Trim(strAddress1) <> "" Then
	    strA = strAddress1 & "<br>" & vbcrlf
	    If Not IsNull(strAddress2) And strAddress2 <> "" Then
		    strA = strA & strAddress2 & "<br>"
	    End If
	    strA = strA & strSuburb & ", " & strState & " " & strPostCode & "<br>" & vbcrlf
	    strA = strA & strCountry & vbcrlf
		strA = strA & "<br><br>" & vbcrlf
    End If
	
	If Not IsNull(boolPODisplay) And boolPODisplay Then
		strA = strA & strPOAddress1 & "<br>"
		If Not IsNull(strPOAddress2) And strPOAddress2 <> "" Then
			strA = strA & strPOAddress2 & "<br>"
		End If
		strA = strA & strPOSuburb & ", " & strPOState & " " & strPOPostCode & "<br>"
		strA = strA & strPOCountry
	End If
	
	DisplayLocationAddress = strA
	
	Err.Clear
	On Error GoTo 0
End Function

Function IconImage(WhatFile)
	On Error Resume Next
	
	Dim s
	
	' Validate input
	If IsNull(WhatFile) Or WhatFile = "" Then
		IconImage = "/images/icons/text.gif"
		Exit Function
	End If
	
	s = ""
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
	
    Err.Clear
    On Error GoTo 0
End Function

%>
