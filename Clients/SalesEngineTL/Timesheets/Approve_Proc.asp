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

Dim Item
Dim intTimesheetId
Dim intTimesheetStatusId
Dim intTimesheetItemId

intTimesheetId = CLng(Request("TimesheetId"))
intTimesheetStatusId = CLng(Request("TimesheetStatusId"))

If intTimesheetStatusId = 1 Then
	For Each Item In Request.Form
		Response.Write(Item & " " & Request(Item) & "<BR>")
		If Left(Item, 12) = "ApproveValue" Then
			intTimesheetItemId = CLng(Replace(Item, "ApproveValue", ""))
			If CLng(Request.Form(Item)) = -1 Then
				sql = "Update TimesheetItems Set FormOutstanding = False, FormApprovedDate = '" & DBDate(ServerToEST(Now())) & "' Where TimesheetItemId = " & intTimesheetItemId
				dbConn.Execute(sql)
				Response.Write sql
			ElseIf CLng(Request.Form(Item)) = 0 Then
'				sql = "Update TimesheetItems Set FormOutstanding = False Where TimesheetItemId = " & intTimesheetItemId
'				dbConn.Execute(sql)
			End If
		End If
	Next

	' Check to see if all items are approved.
	' If all items are approved then make ready to be approved by payroll

	Set rsCheck = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From TimesheetItems Where TimesheetId = " & intTimesheetId & " And FormOutstanding = True"
	Set rsCheck = dbConn.Execute(sql)

	If rsCheck.BOF And rsCheck.EOF Then
		sql = "Update Timesheets Set TimesheetStatusId = 3 Where TimesheetId = " & intTimesheetId
		dbConn.Execute(sql)
	End If

	If IsObject(rsCheck) Then
		rsCheck.Close
		Set rsCheck = Nothing
	End If
ElseIf intTimesheetStatusId = 2 Then
	sql = "Update Timesheets Set TimesheetStatusId = 3 Where TimesheetId = " & intTimesheetId
	dbConn.Execute(sql)
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Timesheet+Approved")

%>