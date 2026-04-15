<%

Response.AddHeader "cache-control", "no-store, private, must-revalidate"

%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<html>
  <head>
    <title>LastUpdated</title>
  </head>
  <body MS_POSITIONING="FlowLayout">
<%

Dim objFileSystemObject
Dim myDBMod
Dim myRealDate

Set objFileSystemObject = Server.CreateObject("Scripting.FileSystemObject")
Set myDBMod = objFileSystemObject.GetFile(Server.MapPath("/Database/TTL.mdb"))

myDBMod = CDate(myNSWFile.DateLastModified)
'myDBMod = (DateAdd( "h", 17, myDBMod))

myServerTime =		FormatDateTime(Now, 2) & " " & FormatDateTime(Now, 4)
myMelbourneTime =	myServerTime

Response.Write "<b>Server Time:</b> " & FormatDateTime(myServerTime, 2) & " " & FormatDateTime(myServerTime, 3) & "<br>"
Response.Write "<b>Melbourne Time:</b> " & FormatDateTime(myMelbourneTime, 2) & " " & FormatDateTime(MyMelbourneTime, 3) & "<br>"

response.Write "<BR><BR>" & dateDiff("h", myServerTime, myMelbourneTime) & " hours difference<BR><BR>"

Response.Write("<b>TTL DB date last modified : </b>" & FormatDateTime(myDBMod, 1) & " " & FormatDateTime(myDBMod, 3))

%>
  </body>
</html>