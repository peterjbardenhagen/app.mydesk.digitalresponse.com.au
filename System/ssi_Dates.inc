<%
' ====================================================
' Function Library for handling dates
'
' Functions:
' * PadNumber(val)
'   - Pads out a value val into a double digit with a leading zero 
' * DayOfWeek(dt, len)
'   - Given a date dt it returns the day of the week as a character string of length len
' * FormatDBToUambiguous(dtFromDB,US0rAUStyle,incTime)
' * FormatDBToAmbiguous(dtFromDB,US0rAUStyle,incTime)
' * FormatTimestampDB(dTimestampToFormat)

Function ConvertToTime(strVal)
	If Len(strVal) = 3 Then
		strVal = CStr("0" & strVal)
	Else
		strVal = CStr(strVal)
	End If
	ConvertToTime = "01/01/2000 " & Left(strVal,2) & ":" & Right(strVal,2)
End Function

Function ConvertToHours(intMinutes)
	If IsNumeric(intMinutes) Then
		If intMinutes > 0 Then
			intNewMinutes = intMinutes Mod 60
			intNewHours = intMinutes / 60
			If InStr(intNewHours,".") > 0 Then
				intNewHours = Replace(Left(intNewHours,InStr(intNewHours,".")),".","")
			End If
			ConvertToHours = intNewHours & " hours"
			If intNewMinutes > 0 Then ConvertToHours = ConvertToHours & ", " & intNewMinutes & " minutes"
		Else
			ConvertToHours = 0
		End If
	End If
End Function

Function DateFileName(strFilename)
	dt = Year(Now) & Month(Now) & Year(Now) & FormatDateTime(Now,3)
	dt = Replace(dt,":","")
	dt = Replace(dt," ","")
	dt = Replace(dt,"AM","")
	dt = Replace(dt,"PM","")
	Randomize()
	dt = dt & CInt(9*Rnd())
	strFileNameExt = Right(strFilename,4)
	strFileNameExt = Replace(strFileNameExt,".","")
	DateFileName = dt & "." & strFileNameExt
End Function

Function PadNumber(val)
	If Len(val) = 1 Then
		PadNumber = "0" & val
	Else 
		PadNumber = val
	End If
End Function 'PadNumber

Function DBDate(dt)
	dt = CDate(dt)
	DBDate = Year(dt) & "/" & PadNumber(Month(dt)) & "/" & PadNumber(Day(dt))
End Function

Function DayOfWeek(dt, ln)
	Dim intDOW, strDOW
	
	intDOW = Weekday(dt, 2) 'The constant 2 is vbMonday
	
	Select Case intDOW
	Case 1
		strDOW = "Monday"
	Case 2
		strDOW = "Tuesday"
	Case 3
		strDOW = "Wednesday"
	Case 4
		strDOW = "Thursday"
	Case 5
		strDOW = "Friday"
	Case 6
		strDOW = "Saturday"
	Case 7
		strDOW = "Sunday"
	End Select
	
	If ln = 0 Then
		DayOfWeek = strDOW
	Else
		DayOfWeek = Left(strDOW, ln)
	End If 
	
End Function 'DayOfWeek

Function FormatDateU(dtFromDB, incTime)
	If IsDate(dtFromDB) Then
		Dim USOrAUStyle, dt_day, dt_month, dt_year, sepChar, dt_time

		sepChar = "-"
		
		dt_day =	PadNumber(Day(dtFromDB))
		dt_month =	Left(MonthName(Month(dtFromDB)),3)
		dt_year =	Year(dtFromDB)
		dt_time =	FormatDateTime(ServerToEST(dtFromDB),4)
			
		FormatDateU = dt_day & "-" & dt_month & "-" & dt_year

		If incTime = True Then
			FormatDateU = FormatDateU & " at " & dt_time
		End If
	Else
		FormatDateU = "Date not available"
	End If
End Function 'FormatDateU

