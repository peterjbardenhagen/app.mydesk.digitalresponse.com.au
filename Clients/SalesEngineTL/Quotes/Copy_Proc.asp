<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

'On Error Resume Next

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%

Dim lngId
Dim sql
Dim strMsg
Dim strErrorDescription

lngId = CLng(Request("Id"))

Function FixQuotes(str)
	If Not(IsNull(str)) And Len(str) > 1 Then
		str = Replace(str,"'","''")
	End If
	FixQuotes = str
End Function

' Copy Quote

sql = "Select * From Quotes Where Qid = " & lngId
Set rs = dbConn.Execute(sql)

Dim dteDate
dteDate = Now

sql = "Insert Into Quotes (RealQid, Revision, Code, ContactId, DivisionId, QuoteDate, QuoteStatusId, Attention, Reference, Terms, Delivery, Validity, InternalNotes, CustomerNotes, PPriceTotal, UnitCostTotal, NettPriceTotal, Margin, QuoteCOSId, QuoteNumber, IncludeInReporting, POid) Values (0, 0, '" & rs("Code") & "', " & rs("ContactId") & ", " & rs("DivisionId") & ", '" & dteDate & "', " & rs("QuoteStatusId") & ", '" & FixQuotes(rs("Attention")) & "', '" & FixQuotes(rs("Reference")) & "', '" & FixQuotes(rs("Terms")) & "', '" & FixQuotes(rs("Delivery")) & "', '" & rs("Validity") & "', '" & FixQuotes(rs("InternalNotes")) & "', '" & FixQuotes(rs("CustomerNotes")) & "', " & rs("PPriceTotal") & ", " & rs("UnitCostTotal") & ", " & rs("NettPriceTotal") & ", " & rs("Margin") & ", " & rs("QuoteCOSId") & ", '" & rs("QuoteNumber") & "', " & rs("IncludeInReporting") & ", " & rs("POId") & ")"
dbConn.Execute(sql)

sql = "Select Top 1 Qid From Quotes Order By Qid DESC"
Set rs2 = dbConn.Execute(sql)

' Copy Quote Contents

sql = "Select * From QuoteContents Where Qid = " & lngId & " Order By QuoteItemId"
Set rs = dbConn.Execute(sql)

Do Until rs.EOF
	sql = "Insert Into QuoteContents (Qid, ProductId, Quantity, Type, Units, Days, ProductCode, Description, UnitCost, PPrice, MinNettPrice, NettPrice, UnitCostSubTotal, PPriceSubTotal, ExtNettPrice) Values (" & rs2("Qid") & ", " & rs("ProductId") & ", " & rs("Quantity") & ", '" & FixQuotes(rs("Type")) & "', " & FixQuotes(rs("Units")) & ", " & FixQuotes(rs("Days")) & ", '" & FixQuotes(rs("ProductCode")) & "', '" & FixQuotes(rs("Description")) & "', " & rs("UnitCost") & ", " & rs("PPrice") & ", " & rs("MinNettPrice") & ", " & rs("NettPrice") & ", " & rs("UnitCostSubTotal") & ", " & rs("PPriceSubTotal") & ", " & rs("ExtNettPrice") & ")"
	dbConn.Execute(sql)
	rs.MoveNext
Loop

' Copy Quote Third Party

'sql = "Select * From QuoteThirdPartyContents Where QuoteId = " & lngId
'Set rs = dbConn.Execute(sql)

'Do Until rs.EOF
'	sql = "Insert Into QuoteThirdPartyContents (QuoteId, Description, Supplier, QuoteNumber, QuoteDate, ExpiryDate, SupplierPartNumber, OurPartNumber, Quantity, Type, UnitCost, NettPrice, Margin, TotalCost, ExtNettPrice) Values (" & rs2("Qid") & ", '" & ("Description") & "', '" & ("Supplier") & "', " & ("QuoteNumber") & ", '" & ("QuoteDate") & "', '" & ("ExpiryDate") & "', '" & ("SupplierPartNumber") & "', '" & ("OurPartNumber") & "', " & ("Quantity") & ", '" & ("Type") & "', " & ("UnitCost") & ", " & ("NettPrice") & ", " & ("Margin") & ", " & ("TotalCost") & ", " & ("ExtNettPrice") & ")"
''	Response.Write(sql) & "<br/>"
'	dbConn.Execute(sql)
'	rs.MoveNext
'Loop

rs.Close
Set rs = Nothing

rs2.Close
Set rs2 = Nothing

If Err.Number <> 0 Then
	'An error occurred, handle it here (display message etc
	Response.Write("An Error Occurred.<br/>")
	Response.Write("<strong>Err</strong>" & Err & "<br/>")
	Response.Write("<strong>Desc</strong>" & Err.Description & "<br/>")
	Response.Write("<strong>Number</strong>" & Err.Number & "<br/>")
	Response.Write("<strong>Source</strong>" & Err.Source & "<br/>")
	Call Err.Clear() 'Err.Number is now 0
Else
	Response.Write("Completed Without Error")
%>
<html>
<head>
<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
<script language="javascript">
	alert('Quote Copied');
	RefreshIFrame_Global_Opener();
	window.close();
</script>
</head>
<body>
</body>
</html>
<%
End If
'Stop trapping errors
On Error Goto 0
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->