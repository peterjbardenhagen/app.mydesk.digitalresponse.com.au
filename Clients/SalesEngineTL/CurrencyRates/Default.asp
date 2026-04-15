<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg

strMsg = Trim(Request("Msg"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table5">
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / Currency Rates /></span></td>
						<td align="right"><a href="Add.asp" class="Header2">Add Currency Rate</a></td>
					</tr>
				</table>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table1">
					<tr>
						<td>
<%

If strMsg <> "" Then

%>
							<br>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table2">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
<%

End If

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From [Currency] Order By CurrencyName"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
							<br/>
							<table width="100%" cellpadding=10 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td class="ListHeaderRow" style="width:250px;"><b>Name</b></td>
									<td class="ListHeaderRow" style="width:100px;"><b>Prefix</b></td>
									<td class="ListHeaderRow"><b>Symbol</b></td>
									<td class="ListHeaderRow" align="right" width=75><b>Action</b></td>
								</tr>
<%

	Do Until rs.EOF

%>
								<tr bgcolor="#ffffff">
									<td style="color:black;border-bottom:1px solid black;width:250px;" valign="top"><%= rs("CurrencyName") %></td>
									<td style="color:black;border-bottom:1px solid black;width:100px;" valign="top"><%= rs("CurrencyPrefix") %></td>
									<td style="color:black;border-bottom:1px solid black;" valign="top"><%= rs("CurrencyRate") %></td>
									<td style="color:black;border-bottom:1px solid black;text-align:right;" width=75 valign="top"><a href="Edit.asp?CurrencyId=<%= rs("CurrencyId") %>">Edit</a> | <a href="Del_Proc.asp?CurrencyId=<%= rs("CurrencyId") %>">Delete</a></td>
								</tr>
<%

		rs.MoveNext
	Loop

%>
							</table>
<%

Else

%>
							<br><p>There are no Currency Rates</p>
<%

End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>	
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
