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

Dim intFileId
Dim strDescription
Dim intCategoryId
Dim sql

intFileId = CLng(Request("FileId"))
strDescription = Trim(Replace(Request("Description"),"'","''"))
intCategoryId = CLng(Request("CategoryId"))

sql = "Update Files Set Description = '" & strDescription & "', CategoryId = '" & intCategoryId & "', LastModifiedDate = ServerToEST(Now()) Where FileId = " & intFileId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default2.asp?CategoryId=" & intCategoryId & "&Msg=File+updated")

%>