<%


If Not(CBool(Request.Cookies("LoggedIn"))) Then
	MyRedirect("/Default.asp?Msg=Request.Cookies+Expired")
End If

%>