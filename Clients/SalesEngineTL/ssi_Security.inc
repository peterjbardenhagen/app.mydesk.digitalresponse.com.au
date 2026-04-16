<%

If Not(Request.Cookies("LoggedIn") And Request.Cookies("ClientSettings")("Prefix") = "TT") Then
	Response.Redirect "Portal/Logoff.asp?Msg=Request.Cookies+Expired"
	Response.End
End If

%>