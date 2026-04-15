<% 

Option Explicit

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

Dim strCategory
Dim strUserLevelAccess
Dim strDivisionAccess
Dim strUserAccess
Dim sql
Dim intNewCategoryId
Dim arrUserLevelAccess
Dim arrDivisionAccess
Dim arrUserAccess
Dim rsCat
Dim Item

strCategory = Trim(Replace(Request("Category"),"'","''"))
strUserLevelAccess = Trim(Request("UserLevelAccess"))
strDivisionAccess = Trim(Request("DivisionAccess"))
strUserAccess = Trim(Request("UserAccess"))

sql = "Insert Into FilesCategories (Category) Values ('" & strCategory & "');"
dbConn.Execute(sql)

sql = "Select @@IDENTITY As NewCategoryId"
Set rsCat = dbConn.Execute(sql)
intNewCategoryId = rsCat("NewCategoryId")

rsCat.Close
Set rsCat = Nothing

arrUserLevelAccess = Split(strUserLevelAccess, ",")

For Each Item In arrUserLevelAccess
	sql = "Insert Into FilesCategoriesUserLevelAccess (CategoryId, UserTypeId) Values (" & intNewCategoryId & ", " & Item & ")"
	dbConn.Execute(sql)
Next

arrDivisionAccess = Split(strDivisionAccess, ",")

For Each Item In arrDivisionAccess
	sql = "Insert Into FilesCategoriesDivisionAccess (CategoryId, DivisionId) Values (" & intNewCategoryId & ", " & Item & ")"
	dbConn.Execute(sql)
Next

arrUserAccess = Split(strUserAccess, ",")

For Each Item In arrUserAccess
	sql = "Insert Into FilesCategoriesUserAccess (CategoryId, UserId) Values (" & intNewCategoryId & ", " & Item & ")"
	dbConn.Execute(sql)
Next

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Category+added")

%>