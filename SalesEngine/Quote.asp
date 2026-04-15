<%

Response.Expires = -1

Dim lngQid
lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/SalesEngine/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/SalesEngine/System/ssi_Dates.inc"-->
<html>
	<head>
		<link rel="stylesheet" type="text/css" href="System/Style.css">
	</head>
	<body>
		<span class="Header3">View Quote</span>
		
<%

' Get Quote
Set rs = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From quotes Where Qid = " & lngQid
Set rs = dbConn.Execute(strSql)

' Get Quote Items
Set rs2 = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From quotecontents Where Qid = " & lngQid
Set rs2 = dbConn.Execute(strSql)

%>

		<br><br>
		<table cellpadding=3 cellspacing=0 border=0>
			<tr>
				<td style="font-weight:bold;">Quote Number:</td>
				<td><%= rs("QuoteNumber") %></td>
			</tr>
			<tr>
				<td style="font-weight:bold;">Quote Date:</td>
				<td><%= FormatDBToUnambiguous(rs("Date"), "AU", False) %></td>
			</tr>
			<tr>
				<td style="font-weight:bold;">Project:</td>
				<td><%= rs("Project") %></td>
			</tr>
			<tr>
				<td style="font-weight:bold;">Wholesaler:</td>
				<td><%= rs("Wholesaler") %></td>
			</tr>
			<tr>
				<td style="font-weight:bold;">Status:</td>
				<td><%= rs("QStatus") %></td>
			</tr>
		</table>
<%

If Not(rs2.BOF And rs2.EOF) Then

	Dim dblRunningTotal
	dblRunningTotal = 0.00

%>
		<br/>
		<table width="100%" cellpadding=3 cellspacing=0 border=0>
			<tr>
				<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap>Type</td>
				<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap>Qty</td>
				<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap>Product</td>
				<td style="font-weight:bold;border-bottom:1px solid black;" valign="top">Description</td>
				<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap align="right">Unit Value</td>
				<td style="font-weight:bold;border-bottom:1px solid black;" valign="top" nowrap align="right">Quote Value</td>
			</tr>
<%

	Do Until rs2.EOF
	
		dblRunningTotal = dblRunningTotal + (rs2("Sale Price")*rs2("Qty"))

%>
			<tr>
				<td style="border-bottom:1px solid #cccccc;" valign="top" nowrap><% If IsNull(rs2("Type")) Then Response.Write "&nbsp;" Else Response.Write rs2("Type") %></td>
				<td style="border-bottom:1px solid #cccccc;" valign="top" nowrap><% If IsNull(rs2("Qty")) Then Response.Write "&nbsp;" Else Response.Write rs2("Qty") %></td>
				<td style="border-bottom:1px solid #cccccc;" valign="top" nowrap><% If IsNull(rs2("Product")) Then Response.Write "&nbsp;" Else Response.Write rs2("Product") %></td>
				<td style="border-bottom:1px solid #cccccc;" valign="top"><% If IsNull(rs2("Description")) Then Response.Write "&nbsp;" Else Response.Write rs2("Description") %></td>
				<td style="border-bottom:1px solid #cccccc;" valign="top" align="right" nowrap><%= FormatCurrency(rs2("Sale Price"),2) %></td>
				<td style="border-bottom:1px solid #cccccc;" valign="top" align="right" nowrap><%= FormatCurrency(rs2("Sale Price")*rs2("Qty"),2) %></td>
			</tr>
<%

		rs2.MoveNext
	Loop

%>
			<tr>
				<td colspan=5></td>
				<td align="right"><b><%= FormatCurrency(dblRunningTotal,2) %></b></td>
			</tr>
		</table>
<%

End If

%>
		
		<br><div align="right"><a href="JavaScript:history.go(-1);"><< Go back to quote list</a></div>
	</body>
</html>
<!--#include virtual="/SalesEngine/System/ssi_dbConn_close.inc"-->