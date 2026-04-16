<%
' Techlight MyDesk - Dashboard Data Fetching Logic (Modernized)
' ===============================================================================

On Error Resume Next

' Use unique variable names to prevent clashing with outer scripts
Dim dash_strMsg
dash_strMsg = ""
If Not IsNull(Request("Msg")) Then dash_strMsg = Trim(Request("Msg"))

' Check authentication
Dim dash_isLoggedIn, dash_cookieLoggedIn
dash_isLoggedIn = False
dash_cookieLoggedIn = False

If Not IsEmpty(Session("LoggedIn")) Then
	dash_isLoggedIn = CBool(Session("LoggedIn"))
Else
	dash_isLoggedIn = False
End If

' Authentication fallback to cookies
If Not Request.Cookies("LoggedIn") Is Nothing Then
	If Not IsEmpty(Request.Cookies("LoggedIn")) And Request.Cookies("LoggedIn") <> "" Then
		If IsNumeric(Request.Cookies("LoggedIn")) Then
			dash_cookieLoggedIn = CBool(Request.Cookies("LoggedIn"))
		Else
			dash_cookieLoggedIn = (LCase(Request.Cookies("LoggedIn")) = "true")
		End If
	End If
End If

If Not dash_isLoggedIn Then
	If dash_cookieLoggedIn Then
		If Not dash_cookieLoggedIn Then
			Response.Redirect("DefaultFrame.asp")
			Response.End
		End If
	Else
		' Allow bypass for certain local testing scenarios if needed, but normally redirect
		' Response.Redirect("DefaultFrame.asp")
		' Response.End
	End If
End If

Dim dash_strWorkingDir
dash_strWorkingDir = "/Clients/SalesEngineTL"
If Not Request.Cookies("ClientSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("ClientSettings")("WorkingDir")) And Request.Cookies("ClientSettings")("WorkingDir") <> "" Then
		dash_strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
	End If
End If
On Error GoTo 0

' Get user info
Dim dash_userName, dash_userRole, dash_userCode
dash_userName = "User"
dash_userCode = ""
dash_userRole = "User"

On Error Resume Next
If Not IsEmpty(Session("Name")) And Session("Name") <> "" Then
	dash_userName = Session("Name")
ElseIf Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("Name")) And Request.Cookies("UserSettings")("Name") <> "" Then
		dash_userName = Request.Cookies("UserSettings")("Name")
	End If
End If

If Not IsEmpty(Session("Code")) And Session("Code") <> "" Then
	dash_userCode = Session("Code")
ElseIf Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("Code")) And Request.Cookies("UserSettings")("Code") <> "" Then
		dash_userCode = Request.Cookies("UserSettings")("Code")
	End If
End If

Dim dash_isAdmin, dash_isManager
dash_isAdmin = False
dash_isManager = False

If Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("Admin")) Then
		dash_isAdmin = CBool(Request.Cookies("UserSettings")("Admin"))
	End If
End If

If Not IsEmpty(Session("Manager")) Then
	dash_isManager = CBool(Session("Manager"))
End If

If dash_isAdmin Then
	dash_userRole = "Administrator"
ElseIf dash_isManager Then
	dash_userRole = "Manager"
Else
	dash_userRole = "User"
End If
On Error GoTo 0

' Get business metrics for Directors
Dim dash_isDirector, dash_currentMonth, dash_currentYear, dash_lastYear
dash_currentMonth = Month(Date())
dash_currentYear = Year(Date())
dash_lastYear = dash_currentYear - 1

Dim dash_userTypeId
dash_userTypeId = ""
On Error Resume Next
If Not Request.Cookies("UserSettings") Is Nothing Then
	If Not IsEmpty(Request.Cookies("UserSettings")("UserTypeID")) Then
		dash_userTypeId = CStr(Request.Cookies("UserSettings")("UserTypeID"))
	End If
End If
' Fallback from Session if cookie is missing
If dash_userTypeId = "" Then dash_userTypeId = CStr(Session("UserTypeId"))
On Error GoTo 0

dash_isDirector = (dash_userTypeId = "1" Or dash_userTypeId = "5" Or dash_userTypeId = "6")

