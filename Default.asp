<%

Response.Redirect("/default2.asp")
Response.End

%>
<html>
	<head>
		<title>MyDesk</title>
		<link rel="shorcut icon" href="/favicon.ico" />
		<meta http-equiv="X-UA-Compatible" content="IE=8" />
		<script language="javascript">
			function openManagementSystem() {
				try {
					var w = window.open ("/Default2.asp", 'winManagementSystem', "menubar=no,location=no,resizable=yes,scrollbars=yes,status=yes");

					if(screen.height == 1024) {
						w.resizeTo(1024, 768);
					}
					else if(screen.height > 800) {
						w.resizeTo(780, 600);
					}
					w.moveTo(0,0);
					w.focus();
				}
				catch (error) {
				}
			}
		</script>
		<style>
			body { overflow: hidden; }
		</style>
	</head>
	<body marginheight=0 marginwidth=0 topmargin=0 leftmargin=0 onload="openManagementSystem()">
		<table bgcolor="#666666" width="100%" height="100%">
			<tr>
				<td align="center" valign="middle">
				<a href="#" onclick="openManagementSystem();"><img src="/Images/MyDesk.gif" border=0 alt="MyDesk"></a>				
				</td>
			</tr>
		</table>
	</body>
</html>