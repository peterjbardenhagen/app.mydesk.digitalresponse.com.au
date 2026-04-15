<%
' Techlight MyDesk - Single client configuration

'If InStr(Request.ServerVariables("URL"), "SalesEngineTL") > 0 Then
	Response.Cookies("ClientSettings")("Prefix") = "TL"
	Response.Cookies("ClientSettings")("State") = "AUS"
	Response.Cookies("ClientSettings")("WorkingDir") = "/Clients/SalesEngineTL"
	Response.Cookies("ClientSettings").Expires = Date() + 1000
	Response.Cookies("ApprovalPassword") = "approveme"
	Response.Cookies("ApprovalPassword").Expires = Date() + 1000
'End If
%>