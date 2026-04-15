
<%

lngQid = CLng(Request("Qid"))
lngDivisionId = CLng(Request("DivisionId"))

%>
<script language="javascript">
	document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Invoices/Add.asp?Qid=<%= lngQid %>&DivisionId=<%= lngDivisionId %>';
</script>