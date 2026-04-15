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

Dim rs
Dim intCategoryId
Dim intFileId
Dim sql
Dim strMsg

intCategoryId = CLng(Request("CategoryId"))
intFileId = CLng(Request("FileId"))

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Files Where FileId = " & intFileId
Set rs = dbConn.Execute(sql)

strFilename = rs("Filename")

myFSO = Server.CreateObject("Scripting.FileSystemObject")

If objMyFSO.FileExists(Server.MapPath("Files") & "\" & strFilename) Then
	objMyFSO.DeleteFile(Server.MapPath("Files") & "\" & strFilename)
End If

sql = "Delete From Files Where FileId = " & intFileId
dbConn.Execute(sql)

If GetErrorCode(err.Description) = 1 Then
	strMsg = "Record+cannot+be+deleten,+as+there+are+historical+records+that+depend+on+it."
Else
	strMsg = "Record+deleted"
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default2.asp?CategoryId=" & intCategoryId & "&Msg=" & strMsg)

%>