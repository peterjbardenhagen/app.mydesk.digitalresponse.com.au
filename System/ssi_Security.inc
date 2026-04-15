<%


If Not(CBool(Request.Cookies("LoggedIn"))) Then
	MyRedirect("/Default2.asp?Msg=Request.Cookies+Expired")
End If

%>