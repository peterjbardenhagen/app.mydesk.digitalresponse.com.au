			<div class="tl-sidebar">
				<!-- Quick Menu -->
				<div class="tl-panel">
					<h3 class="tl-panel-title">
						<svg class="tl-panel-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<circle cx="12" cy="12" r="10"></circle>
							<polyline points="12 6 12 12 16 14"></polyline>
						</svg>
						Quick Access
					</h3>
					<ul class="tl-menu-list">
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Contacts/Add.asp" class="tl-menu-link" target="_self">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Contact
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Quotes/Add.asp" class="tl-menu-link" target="_self">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Quote
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/PurchaseOrders/Add.asp" class="tl-menu-link" target="_self">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<line x1="12" y1="5" x2="12" y2="19"></line>
									<line x1="5" y1="12" x2="19" y2="12"></line>
								</svg>
								New Purchase Order
							</a>
						</li>
						<li class="tl-menu-item">
							<a href="<%= strWorkingDir %>/Reports/" class="tl-menu-link" target="_self">
								<svg class="tl-menu-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
									<path d="M3 3v18h18"></path>
									<path d="M18.7 8l-5.1 5.2-2.8-2.7L7 14.3"></path>
								</svg>
								Reports
							</a>
						</li>
					</ul>
				</div>

				<!-- System Status -->
				<div class="tl-panel">
					<h3 class="tl-panel-title">
						<svg class="tl-panel-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
							<path d="M22 12h-4l-3 9L9 3l-3 9H2"></path>
						</svg>
						System Status
					</h3>
					<div style="font-size: 13px; color: var(--tl-text-light); line-height: 1.8;">
						<div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
							<span>Version:</span>
							<span style="font-weight: 600; color: var(--tl-text);">Techlight MyDesk v3.5</span>
						</div>
						<div style="display: flex; justify-content: space-between; margin-bottom: 8px;">
							<span>Environment:</span>
							<span style="font-weight: 600; color: var(--tl-primary);">
								<% If InStr(Request.ServerVariables("SERVER_NAME"), "localhost") > 0 Then %>Local<% Else %>Production<% End If %>
							</span>
						</div>
						<div style="display: flex; justify-content: space-between;">
							<span>Status:</span>
							<span style="font-weight: 600; color: #22c55e;">Online</span>
						</div>
					</div>
				</div>
			</div>
