<%
' Techlight MyDesk - Edit API Key Process

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

' Security check
If Not Request.Cookies("UserSettings")("Manager") Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

Dim isAdmin
isAdmin = (Request.Cookies("UserSettings")("UserTypeId") > 5)

If Not isAdmin Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<%

' Get form data
Dim lngAPIKeyId, strKeyName, strDescription, intIsActive
lngAPIKeyId = CLng(Request("APIKeyId"))
strKeyName = Trim(Replace(Request("KeyName"), "'", "''"))
strDescription = Trim(Replace(Request("Description"), "'", "''"))

If Request("IsActive") = "-1" Then
	intIsActive = -1
Else
	intIsActive = 0
End If

' Validate required fields
If lngAPIKeyId = 0 Then
	MyRedirect("Default.asp?Msg=Error:+Invalid+API+Key+ID")
End If

If strKeyName = "" Then
	MyRedirect("Edit.asp?APIKeyId=" & lngAPIKeyId & "&Msg=Error:+Key+Name+is+required")
End If

' Verify the key exists
Dim rsCheck, sqlCheck
sqlCheck = "SELECT APIKeyId FROM APIKeys WHERE APIKeyId = " & lngAPIKeyId
Set rsCheck = dbConn.Execute(sqlCheck)

If rsCheck.BOF And rsCheck.EOF Then
	rsCheck.Close
	Set rsCheck = Nothing
	MyRedirect("Default.asp?Msg=Error:+API+Key+not+found")
End If

rsCheck.Close
Set rsCheck = Nothing

' Update the API key
Dim sqlUpdate
sqlUpdate = "UPDATE APIKeys SET "
sqlUpdate = sqlUpdate & "KeyName = '" & strKeyName & "', "
sqlUpdate = sqlUpdate & "Description = '" & strDescription & "', "
sqlUpdate = sqlUpdate & "IsActive = " & intIsActive & " "
sqlUpdate = sqlUpdate & "WHERE APIKeyId = " & lngAPIKeyId

dbConn.Execute sqlUpdate

' Redirect back with success message
MyRedirect("Default.asp?Msg=API+Key+updated+successfully")

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
