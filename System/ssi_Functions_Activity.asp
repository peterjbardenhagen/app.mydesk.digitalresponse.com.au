<%

Function GetActivityTypes(intActivityTypeId)
	On Error Resume Next
	
	Dim rs, sql, s, strSelected
	
	' Validate input
	If IsNull(intActivityTypeId) Or Not IsNumeric(intActivityTypeId) Then
		intActivityTypeId = 0
	Else
		intActivityTypeId = CLng(intActivityTypeId)
	End If
	
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ActivityTypes WHERE Visible = 1 OR ActivityTypeId = " & intActivityTypeId & " ORDER BY InOrder, Visible DESC, ActivityType"
	Set rs = dbConn.Execute(sql)
	
	s = ""
	s = s & "<select name='ActivityTypeId'>" & vbcrlf
	s = s & "<option value=''></option>" & vbcrlf
	
	If Err.Number = 0 Then
		Do Until rs.EOF
			If IsNumeric(rs("ActivityTypeId")) Then
				If CInt(rs("ActivityTypeId")) = CInt(intActivityTypeId) Then strSelected = "selected" Else strSelected = ""
				s = s & "<option value='" & rs("ActivityTypeId") & "' " & strSelected & ">" & rs("ActivityType") & "</option>" & vbcrlf
			End If
			rs.MoveNext
		Loop
	End If
	
	s = s & "</select>" & vbcrlf
	GetActivityTypes = s
	
	rs.Close
	Set rs = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetActivityTypesAsArr()
	On Error Resume Next
	
	Dim arrActivityTypes(100), rs, sql, i
	
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT ActivityTypeId, ActivityType FROM ActivityTypes WHERE Visible = 1 ORDER BY ActivityType"
	Set rs = dbConn.Execute(sql)
	
	i = 0
	If Err.Number = 0 Then
		Do Until rs.EOF
			If i < 100 Then
				arrActivityTypes(i) = rs("ActivityType")
				i = i + 1
			End If
			rs.MoveNext
		Loop
	End If
	
	rs.Close
	Set rs = Nothing
	GetActivityTypesAsArr = arrActivityTypes
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetTimesheetStatusAsArr()
	On Error Resume Next
	
	Dim arrTimesheetStatuses(100), rs, sql, i
	
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT TimesheetStatusId, TimesheetStatus FROM TimesheetStatuses ORDER BY TimesheetStatusId"
	Set rs = dbConn.Execute(sql)
	
	i = 0
	If Err.Number = 0 Then
		Do Until rs.EOF
			If i < 100 Then
				arrTimesheetStatuses(i) = rs("TimesheetStatus")
				i = i + 1
			End If
			rs.MoveNext
		Loop
	End If
	
	rs.Close
	Set rs = Nothing
	GetTimesheetStatusAsArr = arrTimesheetStatuses
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetTimesheetItemsAsArr(intTimesheetId)
	On Error Resume Next
	
	Dim arrTimesheetItems(6, 8, 5), rs, sql, i, j, k
	
	' Validate input
	If IsNull(intTimesheetId) Or Not IsNumeric(intTimesheetId) Then
		GetTimesheetItemsAsArr = arrTimesheetItems
		Exit Function
	End If
	
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM TimesheetItems WHERE TimesheetId = " & CLng(intTimesheetId)
	Set rs = dbConn.Execute(sql)
	
	i = 0
	j = 0
	k = 0
	
	If Err.Number = 0 Then
		Do Until rs.EOF
			If i <= 6 And j <= 8 And k <= 5 Then
				arrTimesheetItems(i, j, k) = rs("ActivityTypeId")
				i = i + 1
				If i > 6 Then
					i = 0
					j = j + 1
				End If
				If j > 8 Then
					j = 0
					k = k + 1
				End If
			End If
			rs.MoveNext
		Loop
	End If
	
	rs.Close
	Set rs = Nothing
	GetTimesheetItemsAsArr = arrTimesheetItems
	
	Err.Clear
	On Error GoTo 0
End Function

%>
