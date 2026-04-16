<% 

Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expiresabsolute = ServerToEST(Now()) - 1 
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim dteUploadFrom
Dim decMinimumValue
Dim sql

dteUploadFrom = Trim(Replace(Request("UploadFrom"),"'","''"))
decMinimumValue = Trim(Replace(Request("MinimumValue"),"'","''"))

sql = "Update [Parameters] Set UploadFrom = '" & dteUploadFrom & "', MinimumValue = '" & FormatCurrency(decMinimumValue,2) & "'"

dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

Response.Redirect("/Portal.asp?Msg=Parameters+Updated")

%>