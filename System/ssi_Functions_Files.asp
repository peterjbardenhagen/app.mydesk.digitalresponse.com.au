<%

Function GetFilesCategories(intCategoryId)
	On Error Resume Next
	
	Dim rsCat, sql, s, strSelected
	
	' Validate input
	If IsNull(intCategoryId) Or Not IsNumeric(intCategoryId) Then
		intCategoryId = 0
	Else
		intCategoryId = CLng(intCategoryId)
	End If
	
	Set rsCat = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM FilesCategories ORDER BY Category"
	Set rsCat = dbConn.Execute(sql)
	
	s = ""
	s = s & "<select name='CategoryId'>" & vbcrlf
	s = s & "<option value=''></option>" & vbcrlf
	
	If Err.Number = 0 Then
		Do Until rsCat.EOF
			If IsNumeric(rsCat("CategoryId")) Then
				If CInt(rsCat("CategoryId")) = CInt(intCategoryId) Then strSelected = "selected" Else strSelected = ""
				s = s & "<option value='" & rsCat("CategoryId") & "' " & strSelected & ">" & rsCat("Category") & "</option>" & vbcrlf
			End If
			rsCat.MoveNext
		Loop
	End If
	
	s = s & "</select>" & vbcrlf
	GetFilesCategories = s
	
	rsCat.Close
	Set rsCat = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetFileCategoriesAsArr(intCategoryId)
	On Error Resume Next
	
	Dim arrUser(500), rs, sql, i
	
	If IsNull(intCategoryId) Or intCategoryId = "" Then
		GetFileCategoriesAsArr = arrUser
		Exit Function
	End If
	
	Set rs = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT * FROM FilesCategories ORDER BY Category"
	Set rs = dbConn.Execute(sql)
	
	i = 0
	If Err.Number = 0 Then
		Do Until rs.EOF
			If i < 500 Then
				arrUser(i) = rs("Category")
				i = i + 1
			End If
			rs.MoveNext
		Loop
	End If
	
	rs.Close
	Set rs = Nothing
	GetFileCategoriesAsArr = arrUser
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetNodePath(categoryId)
	On Error Resume Next
	
	Dim s, n, i, category, rscu, rsfc, maxIterations, iterationCount
	
	' Validate input
	If IsNull(categoryId) Or Not IsNumeric(categoryId) Then
		GetNodePath = ""
		Exit Function
	End If
	
	maxIterations = 10
	iterationCount = 0
	i = 0
	s = ""
	n = "start"
	
	Set rscu = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Category from FilesCategories Where CategoryId = " & CLng(categoryId)
	Set rscu = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not (rscu.BOF and rscu.EOF) Then
	    category = rscu("Category")
	End If
	
	rscu.Close
	Set rscu = Nothing
	
	Do Until n = "" Or i >= 3 Or iterationCount >= maxIterations
	    iterationCount = iterationCount + 1
	    
	    Set rsfc = Server.CreateObject("ADODB.RecordSet")
	    sql = "Select ParentCategoryId from FilesCategories Where CategoryId = " & CLng(categoryId)
	    Set rsfc = dbConn.Execute(sql)
	    
	    If Err.Number = 0 And Not (rsfc.BOF And rsfc.EOF) Then
	        n = GetParentCategory(rsfc("ParentCategoryId"))
	        s = n & "/" & s
	        categoryId = rsfc("ParentCategoryId")
	    Else
	        n = ""
	    End If
	    
	    rsfc.Close
	    Set rsfc = Nothing
	    
	    i = i + 1
	Loop
	
	s = s & category
	If Left(s,1) <> "/" Then s = "/" & s
	GetNodePath = s
	
	Err.Clear
	On Error GoTo 0
End Function

Function GetParentCategory(parentCategoryId)
	On Error Resume Next
	
	Dim rsnp, sql
	
	' Validate input
	If IsNull(parentCategoryId) Or parentCategoryId = "" Or Not IsNumeric(parentCategoryId) Then
		GetParentCategory = ""
		Exit Function
	End If
	
	Set rsnp = Server.CreateObject("ADODB.RecordSet")
	sql = "Select Category from FilesCategories Where CategoryId = " & CLng(parentCategoryId)
	Set rsnp = dbConn.Execute(sql)
	
	If Err.Number = 0 And Not(rsnp.BOF And rsnp.EOF) Then
	    GetParentCategory = rsnp("Category")
	Else
	    GetParentCategory = ""
	End If
	
	rsnp.Close
	Set rsnp = Nothing
	
	Err.Clear
	On Error GoTo 0
End Function

Function ElementInArray(arrArray, strCompare)
	On Error Resume Next
	
	Dim iLoop, bolFound
	
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
	
	Err.Clear
	On Error GoTo 0
End Function

%>
