<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Dates.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Set Upload = Server.CreateObject("SoftArtisans.FileUp")

Upload.Path = Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/TableFiles/Files")
Upload.MaxBytes = 100000000	' Limit files to 100mb
Upload.OverWriteFiles = False

intTableId = Upload.Form("TableId")
intItemId = Upload.Form("ItemId")

i = 1
Do Until i = 4
	If Trim(Upload.Form("File" & i & "_Desc")) <> "" Then
		strUploadFilename = DateFileName(Upload.Form("File" & i).ShortFilename)
		Upload.Form("File" & i).SaveAs(strUploadFilename)
		sql = "Insert Into TableFiles (DateEntered, [TableId], ItemId, Code, Description, Filename, FileSize) " & _
			"Values ('" & ServerToEST(Now()) & "', " & intTableId & ", " & intItemId & ", '" & Request.Cookies("UserSettings")("Code") & "', '" & Trim(Replace(Upload.Form("File" & i & "_Desc"), "'", "''")) & "', '" & strUploadFilename & "', " & Upload.Form("File" & i).TotalBytes/1000 & ")"
		dbConn.Execute(sql)
	End If
	i = i + 1
Loop

If Err <> 0 Then
	bolError = True
Else
	bolError = False
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

If bolError Then
	MyRedirect("Default.asp?TableId=" & intTableId & "&ItemId=" & intItemId & "&Msg=An+error+occurred.+Please+ensure+that+the+file+size+is+under+100mb+otherwise+try+again+later.")
Else
	MyRedirect("Default.asp?TableId=" & intTableId & "&ItemId=" & intItemId & "&Msg=File(s)+added")
End If

%>