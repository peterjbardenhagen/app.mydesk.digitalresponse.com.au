<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.CacheControl = "no-store, private, must-revalidate"

On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngPOId = CLng(Request("POId"))
strAttention = Request("Attention") & ""
strToEmail = Request("ToEmail") & ""
strFromFax = Request("FromFax") & ""
strToFax = Request("ToFax") & ""
strWorkingDir= Request("WorkingDir") & ""
strNotes = Request("Notes") & ""
intMode = Request("Mode")
strNotes = Trim(Replace(Replace(Replace(Replace(Request("Notes"),CHR(10),"<BR>"),CHR(13),"<BR>"),vbclrf,"<BR>"),"&","&amp;")) & ""


%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Update PurchaseOrders Set POStatusId = 4 Where POStatusId < 4 And POid = " & lngPOid
dbConn.Execute(sql)

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

Response.Redirect("/MyDeskASPNet/GeneratePurchaseOrder.aspx?Mode=" & intMode & "&POid=" & lngPOid & "&Attention=" & strAttention & "&ToEmail=" & strToEmail & "&FromFax=" & strFromFax & "&ToFax=" & strToFax & "&CurrencyName=" & strCurrencyName & "&Notes=" & strNotes & "&WorkingDir=" & strWorkingDir)
'Response.Redirect("GeneratePO.aspx?POId=" & lngPOId & "&Attention=" & strAttention & "&ToEmail=" & strToEmail & "&FromFax=" & strFromFax & "&ToFax=" & strToFax & "&Notes=" & strNotes & "&WorkingDir=" & strWorkingDir)

%>