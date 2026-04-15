<%

Function GetPONextLineApprover_Email(lngPOid, boolCapEx)
	Dim strEmail
	Dim strName
	Dim strSql
	strSql = "SELECT TOP 1 Users.Email, Users.Name FROM PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code WHERE (((PurchaseOrders.POId)=" & lngPOid & "));"
	Set rsPONext = dbConn.Execute(strSql)
	GetPONextLineApprover_Email = rsPONext("Email")
End Function

Function GetPOLastLineManager_Email(lngPOid)
	Dim strEmail
	Dim strName
	Dim strSql
	strSql = "SELECT TOP 1 Users.Email, Users.Name FROM PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code WHERE (((PurchaseOrders.POId)=" & lngPOid & "));"
	Set rsPONext = dbConn.Execute(strSql)
	GetPOLastLineManager_Email = rsPONext("Email")
End Function

Function GetPONextLineApprover(lngPOid, boolCapEx)
	Dim strLastLineApproverCode
	Dim strPreviousLineApproverCode
	Dim rsUser
	Dim rsPO
	Dim sql
	Dim decPOApprovalLimit
	Dim decNettPriceTotal
	
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit, Users.Name FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	If Not(rsPO.BOF And rsPO.EOF) Then
		decPOApprovalLimit = rsPO("POApprovalLimit")
		decNettPriceTotal = rsPO("PriceExTotal")
		If decNettPriceTotal <= decPOApprovalLimit Then ' PO creator can approve
			GetPONextLineApprover = "Already approved"
		Else
			strLastLineApproverCode = rsPO("Code")
			strPreviousLineApproverCode = ""
			Do While strLastLineApproverCode <> strPreviousLineApproverCode
				strPreviousLineApproverCode = strLastLineApproverCode
				Set rsUser = Server.CreateObject("ADODB.RecordSet")
				sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & strLastLineApproverCode & "'"
				Set rsUser = dbConn.Execute(sql)
				If Not(rsUser.BOF And rsUser.EOF) Then
					strLastLineApproverCode = rsUser("LineManagerCode")
					decPOApprovalLimit = rsUser("POApprovalLimit")
					If CDbl(decNettPriceTotal) <= CDbl(decPOApprovalLimit) Then
						GetPONextLineApprover = rsUser("Name")
						Exit Function
					End If
				End If
			Loop
		End If
	Else
		GetPONextLineApprover = "An error occurred"
	End If
End Function

Function GetPONextLineApproverCode(lngPOid, boolCapEx)
	Dim strLastLineApproverCode
	Dim strPreviousLineApproverCode
	Dim rsUser
	Dim rsPO
	Dim sql
	Dim decPOApprovalLimit
	Dim decNettPriceTotal
	
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit, Users.Name FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	If Not(rsPO.BOF And rsPO.EOF) Then
		decPOApprovalLimit = rsPO("POApprovalLimit")
		decNettPriceTotal = rsPO("PriceExTotal")
		If decNettPriceTotal <= decPOApprovalLimit Then ' PO creator can approve
			GetPONextLineApproverCode = rsPO("Code")
		Else
			strLastLineApproverCode = rsPO("Code")
			strPreviousLineApproverCode = ""
			Do While strLastLineApproverCode <> strPreviousLineApproverCode
				strPreviousLineApproverCode = strLastLineApproverCode
				Set rsUser = Server.CreateObject("ADODB.RecordSet")
				sql = "Select LineManagerCode, * From Users Inner Join UserRoles On UserRoles.UserRoleId = Users.UserRoleId Where Code = '" & strLastLineApproverCode & "'"
				Set rsUser = dbConn.Execute(sql)
				If Not(rsUser.BOF And rsUser.EOF) Then
					strLastLineApproverCode = rsUser("LineManagerCode")
					decPOApprovalLimit = rsUser("POApprovalLimit")
					If CDbl(decNettPriceTotal) <= CDbl(decPOApprovalLimit) Then
						GetPONextLineApproverCode = rsUser("Code")
						Exit Function
					End If
				End If
			Loop
		End If
	Else
		GetPONextLineApproverCode = ""
	End If
End Function

Function GetPOLastLineManagers(lngPOid)
	Dim rs
	Dim sql
    Dim rspo
	Dim s
	Dim Approver
	sql = "SELECT * FROM PurchaseOrders WHERE POid = " & lngPOid
	Set rs = dbConn.Execute(sql)
	If Not(rs.BOF And rs.EOF) Then
		Approver = rs("Code")
		Do While Approver <> ""
			Set rspo = dbConn.Execute("Select LineManagerCode From Users Where Code = '" & Approver & "'")
			If Not(rspo.BOF And rspo.EOF) Then
				Approver = rspo("LineManagerCode")
				If Approver <> "" Then
					s = s & Approver & ","
				End If
			Else
				Approver = ""
			End If
		Loop
	End If
	GetPOLastLineManagers = s
End Function

Function GetPOLastLineApprover(lngPOid, boolCapEx)
	Dim rsPO
	Dim sql
	Dim decPOApprovalLimit
	Dim strPOLastLineApprover
	
	Set rsPO = Server.CreateObject("ADODB.RecordSet")
	sql = "Select PurchaseOrders.*, UserRoles.POApprovalLimit FROM (PurchaseOrders INNER JOIN Users ON PurchaseOrders.Code = Users.Code) INNER JOIN UserRoles ON Users.UserRoleId = UserRoles.UserRoleId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	If Not(rsPO.BOF And rsPO.EOF) Then
		decPOApprovalLimit = rsPO("POApprovalLimit")
		If rsPO("PriceExTotal") <= decPOApprovalLimit Then ' PO creator can approve
			strPOLastLineApprover = "Already approved"
		Else
			strPOLastLineApprover = GetPOLastLineApprover_Find(rsPO("PriceExTotal"), rsPO("Code"))
		End If
	Else
		strPOLastLineApprover = "An error occurred"
	End If
	GetPOLastLineApprover = strPOLastLineApprover
End Function

Function PurchaseOrderDetails_ForEmail(lngPOid)
	Dim sql
	Dim rsPO
	Dim strPODetails
	Dim strCompany
	sql = "Select * From PurchaseOrders Inner Join Contacts_WithCustomersAndSuppliers_V2 On Contacts_WithCustomersAndSuppliers_V2.ContactId = PurchaseOrders.ContactId Where POid = " & lngPOid
	Set rsPO = dbConn.Execute(sql)
	If Not (rsPO.BOF And rsPO.EOF) Then
		strCompany = rsPO("CompanyName")
		If strCompany = "" Then strCompany = "Non account supplier"
		strPODetails = VbCrLf & VbCrLf
		strPODetails = strPODetails & "All prices are ex. GST" & VbCrLf & VbCrLf
		strPODetails = strPODetails & "Supplier: " & strCompany & VbCrLf
		strPODetails = strPODetails & "Project: " & rsPO("Project") & VbCrLf
		strPODetails = strPODetails & "Total Value: " & FormatCurrency(rsPO("PriceExTotal"),2) & VbCrLf
		sql = "Select * From PurchaseOrderContents Where POid = " & lngPOid & " Order By POItemId"
		Set rsPOC = dbConn.Execute(sql)
		If Not(rsPOC.BOF And rsPOC.EOF) Then
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
End Function

%>
