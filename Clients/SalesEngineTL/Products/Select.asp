<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim intDivisionId
Dim intItemLine

strCode = Trim(Request("Code"))
intDivisionId = CInt(Request("DivisionId"))
intItemLine = CInt(Request("ItemLine"))

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
		<script language="javascript">
		top.window.moveTo(0,0);

		if (document.all) {
			top.window.resizeTo(screen.availWidth,screen.availHeight);
		}
		else if (document.layers||document.getElementById) {
			if (top.window.outerHeight<screen.availHeight||top.window.outerWidth<screen.availWidth){
				top.window.outerHeight = screen.availHeight;
				top.window.outerWidth = screen.availWidth;
			}
		}

		// Select Product For Quote in Selector window - Links to Quotes Window
		function Items_Select_Step1(itemLine, itemProductCode, itemDescription, itemProductId, itemUnitCost, itemMinNettPrice, itemNettPrice, bDivisionManager, bPerUnitPerDay) {
		//	if(parent.window.opener && !parent.window.opener.closed) {
				window.opener.document.parentWindow.Items_SelectInQuotes(itemLine, itemProductCode, itemDescription, itemProductId, itemUnitCost, itemMinNettPrice, itemNettPrice, bDivisionManager, bPerUnitPerDay);
				window.opener.document.parentWindow.Items_InsertLine();
		//	}
			top.window.close();
		}
		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
		</table>
		<br>
		<table width="100%" cellpadding=5 cellspacing=0 border=0>
			<tr>
				<td class="TimesHeader">Select a Product</td>
			</tr>
			<tr>
				<td>
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From ProductCat Where DivisionId = " & intDivisionId & " Order By ProductCat"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
%>
					<table width="100%" cellpadding=3 cellspacing=0 border=0>
						<tr>
							<td>
<%
	Do Until rs.EOF
%>
							<a href="SelectProduct.asp?Code=<%= strCode %>&DivisionId=<%= intDivisionId %>&ItemLine=<%= intItemLine %>&ProductCatId=<%= rs("ProductCatId") %>" target="MyIFrame"><%= rs("ProductCat") %></a>&nbsp;&nbsp;/&nbsp;&nbsp;
<%
		rs.MoveNext
	Loop
%>
							</td>
						</tr>
					</table>
<%
Else
%>
					<p>There are no categories for this division.</p>
<%
End If

rs.Close
Set rs = Nothing

%>
				</td>
			</tr>
		</table>
		<iframe src="SelectProduct_Default.asp" name="MyIFrame" style="width:100%;height:400px;border:1px solid black;"></iframe>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->