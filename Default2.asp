<% 
On Error Resume Next

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

' Clear Cookies
Session.Abandon()

For Each Item In Request.Cookies
'	Response.Cookies(Item).Expires = Date() - 1
	Response.Cookies(Item) = ""
Next

If Err.Number <> 0 Then
	Response.Clear
	Response.Write("<html><head><title>Error</title></head><body>")
	Response.Write("<h1>Error in Default2.asp</h1>")
	Response.Write("<p><strong>Error Number:</strong> " & Err.Number & "</p>")
	Response.Write("<p><strong>Error Description:</strong> " & Err.Description & "</p>")
	Response.Write("<p><strong>Error Source:</strong> " & Err.Source & "</p>")
	Response.Write("<p><strong>Line Number:</strong> " & Err.Line & "</p>")
	Response.Write("<hr>")
	Response.Write("<p><a href=""javascript:history.back()"">Go Back</a></p>")
	Response.Write("</body></html>")
	Response.End
End If

Response.Redirect("/Clients/SalesEngine/Portal/Validate_Portal.asp")
If Err.Number <> 0 Then
	Response.Clear
	Response.Write("<html><head><title>Error</title></head><body>")
	Response.Write("<h1>Redirect Error in Default2.asp</h1>")
	Response.Write("<p><strong>Error Number:</strong> " & Err.Number & "</p>")
	Response.Write("<p><strong>Error Description:</strong> " & Err.Description & "</p>")
	Response.Write("<p><strong>Error Source:</strong> " & Err.Source & "</p>")
	Response.Write("<p><strong>Redirect Target:</strong> /Clients/SalesEngine/Portal/Validate_Portal.asp</p>")
	Response.Write("<hr>")
	Response.Write("<p><a href=""javascript:history.back()"">Go Back</a></p>")
	Response.Write("</body></html>")
	Response.End
End If

Response.End

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<%
Dim strMsg
strMsg = Trim(Request("Msg"))
%>
<html>
	<head>
		<title>MyDesk</title>
		<META name="keywords" content="MyDesk, custom software, web applications, web systems, sales system, Access, SQL, ASP.Net, ASP">
		<META name="description" content="MyDesk">
		<meta name="Robots" content="index,follow" />
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<meta http-equiv="X-UA-Compatible" content="IE=8" />
		<link rel="shortcut icon" href="/favicon.ico">
		<link rel="stylesheet" type="text/css" href="/System/Style.css">
		<script language="JavaScript">

		function setFocus() {
			document.Form1.ClientId.focus();
		}

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		function checkForm() {

			var validFlag = true
			
			if (validFlag) {
			if (emptyField(document.Form1.ClientId)) {
				alert("Please complete the Client Id field.");
				validFlag = false;
				document.Form1.ClientId.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.AccessCode)) {
				alert("Please complete the Access Code field.");
				validFlag = false;
				document.Form1.AccessCode.focus();
			}}

		return validFlag 
		}

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

		</script>
	</head>
	<body bgcolor="#dddddd" onload="setFocus();">
<!--#include virtual="/System/ssi_Header_MyDesk.inc"-->
					<table style="background: url('Images/FP_Bg.jpg');background-repeat: no-repeat;" background="/Images/FP_Bg.jpg" width=100% height=400 cellpadding=0 cellspacing=0 border=0 ID="Table1">
						<tr>
							<td>
								<br/><br/>
<%
If strMsg <> "" Then
	Response.Write("<div style=""position:absolute;top:180px;left:240px;""><p class=""Error"">" & strMsg & "</p></div>")
End If
%>

								<form action="/Clients/SalesEngine/Portal/Validate_Portal.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								

								<!--<div style="position:absolute;top:150px;left:130px;color:white;z-index:50;font-weight:bold;font-size:14px;"><span style="color:white;font-weight:bold;font-size:36px;">*</span>&nbsp;&nbsp;MyDesk will be offline for scheduled maintenance between 6pm and 8pm AEST 13/11/2008.</div>-->

								<div style="position:absolute;top:251px;left:240px;"><span style="font-weight:bold;color:white;">Client Id : </span></div>
								<div style="position:absolute;top:248px;left:340px;"><input type="text" name="ClientId" id="ClientId" size=20 maxlength=50></div>
								<div style="position:absolute;top:305px;left:240px;"><span style="font-weight:bold;color:white;">Access Code : </span></div>
								<div style="position:absolute;top:302px;left:340px;"><input type="password" name="AccessCode" id="AccessCode" size=20 maxlength=50></div>
								<div style="position:absolute;top:270px;left:540px;font-weight:bold;font-size:larger;">
								    <!--There are currently difficulties with the 3rd Party hosting providers for MyDesk. <br /> As such MyDesk is currently offline. Please check back for updates.-->
								    <input type="image" name="Submit" src="/Images/Login.gif" value="Login" ID="Submit1">
								 </div>
								</form>
							</td>
						</tr>
					</table>
				</td>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->