<%

Response.Expires = -1

%>
<!--#include virtual="/SalesEngine/System/ssi_Security.inc"-->
<!--#include virtual="/SalesEngine/System/ssi_dbConn_open.inc"-->
<%

Dim lngQid
Dim strInitials
Dim strComment
Dim strSql

lngQid = CLng(Request("Qid"))
strInitials = Trim(Replace(Request("Initials"),"'","''"))
strComment = Trim(Replace(Request("Comment"),"'","''"))

strSql = "Insert Into ProjectHistory (Qid, Initials, Comment) Values (" & lngQid & ", '" & strInitials & "', '" & strComment & "')"
dbConn.Execute(strSql)

%>
<!--#include virtual="/SalesEngine/System/ssi_dbConn_close.inc"-->
<%

Response.Redirect("Comments.asp?Qid=" & lngQid)

%>