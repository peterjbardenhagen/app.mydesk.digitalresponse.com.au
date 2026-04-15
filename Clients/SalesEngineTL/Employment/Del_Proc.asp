<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim lngEmploymentId
Dim sql
Dim strMsg

lngEmploymentId = CLng(Request("EmploymentId"))

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Employment Where DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") And EmploymentId = " & lngEmploymentId
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	sql = "Delete From Employment Where EmploymentId = " & lngEmploymentId
	dbConn.Execute(sql)

	If GetErrorCode(err.Description) = 1 Then
		strMsg = "Record+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it."
	Else
		strMsg = "Record+deleted"
	End If
Else
	strMsg = "Record+cannot+be+deleten,+as+you+do+not+have+permission."
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=" & strMsg)

%>