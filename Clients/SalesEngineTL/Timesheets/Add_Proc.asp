<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim dteDateWeekEnding
Dim sql
Dim rs
Dim d

intLeave = CInt(Request("Leave"))

for each d in Request.Form
		Response.Write(d &"=" & Request.Form(d) & "<br />")
Next
	
dteDateWeekEnding = DBDate(Request("DateWeekEnding"))

sql = "Insert Into Timesheets (Code, DateWeekEnding, TimesheetStatusId) Values ('" & Request.Cookies("UserSettings")("Code") & "', '" & dteDateWeekEnding & "', 1)"
dbConn.Execute(sql)

rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select Top 1 * From Timesheets Where DateWeekEnding = #" & dteDateWeekEnding & "# Order By TimesheetId Desc"
Set rs = dbConn.Execute(sql)

Dim intTimesheetId
intTimesheetId = rs("TimesheetId")

rs.Close
Set rs = Nothing
			
Dim i
i = 1
Do Until i = 8
	if (Request.Form("startTime" & i)="" Or Request.Form("EndTime" & i)="") then
'		Do nothing.
'		sql = "Insert Into TimesheetItems (TimesheetId, Day, StartTime,FinishTime, ActivityTypeId, FormOutstanding) Values (" & intTimesheetId & ", " & i & ", 0900, 1700,13 ,0)"
	else
		sql = "Insert Into TimesheetItems (TimesheetId, Day, StartTime,FinishTime, ActivityTypeId, FormOutstanding) Values (" & intTimesheetId & ", " & i & "," & Request.Form("startTime" & i) & "," & Request.Form("EndTime" & i) &  ",13 ,0)"
		dbConn.Execute(sql)
	end if
	'Response.Write(sql)
	'response.end
	
	Dim y
	y = 1
	Do Until y = 9
		If CLng(Request.Form("ItemTypeId_" & i & "_" & y)) > 0 then
			Set rs = Server.CreateObject("ADODB.RecordSet")
			sql = "Select * From ActivityTypes Where ActivityTypeId = " & Request.Form("ItemTypeId_" & i & "_" & y)
			Set rs = dbConn.Execute(sql)
			
			If CBool(rs("FormRequired")) Then
				strFormOutstanding = -1
			Else
				strFormOutstanding = 0
			End If
			if isNull(Request.Form("timeFrom" & i & "_" & y)) Or Request.Form("timeFrom" & i & "_" & y) = "" Or Request.Form("timeTo" & i & "_" & y) = "" Then
				rs.Close
				Set rs = Nothing		
			ELSE
				sql = "Insert Into TimesheetItems (TimesheetId, Day, StartTime,FinishTime, ActivityTypeId, FormOutstanding) Values (" & intTimesheetId & ", " & i & ", " & Request.Form("timeFrom" & i & "_" & y) & ", " & Request.Form("timeTo" & i & "_" & y) & ", " & Request.Form("ItemTypeId_" & i & "_" & y) & "," & strFormOutstanding & ")"
				response.write sql
				'response.Write(Request.Form("timeTo" & i & "_" & y))
				'response.end
				dbConn.Execute(sql)' or die(sql)
				rs.Close
				Set rs = Nothing		
			end if
		End If
		y = y + 1
	Loop
	i = i + 1
Loop
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%
'response.End

If intLeave = 1 Then
	MyRedirect("DownloadLeaveForm.asp?Msg=Timesheet+Added")
Else
	MyRedirect("Default.asp?Msg=Timesheet+Added")
End If

%>