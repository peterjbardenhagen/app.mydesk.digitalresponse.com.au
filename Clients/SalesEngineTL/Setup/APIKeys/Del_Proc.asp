<%
Option Explicit

' Techlight MyDesk - Delete API Key Process

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
isAdmin = (Request.Cookies("UserSettings")("UserTypeId") >= 5)

If Not isAdmin Then
	Response.Redirect("../../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<%

' Get API Key ID
Dim lngAPIKeyId
lngAPIKeyId = CLng(Request("APIKeyId"))

If lngAPIKeyId = 0 Then
	MyRedirect("Default.asp?Msg=Error:+Invalid+API+Key+ID")
End If

' Verify the key exists before deleting
Dim rsCheck, sqlCheck
sqlCheck = "SELECT APIKeyId, KeyName FROM APIKeys WHERE APIKeyId = " & lngAPIKeyId
Set rsCheck = dbConn.Execute(sqlCheck)

If rsCheck.BOF And rsCheck.EOF Then
	rsCheck.Close
	Set rsCheck = Nothing
	MyRedirect("Default.asp?Msg=Error:+API+Key+not+found")
End If

Dim strKeyName
strKeyName = rsCheck("KeyName")

rsCheck.Close
Set rsCheck = Nothing

' Delete the API key
Dim sqlDelete
sqlDelete = "DELETE FROM APIKeys WHERE APIKeyId = " & lngAPIKeyId
dbConn.Execute sqlDelete

' Redirect back with success message
MyRedirect("Default.asp?Msg=API+Key+'" & Server.URLEncode(strKeyName) & "'+deleted+successfully")

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
