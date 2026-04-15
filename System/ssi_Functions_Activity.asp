<%

Function GetActivityTypes(intActivityTypeId)
	If Not IsNumeric(intActivityTypeId) Then intActivityTypeId = 0
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM ActivityTypes WHERE Visible = 1 OR ActivityTypeId = " & intActivityTypeId & " ORDER BY InOrder, Visible DESC, ActivityType"
	Set rs = dbConn.Execute(sql)
	s = s & "<select name='ActivityTypeId'>" & vbcrlf
	s = s & "<option value=''></option>" & vbcrlf
	Do Until rs.EOF
		If CInt(rs("ActivityTypeId")) = CInt(intActivityTypeId) Then strSelected = "selected" Else strSelected = ""
		s = s & "<option value='" & rs("ActivityTypeId") & "' " & strSelected & ">" & rs("ActivityType") & "</option>" & vbcrlf
		rs.MoveNext
	Loop
	s = s & "</select>" & vbcrlf
	GetActivityTypes = s
End Function

Function GetActivityTypesAsArr()
	Dim arrActivityTypes(100)

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT ActivityTypeId, ActivityType FROM ActivityTypes WHERE Visible = 1 ORDER BY ActivityType"
	Set rs = dbConn.Execute(sql)
	Dim i
	i = 0
	Do Until rs.EOF
		arrActivityTypes(i) = rs("ActivityType")
		i = i + 1
		rs.MoveNext
	Loop
	GetActivityTypesAsArr = arrActivityTypes
End Function

Function GetTimesheetStatusAsArr()
	Dim arrTimesheetStatuses(100)

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT TimesheetStatusId, TimesheetStatus FROM TimesheetStatuses ORDER BY TimesheetStatusId"
	Set rs = dbConn.Execute(sql)
	Dim i
	i = 0
	Do Until rs.EOF
		arrTimesheetStatuses(i) = rs("TimesheetStatus")
		i = i + 1
		rs.MoveNext
	Loop
	GetTimesheetStatusAsArr = arrTimesheetStatuses
End Function

Function GetTimesheetItemsAsArr(intTimesheetId)
	Dim arrTimesheetItems(6, 8, 5)

	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM TimesheetItems WHERE TimesheetId = " & intTimesheetId
	Set rs = dbConn.Execute(sql)
	Dim i, j, k
	i = 0
	j = 0
	k = 0
	Do Until rs.EOF
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
		rs.MoveNext
	Loop
	GetTimesheetItemsAsArr = arrTimesheetItems
End Function

%>
