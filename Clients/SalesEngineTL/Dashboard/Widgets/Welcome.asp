		<!-- Welcome Section -->
		<div class="tl-welcome-card">
			<h1 class="tl-welcome-title">Welcome back, <%= dash_userName %></h1>
			<p class="tl-welcome-subtitle">You have successfully logged into Techlight MyDesk. You are an <%= dash_userRole %>.</p>
			<div class="tl-welcome-meta">
				<span class="tl-meta-item">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
						<circle cx="12" cy="7" r="4"></circle>
					</svg>
					<%= dash_userCode %>
				</span>
				<span class="tl-meta-item">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
						<line x1="16" y1="2" x2="16" y2="6"></line>
						<line x1="8" y1="2" x2="8" y2="6"></line>
						<line x1="3" y1="10" x2="21" y2="10"></line>
					</svg>
					<%= FormatDateTime(Now(), 1) %>
				</span>
				<span class="tl-meta-item">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M12 2L2 7l10 5 10-5-10-5z"></path>
						<path d="M2 17l10 5 10-5"></path>
						<path d="M2 12l10 5 10-5"></path>
					</svg>
					Techlight MyDesk
				</span>
				<a href="https://techlight.com.au" target="_blank" class="tl-meta-item" style="text-decoration: none; color: var(--tl-primary);">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6"></path>
						<polyline points="15 3 21 3 21 9"></polyline>
						<line x1="10" y1="14" x2="21" y2="3"></line>
					</svg>
					Techlight Website
				</a>
				<a href="https://outlook.office365.com/mail/" target="_blank" class="tl-meta-item" style="text-decoration: none; color: var(--tl-primary);">
					<svg class="tl-meta-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
						<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
						<polyline points="22,6 12,13 2,6"></polyline>
					</svg>
					Outlook
				</a>
			</div>
			
			<!-- Quick Navigation Search -->
			<div class="tl-quick-search">
				<span class="tl-quick-search-label">Quick Navigation</span>
				<form action="<%= dash_strWorkingDir %>/QuickNav.asp" method="get" target="_self" style="display: flex; align-items: center; gap: 8px; flex: 1;">
					<input type="text" name="ID" class="tl-quick-search-input" placeholder="Enter ID #" required>
					<select name="Type" class="tl-quick-search-select">
						<option value="Quote">Quote</option>
						<option value="PurchaseOrder">Purchase Order</option>
						<option value="Invoice">Invoice</option>
						<option value="Contact">Contact</option>
					</select>
					<button type="submit" class="tl-btn-primary">Go</button>
				</form>
			</div>
		</div>
