<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim intProjectId
Dim sql
Dim strMsg
Dim rsCheck

intProjectId = CLng(Request("ProjectId"))

Set rsCheck = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From TimesheetItems Where ProjectId = " & intProjectId
Set rsCheck = dbConn.Execute(sql)

If Not(rsCheck.BOF And rsCheck.EOF) Then
	strMsg = "Record+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it."
Else
	sql = "Delete From Projects Where ProjectId = " & intProjectId
	dbConn.Execute(sql)
	If GetErrorCode(err.Description) = 1 Then
		strMsg = "Record+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it."
	Else
		strMsg = "Record+deleted"
	End If
End If

If IsObject(rsCheck) Then
	rsCheck.Close
	Set rsCheck = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=" & strMsg)

%>