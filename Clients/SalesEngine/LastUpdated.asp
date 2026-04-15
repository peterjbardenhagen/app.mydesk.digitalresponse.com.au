<%

Response.AddHeader "cache-control", "no-store, private, must-revalidate"

%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html>
  <head>
    <title>LastUpdated</title>
    <meta name="GENERATOR" content="Microsoft Visual Studio .NET 7.1">
    <meta name="CODE_LANGUAGE" content="Visual Basic .NET 7.1">
    <meta name=vs_defaultClientScript content="JavaScript">
    <meta name=vs_targetSchema content="http://schemas.microsoft.com/intellisense/ie3-2nav3-0">
  </head>
  <body MS_POSITIONING="FlowLayout">
<%

Dim objFileSystemObject
Dim myNSWFile
Dim myRealDate

Set objFileSystemObject = Server.CreateObject("Scripting.FileSystemObject")
Set myNSWFile = objFileSystemObject.GetFile(Server.MapPath("/Database/Pierlite_NSW.mdb"))

myPierliteNSWDBMod = CDate(myNSWFile.DateLastModified)
myPierliteNSWDBMod = (DateAdd( "h", 17, myPierliteNSWDBMod))

myServerTime =		FormatDateTime(Now, 2) & " " & FormatDateTime(Now, 4)
myMelbourneTime =	dateAdd("h", 17, myServerTime)

Response.Write "<b>Server Time:</b> " & FormatDateTime(myServerTime, 2) & " " & FormatDateTime(myServerTime, 3) & "<br>"
Response.Write "<b>Melbourne Time:</b> " & FormatDateTime(myMelbourneTime, 2) & " " & FormatDateTime(MyMelbourneTime, 3) & "<br>"

response.Write "<BR><BR>" & dateDiff("h", myServerTime, myMelbourneTime) & " hours difference<BR><BR>"

Response.Write("<b>Pierlite NSW date last modified : </b>" & FormatDateTime(myPierliteNSWDBMod, 1) & " " & FormatDateTime(myPierliteNSWDBMod, 3))

%>
  </body>
</html>