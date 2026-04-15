		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1">
				<input type="button" value=" View Despatch Note " onclick="document.location.href='ViewDespatchNote.asp?InvoiceId=<%= lngInvoiceId %>';" ID="Button3" NAME="Button1">
				<input type="button" value=" View History " onclick="document.location.href='ViewHistory.asp?ViewDespatchNote=True&InvoiceId=<%= lngInvoiceId %>';" ID="Button8" NAME="Button1">

<%
If boolPrint Then
%>
				<input type="button" value=" Print " style="font-weight:bold;color:red;" onclick="print();" ID="Button2" NAME="Button2"> (Make sure that you set the orientation to portrait)
<%
Else
%>
				<input type="button" value=" Print " onclick="document.location.href='ViewDespatchNote.asp?InvoiceId=<%= lngInvoiceId %>&Print=True';" ID="Button9" NAME="Button1">
<%
End If
%>

				</td>
			</tr>
		</table>