<%

Function GetPONextLineApprover_Email(lngPOid, boolCapEx)
	On Error Resume Next
	
	Dim strEmail, strName, strSql, rsPONext
	
	' Validate input
	If IsNull(lngPOid) Or Not IsNumeric(lngPOid) Then
		GetPONextLineApprover_Email = ""
		Exit Function
	End If
	
	lngPOid = CLng(lngPOid)
	
	strSql = "SELECT TOP 1 Users.Email, Users.Name FROM PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code WHERE (((PurchaseOrders.POId)=" & lngPOid & "));"
	Set rsPONext = dbConn.Execute(strSql)
	
	If Err.Number = 0 And Not(rsPONext.BOF And rsPONext.EOF) Then
		GetPONextLineApprover_Email = rsPONext("Email")
	Else
		GetPONextLineApprover_Email = ""
	End If
	
	rsPONext.Close
	Set rsPONext = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetPOLastLineManager_Email(lngPOid)
	On Error Resume Next
	
	Dim strEmail, strName, strSql, rsPONext
	
	' Validate input
	If IsNull(lngPOid) Or Not IsNumeric(lngPOid) Then
		GetPOLastLineManager_Email = ""
		Exit Function
	End If
	
	lngPOid = CLng(lngPOid)
	
	strSql = "SELECT TOP 1 Users.Email, Users.Name FROM PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code WHERE (((PurchaseOrders.POId)=" & lngPOid & "));"
	Set rsPONext = dbConn.Execute(strSql)
	
	If Err.Number = 0 And Not(rsPONext.BOF And rsPONext.EOF) Then
		GetPOLastLineManager_Email = rsPONext("Email")
	Else
		GetPOLastLineManager_Email = ""
	End If
	
	rsPONext.Close
	Set rsPONext = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetPONextLineApprover(lngPOid, boolCapEx)
	On Error Resume Next
	
	Dim strLastLineApproverCode, strPreviousLineApproverCode, rsUser, rsPO, sql, decPOApprovalLimit, decNettPriceTotal, maxIterations, iterationCount
	
	' Validate input
	If IsNull(lngPOid) Or Not IsNumeric(lngPOid) Then
		GetPONextLineApprover = "An error occurred"
		Exit Function
	End If
	
	lngPOid = CLng(lngPOid)
	maxIterations = 10
	iterationCount = 0
	
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit, Users.Name FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rsPO.BOF And rsPO.EOF) Then
		decPOApprovalLimit = rsPO("POApprovalLimit")
		decNettPriceTotal = rsPO("PriceExTotal")
		
		If IsNumeric(decPOApprovalLimit) And IsNumeric(decNettPriceTotal) Then
			If CDbl(decNettPriceTotal) <= CDbl(decPOApprovalLimit) Then
				GetPONextLineApprover = "Already approved"
			Else
				strLastLineApproverCode = rsPO("Code")
				strPreviousLineApproverCode = ""
				Do While strLastLineApproverCode <> strPreviousLineApproverCode And iterationCount < maxIterations
					strPreviousLineApproverCode = strLastLineApproverCode
					iterationCount = iterationCount + 1
					
					Set rsUser = Server.CreateObject("ADODB.RecordSet")
					sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & Replace(strLastLineApproverCode, "'", "''") & "'"
					Set rsUser = dbConn.Execute(sql)
					
					If Err.Number = 0 And Not(rsUser.BOF And rsUser.EOF) Then
						strLastLineApproverCode = rsUser("LineManagerCode")
						decPOApprovalLimit = rsUser("POApprovalLimit")
						
						If IsNumeric(decPOApprovalLimit) Then
							If CDbl(decNettPriceTotal) <= CDbl(decPOApprovalLimit) Then
								GetPONextLineApprover = rsUser("Name")
								rsUser.Close
								Set rsUser = Nothing
								Exit Function
							End If
						End If
					End If
					
					rsUser.Close
					Set rsUser = Nothing
				Loop
			End If
		Else
			GetPONextLineApprover = "An error occurred"
		End If
	Else
		GetPONextLineApprover = "An error occurred"
	End If
	
	rsPO.Close
	Set rsPO = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetPONextLineApproverCode(lngPOid, boolCapEx)
	On Error Resume Next
	
	Dim strLastLineApproverCode, strPreviousLineApproverCode, rsUser, rsPO, sql, decPOApprovalLimit, decNettPriceTotal, maxIterations, iterationCount
	
	' Validate input
	If IsNull(lngPOid) Or Not IsNumeric(lngPOid) Then
		GetPONextLineApproverCode = ""
		Exit Function
	End If
	
	lngPOid = CLng(lngPOid)
	maxIterations = 10
	iterationCount = 0
	
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit, Users.Name FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rsPO.BOF And rsPO.EOF) Then
		decPOApprovalLimit = rsPO("POApprovalLimit")
		decNettPriceTotal = rsPO("PriceExTotal")
		
		If IsNumeric(decPOApprovalLimit) And IsNumeric(decNettPriceTotal) Then
			If CDbl(decNettPriceTotal) <= CDbl(decPOApprovalLimit) Then
				GetPONextLineApproverCode = rsPO("Code")
			Else
				strLastLineApproverCode = rsPO("Code")
				strPreviousLineApproverCode = ""
				Do While strLastLineApproverCode <> strPreviousLineApproverCode And iterationCount < maxIterations
					strPreviousLineApproverCode = strLastLineApproverCode
					iterationCount = iterationCount + 1
					
					Set rsUser = Server.CreateObject("ADODB.RecordSet")
					sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & Replace(strLastLineApproverCode, "'", "''") & "'"
					Set rsUser = dbConn.Execute(sql)
					
					If Err.Number = 0 And Not(rsUser.BOF And rsUser.EOF) Then
						strLastLineApproverCode = rsUser("LineManagerCode")
						decPOApprovalLimit = rsUser("POApprovalLimit")
						
						If IsNumeric(decPOApprovalLimit) Then
							If CDbl(decNettPriceTotal) <= CDbl(decPOApprovalLimit) Then
								GetPONextLineApproverCode = rsUser("Code")
								rsUser.Close
								Set rsUser = Nothing
								Exit Function
							End If
						End If
					End If
					
					rsUser.Close
					Set rsUser = Nothing
				Loop
			End If
		Else
			GetPONextLineApproverCode = ""
		End If
	Else
		GetPONextLineApproverCode = ""
	End If
	
	rsPO.Close
	Set rsPO = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetPOLastLineManagers(lngPOid)
	On Error Resume Next
	
	Dim rs, sql, rspo, s, Approver, maxIterations, iterationCount
	
	' Validate input
	If IsNull(lngPOid) Or Not IsNumeric(lngPOid) Then
		GetPOLastLineManagers = ""
		Exit Function
	End If
	
	lngPOid = CLng(lngPOid)
	maxIterations = 10
	iterationCount = 0
	s = ""
	
	sql = "SELECT * FROM PurchaseOrders WHERE POid = " & lngPOid
	Set rs = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rs.BOF And rs.EOF) Then
		Approver = rs("Code")
		
		Do While Approver <> "" And iterationCount < maxIterations
			iterationCount = iterationCount + 1
			
			Set rspo = dbConn.Execute("Select LineManagerCode From Users Where Code = '" & Replace(Approver, "'", "''") & "'")
			
			If Err.Number = 0 And Not(rspo.BOF And rspo.EOF) Then
				Approver = rspo("LineManagerCode")
				If Approver <> "" Then
					s = s & Approver & ","
				End If
			Else
				Approver = ""
			End If
			
			rspo.Close
			Set rspo = Nothing
		Loop
	End If
	
	rs.Close
	Set rs = Nothing
	GetPOLastLineManagers = s
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetPOLastLineApprover(lngPOid, boolCapEx)
	On Error Resume Next
	
	Dim rsPO, sql, decPOApprovalLimit, strPOLastLineApprover
	
	' Validate input
	If IsNull(lngPOid) Or Not IsNumeric(lngPOid) Then
		GetPOLastLineApprover = "An error occurred"
		Exit Function
	End If
	
	lngPOid = CLng(lngPOid)
	
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rsPO.BOF And rsPO.EOF) Then
		decPOApprovalLimit = rsPO("POApprovalLimit")
		
		If IsNumeric(decPOApprovalLimit) And IsNumeric(rsPO("PriceExTotal")) Then
			If rsPO("PriceExTotal") <= decPOApprovalLimit Then
				strPOLastLineApprover = "Already approved"
			Else
				strPOLastLineApprover = GetPOLastLineApprover_Find(rsPO("PriceExTotal"), rsPO("Code"), 0)
			End If
		Else
			strPOLastLineApprover = "An error occurred"
		End If
	Else
		strPOLastLineApprover = "An error occurred"
	End If
	
	rsPO.Close
	Set rsPO = Nothing
	GetPOLastLineApprover = strPOLastLineApprover
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetPOLastLineApprover_Find(decNettPriceTotal, strCode, currentDepth)
	On Error Resume Next
	
	Dim rsUser, sql, strLineManagerCode, decPOApprovalLimit, maxDepth
	
	' Validate inputs
	If IsNull(strCode) Or strCode = "" Then
		GetPOLastLineApprover_Find = "An error occurred"
		Exit Function
	End If
	
	If IsNull(decNettPriceTotal) Or Not IsNumeric(decNettPriceTotal) Then
		GetPOLastLineApprover_Find = "An error occurred"
		Exit Function
	End If
	
	' Add depth limit to prevent infinite recursion
	maxDepth = 10
	If IsNull(currentDepth) Or Not IsNumeric(currentDepth) Then currentDepth = 0
	If currentDepth >= maxDepth Then
		GetPOLastLineApprover_Find = "An error occurred"
		Exit Function
	End If
	
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & Replace(strCode, "'", "''") & "'"
	Set rsUser = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rsUser.BOF And rsUser.EOF) Then
		strLineManagerCode = rsUser("LineManagerCode")
		decPOApprovalLimit = rsUser("POApprovalLimit")
		
		If IsNumeric(decPOApprovalLimit) Then
			If CDbl(decNettPriceTotal) <= CDbl(decPOApprovalLimit) Then
				GetPOLastLineApprover_Find = GetUserFullName(rsUser("Code"))
			Else
				GetPOLastLineApprover_Find = GetPOLastLineApprover_Find(decNettPriceTotal, rsUser("LineManagerCode"), currentDepth + 1)
			End If
		Else
			GetPOLastLineApprover_Find = "An error occurred"
		End If
	Else
		GetPOLastLineApprover_Find = "An error occurred"
	End If
	
	rsUser.Close
	Set rsUser = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function PurchaseOrderDetails_ForEmail(lngPOid)
	On Error Resume Next
	
	Dim sql, rsPO, rsPOC, strPODetails, strCompany
	
	' Validate input
	If IsNull(lngPOid) Or Not IsNumeric(lngPOid) Then
		PurchaseOrderDetails_ForEmail = ""
		Exit Function
	End If
	
	lngPOid = CLng(lngPOid)
	
	sql = "Select * From PurchaseOrders Inner Join Contacts_WithCustomersAndSuppliers_V2 On Contacts_WithCustomersAndSuppliers_V2.ContactId = PurchaseOrders.ContactId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not (rsPO.BOF And rsPO.EOF) Then
		strCompany = rsPO("CompanyName")
		If strCompany = "" Then strCompany = "Non account supplier"
		strPODetails = VbCrLf & VbCrLf
		strPODetails = strPODetails & "All prices are ex. GST" & VbCrLf & VbCrLf
		strPODetails = strPODetails & "Supplier: " & strCompany & VbCrLf
		strPODetails = strPODetails & "Project: " & rsPO("Project") & VbCrLf
		strPODetails = strPODetails & "Total Value: " & FormatCurrency(rsPO("PriceExTotal"),2) & VbCrLf
		
		sql = "Select * From PurchaseOrderContents Where POid = " & lngPOid & " Order By POItemId"
		Set rsPOC = dbConn.Execute(sql)
		
		If Err.Number = 0 And Not(rsPOC.BOF And rsPOC.EOF) Then
			strPODetails = strPODetails & VbCrLf & "PO Items:" & VbCrLf
			Do Until rsPOC.EOF
				strPODetails = strPODetails & Replace(rsPOC("Quantity") & " x " & rsPOC("Description") & " @ " & FormatCurrency(rsPOC("PriceEx"),2), vbcrlf, "") & VbCrLf 
				rsPOC.MoveNext
			Loop
		End If
		
		rsPOC.Close
		Set rsPOC = Nothing
	End If
	
	rsPO.Close
	Set rsPO = Nothing
	PurchaseOrderDetails_ForEmail = strPODetails
	
	Err.Clear
	On Error GoTo 0
End Function

%>
