<%

'On Error Resume Next

dim smtpServer, smtpPort
smtpServer = "techlight-com-au.mail.protection.outlook.com"
smtpPort = 25

Set message = CreateObject ("JMail.Message") 
message.From = "peterb@digitalresponse.com.au"
message.Subject = Request("Subject")

message.ContentType = "text/html" ' or you can put 'text/plain' for plain text emessage 
message.ISOEncodeHeaders = false 
message.ContentTransferEncoding = "8bit"
	message.MailServerUserName = "bertb@techlight.com.au"
	message.MailServerPassword = "mnzpznkrgrdodnmo"


Dim recipient, email
recipient = Request("Recipient")
email = Request("Recipient")

Response.Write(Request("Recipient")+"<br/>")
Response.Write(Request("State")+"<br/>")

'Response.Write(instr(Request("Phone"),"VIC"))



If instr(Request("State"),"SA") > 0 Then
	'Response.Write(email)
	recipient = Replace(email,"state","SA")
	Response.Write(recipient)
End If

'Response.Write(Request("State"))
'Response.Write(instr("VIC",Request("Phone")) > 0)
'Response.Write(instr("VIC",Request("Phone")))

If instr(Request("State"),"VIC") > 0 Then
	'Response.Write(email)
	recipient = Replace(email,"state","VIC")
	Response.Write(recipient)
End If
If instr(Request("State"),"ACT") > 0 Then
	'Response.Write(email)
	recipient = Replace(email,"state","ACT")
	Response.Write(recipient)
End If
If instr(Request("State"),"NSW") > 0 Then
	'Response.Write(email)
	recipient = Replace(email,"state","NSW")
	Response.Write(recipient)
End If
If instr(Request("State"),"QLD") > 0 Then
	'Response.Write(email)
	recipient = Replace(email,"state","QLD")
	Response.Write(recipient)
End If

If instr(recipient,"state") > 0 Then
	'Response.Write(email)
	recipient = email
	Response.Write(recipient)
End If

'message.AddRecipient "testing.counton.com.au"


message.AddRecipient recipient


Dim strMsg
strMsg=""

For Each Item In Request.Form
	strMsg = strMsg & Item & ": " & Request(Item) & "<br>"
Next

message.Body = strMsg
message.Send(smtpServer & ":" & smtpPort)

set message = nothing


'If Err.Number > 0 Then
'	Response.Write(Err.Description)
'Else
	Response.Redirect("http://www.trafficltd.com.au/thankyou.aspx")
'End If
%>