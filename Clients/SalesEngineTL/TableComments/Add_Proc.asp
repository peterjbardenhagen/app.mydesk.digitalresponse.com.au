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

Dim intItemId
Dim intTableId
Dim strComment
Dim strFollowUpRequired
Dim dteFollowUpDate
Dim strToCode
Dim boolReply
Dim sql

intItemId = CLng(Request("ItemId"))
intTableId = CLng(Request("TableId"))
strFollowUpRequired = Trim(Request("FollowUpRequired"))
strComment = Trim(Replace(Request("Comment"), "'", "''"))

If strFollowUpRequired <> "" Then
	strFollowUpRequired = -1
	dteFollowUpDate = DBDate(Request("FollowUpDate"))
Else
	strFollowUpRequired = 0
	dteFollowUpDate = "01-Jan-01"
End If

strToCode = Trim(Request("ToCode"))

If strToCode <> "" Then
	sql = "Insert Into TMail ([Date], ToCode, FromCode, Subject, Message, Read) Values ('" & ServerToEST(Now()) & "', '" & strToCode & "', '" & Request.Cookies("UserSettings")("Code") & "', 'Comment from Quote # " & intItemId & "', '" & strComment & "', 0)"
	dbConn.Execute(sql)
End If

sql = "Insert Into Comments (DateEntered, ItemId, [TableId], Comment, FromCode, FollowUpDate, FollowUpRequired) Values ('" & ServerToEST(Now()) & "', " & intItemId & ", " & intTableId & ", '" & strComment & "', '" & Request.Cookies("UserSettings")("Code") & "', '" & dteFollowUpDate & "', " & strFollowUpRequired & ")"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/TableComments/Comments.asp?ItemId=" & intItemId & "&TableId=" & intTableId)

%>