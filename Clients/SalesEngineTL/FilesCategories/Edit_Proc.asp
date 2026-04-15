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

Dim intCategoryId
Dim strCategory
Dim strUserLevelAccess
Dim strDivisionAccess
Dim strUserAccess
Dim sql
Dim arrUserLevelAccess
Dim arrDivisionAccess
Dim arrUserAccess
Dim rsCat
Dim Item

intCategoryId = CLng(Request("CategoryId"))
strCategory = Trim(Replace(Request("Category"),"'","''"))
strUserLevelAccess = Trim(Request("UserLevelAccess"))
strDivisionAccess = Trim(Request("DivisionAccess"))
strUserAccess = Trim(Request("UserAccess"))

sql = "Update FilesCategories Set Category = '" & strCategory & "' Where CategoryId = " & intCategoryId
dbConn.Execute(sql)

arrUserLevelAccess = Split(strUserLevelAccess, ",")
arrDivisionAccess = Split(strDivisionAccess, ",")
arrUserAccess = Split(strUserAccess, ",")

sql = "Delete From FilesCategoriesUserLevelAccess Where CategoryId = " & intCategoryId
dbConn.Execute(sql)

sql = "Delete From FilesCategoriesDivisionAccess Where CategoryId = " & intCategoryId
dbConn.Execute(sql)

sql = "Delete From FilesCategoriesUserAccess Where CategoryId = " & intCategoryId
dbConn.Execute(sql)

For Each Item In arrUserLevelAccess
	sql = "Insert Into FilesCategoriesUserLevelAccess (CategoryId, UserTypeId) Values (" & intCategoryId & ", " & Item & ")"
	dbConn.Execute(sql)
Next

For Each Item In arrDivisionAccess
	sql = "Insert Into FilesCategoriesDivisionAccess (CategoryId, DivisionId) Values (" & intCategoryId & ", " & Item & ")"
	dbConn.Execute(sql)
Next

For Each Item In arrUserAccess
	sql = "Insert Into FilesCategoriesUserAccess (CategoryId, UserId) Values (" & intCategoryId & ", " & Item & ")"
	dbConn.Execute(sql)
Next

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Category+updated")

%>