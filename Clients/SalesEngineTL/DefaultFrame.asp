<%
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

If Session("LoggedIn") Then
	Response.Redirect("PortalFrame.asp")
End If
%>
<!DOCTYPE html>
<html lang="en">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title>Sign In - Techlight MyDesk</title>
	<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
	<meta http-equiv="Expires" content="0">
	<meta http-equiv="Pragma" content="no-store">
	<link rel="preconnect" href="https://fonts.googleapis.com">
	<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
	<link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
	<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
	<style>
		:root {
			--tl-primary: #00a8b5;
			--tl-primary-dark: #008a94;
			--tl-primary-light: #00c4d3;
			--tl-accent: #d4a574;
			--tl-dark: #1a1f2e;
			--tl-dark-secondary: #242b3d;
			--tl-text: #2d3748;
			--tl-text-light: #718096;
			--tl-text-muted: #a0aec0;
			--tl-bg: #f7fafc;
			--tl-white: #ffffff;
			--tl-error: #e53e3e;
			--tl-success: #38a169;
			--tl-shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
			--tl-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
			--tl-shadow-lg: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
			--tl-radius: 12px;
		}

		* {
			margin: 0;
			padding: 0;
			box-sizing: border-box;
		}

		body {
			font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
			min-height: 100vh;
			display: flex;
			align-items: center;
			justify-content: center;
			background: linear-gradient(135deg, #1a1f2e 0%, #242b3d 50%, #1a1f2e 100%);
			position: relative;
			overflow: hidden;
		}

		/* Animated background elements */
		.bg-pattern {
			position: absolute;
			top: 0;
			left: 0;
			right: 0;
			bottom: 0;
			overflow: hidden;
			z-index: 0;
		}

		.bg-pattern::before {
			content: '';
			position: absolute;
			top: -50%;
			right: -10%;
			width: 600px;
			height: 600px;
			background: radial-gradient(circle, rgba(0,168,181,0.15) 0%, transparent 70%);
			animation: float 20s ease-in-out infinite;
		}

		.bg-pattern::after {
			content: '';
			position: absolute;
			bottom: -30%;
			left: -10%;
			width: 500px;
			height: 500px;
			background: radial-gradient(circle, rgba(212,165,116,0.1) 0%, transparent 70%);
			animation: float 25s ease-in-out infinite reverse;
		}

		@keyframes float {
			0%, 100% { transform: translate(0, 0) scale(1); }
			33% { transform: translate(30px, -30px) scale(1.1); }
			66% { transform: translate(-20px, 20px) scale(0.9); }
		}

		.login-container {
			position: relative;
			z-index: 1;
			width: 100%;
			max-width: 440px;
			padding: 20px;
		}

		.login-card {
			background: var(--tl-white);
			border-radius: var(--tl-radius);
			padding: 48px 40px;
			box-shadow: var(--tl-shadow-lg);
			position: relative;
			overflow: hidden;
		}

		.login-card::before {
			content: '';
			position: absolute;
			top: 0;
			left: 0;
			right: 0;
			height: 4px;
			background: linear-gradient(90deg, var(--tl-primary) 0%, var(--tl-accent) 100%);
		}

		/* Logo Section */
		.logo-section {
			text-align: center;
			margin-bottom: 40px;
		}

		.logo-icon {
			width: 72px;
			height: 72px;
			margin: 0 auto 20px;
			position: relative;
		}

		.logo-icon svg {
			width: 100%;
			height: 100%;
		}

		.logo-title {
			font-size: 28px;
			font-weight: 700;
			color: var(--tl-dark);
			letter-spacing: -0.5px;
			margin-bottom: 4px;
		}

		.logo-title span {
			color: var(--tl-primary);
		}

		.logo-subtitle {
			font-size: 13px;
			color: var(--tl-text-light);
			text-transform: uppercase;
			letter-spacing: 2px;
		}

		/* Welcome Text */
		.welcome-text {
			text-align: center;
			margin-bottom: 32px;
		}

		.welcome-text h2 {
			font-size: 20px;
			font-weight: 600;
			color: var(--tl-text);
			margin-bottom: 8px;
		}

		.welcome-text p {
			font-size: 14px;
			color: var(--tl-text-light);
		}

		/* Form Styles */
		.login-form {
			display: flex;
			flex-direction: column;
			gap: 20px;
		}

		.form-group {
			position: relative;
		}

		.form-label {
			display: block;
			font-size: 13px;
			font-weight: 500;
			color: var(--tl-text);
			margin-bottom: 8px;
		}

		.input-wrapper {
			position: relative;
		}

		.form-input {
			width: 100%;
			padding: 14px 16px 14px 44px;
			border: 2px solid #e2e8f0;
			border-radius: 8px;
			font-size: 15px;
			font-family: inherit;
			color: var(--tl-text);
			background: var(--tl-white);
			transition: all 0.2s ease;
		}

		.form-input:focus {
			outline: none;
			border-color: var(--tl-primary);
			box-shadow: 0 0 0 3px rgba(0,168,181,0.1);
		}

		.form-input::placeholder {
			color: var(--tl-text-muted);
		}

		.input-icon {
			position: absolute;
			left: 16px;
			top: 50%;
			transform: translateY(-50%);
			color: var(--tl-text-muted);
			font-size: 16px;
			transition: color 0.2s ease;
		}

		.form-input:focus + .input-icon,
		.input-wrapper:focus-within .input-icon {
			color: var(--tl-primary);
		}

		/* Error Message */
		.error-message {
			display: flex;
			align-items: center;
			gap: 10px;
			padding: 12px 16px;
			background: #fef2f2;
			border: 1px solid #fecaca;
			border-radius: 8px;
			color: var(--tl-error);
			font-size: 13px;
			margin-bottom: 20px;
		}

		.error-message i {
			font-size: 16px;
		}

		/* Login Button */
		.login-btn {
			width: 100%;
			padding: 14px 24px;
			background: linear-gradient(135deg, var(--tl-primary) 0%, var(--tl-primary-dark) 100%);
			color: white;
			border: none;
			border-radius: 8px;
			font-size: 15px;
			font-weight: 600;
			font-family: inherit;
			cursor: pointer;
			transition: all 0.2s ease;
			box-shadow: 0 4px 6px -1px rgba(0,168,181,0.3);
			margin-top: 8px;
		}

		.login-btn:hover {
			transform: translateY(-1px);
			box-shadow: 0 6px 12px -2px rgba(0,168,181,0.4);
		}

		.login-btn:active {
			transform: translateY(0);
		}

		/* Footer */
		.login-footer {
			text-align: center;
			margin-top: 32px;
			padding-top: 24px;
			border-top: 1px solid #e2e8f0;
		}

		.footer-links {
			display: flex;
			justify-content: center;
			gap: 24px;
			margin-bottom: 16px;
		}

		.footer-links a {
			font-size: 13px;
			color: var(--tl-text-light);
			text-decoration: none;
			transition: color 0.2s ease;
		}

		.footer-links a:hover {
			color: var(--tl-primary);
		}

		.copyright {
			font-size: 12px;
			color: var(--tl-text-muted);
		}

		/* Security Badge */
		.security-badge {
			display: flex;
			align-items: center;
			justify-content: center;
			gap: 8px;
			margin-top: 20px;
			font-size: 12px;
			color: var(--tl-text-muted);
		}

		.security-badge i {
			color: var(--tl-success);
		}

		/* Responsive */
		@media (max-width: 480px) {
			.login-card {
				padding: 32px 24px;
			}

			.logo-title {
				font-size: 24px;
			}

			.footer-links {
				flex-direction: column;
				gap: 12px;
			}
		}
	</style>
	<script>
		function setFocus() {
			document.getElementById('Username').focus();
		}

		function validateForm() {
			var username = document.getElementById('Username').value.trim();
			var password = document.getElementById('Password').value.trim();
			
			if (!username) {
				showError('Please enter your username');
				document.getElementById('Username').focus();
				return false;
			}
			
			if (!password) {
				showError('Please enter your password');
				document.getElementById('Password').focus();
				return false;
			}
			
			return true;
		}

		function showError(message) {
			var errorDiv = document.getElementById('errorContainer');
			errorDiv.innerHTML = '<div class="error-message"><i class="fas fa-exclamation-circle"></i><span>' + message + '</span></div>';
		}
	</script>
</head>
<body onload="setFocus();">
	<div class="bg-pattern"></div>
	
	<div class="login-container">
		<div class="login-card">
			<!-- Logo Section -->
			<div class="logo-section">
				<div class="logo-icon">
					<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
						<defs>
							<linearGradient id="logoGrad" x1="0%" y1="0%" x2="100%" y2="100%">
								<stop offset="0%" style="stop-color:#00e0e0"/>
								<stop offset="100%" style="stop-color:#00c8c8"/>
							</linearGradient>
						</defs>
						<circle cx="50" cy="50" r="45" fill="none" stroke="#1a1f2e" stroke-width="2"/>
						<circle cx="50" cy="50" r="38" fill="none" stroke="url(#logoGrad)" stroke-width="3"/>
						<circle cx="50" cy="50" r="28" fill="none" stroke="#00c8c8" stroke-width="2" opacity="0.6"/>
						<circle cx="50" cy="50" r="18" fill="#1a1f2e" stroke="#00e0e0" stroke-width="2"/>
						<circle cx="50" cy="50" r="8" fill="#00e0e0"/>
					</svg>
				</div>
				<div class="logo-title">Techlight <span>MyDesk</span></div>
				<div class="logo-subtitle">Project Lighting Specialists</div>
			</div>

			<!-- Welcome Text -->
			<div class="welcome-text">
				<h2>Welcome Back</h2>
				<p>Sign in to access your dashboard</p>
			</div>

			<!-- Error Container -->
			<div id="errorContainer">
				<% If strMsg <> "" Then %>
				<div class="error-message">
					<i class="fas fa-exclamation-circle"></i>
					<span><%= strMsg %></span>
				</div>
				<% End If %>
			</div>

			<!-- Login Form -->
			<form action="<%= Session("WorkingDir") %>/Portal/Validate_Portal.asp" method="post" class="login-form" onsubmit="return validateForm();">
				<div class="form-group">
					<label class="form-label">Username</label>
					<div class="input-wrapper">
						<input type="text" id="Username" name="Username" class="form-input" placeholder="Enter your username" autocomplete="username">
						<i class="fas fa-user input-icon"></i>
					</div>
				</div>

				<div class="form-group">
					<label class="form-label">Password</label>
					<div class="input-wrapper">
						<input type="password" id="Password" name="Password" class="form-input" placeholder="Enter your password" autocomplete="current-password">
						<i class="fas fa-lock input-icon"></i>
					</div>
				</div>

				<button type="submit" class="login-btn">
					Sign In
				</button>
			</form>

			<!-- Footer -->
			<div class="login-footer">
				<div class="footer-links">
					<a href="#" onclick="alert('Please contact your administrator to reset your password'); return false;">Forgot Password?</a>
					<a href="https://techlight.com.au" target="_blank">Need Help?</a>
				</div>
				<div class="security-badge">
					<i class="fas fa-shield-alt"></i>
					<span>Secure Connection</span>
				</div>
				<div class="copyright">
					&copy; <%= Year(Now()) %> Techlight. All rights reserved.
				</div>
			</div>
		</div>
	</div>
</body>
</html>
