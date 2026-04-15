<%

Function GetQuoteLastLineApprover(lngQid)
	Dim rsQuote
	Dim sql
	Dim decQuoteApprovalLimit
	Dim decNettPriceTotal
	Dim strQuoteLastLineApprover
	' Get the Quote
	Set rsQuote = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Quotes.*, UserRoles.QuoteApprovalLimit, Users.Name FROM (Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where Qid = " & lngQid
	Set rsQuote = dbConn.Execute(sql)
	If Not(rsQuote.BOF And rsQuote.EOF) Then
		decQuoteApprovalLimit = rsQuote("QuoteApprovalLimit")
		decNettPriceTotal = rsQuote("NettPriceTotal")
		If decNettPriceTotal <= decQuoteApprovalLimit Then ' Quote creator can approve
			strQuoteLastLineApprover = "Already approved"
		Else
			strQuoteLastLineApprover = GetQuoteLastLineApprover_Find(rsQuote("NettPriceTotal"), rsQuote("Code"))
		End If
	Else
		strQuoteLastLineApprover = "An error occurred"
	End If
	rsQuote.Close
	Set rsQuote = Nothing
	GetQuoteLastLineApprover = strQuoteLastLineApprover
End Function

Function GetQuoteLastLineApprover_Find(decNettPriceTotal, strCode)
	Dim rsUser
	Dim rsUser2
	Dim sql
	Dim strLineManagerCode
	Dim decQuoteApprovalLimit
	' Get user's line manager
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & strCode & "'"
	Set rsUser = dbConn.Execute(sql)
	If Not(rsUser.BOF and rsUser.EOF) Then
		strLineManagerCode = rsUser("LineManagerCode")
		decQuoteApprovalLimit = rsUser("QuoteApprovalLimit")
		If CDbl(decNettPriceTotal) =< CDbl(decQuoteApprovalLimit) Then
			GetQuoteLastLineApprover_Find = GetUserFullName(rsUser("Code"))
			Exit Function
		Else
			GetQuoteLastLineApprover_Find = GetQuoteLastLineApprover_Find(decNettPriceTotal, rsUser("LineManagerCode"))
		End If
	Else
		GetQuoteLastLineApprover_Find = "Unknown"
	End If
	rsUser.Close
	Set rsUser = Nothing
End Function

Function QuoteDetails_ForEmail(lngQid)
	Dim sql
	Dim rsQuote
	Dim strQuoteDetails
	Dim strCompany
	sql = "Select * From Quotes Inner Join Contacts_WithCustomersAndSuppliers_V2 On Contacts_WithCustomersAndSuppliers_V2.ContactId = Quotes.ContactId Where Qid = " & lngQid
	Set rsQuote = dbConn.Execute(sql)
	If Not (rsQuote.BOF And rsQuote.EOF) Then
		strCompany = rsQuote("CompanyName")
		If strCompany = "" Then strCompany = "Non account customer"
		strQuoteDetails = VbCrLf & VbCrLf
		strQuoteDetails = strQuoteDetails & "All prices are ex. GST" & VbCrLf & VbCrLf
		strQuoteDetails = strQuoteDetails & "Customer: " & strCompany & VbCrLf
		strQuoteDetails = strQuoteDetails & "Project: " & rsQuote("Reference") & VbCrLf
		strQuoteDetails = strQuoteDetails & "Total Value: " & FormatCurrency(rsQuote("NettPriceTotal"),2) & VbCrLf
		sql = "Select * From QuoteContents Where Qid = " & lngQid & " Order By QuoteItemId"
		Set rsQuoteC = dbConn.Execute(sql)
		If Not(rsQuoteC.BOF And rsQuoteC.EOF) Then
			strQuoteDetails = strQuoteDetails & VbCrLf & "Quote Items:" & VbCrLf
			Do Until rsQuoteC.EOF
				If rsQuoteC("Units") <> 0 Then
					strQuoteDetails = strQuoteDetails & Replace(rsQuoteC("Units") & " units x " & rsQuoteC("Days") & " days x " & rsQuoteC("Description") & " @ " & FormatCurrency(rsQuoteC("NettPrice"),2), vbcrlf, "") & VbCrLf 
				Else
					strQuoteDetails = strQuoteDetails & Replace(rsQuoteC("Quantity") & " x " & rsQuoteC("Description") & " @ " & FormatCurrency(rsQuoteC("NettPrice"),2), vbcrlf, "") & VbCrLf
				End If
				rsQuoteC.MoveNext
			Loop
		End If
		rsQuoteC.Close
		Set rsQuoteC = Nothing
	End If
	rsQuote.Close
	Set rsQuote = Nothing
	QuoteDetails_ForEmail = strQuoteDetails
End Function

%>
