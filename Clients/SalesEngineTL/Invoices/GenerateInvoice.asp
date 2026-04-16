<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.CacheControl = "no-store, private, must-revalidate"

On error resume next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngInvoiceId = CLng(Request("InvoiceId"))
intMode = 1 'CInt(Request("Mode"))
strAttention = Request("Attention") & ""
strToEmail = Request("ToEmail") & ""
strWorkingDir= Request("WorkingDir") & ""
'strCurrencyName = Request("CurrencyName") & ""
'dblCurrencyRate = CDbl(Request("CurrencyRate"))
'strCurrencyPrefix = Request("CurrencyPrefix") & ""
strNotes = Trim(Replace(Replace(Replace(Replace(Request("Notes"),CHR(10),"<BR>"),CHR(13),"<BR>"),vbclrf,"<BR>"),"&","&amp;")) & ""

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Set rsInv = Server.CreateObject("ADODB.RecordSet")
sql = "Select Invoices.*, Invoices.CustomerNotes As CN, Invoices.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, InvoiceStatus.InvoiceStatus From ((Invoices INNER JOIN Users ON Invoices.Code = Users.Code) INNER JOIN InvoiceStatus ON Invoices.InvoiceStatusId = InvoiceStatus.InvoiceStatusId) Where InvoiceId = " & lngInvoiceId
Set rsInv = dbConn.Execute(sql)

strCode = rsInv("Code")

If rsInv("InvoiceStatusId") = 1 Then
	sql = "Update Invoices Set InvoiceStatusId = 2 Where InvoiceId = " & lngInvoiceId
	dbConn.Execute(sql)
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

Response.Redirect("/MyDeskASPNet/GenerateInvoice.aspx?Mode=" & intMode & "&InvoiceId=" & lngInvoiceId & "&Attention=" & strAttention & "&ToEmail=" & strToEmail & "&FromFax=" & strFromFax & "&ToFax=" & strToFax & "&Notes=" & strNotes & "&WorkingDir=" & strWorkingDir)

%>