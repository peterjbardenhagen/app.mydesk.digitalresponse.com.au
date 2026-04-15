<%
'Function to send an alert email
'Script from http://www.aspalliance.com/brettb/ErrorReportEmailer.asp
'Parameters used are:
' ErrorType = The type of error (e.g. "ASP Error")
' ErrorSource= Error source
' ErrorNumber = Error number
' ErrorDescription= Error description
'
'Changes required if you wish to use this script:
'
'1. Change the constant declarations so the email goes to you!
'
'2. If you disable Request.Cookies state then you must comment out the
' part of the script that extracts the details of the Request.Cookies object
'
'3. If you want to use a mail sending object other than ASPMail you need to
' alter the mail sending part of the script
Function SendErrorEmail(ErrorType, ErrorSource, ErrorNumber, ErrorDescription)

	On Error Resume Next

   'Declare variables
    Dim HTML 'The HTML to send in the email
    Dim CollectionItem
    Dim iNumber
    Dim myMail 'Mail Server Component
    Dim QS
    Dim RF

    'Transfer the contents of the QueryString and Form collections to variables
    Set QS = Request.QueryString
    Set RF = Request.Form

    'Declare constants. YOU MUST CHANGE THESE WHEN USING THE SCRIPT ON YOUR OWN SITE
    Const MAIL_FROM_NAME = "MYDESK ERROR HANDLER" 'Name of email sender
    Const MAIL_FROM_EMAIL = "peterb@digitalresponse.com.au" 'Email address of email sender
    Const MAIL_TO_NAME = "PETER BARDENHAGEN" 'Name of email recipient
    Const MAIL_TO_EMAIL = "peterb@digitalresponse.com.au" 'Email address of email recipient
    Const MAIL_SUBJECT = "MyDesk Error Report" 'Title of error report
    Const MAIL_HOST = "localhost" 'Address of the host used to send the mail

    'Generate the top part of the error report
    HTML = "<!DOCTYPE HTML PUBLIC""-//IETF//DTD HTML//EN"">"
    HTML = HTML & "<html>"
    HTML = HTML & "<head>"
    HTML = HTML & "<title>" & MAIL_SUBJECT & "</title>"
    HTML = HTML & "</head>"
    HTML = HTML & "<body bgcolor=""FFFFFF"">"
    HTML = HTML & "<p><font size =""2"" face=""Arial"">"
    HTML = HTML & "<b>" & MAIL_SUBJECT & "</b><br>"
    HTML = HTML & "Error Report Generated: <FONT COLOR=""#3333FF"">" & FormatDateTime(now(), vbLongDate) & ", " & FormatDateTime(now(), vbLongTime) & "</font><br>"
    HTML = HTML & "<hr>"

    'Generate the error report general description
    HTML = HTML & "<b>Details:</b><br>"
    HTML = HTML & "Error In Page: <FONT COLOR=""#FF3333"">" & Request.ServerVariables("PATH_INFO") & "</FONT><BR>"
    HTML = HTML & "Error Type: <FONT COLOR=""#FF3333"">" & ErrorType & "</FONT><BR>"
    HTML = HTML & "Error Source: <FONT COLOR=""#FF3333"">" & ErrorSource & "</FONT><BR>"
    HTML = HTML & "Error Number: <FONT COLOR=""#FF3333"">" & ErrorNumber & "</FONT><BR>"
    HTML = HTML & "Error Description: <FONT COLOR=""#FF3333"">" & ErrorDescription & "</FONT><BR>"
    HTML = HTML & "<hr>"

    'Report the contents of the QueryString collection
    HTML = HTML & "<b>QueryString Collection:</b><br>"
    If QS.Count > 0 Then
    For Each CollectionItem In QS
            HTML = HTML & CollectionItem & " : <FONT COLOR=""#3333FF"">" & QS(CollectionItem) & "</FONT><br>"
    Next
    Else
    HTML = HTML & "<FONT COLOR=""#FF3333"">The QueryString collection is empty</FONT><br>"
    End If

    HTML = HTML & "<hr>"

   'Report the contents of the Form collection
    HTML = HTML & "<b>Form Collection:</b><br>"

    If RF.Count > 0 Then
    For Each CollectionItem In RF
            HTML = HTML & CollectionItem & " : <FONT COLOR=""#FF3333"">" & RF(CollectionItem) & "</FONT><br>"
    Next
    Else
    HTML = HTML & "<FONT COLOR=""#3333FF"">The Form collection is empty</FONT><br>"
    End If

    HTML = HTML & "<hr>"

   'Report the Server object properties
    HTML = HTML & "<b>Server Settings:</b><br>"
    HTML = HTML & "ScriptTimeout: <FONT COLOR=""#FF3333"">" & Server.ScriptTimeout & "</FONT><BR>"

    HTML = HTML & "<hr>"

    'Report the Request.Cookies object properties and the contents of the Request.Cookies collection
    'IMPORTANT: If you have disabled Request.Cookiess either in IIS or
    'by use of the @ENABLERequest.CookiesSTATE = FALSE directive then you MUST comment out this section
    HTML = HTML & "<b>Request.Cookies Settings:</b><br>"
    HTML = HTML & "CodePage: <FONT COLOR=""#FF3333"">" & Request.Cookies.CodePage & "</FONT><BR>"
    HTML = HTML & "LCID: <FONT COLOR=""#FF3333"">" & Request.Cookies.LCID & "</FONT><BR>"
    HTML = HTML & "Request.CookiesID: <FONT COLOR=""#FF3333"">" & Request.Cookies.Request.CookiesID & "</FONT><BR>"
    HTML = HTML & "Timeout: <FONT COLOR=""#FF3333"">" & Request.Cookies.TimeOut & "</FONT><BR>"

    HTML = HTML & "<hr>"

    HTML = HTML & "<b>Request.Cookies Collection:</b><br>"

    For iNumber = 1 To Request.Cookies.Contents.Count
        If IsObject(Request.Cookies.Contents(iNumber)) Then
            HTML = HTML & Request.Cookies.Contents.Key(iNumber) & "<FONT COLOR=""#3333FF"">[Object]</FONT><BR>"
        Else
            If IsArray(Request.Cookies.Contents(iNumber)) Then
                HTML = HTML & Request.Cookies.Contents.Key(iNumber) & "<FONT COLOR=""#3333FF"">[Array]</FONT><BR>"
            Else
                HTML = HTML & Request.Cookies.Contents.Key(iNumber) & ": <FONT COLOR=""#3333FF"">" & Request.Cookies.Contents(iNumber) & "</FONT><BR>"
            End If
        End If
    Next

    HTML = HTML & "<hr>"

   'Report the contents of the Application collection
    HTML = HTML & "<b>Application Collection:</b><br>"

    For iNumber = 1 To Application.Contents.Count
        If IsObject(Application.Contents(iNumber)) Then
            HTML = HTML & Application.Contents.Key(iNumber) & "<FONT COLOR=""#3333FF"">[Object]</FONT><BR>"
        Else
            If IsArray(Application.Contents(iNumber)) Then
                HTML = HTML & Application.contents.Key(iNumber) & "<FONT COLOR=""#3333FF"">[Array]</FONT><BR>"
            Else
                HTML = HTML & Application.contents.Key(iNumber) & ": <FONT COLOR=""#3333FF"">" & Application.Contents(iNumber) & "</FONT><BR>"
            End If
        End If
    Next

    HTML = HTML & "<hr>"

   'Report the contents of the Server Variables collection
    HTML = HTML & "<b>Server Variables:</b><br>"

    For Each CollectionItem in request.servervariables
        If CollectionItem <> "ALL_HTTP" and CollectionItem <> "ALL_RAW" then
            HTML = HTML & CollectionItem & " : <FONT COLOR=""#3333FF"">" & request.servervariables(CollectionItem) & "</FONT><br>"
        End If
    Next

    HTML = HTML & "</body>"
    HTML = HTML & "</html>"

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
		.From = MAIL_FROM_EMAIL
		.To = MAIL_FROM_EMAIL
		.Subject = MAIL_SUBJECT & " (" & Request.ServerVariables("SERVER_NAME") & ")"
		.HtmlBody = HTML
		.Send
	End With

	'Cleanup
	Set ObjCDO = Nothing
	Set iConf = Nothing
	Set Flds = Nothing
	response.write Request.Cookies("ClientSettings")("WorkingDir") 
	Response.Redirect(Request.Cookies("ClientSettings")("WorkingDir") & "/Portal/Error.asp")
	Response.End
End Function

%>
