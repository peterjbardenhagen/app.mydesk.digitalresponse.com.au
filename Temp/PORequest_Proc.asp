<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

Dim strReqContactNameNo, strReqDivision
Dim strRegistrationNo, strVehicle, strMileage
Dim strContactNameNo, strRequestNotes
Dim strRepair1, strRepair2, strRepair3
Dim strPrefContactNameNo, strPrefCompany, strReqLocation, strReqState

strReqContactNameNo = Trim(Request("ReqContactNameNo"))
strReqDivision = Trim(Request("ReqDivision"))
strReqLocation = Trim(Request("ReqLocation"))
strReqState = Trim(Request("ReqState"))
strRegistrationNo = Trim(Request("RegistrationNo"))
strVehicle = Trim(Request("Vehicle"))
strMileage = Trim(Request("Mileage"))
strContactNameNo = Trim(Request("ContactNameNo"))
strRepair1 = Trim(Request("Repair1"))
strRepair2 = Trim(Request("Repair2"))
strRepair3 = Trim(Request("Repair3"))
strRequestNotes = Trim(Request("RequestNotes"))
strPrefContactNameNo = Trim(Request("PrefContactNameNo"))
strPrefCompany = Trim(Request("PrefCompany"))
strPrefAppDate = Trim(Request("PrefAppDate"))
strPrefAppTime = Trim(Request("PrefAppTime"))
strArrowboardManufacturer = Trim(Request("ArrowboardManufacturer"))
strArrowboardSerialNo = Trim(Request("ArrowboardSerialNo"))
strArrowboardFault = Trim(Request("ArrowboardFault"))

If strRepair1 <> "" Then strRepair1 = "Yes" Else strRepair1 = "No"
If strRepair2 <> "" Then strRepair2 = "Yes" Else strRepair2 = "No"
If strRepair3 <> "" Then strRepair3 = "Yes" Else strRepair3 = "No"

strBody = "This is the result of a Purchase Order Request for Vehicle Maintenance.<br><br>" & vbcrlf
strBody = strBody & "<b>Requested By: </b>" & strReqContactNameNo & "<br>" & vbcrlf
strBody = strBody & "<b>Requested By Division: </b>" & strReqDivision & "<br>" & vbcrlf
strBody = strBody & "<b>Requested By Location: </b>" & strReqLocation & "<br>" & vbcrlf
strBody = strBody & "<b>Requested By State: </b>" & strReqState & "<br>" & vbcrlf
strBody = strBody & "<b>Registration Number: </b>" & strRegistrationNo & "<br>" & vbcrlf
strBody = strBody & "<b>Vehicle: </b>" & strVehicle & "<br>" & vbcrlf
strBody = strBody & "<b>Mileage (km): </b>" & strMileage & "<br>" & vbcrlf
strBody = strBody & "<b>Contact Details: </b>" & strContactNameNo & "<br>" & vbcrlf
strBody = strBody & "<b>Breakdown: </b>" & strRepair1 & "<br>" & vbcrlf
strBody = strBody & "<b>Repair/Maintenance: </b>" & strRepair2 & "<br>" & vbcrlf
strBody = strBody & "<b>Replacement/Spare Parts: </b>" & strRepair3 & "<br>" & vbcrlf
strBody = strBody & "<b>Arrowboard Manufacturer: </b>" & strManufacturer & "<br>" & vbcrlf
strBody = strBody & "<b>Arrowboard Serial No.: </b>" & strArrowboardSerialNo & "<br>" & vbcrlf
strBody = strBody & "<b>Arrowboard Fault: </b>" & strArrowboardFault & "<br>" & vbcrlf
strBody = strBody & "<b>Request: </b>" & strRequestNotes & "<br>" & vbcrlf
If strPrefContactNameNo <> "" Or strPrefCompany <> "" Then
	strBody = strBody & "<b>Preferred Supplier's Contact Details: </b>" & strPrefContactNameNo & "<br>" & vbcrlf
	strBody = strBody & "<b>Preferred Supplier's Company Name: </b>" & strPrefCompany & "<br>" & vbcrlf
End If
strBody = strBody & "<b>Preferred Appointment Date: </b>" & strPrefAppDate & "<br>" & vbcrlf
strBody = strBody & "<b>Preferred Appointment Time: </b>" & strPrefAppTime & "<br>" & vbcrlf

Select Case strReqDivision
    Case "Traffic Management Division"
'        SendMail "fleetmanagement@trafficltd.com.au", "peta-leee@trafficltd.com.au", strReqState & " - Purchase Order Request", strBody
'        SendMail "fleetmanagement@trafficltd.com.au", "conp@trafficltd.com.au", strReqState & " - Purchase Order Request", strBody
        Select Case strReqState
            Case "Queensland"
	            SendMail "fleetmanagement@trafficltd.com.au", "peta-leee@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD(QLD)", strBody
	            SendMail "fleetmanagement@trafficltd.com.au", "conp@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD(QLD)", strBody
            Case "New South Wales"
	            SendMail "fleetmanagement@trafficltd.com.au", "peta-leee@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD(NSW)", strBody
	            SendMail "fleetmanagement@trafficltd.com.au", "conp@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD(NSW)", strBody
            Case "Victoria"
	            SendMail "fleetmanagement@trafficltd.com.au", "billh@trafficltd.com.au", strReqState & " - Purchase Order Request- TMD(VIC)", strBody
	            SendMail "fleetmanagement@trafficltd.com.au", "peta-leee@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD(VIC)", strBody	            
	            SendMail "fleetmanagement@trafficltd.com.au", "conp@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD(VIC)", strBody
            Case "South Australia"
	            SendMail "fleetmanagement@trafficltd.com.au", "peta-leee@trafficltd.com.au", strReqState & " - Purchase Order Request- TMD(SA)", strBody
	            SendMail "fleetmanagement@trafficltd.com.au", "conp@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD(SA)", strBody
            Case Else
	            SendMail "fleetmanagement@trafficltd.com.au", "conp@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD", strBody
	  	    SendMail "fleetmanagement@trafficltd.com.au", "peta-leee@trafficltd.com.au", strReqState & " - Purchase Order Request - TMD", strBody
        End Select
    Case Else
        SendMail "fleetmanagement@trafficltd.com.au", "conp@trafficltd.com.au", strReqState & " - Purchase Order Request", strBody
        SendMail "fleetmanagement@trafficltd.com.au", "peta-leee@trafficltd.com.au", strReqState & " - Purchase Order Request", strBody
End Select        


Sub SendMail(strFromEmail, strToEmail, strSubject, strBody)
	dim smtpServer, smtpPort
	smtpServer = "techlight-com-au.mail.protection.outlook.com"
	smtpPort = 587

	Set message = CreateObject ("JMail.Message")
	message.From = strFromEmail
	message.Subject = strSubject
	message.AddRecipient strToEmail
	message.ContentType = "text/html" ' or you can put 'text/plain' for plain text emessage 
	message.ISOEncodeHeaders = false 
	message.ContentTransferEncoding = "8bit"
	message.Body = strBody
	message.MailServerUsername = "bertb@techlight.com.au"
	message.MailServerPassword = "mnzpznkrgrdodnmo"
	message.Send(smtpServer & ":" & smtpPort)

	set message = nothing
End Sub

'Cleanup
Set ObjCDO = Nothing
Set iConf = Nothing
Set Flds = Nothing

If err.number > 0 Then
	Response.Redirect("http://www.trafficltd.com.au/porequest_failed.htm")
Else
	Response.Redirect("http://www.trafficltd.com.au/porequest_success.htm")
End If

%>