<% 
'===================================================================
' Global error handling include – replace the old ssi_Errors.asp
'===================================================================

On Error Resume Next

'---------------------------------------------------------------
' Constants – adjust to your environment
'---------------------------------------------------------------
Const ERR_MAIL_FROM_NAME = "MyDesk Error Handler"
Const ERR_MAIL_FROM_EMAIL = "peterb@digitalresponse.com.au"
Const ERR_MAIL_TO_NAME   = "Peter Bardenhagen"
Const ERR_MAIL_TO_EMAIL   = "peterb@digitalresponse.com.au"
Const ERR_MAIL_SUBJECT   = "MyDesk Error Report"
Const ERR_MAIL_HOST      = "localhost"

' Path for the flat‑file log (ensure the folder exists and is writable)
Const ERR_LOG_PATH = "/Logs/ASP_ErrorLog.txt"

'---------------------------------------------------------------
' Main entry point – call this function from any page when an error
' occurs (e.g. in the global include or in a Catch‑all block).
'---------------------------------------------------------------
Function HandleError()
    Dim errNum, errSrc, errDesc, errLine, errURL
    errNum  = Err.Number
    errSrc  = Err.Source
    errDesc = Err.Description
    errLine = 0 ' ASP 500.100 usually provides this via Server.GetLastError()
    errURL  = Request.ServerVariables("URL")
    
    ' Only proceed if there is actually an error
    If errNum <> 0 Then
        ' 1. Log to flat file
        Call LogErrorToFile(errNum, errSrc, errDesc, errURL)
        
        ' 2. Send Email alert
        Call SendErrorEmail(errNum, errSrc, errDesc, errURL)
        
        ' 3. Show Friendly Error Page
        Call ShowFriendlyErrorPage(errNum, errSrc, errDesc, errURL)
        
        ' Halt further execution
        Response.End
    End If
End Function

'---------------------------------------------------------------
' LogErrorToFile – appends error details to a flat text file
'---------------------------------------------------------------
Sub LogErrorToFile(num, src, desc, url)
    On Error Resume Next
    Dim fso, ts, logPath
    logPath = Server.MapPath(ERR_LOG_PATH)
    
    Set fso = Server.CreateObject("Scripting.FileSystemObject")
    ' 8 = ForAppending, True = Create if not exists
    Set ts = fso.OpenTextFile(logPath, 8, True)
    
    ts.WriteLine "---"
    ts.WriteLine "DateTime   : " & Now()
    ts.WriteLine "URL        : " & url
    ts.WriteLine "Error Num  : " & num
    ts.WriteLine "Source     : " & src
    ts.WriteLine "Description: " & desc
    ts.WriteLine "User Agent : " & Request.ServerVariables("HTTP_USER_AGENT")
    ts.WriteLine "Remote IP  : " & Request.ServerVariables("REMOTE_ADDR")
    ts.WriteLine "---"
    
    ts.Close
    Set ts = Nothing
    Set fso = Nothing
End Sub

'---------------------------------------------------------------
' SendErrorEmail – sends a technical report via email
'---------------------------------------------------------------
Sub SendErrorEmail(num, src, desc, url)
    On Error Resume Next
    Dim objMail, strBody
    
    strBody = "<html><body>" & _
              "<h2>Technical Error Report</h2>" & _
              "<p><b>URL:</b> " & url & "</p>" & _
              "<p><b>Time:</b> " & Now() & "</p>" & _
              "<p><b>Error Number:</b> " & num & "</p>" & _
              "<p><b>Source:</b> " & src & "</p>" & _
              "<p><b>Description:</b> " & desc & "</p>" & _
              "<hr>" & _
              "<p><b>Remote IP:</b> " & Request.ServerVariables("REMOTE_ADDR") & "</p>" & _
              "<p><b>User Agent:</b> " & Request.ServerVariables("HTTP_USER_AGENT") & "</p>" & _
              "</body></html>"

    Set objMail = Server.CreateObject("CDO.Message")
    objMail.Subject = ERR_MAIL_SUBJECT & " [" & num & "]"
    objMail.From    = ERR_MAIL_FROM_NAME & " <" & ERR_MAIL_FROM_EMAIL & ">"
    objMail.To      = ERR_MAIL_TO_NAME   & " <" & ERR_MAIL_TO_EMAIL   & ">"
    objMail.HTMLBody = strBody
    
    ' Use local relay or SendGrid config if necessary
    objMail.Configuration.Fields.Item("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
    objMail.Configuration.Fields.Item("http://schemas.microsoft.com/cdo/configuration/smtpserver") = ERR_MAIL_HOST
    objMail.Configuration.Fields.Update
    
    objMail.Send
    Set objMail = Nothing
End Sub

'---------------------------------------------------------------
' ShowFriendlyErrorPage – displays the branded error UI
'---------------------------------------------------------------
Sub ShowFriendlyErrorPage(num, src, desc, url)
    Response.Clear
    Response.Status = "500 Internal Server Error"
%>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <title>Application Error</title>
    <link rel="stylesheet" href="/System/Style_Modern.css">
</head>
<body class="tl-body tl-error-page">
    <div class="tl-card" style="max-width: 600px; margin: 100px auto; padding: 40px; text-align: center;">
        <h1 style="color: var(--tl-error); margin-bottom: 20px;">An unexpected error occurred</h1>
        <p>We've logged the technical details and our team has been notified.</p>
        <div style="background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; text-align: left; font-family: monospace; font-size: 13px; color: #666;">
            <strong>Error Location:</strong> <%= url %><br>
            <strong>Error Details:</strong> <%= desc %>
        </div>
        <div style="display: flex; gap: 15px; justify-content: center;">
            <a href="javascript:history.back()" class="tl-btn tl-btn-secondary">Go Back</a>
            <a href="/" class="tl-btn tl-btn-primary">Home</a>
        </div>
    </div>
</body>
</html>
<%
End Sub

'---------------------------------------------------------------
' Legacy Wrapper – so old calls to SendErrorEmail don't break
'---------------------------------------------------------------
Sub SendErrorEmailWrapper(strSubject, strBody)
    Call LogErrorToFile("LEGACY", strSubject, strBody, "LEGACY_WRAPPER")
    Call SendErrorEmail("LEGACY", strSubject, strBody, "LEGACY_WRAPPER")
End Sub
%>