Function FormatDateU2(dtFromDB, incTime)

	Dim USOrAUStyle, dt_day, dt_month, dt_year, sepChar, dt_time
	
	sepChar = "-"
	
	dt_day =	PadNumber(Day(dtFromDB))
	dt_month =	PadNumber(Month(dtFromDB))
	dt_year =	Year(dtFromDB)
	dt_time =	FormatDateTime(ServerToEST(dtFromDB),4)
		
	FormatDateU2 = "<!--" & dt_year & "-" & dt_month & "-" & dt_day

	If incTime = True Then
		FormatDateU2 = FormatDateU2 & " at " & dt_time
	End If
	
	FormatDateU2 = FormatDateU2 & "-->" & dt_day & "-" & MonthName(dt_month, true) & "-" & dt_year
	
End Function

Function FormatDateU3(dtFromDB)
	Dim dt_day, dt_month, dt_year, sepChar
	
	sepChar = "-"
	dt_day = PadNumber(Day(dtFromDB))
	dt_month = PadNumber(Month(dtFromDB))
	dt_year = Year(dtFromDB)
		
	FormatDateU3 = "<!--" & dt_year & "-" & dt_month & "-" & dt_day & "-->" & dt_day & "-" & dt_month & "-" & dt_year
End Function

Function FormatDateULong(dtFromDB, AUOrUSStyle, incTime)
	Dim dt_weekday, dt_day, dt_month, dt_year, sepChar, dt_time
	
	sepChar = " "
	
	dt_weekday = DayOfWeek(dtFromDB, 0)
	dt_day = PadNumber(Day(dtFromDB))
	dt_month = MonthName(Month(dtFromDB))
	dt_year = Year(dtFromDB)
	dt_time = PadNumber(Hour(dtFromDB)) & ":" & PadNumber(Minute(dtFromDB))
		
	Select Case AUOrUSStyle
	Case "US"
		FormatDateULong = dt_Weekday & ", " & dt_month & sepChar & dt_day & sepChar & dt_year
	Case "AU"
		FormatDateULong = dt_Weekday & ", " & dt_day & sepChar & dt_month & sepChar & dt_year
	Case Else
		FormatDateULong = dt_Weekday & ", " & dt_day & sepChar & dt_month & sepChar & dt_year
	End Select

	If incTime = True Then
		FormatDateULong = FormatDateULong & " " & dt_time
	End If
End Function 'FormatDateULong

Function FormatDBToAmbiguous(dtFromDB,US0rAUStyle,incTime)
	Dim USOrAUStyle, dt_day, dt_month, dt_year, sepChar, dt_time
	
	sepChar = "/"
	
	dtFromDB = FormatDateTime(dtFromDB,0)
	
	dt_day = PadNumber(Day(dtFromDB))
	dt_month = PadNumber(Month(dtFromDB))
	dt_year = Right(Year(dtFromDB),4)
	dt_time = PadNumber(Hour(dtFromDB)) & ":" & PadNumber(Minute(dtFromDB))
		
	Select Case USOrAUStyle
	Case "US"
		FormatDBToAmbiguous = dt_month & sepChar & dt_day & sepChar & dt_year
	Case "AU"
		FormatDBToAmbiguous = dt_day & sepChar & dt_month & sepChar & dt_year
	Case Else
		FormatDBToAmbiguous = dt_day & sepChar & dt_month & sepChar & dt_year
	End Select

	If incTime = True Then
		FormatDBToAambiguous = FormatDBToAmbiguous & " " & dt_time
	End If
End Function 'FormatDBToAmbiguous

