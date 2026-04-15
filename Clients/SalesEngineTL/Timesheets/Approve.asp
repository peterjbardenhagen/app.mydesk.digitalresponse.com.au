<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
<%

Dim intTimesheetId

intTimesheetId = CLng(Request("TimesheetId"))

Dim rsTS
Dim rsTSI
Dim sql
Dim dteDateWeekEnding
Dim arrActivityTypes

Set rsTS = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Timesheets.*, Users.Name, TimesheetStatus.TimesheetStatus As [Status] FROM (Timesheets INNER JOIN TimesheetStatus ON Timesheets.TimesheetStatusId = TimesheetStatus.TimesheetStatusId) INNER JOIN Users ON Timesheets.Code = Users.Code WHERE Timesheets.TimesheetId = " & intTimesheetId
Set rsTS = dbConn.Execute(sql)

dteDateWeekEnding = rsTS("DateWeekEnding")
intTimesheetStatusId = rsTS("TimesheetStatusId")

Set rsTSI = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM TimesheetItems WHERE TimesheetId = " & intTimesheetId
Set rsTSI = dbConn.Execute(sql)

arrActivityTypes = GetActivityTypesAsArr()

If CLng(rsTS("TimesheetStatusId")) = 1 Or (CLng(rsTS("TimesheetStatusId")) = 2) Then ' Pending Approval by line manager (1) or payroll (2)

%>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Timesheets</a> / Approve Timesheet /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table5">
								<tr>
									<td style="font-weight:bold;">Name:</td>
									<td><%= rsTS("Name") %></td>
								</tr>
								<tr>
									<td style="font-weight:bold;">Date Week Ending:</td>
									<td><%= FormatDateU(dteDateWeekEnding, False) %></td>
								</tr>
								<tr>
									<td style="font-weight:bold;">Status:</td>
									<td><%= rsTS("Status") %></td>
								</tr>
							</table>
<%
	If Not(rsTSI.BOF And rsTSI.EOF) Then
%>
							<br/>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table6">
							<form method="post" action="Approve_Proc.asp" name="Form1" id="Form1">
								<input type="hidden" name="TimesheetId" value="<%= intTimesheetId %>">
								<input type="hidden" name="TimesheetStatusId" value="<%= intTimesheetStatusId %>">
								<tr>
									<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap width=75>Day</td>
									<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap width=80>Start Time</td>
									<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap width=80>Finish Time</td>
									<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap width=140>Hours & Minutes</td>
									<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap>Activity</td>
									<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap width=150>Form Outstanding</td>
									<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap width=150>Form Approved</td>
									<td style="font-weight:bold;border-bottom:1px solid black;text-align:center;" valign="top" nowrap width=100>Approve</td>
								</tr>
<%
		Do Until rsTSI.EOF
			boolNotRequired = False
%>
								<tr>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=75><%= FormatDateU(DateAdd("d", rsTSI("Day")-7, dteDateWeekEnding), False) %></td>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=80><%= rsTSI("StartTime") %></td>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=80><%= rsTSI("FinishTime") %></td>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=140><%= ConvertToHours(DateDiff("n", ConvertToTime(rsTSI("StartTime")), ConvertToTime(rsTSI("FinishTime")))) %></td>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap><%= arrActivityTypes(rsTSI("ActivityTypeId")) %></td>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=150><% If CBool(rsTSI("FormOutstanding")) Then Response.Write("<span style=""color:red;"">Yes</span>") Else Response.Write("No") %></td>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=150><%
									
			If rsTSI("FormApprovedDate") <> "" Then
				Response.Write(FormatDateU(rsTSI("FormApprovedDate"), False))
			ElseIf CBool(rsTSI("FormOutstanding")) Then
				Response.Write("No")
			Else
				Response.Write("Not required")
				boolNotRequired = True
			End If

%>&nbsp;</td>
									<td style="<% If rsTSI("ActivityTypeId") = 13 Then %>background-color:#eeeeee;<% End If %>;border-bottom:1px solid #cccccc;" valign="top" nowrap width=100 align="center"><% If boolNotRequired Then %><% ElseIf CBool(rsTSI("FormOutstanding")) And IsNull(rsTSI("FormApprovedDate")) Then %><input type="checkbox" name="Approve<%= rsTSI("TimesheetItemId") %>" value=-1 style="border:0px;" ID="Checkbox1" onclick="if(document.Form1.Approve<%= rsTSI("TimesheetItemId") %>.checked){document.Form1.ApproveValue<%= rsTSI("TimesheetItemId") %>.value = -1;}else{document.Form1.ApproveValue<%= rsTSI("TimesheetItemId") %>.value = 0;}"><% Else %><img src="/Images/Checked.gif" border=0 alt=""><% End If %>&nbsp;</td>
								</tr>
								<input type="hidden" name="ApproveValue<%= rsTSI("TimesheetItemId") %>" value="<% If boolNotRequired Then %>0<% ElseIf CBool(rsTSI("FormOutstanding")) And IsNull(rsTSI("FormApprovedDate")) Then %>0<% Else %>1<% End If %>">
<%

			rsTSI.MoveNext
		Loop

%>
								<tr>
									<td colspan=10 align="right"><br><input type="submit" value="Submit (Approve)"></td>
								</tr>
							</form>
							</table>
<%

	End If
ElseIf CLng(rsTS("TimesheetStatusId")) = 2 And CLng(Request.Cookies("UserSettings")("UserTypeId")) <> 2 Then ' Pending Approval by payroll and current user is not payroll.
%>
							<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table2">
								<tr>
									<td>
									<br/>
									<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Timesheets</a> / Approve Timesheet /></span>
									<br/><br/>This timesheet must be approved by a line manager.</td>
								</tr>
							</table>
<%
ElseIf intTimesheetStatusId = 3 Then ' Already approved
%>
							<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td>
									<br/>
									<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Timesheets</a> / Approve Timesheet /></span>
									<br/><br/>This timesheet has already been approved by a line manager.</td>
								</tr>
							</table>
<%

End If

%>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->