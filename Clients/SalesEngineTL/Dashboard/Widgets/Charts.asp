		<% If dash_isDirector Then %>
		<!-- Charts Row -->
		<div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 20px; margin-bottom: 24px;">
			<!-- Monthly Performance Chart -->
			<div class="tl-panel" style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); max-height: 200px; overflow: hidden;">
				<h3 style="font-size: 16px; font-weight: 600; color: var(--tl-dark); margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
						<line x1="18" y1="20" x2="18" y2="10"></line>
						<line x1="12" y1="20" x2="12" y2="4"></line>
						<line x1="6" y1="20" x2="6" y2="14"></line>
					</svg>
					Monthly Quote Performance vs Last Year
				</h3>
				<div style="height: 120px; overflow: hidden;">
					<canvas id="monthlyChart"></canvas>
				</div>
			</div>

			<!-- Revenue Breakdown Chart -->
			<div class="tl-panel" style="background: white; border-radius: 12px; padding: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.08); max-height: 200px; overflow: hidden;">
				<h3 style="font-size: 16px; font-weight: 600; color: var(--tl-dark); margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px; color: var(--tl-primary);">
						<path d="M21.21 15.89A10 10 0 1 1 8 2.83"></path>
						<path d="M22 12A10 10 0 0 0 12 2v10z"></path>
					</svg>
					Invoice Breakdown This Year
				</h3>
				<div style="height: 120px; overflow: hidden;">
					<canvas id="revenueChart"></canvas>
				</div>
			</div>
		</div>
		<% End If %>
