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
<%

Dim intProductId
Dim intProductCatId
Dim intDivisionId
Dim strProductCode
Dim strProductName
Dim strProductDesc
Dim decUnitCost
Dim decNettPrice
Dim decMinNettPrice
Dim strPerUnitPerDay
Dim sql

intProductId = CLng(Request("ProductId"))
intProductCatId = CLng(Request("ProductCatId"))
intDivisionId = CInt(Request("DivisionId"))
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

sql = "Update Products Set ProductCatId = " & intProductCatId & ", DivisionId = " & intDivisionId & ", ProductCode = '" & strProductCode & "', ProductName = '" & strProductName & "', ProductDesc = '" & strProductDesc & "', UnitCost = " & decUnitCost & ", NettPrice = " & decNettPrice & ", MinNettPrice = " & decMinNettPrice & ", PerUnitPerDay = " & strPerUnitPerDay & " Where ProductId = " & intProductId
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & intDivisionId & "&Msg=Product+updated")

%>