<%

Function GetFilesCategories(intCategoryId)
	If Not IsNumeric(intCategoryId) Then intCategoryId = 0
	Set rsCat = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM FilesCategories ORDER BY Category"
	Set rsCat = dbConn.Execute(sql)
	s = s & "<select name='CategoryId'>" & vbcrlf
	s = s & "<option value=''></option>" & vbcrlf
	Do Until rsCat.EOF
		If CInt(rsCat("CategoryId")) = CInt(intCategoryId) Then strSelected = "selected" Else strSelected = ""
		s = s & "<option value='" & rsCat("CategoryId") & "' " & strSelected & ">" & rsCat("Category") & "</option>" & vbcrlf
		rsCat.MoveNext
	Loop
	s = s & "</select>" & vbcrlf
	GetFilesCategories = s
End Function

Function GetFileCategoriesAsArr(intCategoryId)
	If intCategoryId <> "" Then
		Dim arrUser(500)
		Dim i
		Set rs = Server.CreateObject("ADODB.RecordSet")
		sql = "SELECT * FROM FilesCategories ORDER BY Category"
		Set rs = dbConn.Execute(sql)
		i = 0
		Do Until rs.EOF
			arrUser(i) = rs("Category")
			i = i + 1
			rs.MoveNext
		Loop
		GetFileCategoriesAsArr = arrUser
	End If
End Function

Function GetNodePath(categoryId)
	Dim s
	Dim n
	Dim i
	Dim category
	i = 0
	Set rscu = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Category from FilesCategories Where CategoryId = " & categoryId
	Set rscu = dbConn.Execute(sql)
	If Not (rscu.BOF and rscu.EOF) Then
	    category = rscu("Category")
	End If
	n = "start"
	Do Until n = "" Or i = 3
	    Set rsfc = Server.CreateObject("ADODB.RecordSet")
	    sql = "Select ParentCategoryId from FilesCategories Where CategoryId = " & categoryId
	    Set rsfc = dbConn.Execute(sql)
	    If Not (rsfc.BOF And rsfc.EOF) Then
	        n = GetParentCategory(rsfc("ParentCategoryId"))
	        s = n & "/" & s
	        categoryId = rsfc("ParentCategoryId")
	    End If
	    i = i + 1
	Loop
	s = s & category
	If Left(s,1) <> "/" Then s = "/" & s
	GetNodePath = s
End Function

Function GetParentCategory(parentCategoryId)
    If parentCategoryId <> "" Then
		Dim rsnp
		Set rsnp = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Category from FilesCategories Where CategoryId = " & parentCategoryId
		Set rsnp = dbConn.Execute(sql)
		If Not(rsnp.BOF And rsnp.EOF) Then
		    GetParentCategory = rsnp("Category")
		Else
		    GetParentCategory = ""
		End If
		rsnp.Close
		Set rsnp = Nothing
    End If
End Function

Function ElementInArray(arrArray, strCompare)
	Dim iLoop
	Dim bolFound
	bolFound = False
	If IsArray(arrArray) Then
		For iLoop = 0 To UBound(arrArray)
			If CStr(arrArray(iLoop)) = CStr(strCompare) Then
				bolFound = True
				Exit For
			End If
		Next
	End If
	ElementInArray = bolFound
End Function

%>
