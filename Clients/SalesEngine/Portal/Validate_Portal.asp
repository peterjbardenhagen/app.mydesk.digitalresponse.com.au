<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<%

If InStr(Request.ServerVariables("SERVER_NAME"),"dev") > 0 Or InStr(Request.ServerVariables("SERVER_NAME"),"stage") > 0 Then
	Session("SMTPServer") = "techlight-com-au.mail.protection.outlook.com"
Else
	Session("SMTPServer") = "techlight-com-au.mail.protection.outlook.com"
End If

Session("LoggedIn") = False

Session("Stylesheet") = "Style.css"
Session("MainBgColor") = "#cccccc"
Session("BgColor2") = "#005b89"
Session("BgColor3") = "#005b89"
Session("Prefix") = "TL"
Session("State") = "ALL"
Session("PortalCompany") = "Techlight"
Session("WorkingDir") = "/Clients/SalesEngineTL"
Session("HomeColor1") = "#005b89"
MyRedirect("/Clients/SalesEngineTL")

%>