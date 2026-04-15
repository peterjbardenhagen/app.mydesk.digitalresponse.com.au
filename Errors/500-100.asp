<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%@Language="VBSCRIPT"%>
<%
  Option Explicit
'  On Error Resume Next
'  Response.Clear
  Dim objError
  Set objError = Server.GetLastError()
%>
<html>
<head>
<title>ASP 500 Error</title>
<style>
BODY  { FONT-FAMILY: Arial; FONT-SIZE: 10pt;
        BACKGROUND: #ffffff; COLOR: #000000;
        MARGIN: 15px; }
H2    { FONT-SIZE: 16pt; COLOR: #ff0000; }
TABLE { BACKGROUND: #000000; PADDING: 5px; }
TH    { BACKGROUND: #0000ff; COLOR: #ffffff; }
TR    { BACKGROUND: #cccccc; COLOR: #000000; }
</style>
</head>
<body>

<h2 align="center">An error has occurred</h2>

<p align="center">An error occurred processing the page you requested.<br>
Please see the details below for more information.</p>

<center>
<table>
<tr>
<td>
<%
  Response.Write("An Error Occurred.<br/>")
  Response.Write("<strong>Err</strong>" & Err & "<br/>")
  Response.Write("<strong>Desc</strong>" & Err.Description & "<br/>")
'  Response.Write("<strong>Info</strong>" & Err.Information & "<br/>")
  Response.Write("<strong>Number</strong>" & Err.Number & "<br/>")
%>
</td>
</tr>
</table>
</center>

<div align="center"><center>

<table>
<% If Len(CStr(objError.ASPCode)) > 0 Then %>
  <tr>
    <th nowrap align="left" valign="top">IIS Error Number</th>
    <td align="left" valign="top"><%=objError.ASPCode%></td>
  </tr>
<% End If %>
<% If Len(CStr(objError.Number)) > 0 Then %>
  <tr>
    <th nowrap align="left" valign="top">COM Error Number</th>
    <td align="left" valign="top"><%=objError.Number%>
    <%=" (0x" & Hex(objError.Number) & ")"%></td>
  </tr>
<% End If %>
<% If Len(CStr(objError.Source)) > 0 Then %>
  <tr>
    <th nowrap align="left" valign="top">Error Source</th>
    <td align="left" valign="top"><%=objError.Source%></td>
  </tr>
<% End If %>
<% If Len(CStr(objError.File)) > 0 Then %>
  <tr>
    <th nowrap align="left" valign="top">File Name</th>
    <td align="left" valign="top"><%=objError.File%></td>
  </tr>
<% End If %>
<% If Len(CStr(objError.Line)) > 0 Then %>
  <tr>
    <th nowrap align="left" valign="top">Line Number</th>
    <td align="left" valign="top"><%=objError.Line%></td>
  </tr>
<% End If %>
<% If Len(CStr(objError.Description)) > 0 Then %>
  <tr>
    <th nowrap align="left" valign="top">Brief Description</th>
    <td align="left" valign="top"><%=objError.Description%></td>
  </tr>
<% End If %>
<% If Len(CStr(objError.ASPDescription)) > 0 Then %>
  <tr>
    <th nowrap align="left" valign="top">Full Description</th>
    <td align="left" valign="top"><%=objError.ASPDescription%></td>
  </tr>
<% End If %>
</table>
</center></div>
</body>
</html>
<%
Dim strSubject
strSubject = "Mydesk Error Notification (" & Request.ServerVariables("Server_Name") & ")"
Dim strBody
strBody = "<html><head><style>body,p,td{font-family:arial;font-size:10pt;}</style></head><body>"
If Len(CStr(objError.ASPCode)) > 0 Then
    strBody = strBody & "<p>IIS Error Number : " & objError.ASPCode & "</p>"
End If
If Len(CStr(objError.Number)) > 0 Then
    strBody = strBody & "<p>COM Error Number : " & objError.Number & " (0x" & Hex(objError.Number) & ")" & "</p>"
End If
If Len(CStr(objError.Source)) > 0 Then
    strBody = strBody & "<p>Error Source : " & objError.Source &  "</p>"
End If
If Len(CStr(objError.File)) > 0 Then
    strBody = strBody & "<p>File Name : " & objError.File &  "</p>"
End If
If Len(CStr(objError.Line)) > 0 Then
    strBody = strBody & "<p>Line Number : " & objError.Line &  "</p>"
End If
If Len(CStr(objError.Description)) > 0 Then
    strBody = strBody & "<p>Brief Description : " & objError.Description &  "</p>"
End If
If Len(CStr(objError.ASPDescription)) > 0 Then
    strBody = strBody & "<p>Full Description : " & objError.ASPDescription &  "</p>"
End If
If Len(Request.Cookies("UserSettings")("UserTypeId")) Then
    strBody = strBody & "<p>User Type Id : " & Request.Cookies("UserSettings")("UserTypeId") &  "</p>"
End If
If Len(Request.Cookies("UserSettings")("Name")) Then
    strBody = strBody & "<p>User Name : " & Request.Cookies("UserSettings")("Name") &  "</p>"
Else
    If Len(Session("Name")) Then
        strBody = strBody & "<p>User Name : " & Session("Name") &  "</p>"
    End If
End If

strBody = strBody & "</body></html>"

Sub SendMail(strFromEmail, strToEmail, strSubject, strBody)
	Dim objCDO
	Dim iConf
	Dim Flds

	Const cdoSendUsingPort = 2

	Set objCDO = Server.CreateObject("CDO.Message")
	Set iConf = Server.CreateObject("CDO.Configuration")

	Set Flds = iConf.Fields
	With Flds
        .Item("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserver") = "smtp.sendgrid.net"
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = 587
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpconnectiontimeout") = 60
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 1
        .Item("http://schemas.microsoft.com/cdo/configuration/sendusername") = "apikey"
        .Item("http://schemas.microsoft.com/cdo/configuration/sendpassword") = "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"
		.Item("http://schemas.microsoft.com/cdo/configuration/sendtls") = true
		.Update
	End With

	Set objCDO.Configuration = iConf
	With objCDO
		.From = strFromEmail
		.To = strToEmail
		.Subject = strSubject
		.HtmlBody = strBody
		.Send
	End With
    Response.Write "<p align=""center"">Email has been sent to IT Support.</p><p align=""center""><a href=""javascript:history.go(-1);"">Click here to go back</a></p>"
    'Cleanup
    Set ObjCDO = Nothing
    Set iConf = Nothing
    Set Flds = Nothing
End Sub

Dim strFromEmail
strFromEmail = "peterb@digitalresponse.com.au"

Dim strToEmail
strToEmail = "peterb@digitalresponse.com.au"

If Session("Name") <> "Peter Bardenhagen" Then
    If strFromEmail <> "" Then
	    SendMail strFromEmail, strToEmail, strSubject, strBody
    End If
End If

%>