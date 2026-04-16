<%
' Techlight MyDesk - Add API Key Process

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

' Get form data
Dim strKeyName, strDescription, intIsActive
strKeyName = Trim(Replace(Request("KeyName"), "'", "''"))
strDescription = Trim(Replace(Request("Description"), "'", "''"))

If Request("IsActive") = "-1" Then
	intIsActive = -1
Else
	intIsActive = 0
End If

' Validate required fields
If strKeyName = "" Then
	MyRedirect("Add.asp?Msg=Error:+Key+Name+is+required")
End If

' Generate a unique API key
Function GenerateAPIKey()
	Dim strKey, strChars, i, intLength
	strChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
	intLength = 32
	strKey = "tl_"
	
	Randomize
	For i = 1 To intLength
		strKey = strKey & Mid(strChars, Int((Len(strChars) * Rnd) + 1), 1)
	Next
	
	GenerateAPIKey = strKey
End Function

' Generate the key
Dim strAPIKey
strAPIKey = GenerateAPIKey()

' Check if key already exists (very unlikely but possible)
Dim rsCheck, sqlCheck
sqlCheck = "SELECT APIKeyId FROM APIKeys WHERE APIKey = '" & strAPIKey & "'"
Set rsCheck = dbConn.Execute(sqlCheck)

Do While Not (rsCheck.BOF And rsCheck.EOF)
	' Regenerate if collision
	strAPIKey = GenerateAPIKey()
	rsCheck.Close
	sqlCheck = "SELECT APIKeyId FROM APIKeys WHERE APIKey = '" & strAPIKey & "'"
	Set rsCheck = dbConn.Execute(sqlCheck)
Loop

rsCheck.Close
Set rsCheck = Nothing

' Insert the new API key
Dim sqlInsert
sqlInsert = "INSERT INTO APIKeys (KeyName, Description, APIKey, IsActive, DateCreated, CreatedBy) VALUES ("
sqlInsert = sqlInsert & "'" & strKeyName & "', "
sqlInsert = sqlInsert & "'" & strDescription & "', "
sqlInsert = sqlInsert & "'" & strAPIKey & "', "
sqlInsert = sqlInsert & intIsActive & ", "
sqlInsert = sqlInsert & "#" & ServerToEST(Now()) & "#, "
sqlInsert = sqlInsert & "'" & Request.Cookies("UserSettings")("Code") & "')"

dbConn.Execute sqlInsert

' Redirect to success page showing the generated key
MyRedirect("ViewKey.asp?APIKey=" & strAPIKey & "&Msg=API+Key+created+successfully")

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
