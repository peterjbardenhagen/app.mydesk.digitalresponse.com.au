<%

If Not(Request.Cookies("LoggedIn")) Then
	Response.Redirect "Portal/Logoff.asp?Msg=Request.Cookies+Expired"
	Response.End
End If

%>