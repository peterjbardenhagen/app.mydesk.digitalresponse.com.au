<%

' Check if user is logged in with error handling
Dim isLoggedIn
isLoggedIn = False

On Error Resume Next
If Not Request.Cookies("LoggedIn") Is Nothing Then
	If Not IsEmpty(Request.Cookies("LoggedIn")) Then
		Dim cookieValue
		cookieValue = Request.Cookies("LoggedIn")
		If IsNumeric(cookieValue) Then
			isLoggedIn = CBool(cookieValue)
		Else
			If LCase(cookieValue) = "true" Then
				isLoggedIn = True
			End If
		End If
	End If
End If
Err.Clear
On Error GoTo 0

If Not isLoggedIn Then
	Response.Redirect "/Default.asp?Msg=Request.Cookies+Expired"
	Response.End
End If

%>