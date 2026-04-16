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

Dim intCommentId
Dim sql

intCommentId = CLng(Request("CommentId"))

sql = "Update Comments Set FollowUpComplete = True Where CommentId = " & intCommentId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirectWithTarget Request.Cookies("ClientSettings")("WorkingDir") & "/PortalFrame.asp?Msg=Follow+up+marked+complete+successfully", "#FollowUps"

%>