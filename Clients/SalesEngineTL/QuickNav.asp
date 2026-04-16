<%
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1

Dim id, navType
id = Trim(Request("ID"))
navType = Trim(Request("Type"))

If id <> "" And IsNumeric(id) Then
    Select Case navType
        Case "Quote"
            Response.Redirect("Quotes/View.asp?Qid=" & id)
        Case "PurchaseOrder"
            Response.Redirect("PurchaseOrders/View.asp?POid=" & id)
        Case "Invoice"
            Response.Redirect("Invoices/View.asp?InvoiceId=" & id)
        Case "Contact"
            Response.Redirect("Contacts/Edit.asp?ContactId=" & id)
        Case Else
            Response.Redirect("Dashboard.asp?Msg=Invalid+Quick+Nav+Type")
    End Select
Else
    Response.Redirect("Dashboard.asp?Msg=Please+enter+a+valid+numeric+ID")
End If
%>
