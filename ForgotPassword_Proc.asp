<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next

Dim strEmail, rsUser, strToken, dtmExpiry
Dim mailBody, strBaseURL

strEmail = Trim(Request.Form("ResetEmail") & "")

If strEmail <> "" Then
    ' Check if user exists
    Set rsUser = Server.CreateObject("ADODB.Recordset")
    ' Try email or username match
    rsUser.Open "SELECT * FROM Users WHERE Email = '" & Replace(strEmail, "'", "''") & "' OR Username = '" & Replace(strEmail, "'", "''") & "'", dbConn, 3, 3
    
    If Not rsUser.EOF Then
        ' Generate a simple pseudo-random GUID-like token
        Randomize
        strToken = Replace(Mid(CreateObject("Scriptlet.TypeLib").Guid, 2, 36), "-", "") & Hex(Int((999999999 * Rnd) + 1))
        dtmExpiry = DateAdd("h", 1, Now()) ' Valid for 1 hour
        
        ' Update user record
        rsUser("LoginToken") = strToken
        rsUser("LoginTokenExpiry") = dtmExpiry
        rsUser.Update
        
        ' Build email
        strBaseURL = GetBaseURL()
        
        mailBody = "<html><body style='font-family: Arial, sans-serif;'>"
        mailBody = mailBody & "<h2 style='color:#00a8b5;'>Secure Auto-Login</h2>"
        mailBody = mailBody & "<p>Hi " & rsUser("Name") & ",</p>"
        mailBody = mailBody & "<p>You recently requested to automatically login to your Techlight MyDesk account. Please click the button below to sign in securely. This link is valid for 1 hour.</p>"
        mailBody = mailBody & "<p style='margin: 30px 0;'><a href='" & strBaseURL & "/AutoLogin.asp?t=" & strToken & "' style='background:#00a8b5;color:white;padding:12px 24px;text-decoration:none;border-radius:4px;font-weight:bold;'>Securely Sign In</a></p>"
        mailBody = mailBody & "<p>If you didn't request this, you can safely ignore this email.</p>"
        mailBody = mailBody & "<hr style='border:none;border-top:1px solid #eee;margin-top:20px;'><p style='color:#666;font-size:12px;'>Digital Response Technical Support</p>"
        mailBody = mailBody & "</body></html>"
        
        ' In classic ASP, sendmail assumes properly configured SMTP server inside ssi_Functions_Core.asp
        Call SendMail("info@digitalresponse.com.au", rsUser("Email") & "", "Techlight MyDesk - Secure Auto-Login", mailBody)
    End If
    
    If rsUser.State = 1 Then rsUser.Close
    Set rsUser = Nothing
End If

Response.Redirect "/Default.asp?Msg=" & Server.URLEncode("If an account matches that address, a secure login link has been sent.")
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
