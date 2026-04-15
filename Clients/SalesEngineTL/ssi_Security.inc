<%

If Not(Request.Cookies("LoggedIn") And Request.Cookies("ClientSettings")("Prefix") = "TT") Then
	MyRedirect("Portal/Logoff.asp?Msg=Request.Cookies+Expired")
End If

%>