Function FormatTimestampDB(dTimestampToFormat)
	' Formats to "m/d/yyyy h:mm:ss AM" format
	' Change as appropriate to match your DB
	Dim strMonth, strDay, strYear
	Dim strHour, strMinute, strSecond
	Dim strAMPM

	strMonth = Month(dTimestampToFormat)
	strDay = Day(dTimestampToFormat)
	strYear = Year(dTimestampToFormat)
	'strYear = Right(Year(dTimestampToFormat), 2)

	strHour = Hour(dTimestampToFormat) Mod 12
	If strHour = 0 Then strHour = 12

	If Hour(dTimestampToFormat) < 12 Then
		strAMPM = "AM"
	Else
		strAMPM = "PM"
	End If

	strMinute = Minute(dTimestampToFormat)
	If Len(strMinute) = 1 Then strMinute = "0" & strMinute

	strSecond = Second(dTimestampToFormat)
	If Len(strSecond) = 1 Then strSecond = "0" & strSecond

	' "d/m/yyyy h:mm:ss AM" for all those who have had problems.
	'FormatTimestampDB = strDay & "/" & strMonth & "/" & strYear & " " & strHour & ":" & strMinute & ":" & strSecond & " " & strAMPM

	FormatTimestampDB = strMonth & "/" & strDay & "/" & strYear
End Function

Function GetFirstEndOfWeek(intYear)
	Dim intTestFirstDayOfWeek
	Dim dteCurrentDate
	Dim dteStartDate
	Dim i
	i = 0
	dteStartDate = "01-Jan-" & intYear
	dteCurrentDate = dteStartDate
	Do Until intTestFirstDayOfWeek = 1
		intTestFirstDayOfWeek = DatePart("w", dteCurrentDate, 1)
		response.Write(intTestFirstDayOfWeek & " " & dteCurrentDate & "<br>")
		If Not intTestFirstDayOfWeek = 1 Then
			dteCurrentDate = DateAdd("d", 1, dteCurrentDate)
		End If
	Loop
	GetFirstEndOfWeek = dteCurrentDate
End Function

