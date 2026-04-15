<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim intTableId
Dim intItemId

intTableId = CLng(Request("TableId"))
intItemId = CLng(Request("ItemId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
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
				if (emptyField(document.Form1.File1) && emptyField(document.Form1.File2) && emptyField(document.Form1.File3)) {
					alert("Please ensure that you are uploading at least one file.");
					validFlag = false;
				}
			}

			if (validFlag) {
				if ((emptyField(document.Form1.File1) && (!emptyField(document.Form1.File2))) || (!emptyField(document.Form1.File3) && (emptyField(document.Form1.File1) || emptyField(document.Form1.File2)))) {
					alert("Please ensure that you have selected a file for File 1#, or File #2 in order");
					validFlag = false;
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File1)) {
					if (emptyField(document.Form1.File1_Desc)) {
						alert("Please check that you have entered a Description for File #1");
						validFlag = false;
					}
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File2)) {
					if (emptyField(document.Form1.File2_Desc)) {
						alert("Please check that you have entered a Description for File #2");
						validFlag = false;
					}
				}
			}

			if (validFlag) {
				if (!emptyField(document.Form1.File3)) {
					if (emptyField(document.Form1.File3_Desc)) {
						alert("Please check that you have entered a Description for File #3");
						validFlag = false;
					}
				}
			}
			return validFlag
		}

		</script>
	</head>
	<body>
		<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table4">
			<tr>
				<td><span class="Header3">Add File</span></td>
				<td align="right"><a href="#" onclick="parent.parent.MainFrame.location.href=parent.parent.MainFrame.location.href;"><< Back</a></td>
			</tr>
		</table>
		<br/>
		<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
			<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();" ENCTYPE="multipart/form-data">
			<input type="hidden" name="TableId" id="TableId" value="<%= intTableId %>">
			<input type="hidden" name="ItemId" id="ItemId" value="<%= intItemId %>">
			<tr>
				<td></td>
				<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #1</td>
				<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table1"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File1_Desc" size=30 maxlength=50 ID="Text1"></td></tr></table></td>
				<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table3"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File1" ID="File1"></td></tr></table></td>
			</tr>
			<tr>
				<td colspan=4><hr></td>
			</tr>
			<tr>
				<td></td>
				<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #2</td>
				<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table5"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File2_Desc" size=30 maxlength=50 ID="Text2"></td></tr></table></td>
				<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table6"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File2" ID="File2"></td></tr></table></td>
			</tr>
			<tr>
				<td colspan=4><hr></td>
			</tr>
			<tr>
				<td></td>
				<td valign="middle" style="font-weight:bold;"><img src="/Images/Spacer.gif" width=50 height=1 border=0 alt=""><br>File #3</td>
				<td valign="middle" style="font-weight:bold;"><table cellpadding=0 cellspacing=0 border=0 ID="Table8"><tr><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br>Description</td><td>&nbsp;&nbsp;</td><td><input type="text" name="File3_Desc" size=30 maxlength=50 ID="Text3"></td></tr></table></td>
				<td valign="middle"><table cellpadding=0 cellspacing=0 border=0 ID="Table10"><tr><td width=20><img src="/Images/Spacer.gif" width=20 height=1 border=0 alt=""></td><td style="font-weight:bold;"><img src="/Images/Spacer.gif" width=25 height=1 border=0 alt=""><br>File</td><td>&nbsp;&nbsp;</td><td><INPUT TYPE=FILE SIZE=20 NAME="File3" ID="File3"></td></tr></table></td>
			</tr>
			<tr>
				<td colspan=4><hr></td>
			</tr>
			<tr>
				<td colspan=4 valign="top" align="right"><input type="button" value="Cancel" onclick="parent.parent.MainFrame.location.href=parent.parent.MainFrame.location.href;">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
			</tr>
			</form>
		</table>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
