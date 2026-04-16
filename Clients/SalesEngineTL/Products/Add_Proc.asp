<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim intDivisionId
Dim intProductCatId
Dim strProductCode
Dim strProductName
Dim strProductDesc
Dim decUnitCost
Dim decNettPrice
Dim decMinNettPrice
Dim strPerUnitPerDay
Dim sql

intDivisionId = CInt(Request("DivisionId"))
intProductCatId = CInt(Request("ProductCatId"))
strProductCode = Trim(Replace(Request("ProductCode"),"'","''"))
strProductName = Trim(Replace(Request("ProductName"),"'","''"))
strProductDesc = Trim(Replace(Request("ProductDesc"),"'","''"))
decUnitCost = Trim(Request("UnitCost"))
decNettPrice = Trim(Request("NettPrice"))
decMinNettPrice = Trim(Request("MinNettPrice"))
strPerUnitPerDay = Trim(Request("PerUnitPerDay"))

If Len(decUnitCost) = 0 Then
	decUnitCost = 0
End If

If Len(decNettPrice) = 0 Then
	decNettPrice = 0
End If

If Len(decMinNettPrice) = 0 Then
	decMinNettPrice = 0
End If

sql = "Insert Into Products (ProductCatId, DivisionId, PerUnitPerDay, ProductCode, ProductName, ProductDesc, UnitCost, NettPrice, MinNettPrice) Values (" & intProductCatId & ", " & intDivisionId & ", " & strPerUnitPerDay & ", '" & strProductCode & "', '" & strProductName & "', '" & strProductDesc & "', " & decUnitCost & ", " & decNettPrice & ", " & decMinNettPrice & ")"
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & intDivisionId & "&Msg=Product+added")

%>