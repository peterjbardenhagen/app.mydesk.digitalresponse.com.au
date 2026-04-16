<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Dim lngRFQid
Dim strQueryString

lngRFQid = CLng(Request("RFQid"))

Set rsRFQ = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From RFQ Where RFQid = " & lngRFQid
Set rsRFQ = dbConn.Execute(sql)

strQueryString = "Code=" & rsRFQ("Code") & "&ContactId=" & rsRFQ("ContactId") & "&DivisionId=" & rsRFQ("DivisionId") & "&PODate=" & rsRFQ("RFQDate") & "&POStatusId=1&PriceExTotal=" & rsRFQ("TotalEx") & "&PriceIncTotal=" & rsRFQ("TotalInc")

' Loop through the RFQ Contents
Set rsRFQC = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From RFQContents Where RFQid = " & lngRFQid
Set rsRFQC = dbConn.Execute(sql)

Do Until rsRFQC.EOF
	strQueryString = strQueryString & "&Quantity=" & rsRFQC("Quantity") & "&Description=" & rsRFQC("Description") & "&PriceEx=" & rsRFQC("PriceEx") & "&PriceExSubTotal=" & rsRFQC("PriceExSubTotal")
	rsRFQC.MoveNext
Loop

rsRFQC.Close
Set rsRFQC = Nothing

rsRFQ.Close
Set rsRFQ = Nothing

strQueryString = Replace(strQueryString, Chr(10), "")

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/PurchaseOrders/Add2.asp?" & strQueryString)

%>