Function GetAllWorkingWeeksOfYear(intYear)
	Dim dteStartDate
	Dim dteCurrentDate
	Dim dteNextEndOfWeek
	Dim i
	Dim x
	Dim s
	dteStartDate = GetFirstEndOfWeek(intYear)
	dteCurrentDate = dteStartDate
	dteNextEndOfWeek = GetNextEndOfWeek()
	i = 0
	x = 0
	Do Until i = 60
		If x = 52 Then x = 0
		If FormatDateU(dteCurrentDate, False) = FormatDateU(dteNextEndOfWeek, False) Then
			s = s + "<option selected value=""" & FormatDateTime(dteCurrentDate, vbShortDate) & """>" & x+1 & ". " & FormatDateU(dteCurrentDate, False) & "</option>" & vbNewLine
		Else
			s = s + "<option value=""" & FormatDateTime(dteCurrentDate, vbShortDate) & """>" & x+1 & ". " & FormatDateU(dteCurrentDate, False) & "</option>" & vbNewLine
		End If
		dteCurrentDate = DateAdd("ww", 1, dteCurrentDate)
		i = i + 1
		x = x + 1
	Loop
	GetAllWorkingWeeksOfYear = s
End Function

Function GetAllWorkingWeeksOfYear_ForAddingTimesheets(intYear, strCode)
	Dim dteStartDate
	Dim dteCurrentDate
	Dim dteNextEndOfWeek
	Dim i
	Dim x
	Dim s
	dteStartDate = GetFirstEndOfWeek(intYear)
	dteCurrentDate = dteStartDate
	dteNextEndOfWeek = GetNextEndOfWeek()
	i = 0
	x = 0
	Do Until i = 60
		If x = 52 Then x = 0
		Set rsCD = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT * FROM Timesheets WHERE DateWeekending = #" & FormatDateU(dteCurrentDate, False) & "# AND Code = '" & strCode & "'"
		Set rsCD = dbConn.Execute(sql)
		If (rsCD.BOF and rsCD.EOF) Then
			If FormatDateU(dteCurrentDate, False) = FormatDateU(dteNextEndOfWeek, False) Then
				s = s + "<option selected value=""" & FormatDateTime(dteCurrentDate, vbShortDate) & """>" & x+1 & ". " & FormatDateU(dteCurrentDate, False) & "</option>" & vbNewLine
			Else
				s = s + "<option value=""" & FormatDateTime(dteCurrentDate, vbShortDate) & """>" & x+1 & ". " & FormatDateU(dteCurrentDate, False) & "</option>" & vbNewLine
			End If
		End If
		If IsObject(rsCD) Then
			rsCD.Close
			Set rsCD = Nothing
		End If
		dteCurrentDate = DateAdd("ww", 1, dteCurrentDate)
		i = i + 1
		x = x + 1
	Loop
	GetAllWorkingWeeksOfYear_ForAddingTimesheets = s
End Function

Function GetAllWorkingWeeksOfYear_ForEditingTimesheets(intYear, strCode, dteSelectedDate)
	Dim dteStartDate
	Dim dteCurrentDate
	Dim dteNextEndOfWeek
	Dim i
	Dim x
	Dim s
	dteStartDate = GetFirstEndOfWeek(intYear)
	dteCurrentDate = dteStartDate
	dteNextEndOfWeek = GetNextEndOfWeek()
	i = 0
	x = 0
	Do Until i = 60
		If x = 52 Then x = 0
		Set rsCD = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT * FROM Timesheets WHERE DateWeekending = #" & FormatDateU(dteCurrentDate, False) & "# AND Code = '" & strCode & "'"
		Set rsCD = dbConn.Execute(sql)
		If (rsCD.BOF and rsCD.EOF) And Not (CDate(dteSelectedDate) = CDate(dteCurrentDate)) Then
			If FormatDateU(dteCurrentDate, False) = FormatDateU(dteNextEndOfWeek, False) Then
				s = s + "<option selected value=""" & FormatDateTime(dteCurrentDate, vbShortDate) & """>" & x+1 & ". " & FormatDateU(dteCurrentDate, False) & "</option>" & vbNewLine
			Else
				s = s + "<option value=""" & FormatDateTime(dteCurrentDate, vbShortDate) & """>" & x+1 & ". " & FormatDateU(dteCurrentDate, False) & "</option>" & vbNewLine
			End If
		End If
		If IsObject(rsCD) Then
			rsCD.Close
			Set rsCD = Nothing
		End If
		dteCurrentDate = DateAdd("ww", 1, dteCurrentDate)
		i = i + 1
		x = x + 1
	Loop
	GetAllWorkingWeeksOfYear_ForAddingTimesheets = s
End Function

Function GetNextEndOfWeek()
	Dim intTestLastDayOfWeek
	Dim dteCurrentDate
	Dim dteStartDate
	Dim i
	i = 0
	dteStartDate = ServerToEST(Now())
	dteCurrentDate = dteStartDate
	Do Until intTestLastDayOfWeek = 7 Or i = 10
		intTestLastDayOfWeek = DatePart("w", dteCurrentDate, 1)
		If intTestLastDayOfWeek <> 7 Then
			dteCurrentDate = DateAdd("d", 1, dteCurrentDate)
		End If
		i = i + 1
	Loop
	GetNextEndOfWeek = FormatDateTime(dteCurrentDate, vbShortDate)
End Function

Function GetNextEndOfWeekFromDate(dteDate)
	Dim intTestLastDayOfWeek
	Dim dteCurrentDate
	Dim dteStartDate
	Dim i
	i = 0
	dteCurrentDate = dteDate
	Do Until intTestLastDayOfWeek = 7 Or i = 10
		intTestLastDayOfWeek = DatePart("w", dteCurrentDate, 1)
		If intTestLastDayOfWeek <> 1 Then
			dteCurrentDate = DateAdd("d", 1, dteCurrentDate)
		End If
		i = i + 1
	Loop
	GetNextEndOfWeekFromDate = FormatDateTime(dteCurrentDate, vbShortDate)
End Function

%>