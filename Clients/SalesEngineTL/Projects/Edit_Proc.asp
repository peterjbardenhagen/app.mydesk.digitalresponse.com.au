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
<%

Dim intProjectId
Dim strProject
Dim strVisible
Dim sql

intProjectId = CLng(Request("ProjectId"))
strProject = Trim(Replace(Request("Project"),"'","''"))
strVisible = Trim(Request("Visible"))

sql = "Update Projects Set Project = '" & strProject & "', Visible = " & strVisible & " Where ProjectId = " & intProjectId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Project+updated")

%>