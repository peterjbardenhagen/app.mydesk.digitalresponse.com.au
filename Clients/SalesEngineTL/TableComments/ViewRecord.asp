<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim intCommentId
intCommentId = CLng(Request("CommentId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%
Dim rs
Dim sql
Dim strPathToView

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Tables.PathToView, ItemId FROM Comments INNER JOIN Tables ON Comments.TableId = Tables.TableId"
Set rs = dbConn.Execute(sql)

strPathToView = Request.Cookies("ClientSettings")("WorkingDir") & "/" & Replace(rs("PathToView"), "[ID]", rs("ItemId"))

rs.Close
Set rs = Nothing
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect(strPathToView)

%>