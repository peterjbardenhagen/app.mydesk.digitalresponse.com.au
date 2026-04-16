		<% If isDirector1 Then %>
		<!-- Exceptions Row -->
		<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; margin-bottom: 24px;">
			<!-- Priority Alerts -->
			<div class="tl-panel" style="background: linear-gradient(135deg, #fff5f5 0%, #ffffff 100%); border-left: 4px solid #e53e3e; border-radius: 12px; padding: 20px;">
				<h3 style="font-size: 15px; font-weight: 600; color: #c53030; margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
						<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path>
						<line x1="12" y1="9" x2="12" y2="13"></line>
						<line x1="12" y1="17" x2="12.01" y2="17"></line>
					</svg>
					Exceptions Requiring Attention
				</h3>
				
				<% If pendingQuotesOver30Days > 0 Then %>
				<div class="tl-exception-item">
					<div style="display: flex; align-items: center; gap: 10px;">
						<div class="tl-exception-icon">
							<svg viewBox="0 0 24 24" fill="none" stroke="#c53030" stroke-width="2" style="width: 16px; height: 16px;">
								<circle cx="12" cy="12" r="10"></circle>
								<polyline points="12 6 12 12 16 14"></polyline>
							</svg>
						</div>
						<div>
							<p style="font-weight: 600; color: #742a2a; font-size: 13px;"><%= pendingQuotesOver30Days %> Quotes Pending > 30 Days</p>
							<p style="font-size: 11px; color: #9b2c2c;">Require follow-up with customers</p>
						</div>
					</div>
					<a href="<%= strWorkingDir %>/Quotes/" target="_self" class="tl-btn-xs tl-btn-danger">View</a>
				</div>
				<% End If %>

				<% If invoicesOverdue > 0 Then %>
				<div class="tl-exception-item">
					<div style="display: flex; align-items: center; gap: 10px;">
						<div class="tl-exception-icon">
							<svg viewBox="0 0 24 24" fill="none" stroke="#c53030" stroke-width="2" style="width: 16px; height: 16px;">
								<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
								<line x1="3" y1="9" x2="21" y2="9"></line>
							</svg>
						</div>
						<div>
							<p style="font-weight: 600; color: #742a2a; font-size: 13px;"><%= invoicesOverdue %> Overdue Invoices</p>
							<p style="font-size: 11px; color: #9b2c2c;">Payment collection required</p>
						</div>
					</div>
					<a href="<%= strWorkingDir %>/Invoices/" target="_self" class="tl-btn-xs tl-btn-danger">View</a>
				</div>
				<% End If %>

				<% If pendingApprovalPOs > 0 Then %>
				<div class="tl-exception-item">
					<div style="display: flex; align-items: center; gap: 10px;">
						<div class="tl-exception-icon">
							<svg viewBox="0 0 24 24" fill="none" stroke="#c53030" stroke-width="2" style="width: 16px; height: 16px;">
								<path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path>
							</svg>
						</div>
						<div>
							<p style="font-weight: 600; color: #742a2a; font-size: 13px;"><%= pendingApprovalPOs %> POs Pending Approval</p>
							<p style="font-size: 11px; color: #9b2c2c;">Awaiting director approval</p>
						</div>
					</div>
					<a href="<%= strWorkingDir %>/PurchaseOrders/" target="_self" class="tl-btn-xs tl-btn-danger">View</a>
				</div>
				<% End If %>

				<% If pendingQuotesOver30Days = 0 AND invoicesOverdue = 0 AND pendingApprovalPOs = 0 Then %>
				<div style="text-align: center; padding: 20px; color: #9b2c2c;">
					<svg viewBox="0 0 24 24" fill="none" stroke="#48bb78" stroke-width="2" style="width: 40px; height: 40px; margin-bottom: 8px;">
						<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
						<polyline points="22 4 12 14.01 9 11.01"></polyline>
					</svg>
					<p style="font-size: 14px; color: #38a169;">All caught up! No exceptions.</p>
				</div>
				<% End If %>
			</div>

			<!-- Priority Actions (Integrated here for Directors) -->
			<!-- Logic for Priority Actions can stay in its own widget but we call it here -->
<% Server.Execute("/Clients/SalesEngineTL/Dashboard/Widgets/PriorityTasks.asp") %>
		</div>
		<% End If %>
