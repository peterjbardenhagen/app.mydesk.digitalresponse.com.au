<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim rs
Dim lngQuoteCOSId
Dim sql
Dim strMsg

lngQuoteCOSId = CLng(Request("QuoteCOSId"))

sql = "Delete From QuoteCOS Where QuoteCOSId = " & lngQuoteCOSId
dbConn.Execute(sql)

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From QuoteCOS Where QuoteCOSId = " & lngQuoteCOSId
Set rs = dbConn.Execute(sql)

If err.Number = 0 Then
	If Not(rs.BOF And rs.EOF) Then
		myFSO = Server.CreateObject("Scripting.FileSystemObject")
		If objMyFSO.FileExists(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files") & "\" & rs("QuoteCOSFile")) Then
			objMyFSO.DeleteFile(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files") & "\" & rs("QuoteCOSFile"))
		End If
	End If

	rs.Close
	Set rs = Nothing
End If

If GetErrorCode(err.Description) = 1 Then
	strMsg = "Record+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it."
Else
	strMsg = "Record+deleted"
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=" & strMsg)

%>