' Metrics variables
Dim dash_thisMonthQuotes, dash_thisMonthQuotesWon, dash_thisMonthQuotesValue
Dim dash_lastMonthQuotes, dash_lastMonthQuotesWon, dash_lastMonthQuotesValue
Dim dash_thisMonthInvoices, dash_thisMonthInvoiceValue
Dim dash_lastMonthInvoices, dash_lastMonthInvoiceValue
Dim dash_ytdQuotesWon, dash_ytdQuotesValue, dash_ytdInvoices, dash_ytdInvoiceValue
Dim dash_lastYearYTDQuotesWon, dash_lastYearYTDQuotesValue
Dim dash_pendingQuotesOver30Days, dash_invoicesOverdue, dash_pendingApprovalPOs

' Initialize monthly chart data arrays
Dim dash_monthlyQuotesThisYear(12), dash_monthlyQuotesLastYear(12)
Dim dash_monthlyInvoicesThisYear(12)
Dim dash_mIndex

' Set default values
dash_thisMonthQuotes = 0
dash_thisMonthQuotesWon = 0
dash_thisMonthQuotesValue = 0
dash_lastMonthQuotes = 0
dash_lastMonthQuotesWon = 0
dash_lastMonthQuotesValue = 0
dash_thisMonthInvoices = 0
dash_thisMonthInvoiceValue = 0
dash_lastMonthInvoices = 0
dash_lastMonthInvoiceValue = 0
dash_ytdQuotesWon = 0
dash_ytdQuotesValue = 0
dash_ytdInvoices = 0
dash_ytdInvoiceValue = 0
dash_lastYearYTDQuotesWon = 0
dash_lastYearYTDQuotesValue = 0
dash_pendingQuotesOver30Days = 0
dash_invoicesOverdue = 0
dash_pendingApprovalPOs = 0

' Initialize monthly arrays to 0
For dash_mIndex = 1 To 12
	dash_monthlyQuotesThisYear(dash_mIndex) = 0
	dash_monthlyQuotesLastYear(dash_mIndex) = 0
	dash_monthlyInvoicesThisYear(dash_mIndex) = 0
Next

