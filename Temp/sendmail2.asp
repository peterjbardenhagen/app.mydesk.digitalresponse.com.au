<%

	Dim objCDO
	Dim iConf
	Dim Flds

	Const cdoSendUsingPort = 2

	Set objCDO = Server.CreateObject("CDO.Message")
	Set iConf = Server.CreateObject("CDO.Configuration")

	Set Flds = iConf.Fields
With Flds



'        .Item("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserver") = "techlight-com-au.mail.protection.outlook.com"
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = 25
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpconnectiontimeout") = 60
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 1
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpusessl") = true
'        .Item("http://schemas.microsoft.com/cdo/configuration/sendusername") = "bertb@techlight.com.au"
'        .Item("http://schemas.microsoft.com/cdo/configuration/sendpassword") = "mnzpznkrgrdodnmo"	



        .Item("http://schemas.microsoft.com/cdo/configuration/sendusing") = 2
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserver") = "smtp.sendgrid.net"
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpserverport") = 587
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpconnectiontimeout") = 60
        .Item("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate") = 1
'        .Item("http://schemas.microsoft.com/cdo/configuration/smtpusessl") = true
        .Item("http://schemas.microsoft.com/cdo/configuration/sendusername") = "apikey"
        .Item("http://schemas.microsoft.com/cdo/configuration/sendpassword") = "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"
		.Item("http://schemas.microsoft.com/cdo/configuration/sendtls") = true
	.Update
End With


	Dim strMsg

	strMsg=""

	For Each Item In Request.Form
		If Item = "LastName" Or Item = "FirstName" Or Item = "Message" Or Item = "Telephone" Or Item = "Email" Then
			strMsg = strMsg & Item & ": " & Request(Item) & "<br>"
		End If
	Next



	If (Request("Email") <> "yjdisantoyjdissemin@gmail.com" And Request("Email") <> "kayleighbpsteamship@gmail.com" And Request("Email") <> "katiakuznetsova17059@mail.ru") Then

		Set objCDO.Configuration = iConf
		With objCDO
			.From = "peterb@digitalresponse.com.au"
			.To = "peterb@digitalresponse.com.au"
			.Subject = "Website enquiry"
			.HtmlBody = strMsg
			.Send
		End With


		set message = nothing
	End If

	Response.Redirect("https://www.digitalresponse.com.au/thank-you.html?" & Request("good_url"))

%>

