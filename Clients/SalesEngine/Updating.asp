<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="System/ssi_Functions.asp"-->
<html>
	<head>
		<title>SalesEngine</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style.css">
		<META HTTP-EQUIV=Refresh CONTENT="10; URL=/Clients/SalesEngine">
	</head>
	<body marginheight=0 marginwidth=0 topmargin=0 leftmargin=0 bgcolor="#dddddd">

<!--#include virtual="System/ssi_Header.inc"-->

				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td>
						<br/><br/>
						<center>
							<strong>SalesEngine is updating. Please wait or try again later.</strong>
						</center>
						</td>
					</tr>
				</table>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
