<%

If Not(Request.Cookies("LoggedIn")) Then
	MyRedirect("/Clients/SalesEngine/Portal/Logoff.asp?Msg=Session+Expired")
End If

%>