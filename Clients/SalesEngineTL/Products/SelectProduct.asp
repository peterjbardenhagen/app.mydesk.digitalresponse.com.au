<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim intDivisionId
Dim intItemLine
Dim intProductCatId

strCode = Trim(Request("Code"))
intDivisionId = CInt(Request("DivisionId"))
intItemLine = CInt(Request("ItemLine"))
intProductCatId = CInt(Request("ProductCatId"))

Dim boolDivisionManager
boolDivisionManager = SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), intDivisionId)

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Quotes.js"></script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" cellpadding=5 cellspacing=0 border=0>
<%

Set rsCat = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From ProductCat Where ProductCatId = " & intProductCatId
Set rsCat = dbConn.Execute(sql)

strProductCat = rsCat("ProductCat")

%>
			<tr>
				<td style="font-weight:bold;">Please select a product from "<%= strProductCat %>"</td>
			</tr>
			<tr>
				<td>
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Products Where DivisionId = " & intDivisionId & " And ProductCatId = " & intProductCatId & " Order By ProductName, ProductDesc"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
%>
					<table width="100%" cellpadding=3 cellspacing=0 border=0>
						<tr>
							<td class="ListHeaderRow" width=150>Product Code</td>
							<td class="ListHeaderRow" width=250>Product Name</td>
							<td class="ListHeaderRow">Product Desc.</td>
<%

	If boolDivisionManager Then

%>
							<td width=120 class="ListHeaderRow" style="text-align:right;">Unit Cost</td>
<%

	End If

%>
							<td width=120 class="ListHeaderRow" style="text-align:right;">Nett Price</td>
<%

	If boolDivisionManager Then

%>
							<td width=120 class="ListHeaderRow" style="text-align:right;">Minimum Nett Price</td>
<%

	End If

%>
						</tr>
<%
	Do Until rs.EOF
%>
						<tr>
							<td width=150 style="border-bottom:1px solid #cccccc;"><%= rs("ProductCode") %></td>
							<td width=250 style="border-bottom:1px solid #cccccc;"><a href="javascript:parent.Items_Select_Step1('<%= intItemLine %>', '<%= rs("ProductCode") %>', '<%= rs("ProductDesc") %>', <%= rs("ProductId") %>, <%= rs("UnitCost") %>, <%= rs("MinNettPrice") %>, <%= rs("NettPrice") %>, '<%= boolDivisionManager %>', '<%= rs("PerUnitPerDay") %>')"><%= rs("ProductName") %></a></td>
							<td style="border-bottom:1px solid #cccccc;"><%= rs("ProductDesc") %></td>
<%

If boolDivisionManager Then

%>
							<td width=120 style="border-bottom:1px solid #cccccc;text-align:right;"><%= FormatCurrency(rs("UnitCost"),2) %></td>
<%

End If

%>
							<td width=120 style="border-bottom:1px solid #cccccc;text-align:right;"><%= FormatCurrency(rs("NettPrice"),2) %></td>
<%

If boolDivisionManager Then

%>
							<td width=120 style="border-bottom:1px solid #cccccc;text-align:right;"><%= FormatCurrency(rs("MinNettPrice"),2) %></td>
<%

End If

%>
						</tr>
<%
		rs.MoveNext
	Loop
%>
					</table>
<%
Else
%>
					<p>There are no products in this category.</p>
<%
End If

rs.Close
Set rs = Nothing

%>
				</td>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->