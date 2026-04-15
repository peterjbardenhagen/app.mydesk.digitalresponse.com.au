<%@ Language=VBscript %>
<%

Response.Expires = -1

Server.ScriptTimeout = 2000000

Dim ObjMyFSO
Dim Form1
Dim Path
Dim File1
Dim File1Source
Dim File1Target1
Dim File1Name
Dim File1Size
Dim cmd

%>
<!--include virtual="System/ssi_Security.asp"-->
<!--include virtual="System/ssi_DBConn.asp"-->
<!--include virtual="System/ssi_Lib.asp"-->
<%

Set Form1 =		Server.CreateObject("SoftArtisans.FileUp")
Form1.Path =	Server.MapPath(Request.Cookies("ClientSettings")("WorkingDir") & "/Files")

path =			Form1.Path

Form1.Form("File1").Save

Set objMyFSO =		Server.CreateObject("scripting.FileSystemObject")

If objMyFSO.FileExists(Form1.Path & "\" & File1Name_New) Then
End If

%>
<!--include virtual="System/ssi_DBConn_Close.asp"-->
<%

Response.Redirect("Default.asp")

%>