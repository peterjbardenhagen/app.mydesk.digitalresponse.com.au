<%
Option Explicit

lngInvoiceId = CLng(Request("InvoiceId"))
lngDivisionId = CLng(Request("DivisionId"))

%>
<script language="javascript">
	parent.document.location.href = '<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Invoices/Edit.asp?InvoiceId=<%= lngInvoiceId %>&DivisionId=<%= lngDivisionId %>';
</script>