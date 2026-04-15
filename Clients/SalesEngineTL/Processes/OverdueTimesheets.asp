<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim dteDate
Dim dteDateFrom
Dim dteDateTo
Dim strCode

dteDate = CDate(dateAdd("d",-3,Now()))
dteDateFrom = CDate(DateAdd("d", -6, dteDate))
dteDateTo = CDate(DateAdd("d", 1, dteDate))
strCode = "All"
'strCode = "MD0328"
%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim rsUsers
Dim sqlUsers

Set rsUsers = Server.CreateObject("ADODB.RecordSet")
If strCode = "All" Then
	sqlUsers = "SELECT Code, Name,RequiresTimesheet FROM Users WHERE Active = -1 ORDER BY Name"
Else
	sqlUsers = "SELECT Code, Name,RequiresTimesheet FROM Users WHERE Code = '" & strCode & "' AND Active = -1"
End If
Set rsUsers = dbConn.Execute(sqlUsers)

Do Until rsUsers.EOF
	Dim strName

	strName = rsUsers("Name")
	strCode = rsUsers("Code")
	if rsUsers("RequiresTimesheet") then
	
%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span style="font-size:14px;font-weight:bold;"><%= strName %></span><br><br>
<%

	dteDate = CDate(GetNextEndOfWeekFromDate(dteDateFrom))

	Dim i
	
	i = 0
	
		While dteDate < dteDateTo
			If dteDate < dteDateTo Then
				Dim rsTS
				Dim sql
				
				Set rsTS = Server.CreateObject("ADODB.RecordSet")
				sql = "SELECT * FROM Timesheets WHERE DateWeekEnding = #" & DBDate(dteDate) & "# AND Code = '" & strCode & "'"
				'Response.Write(sql)
				'Response.End
				Set rsTS = dbConn.Execute(sql)
				If (rsTS.BOF And rsTS.EOF) Then
					If i = 0 Then Response.Write("<b>Timesheet(s) missing for the following date(s):</b><br><br>")

	%>
					<li><%= FormatDateTime(dteDate,1) %></li><br>
	<%

					SendReminderMsg strCode, dteDate

					i = i + 1
				End If
				If IsObject(rsTS) Then
					rsTS.Close
					Set rsTS = Nothing
				End If
			End If
			dteDate = DateAdd("ww", 1, dteDate)
		WEnd
	

	If i = 0 Then
		Response.Write("<b>There are no timesheet(s) missing for the selected date range:</b><br><br>")
	End If

%>
				</td>
			</tr>
		</table>
		<hr>
<%
	end if
	rsUsers.MoveNext
Loop


If IsObject(rsUsers) Then
	rsUsers.Close
	Set rsUsers = Nothing
End If

Sub SendReminderMsg(strToCode, dteDate)
	Dim strMessage
	strMessage = "Your timesheet for week ending " & FormatDateTime(dteDate, 1) & " has not been entered."
	sql = "Insert Into TMail ([Date], ToCode, FromCode, Subject, Message, Read) Values ('" & ServerToEST(Now()) & "', '" & strToCode & "', 'MDADMIN', 'Timesheet Reminder', '" & strMessage & "', 0)"
	dbConn.Execute(sql)
End Sub

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->