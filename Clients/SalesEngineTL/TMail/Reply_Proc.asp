<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim strToCode
Dim strSubject
Dim strMessage
Dim sql

strToCode = Trim(Request("ToCode"))
strSubject = Trim(Replace(Request("Subject"),"'","''"))
strMessage = Trim(Replace(Request("Message"),"'","''"))

sql = "Insert Into TMail ([Date], ToCode, FromCode, Subject, Message, Read) Values ('" & ServerToEST(Now()) & "', '" & strToCode & "', '" & Request.Cookies("UserSettings")("Code") & "', '" & strSubject & "', '" & strMessage & "', 0)"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Message+replied+to")

%>