<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Set Upload = Server.CreateObject("AspUtil.FileUpload")

Upload.directory = Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files")
'Upload.MaxBytes = 100000000	' Limit files to 100mb
'Upload.OverWriteFiles = True

lngQuoteCOSId = Request("QuoteCOSId")

sql = "Select * From QuoteCOS Where QuoteCOSId = " & lngQuoteCOSId
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	myFSO = Server.CreateObject("Scripting.FileSystemObject")
	If objMyFSO.FileExists(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files") & "\" & rs("QuoteCOSFile")) Then
		objMyFSO.DeleteFile(Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files") & "\" & rs("QuoteCOSFile"))
	End If
End If

rs.Close
Set rs = Nothing

'Upload.SetMaxSize 100000000	' Limit files to 100mb
intCount = Upload.SaveVirtual(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files")

If CLng(Request("QuoteCOSFile").TotalBytes) > 0 Then
	strUploadFilename = Request("QuoteCOSFile").ShortFilename
	Request("QuoteCOSFile").Save
	sql = "Update QuoteCOS Set QuoteCOS = '" & Replace(Request("QuoteCOS"),"'","''") & "', QuoteCOSFile = '" & Replace(Replace(File.Path, Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/FilesLibrary/Files") & "\", ""), "'", "''") & "' Where QuoteCOSId = " & lngQuoteCOSId
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