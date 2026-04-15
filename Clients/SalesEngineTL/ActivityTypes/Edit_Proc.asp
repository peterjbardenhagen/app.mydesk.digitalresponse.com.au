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

Dim intActivityTypeId
Dim strActivityCode
Dim strActivityType
Dim strFormRequired
Dim strVisible
Dim sql

intActivityTypeId = CLng(Request("ActivityTypeId"))
strActivityCode = Trim(Replace(Request("ActivityCode"),"'","''"))
strActivityType = Trim(Replace(Request("ActivityType"),"'","''"))
strFormRequired = Trim(Request("FormRequired"))
strVisible = 1

sql = "Update ActivityTypes Set ActivityCode = '" & strActivityCode & "', ActivityType = '" & strActivityType & "', FormRequired = " & strFormRequired & ", Visible = " & strVisible & " Where ActivityTypeId = " & intActivityTypeId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Activity+Type+updated")

%>