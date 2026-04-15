<%
' Techlight MyDesk - Single client configuration
If InStr(Request.ServerVariables("URL"), "SalesEngineTL") > 0 Then
	Response.Cookies("Prefix") = "TL"
	Response.Cookies("Prefix").Expires = Date() + 1000
	Response.Cookies("State") = "AUS"
	Response.Cookies("State").Expires = Date() + 1000
	Response.Cookies("WorkingDir") = "/Clients/SalesEngineTL"
	Response.Cookies("WorkingDir").Expires = Date() + 1000
	Response.Cookies("ApprovalPassword") = "approveme"
	Response.Cookies("ApprovalPassword").Expires = Date() + 1000
End If
%>