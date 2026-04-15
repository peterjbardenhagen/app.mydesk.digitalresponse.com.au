<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim intTimesheetId
intTimesheetId = CLng(Request("TimesheetId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim rsTS
Dim rsTSI
Dim sql
Dim dteDateWeekEnding
Dim arrActivityTypes

Set rsTS = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Timesheets.*, Users.Name, TimesheetStatus.TimesheetStatus As [Status] FROM (Timesheets INNER JOIN TimesheetStatus ON Timesheets.TimesheetStatusId = TimesheetStatus.TimesheetStatusId) INNER JOIN Users ON Timesheets.Code = Users.Code WHERE Timesheets.TimesheetId = " & intTimesheetId
Set rsTS = dbConn.Execute(sql)

dteDateWeekEnding = rsTS("DateWeekEnding")

Set rsTSI = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT TimesheetItems.* FROM TimesheetItems WHERE TimesheetId = " & intTimesheetId
Set rsTSI = dbConn.Execute(sql)

arrActivityTypes = GetActivityTypesAsArr()

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
		</table>
		<br>
		<span class="TimesHeader">Timesheet</span><br>
		<br>
		<table cellpadding=3 cellspacing=0 border=0 ID="Table5">
			<tr>
				<td style="font-weight:bold;">Name</td>
				<td><%= rsTS("Name") %></td>
			</tr>
			<tr>
				<td style="font-weight:bold;">Date Week Ending</td>
				<td><%= FormatDateU(dteDateWeekEnding, False) %></td>
			</tr>
			<tr>
				<td style="font-weight:bold;">Status</td>
				<td><%= rsTS("Status") %></td>
			</tr>
		</table>
<%
If Not(rsTSI.BOF And rsTSI.EOF) Then
%>
		<br/>
		<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table6">
			<tr>
				<td class="HeaderRow" nowrap width=75>Day</td>
				<td class="HeaderRow" valign="top" nowrap width=80>Start Time</td>
				<td class="HeaderRow" valign="top" nowrap width=80>Finish Time</td>
				<td class="HeaderRow" valign="top" nowrap width=140>Hours & Minutes</td>
				<td class="HeaderRow" valign="top" nowrap>Activity</td>
				<td class="HeaderRow" valign="top" nowrap width=150>Form Outstanding</td>
				<td class="HeaderRow" valign="top" nowrap width=150>Form Approved</td>
			</tr>
<%
	Do Until rsTSI.EOF
%>
			<tr>
				<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#cccccc;<% End If %>border-bottom:1px solid #cccccc;" valign="top" nowrap width=75><%= FormatDateU(DateAdd("d", rsTSI("Day")-7, dteDateWeekEnding), False) %></td>
				<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#cccccc;<% End If %>border-bottom:1px solid #cccccc;" valign="top" nowrap width=80><%= rsTSI("StartTime") %></td>
				<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#cccccc;<% End If %>border-bottom:1px solid #cccccc;" valign="top" nowrap width=80><%= rsTSI("FinishTime") %></td>
				<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#cccccc;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=140><%= ConvertToHours(DateDiff("n", ConvertToTime(rsTSI("StartTime")), ConvertToTime(rsTSI("FinishTime")))) %></td>
				<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#cccccc;<% End If %>border-bottom:1px solid #cccccc;" valign="top" nowrap><%= arrActivityTypes(rsTSI("ActivityTypeId")) %></td>
				<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#cccccc;<% End If %>border-bottom:1px solid #cccccc;" valign="top" nowrap width=150><% If CBool(rsTSI("FormOutstanding")) Then Response.Write("<span style=""color:red;"">Yes</span>") Else Response.Write("No") %></td>
				<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#cccccc;<% End If %>border-bottom:1px solid #cccccc;" valign="top" nowrap width=150><% If rsTSI("FormApprovedDate") <> "" Then Response.Write(rsTSI("FormApprovedDate")) Else If CBool(rsTSI("FormOutstanding")) Then Response.Write("No") Else Response.Write("Not required") %>&nbsp;</td>
			</tr>
<%

		rsTSI.MoveNext
	Loop

%>
		</table>
<%

End If

%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->