<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim dteDateFrom
Dim dteDateTo
Dim strName
Dim dteDate

strCode =		Trim(Request.Form("Code"))
dteDateFrom =	CDate(Request.Form("DateFrom"))
dteDateTo =		CDate(Request.Form("DateTo"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
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
				<td>
				<input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Or Request.Cookies("UserSettings")("UserTypeId") = 4 Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %>
				</td>
			</tr>
		</table>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table2">
			<tr>
				<td valign="top"><span class="TimesHeader">Missing Timesheets Report</span><br><br>
				<span class="TimesItalicBold">Occuring Between <%= FormatDateTime(dteDateFrom, 1) %> and <%= FormatDateTime(dteDateTo, 1) %><br>
				As at <%= FormatDateTime(ServerToEST(Now()),1) %></span><br><br>
				

<%

Set rsUsers = Server.CreateObject("ADODB.RecordSet")
If strCode = "All" Then
	sqlUsers = "SELECT Code, Name, RequiresTimesheet FROM Users WHERE Active = -1 AND (DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") Or Code = '" & Request.Cookies("UserSettings")("Code") & "') ORDER BY Name"
Else
	sqlUsers = "SELECT Code, Name, RequiresTimesheet FROM Users WHERE Code = '" & strCode & "' AND Active = -1"
End If
Set rsUsers = dbConn.Execute(sqlUsers)

Do Until rsUsers.EOF
	strName = rsUsers("Name")
	strCode = rsUsers("Code")
	if rsUsers("RequiresTimesheet") then
%>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span style="font-size:14px;font-weight:bold;"><%= strName %></span><br><br>
<%

	dteDate = CDate(GetNextEndOfWeekFromDate(dteDateFrom))

	i = 0
	While dteDate < dteDateTo
		If dteDate < dteDateTo Then
			Set rsTS = Server.CreateObject("ADODB.RecordSet")
			sql = "SELECT * FROM Timesheets WHERE DateWeekEnding = #" & DBDate(dteDate) & "# AND Code = '" & strCode & "'"
			Set rsTS = dbConn.Execute(sql)
			If (rsTS.BOF And rsTS.EOF) Then
				If i = 0 Then Response.Write("<b>Timesheet(s) missing for the following date(s):</b><br><br>")

%>
				<li><%= FormatDateTime(dteDate,1) %></li><br>
<%

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

%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->