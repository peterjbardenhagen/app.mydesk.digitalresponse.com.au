<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim intDivisionId
Dim strProject
Dim sql

intDivisionId = CLng(Request("DivisionId"))
strProject = Trim(Replace(Request("Project"),"'","''"))

sql = "Insert Into Projects (DivisionId, Project, Visible) Values (" & intDivisionId & ", '" & strProject & "', 1)"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Project+added")

%>