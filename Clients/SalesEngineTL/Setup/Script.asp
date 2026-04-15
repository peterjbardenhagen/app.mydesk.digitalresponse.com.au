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

sql = "Select * From Products Where DivisionId = 7"
Set rs = dbConn.Execute(sql)

Do Until rs.EOF
	sql = "Update Products Set NettPrice = " & rs("MinNettPrice") & " Where ProductId = " & rs("ProductId")
	dbConn.Execute(sql)
	rs.MoveNext
Loop

rs.Close
Set rs = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp")

%>