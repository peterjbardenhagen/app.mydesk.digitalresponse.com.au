		<script language="javascript">
			function CreatePurchaseOrder() {
				document.location.href='Transporter_QuoteToPO.asp?Parent=True&Qid=<%= lngQid %>&DivisionId=<%= rsQu("QDivisionId") %>';
			}
			function InvoiceQuote() {
				document.location.href='Transporter_QuoteToInvoice.asp?Parent=False&Qid=<%= lngQid %>&DivisionId=<%= rsQu("QDivisionId") %>';
			}
		</script>


		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"> <input type="button" value=" View Quote " onclick="document.location.href='View.asp?Qid=<%= lngQid %>';" ID="Button4" NAME="Button1"> <input type="button" value=" View History " onclick="document.location.href='ViewHistory.asp?Qid=<%= lngQid %>';" ID="Button8" NAME="Button1">
				<input type="button" value=" Update Status " onclick="document.location.href='UpdateStatus.asp?Qid=<%= lngQid %>';" />

<%
' If is next approver, or approval limit is high enough, or 
If (rsQu("QuoteStatusId") = 1 Or rsQu("QuoteStatusId") = 9) And GetQuoteLineApprover_Check(lngQid,Request.Cookies("UserSettings")("Code")) Or CheckForLine(rsQu("Code"),Request.Cookies("UserSettings")("Code"), lngQid, True, False) Then
%>
				<input type="button" value=" Decline " style="color:red;" onclick="document.location.href='Decline.asp?Qid=<%= lngQid %>'" ID="Button7" NAME="Button7">
				<input type="button" value=" Approve " style="color:red;" onclick="document.location.href='Approve.asp?Qid=<%= lngQid %>'" ID="Button6" NAME="Button6">
<%
End If
If (rsQu("QuoteStatusId") = 2 Or rsQu("QuoteStatusId") = 3 Or rsQu("QuoteStatusId") = 4 Or rsQu("QuoteStatusId") = 7 Or rsQu("QuoteStatusId") = 10) Or (GetQuoteLastLineApprover(rsQu("Qid")) = "Already approved") Then
%>
				<input type="button" value=" Invoice Quote " onclick="InvoiceQuote()">
				<input type="button" value=" Generate Purchase Order " onclick="CreatePurchaseOrder()">
				<input type="button" value=" Email " onclick="document.location.href='Email.asp?Qid=<%= lngQid %>';" ID="Button3" NAME="Button2"> 
<%
	If boolPrint Then
%>
				<input type="button" value=" Print " style="font-weight:bold;color:red;" onclick="print();" ID="Button2" NAME="Button2"> (Make sure that you set the orientation to portrait)
<%
	Else
%>
				<input type="button" value=" Print " onclick="if(confirm('If you proceed the Quotes status will be set to issued.\nAre you sure you want to proceed?')){document.location.href='View.asp?Qid=<%= lngQid %>&Print=True'}" ID="Button9" NAME="Button1">
<%
	End If
Else
	If Request.Cookies("UserSettings")("Code") = "TL0084" Then ' Hannah G
%>
				<input type="button" value=" Print " style="font-weight:bold;color:red;" onclick="print();" ID="Button11" NAME="Button2"> (Make sure that you set the orientation to portrait)
<%
	End If
End If
%>
				</td>
			</tr>
		</table>