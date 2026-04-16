			<!-- Priority Actions -->
			<div class="tl-panel" style="background: linear-gradient(135deg, #fffaf0 0%, #ffffff 100%); border-left: 4px solid #d69e2e; border-radius: 12px; padding: 20px;">
				<h3 style="font-size: 15px; font-weight: 600; color: #975a16; margin-bottom: 16px; display: flex; align-items: center; gap: 8px;">
					<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="width: 20px; height: 20px;">
						<path d="M9 11l3 3L22 4"></path>
						<polyline points="21 12 21 19 5 19 5 5 16 5"></polyline>
					</svg>
					Priority Actions
				</h3>
				
				<div style="display: flex; flex-direction: column; gap: 10px;">
					<a href="<%= dash_strWorkingDir %>/Admin/MYOBData.asp" target="_self" class="tl-priority-item">
						<div class="tl-priority-icon">
							<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
								<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
							</svg>
						</div>
						<div style="flex: 1;">
							<p style="font-weight: 600; color: #744210; font-size: 13px;">Export MYOB Data</p>
							<p style="font-size: 11px; color: #975a16;">Monthly financial export for accounting</p>
						</div>
						<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
							<polyline points="9 18 15 12 9 6"></polyline>
						</svg>
					</a>

					<a href="<%= dash_strWorkingDir %>/Quotes/?status=pending" target="_self" class="tl-priority-item">
						<div class="tl-priority-icon">
							<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
								<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
							</svg>
						</div>
						<div style="flex: 1;">
							<p style="font-weight: 600; color: #744210; font-size: 13px;">Review Pending Quotes</p>
							<p style="font-size: 11px; color: #975a16;">Follow up on outstanding quotes</p>
						</div>
						<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
							<polyline points="9 18 15 12 9 6"></polyline>
						</svg>
					</a>

					<a href="<%= dash_strWorkingDir %>/Admin/" target="_self" class="tl-priority-item">
						<div class="tl-priority-icon">
							<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
								<path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"></path>
							</svg>
						</div>
						<div style="flex: 1;">
							<p style="font-weight: 600; color: #744210; font-size: 13px;">Admin Dashboard</p>
							<p style="font-size: 11px; color: #975a16;">Financial reports and analytics</p>
						</div>
						<svg viewBox="0 0 24 24" fill="none" stroke="#975a16" stroke-width="2" style="width: 16px; height: 16px;">
							<polyline points="9 18 15 12 9 6"></polyline>
						</svg>
					</a>
				</div>
			</div>
