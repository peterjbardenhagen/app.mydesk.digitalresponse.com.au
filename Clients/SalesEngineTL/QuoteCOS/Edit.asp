<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngQuoteCOSId
lngQuoteCOSId = CLng(Request("QuoteCOSId"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Select * From QuoteCOS Where QuoteCOSId = " & lngQuoteCOSId
Set rs = dbConn.Execute(sql)

%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<script language="JavaScript">

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
				if (emptyField(document.Form1.QuoteCOS)) {
					alert("Please ensure that you have entered a Description.");
					validFlag = false;
					document.Form1.QuoteCOS.focus();
				}
			}

			if (validFlag) {
				if (emptyField(document.Form1.QuoteCOSFile)) {
					alert("Please ensure that you have selected a File to upload.");
					validFlag = false;
					document.Form1.QuoteCOSFile.focus();
				}
			}

			return validFlag
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="../Setup">Setup</a> / <a href="Default.asp">Conditions of Sale</a> / Edit File /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" ENCTYPE="multipart/form-data" accept-charset="utf-8">
								<input type="hidden" name="QuoteCOSId" value="<%= lngQuoteCOSId %>">
								<tr>
									<td></td>
									<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File</td>
									<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table3"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="QuoteCOS" size=30 maxlength=50 ID="Text1" value="<%= rs("QuoteCOS") %>"></td></tr></table></td>
									<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table5"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="QuoteCOSFile" ID="QuoteCOSFile"></td></tr></table></td>
								</tr>
								<tr>
									<td colspan=4 valign="top" align="right"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
								</tr>
								</form>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</body>
</html>
<%

rs.Close
Set rs = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->