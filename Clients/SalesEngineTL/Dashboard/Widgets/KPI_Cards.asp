		<% If dash_isDirector Then %>
		<!-- KPI Cards Row -->
		<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); gap: 16px; margin-bottom: 24px;">
			<!-- Quotes Won This Month -->
			<div class="tl-kpi-card" style="background: linear-gradient(135deg, #00a8b5 0%, #00c4d3 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(0,168,181,0.3);">
				<div style="display: flex; justify-content: space-between; align-items: start;">
					<div>
						<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">Quotes Won This Month</p>
						<h3 style="font-size: 32px; font-weight: 700; margin: 0;"><%= dash_thisMonthQuotesWon %></h3>
						<p style="font-size: 14px; margin-top: 4px;">$<%= FormatNumber(dash_thisMonthQuotesValue, 2) %></p>
					</div>
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
						<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
						<polyline points="14 2 14 8 20 8"></polyline>
					</svg>
				</div>
				<% If dash_lastMonthQuotesWon > 0 Then %>
				<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
					<% If dash_thisMonthQuotesWon >= dash_lastMonthQuotesWon Then %>
						<span style="color: #90EE90;">&#9650; <%= FormatNumber(((dash_thisMonthQuotesWon-dash_lastMonthQuotesWon)/dash_lastMonthQuotesWon)*100, 1) %>%</span> vs last month
					<% Else %>
						<span style="color: #FFB6C1;">&#9660; <%= FormatNumber(((dash_lastMonthQuotesWon-dash_thisMonthQuotesWon)/dash_lastMonthQuotesWon)*100, 1) %>%</span> vs last month
					<% End If %>
				</div>
				<% End If %>
			</div>

			<!-- Invoices This Month -->
			<div class="tl-kpi-card" style="background: linear-gradient(135deg, #d4a574 0%, #e8c088 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(212,165,116,0.3);">
				<div style="display: flex; justify-content: space-between; align-items: start;">
					<div>
						<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">Invoices This Month</p>
						<h3 style="font-size: 32px; font-weight: 700; margin: 0;"><%= dash_thisMonthInvoices %></h3>
						<p style="font-size: 14px; margin-top: 4px;">$<%= FormatNumber(dash_thisMonthInvoiceValue, 2) %></p>
					</div>
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
						<rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
						<line x1="3" y1="9" x2="21" y2="9"></line>
					</svg>
				</div>
				<% If dash_lastMonthInvoices > 0 Then %>
				<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
					<% If dash_thisMonthInvoices >= dash_lastMonthInvoices Then %>
						<span style="color: #90EE90;">&#9650; <%= FormatNumber(((dash_thisMonthInvoices-dash_lastMonthInvoices)/dash_lastMonthInvoices)*100, 1) %>%</span> vs last month
					<% Else %>
						<span style="color: #FFB6C1;">&#9660; <%= FormatNumber(((dash_lastMonthInvoices-dash_thisMonthInvoices)/dash_lastMonthInvoices)*100, 1) %>%</span> vs last month
					<% End If %>
				</div>
				<% End If %>
			</div>

			<!-- YTD Performance -->
			<div class="tl-kpi-card" style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(102,126,234,0.3);">
				<div style="display: flex; justify-content: space-between; align-items: start;">
					<div>
						<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">YTD Quotes Won</p>
						<h3 style="font-size: 18px; font-weight: 700; margin: 0;">$<%= FormatNumber(dash_ytdQuotesValue, 2) %></h3>
						<p style="font-size: 14px; margin-top: 4px;"><%= dash_ytdQuotesWon %> quotes</p>
					</div>
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
						<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
					</svg>
				</div>
				<% If dash_lastYearYTDQuotesValue > 0 Then %>
				<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
					<% If dash_ytdQuotesValue >= dash_lastYearYTDQuotesValue Then %>
						<span style="color: #90EE90;">&#9650; <%= FormatNumber(((dash_ytdQuotesValue-dash_lastYearYTDQuotesValue)/dash_lastYearYTDQuotesValue)*100, 1) %>%</span> vs last year YTD
					<% Else %>
						<span style="color: #FFB6C1;">&#9660; <%= FormatNumber(((dash_lastYearYTDQuotesValue-dash_ytdQuotesValue)/dash_lastYearYTDQuotesValue)*100, 1) %>%</span> vs last year YTD
					<% End If %>
				</div>
				<% End If %>
			</div>

			<!-- Win Rate -->
			<div class="tl-kpi-card" style="background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); color: white; border-radius: 12px; padding: 20px; box-shadow: 0 4px 12px rgba(17,153,142,0.3);">
				<div style="display: flex; justify-content: space-between; align-items: start;">
					<div>
						<p style="font-size: 12px; opacity: 0.9; margin-bottom: 4px;">Quote Win Rate</p>
						<h3 style="font-size: 32px; font-weight: 700; margin: 0;">
							<% If dash_thisMonthQuotes > 0 Then %>
								<%= FormatNumber((dash_thisMonthQuotesWon/dash_thisMonthQuotes)*100, 1) %>%
							<% Else %>
								0%
							<% End If %>
						</h3>
						<p style="font-size: 14px; margin-top: 4px;"><%= dash_thisMonthQuotesWon %> / <%= dash_thisMonthQuotes %> quotes</p>
					</div>
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 40px; height: 40px; opacity: 0.3;">
						<polyline points="23 6 13.5 15.5 8.5 10.5 1 18"></polyline>
						<polyline points="17 6 23 6 23 12"></polyline>
					</svg>
				</div>
				<div style="margin-top: 12px; font-size: 12px; opacity: 0.9;">
					<% If dash_thisMonthQuotesWon >= dash_lastMonthQuotesWon Then %>
						<span style="color: #90EE90;">&#9650;</span> Trending up
					<% Else %>
						<span style="color: #FFB6C1;">&#9660;</span> Trending down
					<% End If %>
				</div>
			</div>
		</div>
		<% End If %>
