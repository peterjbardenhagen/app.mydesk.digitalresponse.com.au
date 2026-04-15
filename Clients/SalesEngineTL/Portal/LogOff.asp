<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<%

Session.Abandon

For Each Item In Request.Cookies
	Response.Cookies(Item).Expires = Date() - 1
	Response.Cookies(Item) = ""
Next

If Request("Msg") = "" Then
	MyRedirect("/?Msg=You+have+logged+off+successfully")
Else
	MyRedirect("/Default.asp?Msg=" & Request("Msg"))
End If

%>