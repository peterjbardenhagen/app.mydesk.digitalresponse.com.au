			<!-- Recent Activity -->
			<div class="tl-card">
				<div class="tl-card-header">
					<h3 class="tl-card-title">
						<svg class="tl-panel-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
						</svg>
						Recent Activity
					</h3>
					<a href="ActivityLog.asp" class="tl-btn tl-btn-ghost tl-btn-sm">View All</a>
				</div>
				<div class="tl-card-body" style="padding: 0;">
					<table class="tl-data-table">
						<thead>
							<tr>
								<th>Activity</th>
								<th>Area</th>
								<th style="text-align: right;">When</th>
							</tr>
						</thead>
						<tbody>
<%
' Fetch user history
If IsObject(dbConn) Then
    Dim rsHist
    On Error Resume Next
    Set rsHist = dbConn.Execute("SELECT TOP 10 * FROM UserHistory WHERE UserCode = '" & Replace(userCode, "'", "''") & "' ORDER BY ActionDate DESC")
    If Err.Number <> 0 Then
        Err.Clear
        Set rsHist = dbConn.Execute("SELECT TOP 10 * FROM UserHistory WHERE UserCode = '" & Replace(userCode, "'", "''") & "' ORDER BY VisitDate DESC")
    End If
    On Error GoTo 0
    
    If Not rsHist Is Nothing Then
        If Not rsHist.EOF Then
            Do Until rsHist.EOF
                Dim histUrl, histDate, pageTitle, areaName, pageIcon, timeAgo, diffMin
                histUrl = rsHist("PageUrl")
                On Error Resume Next
                histDate = rsHist("ActionDate")
                If IsEmpty(histDate) Or IsNull(histDate) Then histDate = rsHist("VisitDate")
                On Error GoTo 0
                
                pageTitle = rsHist("PageTitle")
                areaName = "Other"
                If InStr(LCase(histUrl), "/quotes/") > 0 Then areaName = "Quotes"
                If InStr(LCase(histUrl), "/invoices/") > 0 Then areaName = "Invoices"
                If InStr(LCase(histUrl), "/purchaseorders/") > 0 Then areaName = "Purchases"
                If InStr(LCase(histUrl), "/contacts/") > 0 Then areaName = "Contacts"
                If InStr(LCase(histUrl), "/setup/") > 0 Then areaName = "Setup"
                
                If pageTitle = "" Or IsNull(pageTitle) Then
                    pageTitle = areaName
                    If InStr(histUrl, "Qid=") > 0 Then pageTitle = "Quote #" & Mid(histUrl, InStr(histUrl, "Qid=") + 4)
                    If InStr(histUrl, "Pid=") > 0 Then pageTitle = "PO #" & Mid(histUrl, InStr(histUrl, "Pid=") + 4)
                    If InStr(histUrl, "InvId=") > 0 Then pageTitle = "Invoice #" & Mid(histUrl, InStr(histUrl, "InvId=") + 6)
                End If
                
                timeAgo = "Unknown"
                If IsDate(histDate) Then
                    diffMin = DateDiff("n", histDate, Now())
                    If diffMin < 1 Then
                        timeAgo = "Just now"
                    ElseIf diffMin < 60 Then
                        timeAgo = diffMin & "m ago"
                    ElseIf diffMin < 1440 Then
                        timeAgo = Int(diffMin/60) & "h ago"
                    Else
                        timeAgo = Int(diffMin/1440) & "d ago"
                    End If
                End If
%>
							<tr>
								<td>
									<a href="<%= histUrl %>" class="tl-nav-link" style="color: var(--tl-primary); font-weight: 500; padding: 0; display: inline-flex; align-items: center; gap: 8px;">
										<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path><polyline points="15 3 21 3 21 9"></polyline><line x1="10" y1="14" x2="21" y2="3"></line></svg>
										<%= pageTitle %>
									</a>
								</td>
								<td><span class="tl-badge tl-badge-info"><%= areaName %></span></td>
								<td style="text-align: right; color: var(--tl-text-light); font-size: 12px;"><%= timeAgo %></td>
							</tr>
<%
                rsHist.MoveNext
            Loop
        Else
%>
							<tr>
								<td colspan="3" class="tl-empty-state" style="padding: 24px;">No recent activity found</td>
							</tr>
<%
        End If
        If Not rsHist Is Nothing Then rsHist.Close : Set rsHist = Nothing
    End If
End If
%>
						</tbody>
					</table>
				</div>
			</div>
