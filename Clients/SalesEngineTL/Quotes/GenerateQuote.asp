<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngQid = CLng(Request("Qid"))
intMode = CInt(Request("Mode"))
strAttention = Request("Attention") & ""
strToEmail = Request("ToEmail") & ""
strFromFax = Request("FromFax") & ""
strToFax = Request("ToFax") & ""
strWorkingDir = Request("WorkingDir") & ""
'strCurrencyName = Request("CurrencyName") & ""
'dblCurrencyRate = CDbl(Request("CurrencyRate"))
'strCurrencyPrefix = Request("CurrencyPrefix") & ""
strNotes = Trim(Replace(Replace(Replace(Replace(Request("Notes"),CHR(10),"<BR>"),CHR(13),"<BR>"),vbclrf,"<BR>"),"&","&amp;")) & ""

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, Quotes.DivisionId As QDivisionId, Users.*, QuoteCOS.QuoteCOSFile, QuoteStatus.QuoteStatus From ((Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) LEFT OUTER JOIN QuoteCOS ON Quotes.QuoteCOSId = QuoteCOS.QuoteCOSId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(sql)

strCode = rsQu("Code")

If rsQu("QuoteStatusId") = 1 Then
	sql = "Update Quotes Set QuoteStatusId = 2 Where Qid = " & lngQid
	dbConn.Execute(sql)
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

Response.Redirect("/MyDeskASPNet/GenerateQuote.aspx?Mode=" & intMode & "&Qid=" & lngQid & "&Attention=" & strAttention & "&ToEmail=" & strToEmail & "&FromFax=" & strFromFax & "&ToFax=" & strToFax & "&Notes=" & strNotes & "&WorkingDir=" & strWorkingDir)

%>