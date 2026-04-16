<%


If Not(CBool(Request.Cookies("LoggedIn"))) Then
	Response.Redirect "/Default.asp?Msg=Request.Cookies+Expired"
	Response.End
End If

%>