<% 
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="System/ssi_Functions.asp"-->
<%

Response.Redirect("/Portal.asp")

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->