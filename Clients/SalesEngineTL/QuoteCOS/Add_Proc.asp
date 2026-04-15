<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Dates.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Set Upload = Server.CreateObject("AspUtil.FileUpload")

Upload.directory = Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files")
'Upload.MaxBytes = 100000000	' Limit files to 100mb
'Upload.OverWriteFiles = True

If CLng(Request("QuoteCOSFile").TotalBytes) > 0 Then
	strUploadFilename = Request("QuoteCOSFile").ShortFilename
	Request("QuoteCOSFile").Save
	sql = "Insert Into QuoteCOS (QuoteCOS, QuoteCOSFile) Values ('" & Replace(Request("QuoteCOS"),"'","''") & "', '" & strUploadFilename & "')"
	dbConn.Execute(sql)
End If

If Err <> 0 Then
	bolError = True
Else
	bolError = False
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

If bolError Then
	MyRedirect("Default.asp?Msg=An+error+occurred.+Please+ensure+that+the+file+size+is+under+100mb+otherwise+try+again+later.")
Else
	MyRedirect("Default.asp?Msg=File(s)+added")
End If

%>