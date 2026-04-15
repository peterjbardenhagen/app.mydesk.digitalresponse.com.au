<%

If boolForFaxEmail Then
	strCurrencyName = Trim(Request("CurrencyName"))
	dblCurrencyRate = CDbl(Request("CurrencyRate"))
	strCurrencyPrefix = Trim(Request("CurrencyPrefix"))
Else
	strCurrencyName = Trim(Session("CurrencyName"))
	dblCurrencyRate = CDbl(Session("CurrencyRate"))
	strCurrencyPrefix = Trim(Session("CurrencyPrefix"))
%>
<table ID="Table1">
	<form name="FormCurrency" method="post" action="?<%= Request.ServerVariables("QUERY_STRING") %>">
	<tr>
		<td>
		<b>Currency:</b>
		<select name="CurrencyName" ID="Select1">
<%

Set rsCurrency = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From [Currency] Order By CurrencyName"
Set rsCurrency = dbConn.Execute(sql)

Do Until rsCurrency.EOF
	If rsCurrency("CurrencyName") = Session("CurrencyName") Then
%>
			<option selected value="<%= rsCurrency("CurrencyName") %>"><%= rsCurrency("CurrencyName") %>
<%
	Else
%>
			<option value="<%= rsCurrency("CurrencyName") %>"><%= rsCurrency("CurrencyName") %>
<%
	End If
	rsCurrency.MoveNext
Loop

rsCurrency.Close
Set rsCurrency = Nothing

%>
		</select>
		<input type="submit" value="Select currency" ID="Submit1" NAME="Submit1">
		</td>
	</tr>
	</form>
</table>
<%
End If

%>