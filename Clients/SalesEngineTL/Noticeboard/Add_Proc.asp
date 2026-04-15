<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim dteDateExpires
Dim strHeading
Dim strMessage
Dim sql

dteDateExpires = DBDate(Request("DateExpires"))
strHeading = Trim(Replace(Request("Heading"),"'","''"))
strMessage = Trim(Replace(Request("Message"),"'","''"))

sql = "Insert Into Noticeboard (Code, Heading, Message, DateExpires) Values ('" & Request.Cookies("UserSettings")("Code") & "', '" & strHeading & "', '" & strMessage & "', '" & dteDateExpires & "')"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Notice+added")

%>