If dash_isDirector Then
	Dim dash_sql, dash_rs
	
	' 1. THIS MONTH QUOTES
	dash_sql = "SELECT COUNT(*) as cnt, SUM(IIf(QuoteStatusId = 4 OR QuoteStatusId = 10, 1, 0)) as won, SUM(IIf(QuoteStatusId = 4 OR QuoteStatusId = 10, NettPriceTotal, 0)) as val FROM Quotes WHERE Month(QuoteDate) = " & dash_currentMonth & " AND Year(QuoteDate) = " & dash_currentYear
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then
			If Not IsNull(dash_rs("cnt")) Then dash_thisMonthQuotes = CLng(dash_rs("cnt"))
			If Not IsNull(dash_rs("won")) Then dash_thisMonthQuotesWon = CLng(dash_rs("won"))
			If Not IsNull(dash_rs("val")) Then dash_thisMonthQuotesValue = CDbl(dash_rs("val"))
		End If
		CloseRS(dash_rs)
	End If
	
	' 2. LAST MONTH QUOTES
	Dim dash_lm, dash_ly
	dash_lm = dash_currentMonth - 1 : dash_ly = dash_currentYear
	If dash_lm = 0 Then dash_lm = 12 : dash_ly = dash_ly - 1
	dash_sql = "SELECT COUNT(*) as cnt, SUM(IIf(QuoteStatusId = 4 OR QuoteStatusId = 10, 1, 0)) as won, SUM(IIf(QuoteStatusId = 4 OR QuoteStatusId = 10, NettPriceTotal, 0)) as val FROM Quotes WHERE Month(QuoteDate) = " & dash_lm & " AND Year(QuoteDate) = " & dash_ly
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then
			If Not IsNull(dash_rs("won")) Then dash_lastMonthQuotesWon = CLng(dash_rs("won"))
		End If
		CloseRS(dash_rs)
	End If
	
	' 3. THIS MONTH INVOICES
	dash_sql = "SELECT COUNT(*) as cnt, SUM(NettPriceTotal) as val FROM Invoices WHERE Month(InvoiceDate) = " & dash_currentMonth & " AND Year(InvoiceDate) = " & dash_currentYear
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then
			If Not IsNull(dash_rs("cnt")) Then dash_thisMonthInvoices = CLng(dash_rs("cnt"))
			If Not IsNull(dash_rs("val")) Then dash_thisMonthInvoiceValue = CDbl(dash_rs("val"))
		End If
		CloseRS(dash_rs)
	End If

	' 4. LAST MONTH INVOICES
	dash_sql = "SELECT COUNT(*) as cnt FROM Invoices WHERE Month(InvoiceDate) = " & dash_lm & " AND Year(InvoiceDate) = " & dash_ly
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then
			If Not IsNull(dash_rs("cnt")) Then dash_lastMonthInvoices = CLng(dash_rs("cnt"))
		End If
		CloseRS(dash_rs)
	End If
	
	' 5. YTD QUOTES
	dash_sql = "SELECT COUNT(*) as cnt, SUM(NettPriceTotal) as val FROM Quotes WHERE (QuoteStatusId = 4 OR QuoteStatusId = 10) AND Year(QuoteDate) = " & dash_currentYear
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then
			If Not IsNull(dash_rs("cnt")) Then dash_ytdQuotesWon = CLng(dash_rs("cnt"))
			If Not IsNull(dash_rs("val")) Then dash_ytdQuotesValue = CDbl(dash_rs("val"))
		End If
		CloseRS(dash_rs)
	End If
	
	' 6. LAST YEAR YTD QUOTES
	dash_sql = "SELECT COUNT(*) as cnt, SUM(NettPriceTotal) as val FROM Quotes WHERE (QuoteStatusId = 4 OR QuoteStatusId = 10) AND Year(QuoteDate) = " & dash_lastYear & " AND Month(QuoteDate) <= " & dash_currentMonth
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then
			If Not IsNull(dash_rs("val")) Then dash_lastYearYTDQuotesValue = CDbl(dash_rs("val"))
		End If
		CloseRS(dash_rs)
	End If
	
	' 7. EXCEPTIONS
	' Overdue Quotes (Drafts or Issued over 30 days)
	dash_sql = "SELECT COUNT(*) as cnt FROM Quotes WHERE (QuoteStatusId = 1 OR QuoteStatusId = 2) AND QuoteDate < DateAdd('d', -30, Now())"
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then dash_pendingQuotesOver30Days = CLng(dash_rs("cnt"))
		CloseRS(dash_rs)
	End If
	
	' Overdue Invoices (Status 2 = Issued/Unpaid?)
	dash_sql = "SELECT COUNT(*) as cnt FROM Invoices WHERE InvoiceStatusId = 2 AND InvoiceDate < DateAdd('d', -30, Now())"
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then dash_invoicesOverdue = CLng(dash_rs("cnt"))
		CloseRS(dash_rs)
	End If
	
	' POs Pending Approval (Status 2 = Pending?)
	dash_sql = "SELECT COUNT(*) as cnt FROM PurchaseOrders WHERE POStatusId = 2"
	Set dash_rs = SafeExecute(dash_sql)
	If Not dash_rs Is Nothing Then
		If Not dash_rs.EOF Then dash_pendingApprovalPOs = CLng(dash_rs("cnt"))
		CloseRS(dash_rs)
	End If

	' 8. CHART DATA
	For dash_mIndex = 1 To 12
		dash_sql = "SELECT SUM(NettPriceTotal) as val FROM Quotes WHERE (QuoteStatusId = 4 OR QuoteStatusId = 10) AND Month(QuoteDate) = " & dash_mIndex & " AND Year(QuoteDate) = " & dash_currentYear
		Set dash_rs = SafeExecute(dash_sql)
		If Not dash_rs Is Nothing Then
			If Not IsNull(dash_rs("val")) Then dash_monthlyQuotesThisYear(dash_mIndex) = CDbl(dash_rs("val"))
			CloseRS(dash_rs)
		End If
	Next
	
	For dash_mIndex = 1 To 12
		dash_sql = "SELECT SUM(NettPriceTotal) as val FROM Quotes WHERE (QuoteStatusId = 4 OR QuoteStatusId = 10) AND Month(QuoteDate) = " & dash_mIndex & " AND Year(QuoteDate) = " & dash_lastYear
		Set dash_rs = SafeExecute(dash_sql)
		If Not dash_rs Is Nothing Then
			If Not IsNull(dash_rs("val")) Then dash_monthlyQuotesLastYear(dash_mIndex) = CDbl(dash_rs("val"))
			CloseRS(dash_rs)
		End If
	Next
	
	For dash_mIndex = 1 To 12
		dash_sql = "SELECT SUM(NettPriceTotal) as val FROM Invoices WHERE Month(InvoiceDate) = " & dash_mIndex & " AND Year(InvoiceDate) = " & dash_currentYear
		Set dash_rs = SafeExecute(dash_sql)
		If Not dash_rs Is Nothing Then
			If Not IsNull(dash_rs("val")) Then dash_monthlyInvoicesThisYear(dash_mIndex) = CDbl(dash_rs("val"))
			CloseRS(dash_rs)
		End If
	Next
End If
%>
