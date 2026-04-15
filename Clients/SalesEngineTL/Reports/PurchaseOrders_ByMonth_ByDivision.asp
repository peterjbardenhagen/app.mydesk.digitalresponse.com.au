<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

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
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
        <link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Back " onclick="document.location.href='Default.asp';" ID="Button1" NAME="Button1"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
			</tr>
		</table>
		<br>
		<table width=1000 cellpadding=3 cellspacing=0 border=0 ID="Table1">
			<tr>
				<td valign="top"><span class="TimesHeader">My Purchase Orders Report</span><br><br>
				<span class="TimesItalicBold">By Division, By Month, By Status<br>
				As at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
			<tr>
				<td style="font-style:italic;"><br>All prices are ex. GST.<br><br></td>
			</tr>
		</table>
<%
Set rsPO = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From POAnalysis_Crosstab Order By DivisionCode, POStatus, CapEx"
Set rsPO = dbConn.Execute(sql)

Dim i
Dim strBgColor

i = 0
strBgColor = "#dddddd"

If Not(rsPO.BOF And rsPO.EOF) Then
%>
		<table width="1000" cellpadding=3 cellspacing=0 border=0>
			<tr>
				<td nowrap class="HeaderRow" style="width:70px;border-right:1px solid black;">Division</td>
				<td nowrap class="HeaderRow" style="width:80px;border-right:1px solid black;">Cap Ex</td>
				<td nowrap class="HeaderRow" style="width:80px;border-right:1px solid black;">Status</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">Total</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">JAN</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">FEB</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">MAR</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">APR</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">MAY</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">JUN</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">JUL</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">AUG</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">SEP</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">OCT</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">NOV</td>
				<td class="HeaderRow" style="width:110px;text-align:right;border-right:1px solid black;">DEC</td>
			</tr>
<%
	Do Until rsPO.EOF
%>
            <tr style="background:<%= strBgColor %>">
                <td nowrap style="border-right:1px solid black;"><%= rsPO("DivisionCode") %></td>
                <td nowrap style="border-right:1px solid black;"><%= rsPO("CapEx") %></td>
                <td nowrap style="border-right:1px solid black;"><%= rsPO("POStatus") %></td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Total of PriceExSubTotal") <> "" Then Response.Write(FormatCurrency(rsPO("Total Of PriceExSubTotal"),0)) %></td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Jan") <> "" Then Response.Write(FormatCurrency(rsPO("Jan"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Feb") <> "" Then Response.Write(FormatCurrency(rsPO("Feb"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Mar") <> "" Then Response.Write(FormatCurrency(rsPO("Mar"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Apr") <> "" Then Response.Write(FormatCurrency(rsPO("Apr"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("May") <> "" Then Response.Write(FormatCurrency(rsPO("May"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Jun") <> "" Then Response.Write(FormatCurrency(rsPO("Jun"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Jul") <> "" Then Response.Write(FormatCurrency(rsPO("Jul"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Aug") <> "" Then Response.Write(FormatCurrency(rsPO("Aug"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Sep") <> "" Then Response.Write(FormatCurrency(rsPO("Sep"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Oct") <> "" Then Response.Write(FormatCurrency(rsPO("Oct"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Nov") <> "" Then Response.Write(FormatCurrency(rsPO("Nov"),0)) %>&nbsp;</td>
                <td style="width:110px;text-align:right;border-right:1px solid black;"><% If rsPO("Dec") <> "" Then Response.Write(FormatCurrency(rsPO("Dec"),0)) %>&nbsp;</td>
            </tr>
<%
        i = i + 1
        If i = 2 Then i = 0
        If i = 0 Then
            strBgColor = "#dddddd"
        Else
            strBgColor = "#ffffff"
        End If
		rsPO.MoveNext
	Loop

%>
		</table>
<%
End If
rsPO.Close
Set rsPO = Nothing
%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->