<%
' ===============================================================================
' Techlight MyDesk - Unified Entry Point
' ===============================================================================
' Simplified workflow:
' 1. This is the ONLY entry point for the application
' 2. Checks if user is already logged in (Session + Cookies)
' 3. If logged in: Redirects to main application
' 4. If not logged in: Shows unified login page
' ===============================================================================

On Error Resume Next

' Cache control headers
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

' Include required system files - Var.asp MUST be first to set WorkingDir
%>
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<%

' Initialize session variables for Techlight (always the same)
' Var.asp already sets WorkingDir cookie, now set session if needed
If Session("WorkingDir") = "" Then
    Session("Stylesheet") = "Style.css"
    Session("MainBgColor") = "#cccccc"
    Session("BgColor2") = "#005b89"
    Session("BgColor3") = "#005b89"
    Session("Prefix") = "TL"
    Session("State") = "ALL"
    Session("PortalCompany") = "Techlight"
    Session("WorkingDir") = "/Clients/SalesEngineTL"
    Session("HomeColor1") = "#005b89"
End If

' HARDEN: Always ensure WorkingDir is set - never allow null or empty
If Session("WorkingDir") = "" Or IsNull(Session("WorkingDir")) Then
    Session("WorkingDir") = "/Clients/SalesEngineTL"
End If

' Check if user is already logged in
Dim isLoggedIn
isLoggedIn = False

' Check Session first with error handling
On Error Resume Next
Dim sessionLoggedIn
sessionLoggedIn = Session("LoggedIn")
If Err.Number = 0 And Not IsNull(sessionLoggedIn) Then
    If CBool(sessionLoggedIn) = True Then
        isLoggedIn = True
    End If
End If
Err.Clear
On Error GoTo 0

' Also check Cookies as backup with error handling
If Not isLoggedIn Then
    On Error Resume Next
    Dim cookieLoggedIn
    cookieLoggedIn = Request.Cookies("LoggedIn")
    If Err.Number = 0 And Not IsNull(cookieLoggedIn) Then
        If CStr(cookieLoggedIn) = "True" Then
            ' Restore session from cookies if needed with error handling
            On Error Resume Next
            Dim currentSessionLoggedIn
            currentSessionLoggedIn = Session("LoggedIn")
            If Err.Number <> 0 Or currentSessionLoggedIn = "" Or CBool(currentSessionLoggedIn) = False Then
                Session("LoggedIn") = True
                On Error Resume Next
                Session("Code") = Request.Cookies("UserSettings")("Code")
                Session("Name") = Request.Cookies("UserSettings")("Name")
                Session("Email") = Request.Cookies("UserSettings")("Email")
                Session("Initials") = Request.Cookies("UserSettings")("Initials")
                Session("DivisionId") = Request.Cookies("UserSettings")("DivisionId")
                Session("Division") = Request.Cookies("UserSettings")("Division")
                Session("UserTypeId") = Request.Cookies("UserSettings")("UserTypeId")
                Session("LocationId") = Request.Cookies("UserSettings")("LocationId")
                Session("ExpenseTypeGroupId") = Request.Cookies("UserSettings")("ExpenseTypeGroupId")
                Err.Clear
            End If
            Err.Clear
            isLoggedIn = True
        End If
    End If
    Err.Clear
    On Error GoTo 0
End If

' If already logged in, redirect to main application
If isLoggedIn Then
    ' Ensure WorkingDir is set before redirect
    Dim redirectPath
    redirectPath = ""
    On Error Resume Next
    If Session("WorkingDir") <> "" And Not IsNull(Session("WorkingDir")) Then
        redirectPath = Session("WorkingDir") & "/Dashboard.asp"
    Else
        redirectPath = "/Clients/SalesEngineTL/Dashboard.asp"
    End If
    On Error GoTo 0
    
    If redirectPath <> "" Then
        Response.Redirect redirectPath
        Response.End
    End If
End If

' Not logged in - show the unified login page
Dim strMsg
strMsg = Trim(Request("Msg"))
%>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Techlight MyDesk - Sign In</title>
    <link rel="shortcut icon" href="/favicon.ico">
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
            50% { transform: translate(-30px, -30px) scale(1.1); }
        }

        .login-wrapper {
            position: relative;
            z-index: 1;
            width: 100%;
            max-width: 440px;
            padding: 20px;
        }

        .login-container {
            background: var(--tl-white);
            border-radius: var(--tl-radius);
            box-shadow: var(--tl-shadow-lg);
            padding: 40px;
            position: relative;
            overflow: hidden;
        }

        .login-container::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 4px;
            background: linear-gradient(90deg, var(--tl-primary) 0%, var(--tl-primary-light) 100%);
        }

        .logo-section {
            text-align: center;
            margin-bottom: 32px;
        }

        .logo-icon {
            width: 64px;
            height: 64px;
            background: linear-gradient(135deg, var(--tl-primary) 0%, var(--tl-primary-dark) 100%);
            border-radius: 16px;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            margin-bottom: 16px;
            box-shadow: var(--tl-shadow);
        }

        .logo-icon svg {
            width: 36px;
            height: 36px;
            color: white;
        }

        .logo-title {
            font-size: 1.75rem;
            font-weight: 700;
            color: var(--tl-dark);
            letter-spacing: -0.5px;
        }

        .logo-title span {
            color: var(--tl-primary);
        }

        .logo-subtitle {
            font-size: 0.875rem;
            color: var(--tl-text-muted);
            margin-top: 4px;
        }

        .welcome-text {
            text-align: center;
            margin-bottom: 28px;
        }

        .welcome-text h2 {
            font-size: 1.25rem;
            font-weight: 600;
            color: var(--tl-dark);
            margin-bottom: 4px;
        }

        .welcome-text p {
            font-size: 0.9375rem;
            color: var(--tl-text-light);
        }

        .error-message {
            background: #fff5f5;
            border-left: 4px solid var(--tl-error);
            color: var(--tl-error);
            padding: 12px 16px;
            border-radius: 8px;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            gap: 10px;
            font-size: 0.875rem;
        }

        .login-form .form-group {
            margin-bottom: 20px;
        }

        .form-label {
            display: block;
            font-size: 0.8125rem;
            font-weight: 600;
            color: var(--tl-dark);
            margin-bottom: 6px;
            text-transform: uppercase;
            letter-spacing: 0.3px;
        }

        .input-wrapper {
            position: relative;
        }

        .form-input {
            width: 100%;
            padding: 12px 16px 12px 44px;
            border: 2px solid #e2e8f0;
            border-radius: 8px;
            font-size: 0.9375rem;
            font-family: inherit;
            color: var(--tl-text);
            background: var(--tl-white);
            transition: all 0.2s ease;
        }

        .form-input::placeholder {
            color: var(--tl-text-muted);
        }

        .form-input:focus {
            outline: none;
            border-color: var(--tl-primary);
            box-shadow: 0 0 0 3px rgba(0, 168, 181, 0.1);
        }

        .input-icon {
            position: absolute;
            left: 14px;
            top: 50%;
            transform: translateY(-50%);
            color: var(--tl-text-muted);
            font-size: 1.125rem;
        }

        .login-btn {
            width: 100%;
            padding: 14px 24px;
            background: linear-gradient(135deg, var(--tl-primary) 0%, var(--tl-primary-dark) 100%);
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 1rem;
            font-weight: 600;
            font-family: inherit;
            cursor: pointer;
            transition: all 0.2s ease;
            box-shadow: var(--tl-shadow);
            margin-top: 8px;
        }

        .login-btn:hover {
            transform: translateY(-1px);
            box-shadow: var(--tl-shadow-lg);
        }

        .login-btn:active {
            transform: translateY(0);
        }

        .login-footer {
            margin-top: 28px;
            text-align: center;
        }

        .footer-links {
            display: flex;
            justify-content: center;
            gap: 20px;
            margin-bottom: 16px;
        }

        .footer-links a {
            color: var(--tl-text-light);
            text-decoration: none;
            font-size: 0.875rem;
            font-weight: 500;
            transition: color 0.2s ease;
        }

        .footer-links a:hover {
            color: var(--tl-primary);
        }

        .security-badge {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            color: var(--tl-success);
            font-size: 0.8125rem;
            font-weight: 500;
            margin-bottom: 12px;
        }

        .copyright {
            font-size: 0.75rem;
            color: var(--tl-text-muted);
        }

        @media (max-width: 480px) {
            .login-wrapper {
                padding: 16px;
            }
            
            .login-container {
                padding: 32px 24px;
            }
            
            .logo-title {
                font-size: 1.5rem;
            }
        }

        /* Modals */
        .tl-modal-overlay {
            display: none;
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.5);
            backdrop-filter: blur(4px);
            z-index: 1000;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .tl-modal {
            background: var(--tl-white);
            border-radius: var(--tl-radius);
            padding: 32px;
            width: 100%;
            max-width: 400px;
            box-shadow: var(--tl-shadow-lg);
            position: relative;
            animation: modalFadeIn 0.3s ease;
        }
        @keyframes modalFadeIn {
            from { opacity: 0; transform: translateY(20px); }
            to { opacity: 1; transform: translateY(0); }
        }
        .tl-modal h3 {
            font-size: 1.25rem;
            color: var(--tl-dark);
            margin-bottom: 12px;
            font-weight: 600;
        }
        .tl-modal p {
            font-size: 0.9375rem;
            color: var(--tl-text-light);
            margin-bottom: 24px;
            line-height: 1.5;
        }
        .tl-close-btn {
            position: absolute;
            top: 16px; right: 16px;
            background: none; border: none;
            font-size: 1.25rem; color: var(--tl-text-muted);
            cursor: pointer;
            transition: color 0.2s ease;
        }
        .tl-close-btn:hover { color: var(--tl-dark); }
        
        .support-info {
            background: var(--tl-bg);
            border-radius: 8px;
            padding: 16px;
            margin-bottom: 24px;
            border: 1px solid #e2e8f0;
        }
        .support-row {
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 12px;
            font-size: 0.9375rem;
        }
        .support-row:last-child { margin-bottom: 0; }
        .support-row i { color: var(--tl-primary); width: 20px; text-align: center; }
        .support-row a { color: var(--tl-dark); text-decoration: none; font-weight: 500; }
        .support-row a:hover { color: var(--tl-primary); }
    </style>
</head>
<body>
    <div class="bg-pattern"></div>
    
    <div class="login-wrapper">
        <div class="login-container">
            <div class="logo-section">
                <div class="logo-icon">
                    <svg viewBox="0 0 80 80" xmlns="http://www.w3.org/2000/svg" style="width: 48px; height: 48px;">
                      <defs>
                        <filter id="tealGlow" x="-80%" y="-80%" width="260%" height="260%">
                          <feGaussianBlur stdDeviation="4" result="coloredBlur"/>
                          <feMerge>
                            <feMergeNode in="coloredBlur"/>
                            <feMergeNode in="coloredBlur"/>
                            <feMergeNode in="SourceGraphic"/>
                          </feMerge>
                        </filter>
                        <radialGradient id="innerDot" cx="50%" cy="50%" r="50%">
                          <stop offset="0%" stop-color="#ffffff"/>
                          <stop offset="60%" stop-color="#c0f0f0"/>
                          <stop offset="100%" stop-color="#40d8d8"/>
                        </radialGradient>
                      </defs>
                      <g transform="translate(40,40)" filter="url(#tealGlow)">
                        <circle cx="0" cy="0" r="30" fill="none" stroke="#ffffff" stroke-width="1.5" opacity="0.3"/>
                        <circle cx="0" cy="0" r="33" fill="none" stroke="#ffffff" stroke-width="0.8" opacity="0.2"/>
                        <circle cx="0" cy="0" r="26" fill="none" stroke="#ffffff" stroke-width="2.5" opacity="0.9"/>
                        <circle cx="0" cy="0" r="20.5" fill="none" stroke="#ffffff" stroke-width="2" opacity="0.8"/>
                        <circle cx="0" cy="0" r="16.5" fill="#048894" opacity="0.95"/>
                        <circle cx="0" cy="0" r="16" fill="none" stroke="#ffffff" stroke-width="2" opacity="0.9"/>
                        <circle cx="0" cy="0" r="12" fill="#036a73"/>
                        <circle cx="0" cy="0" r="11.5" fill="none" stroke="#e0ffff" stroke-width="1.5" opacity="0.8"/>
                        <circle cx="0" cy="0" r="8" fill="#024147"/>
                        <circle cx="0" cy="0" r="5.5" fill="url(#innerDot)" opacity="1"/>
                      </g>
                    </svg>
                </div>
                <div class="logo-title">Techlight <span>MyDesk</span></div>
                <div class="logo-subtitle">Project Lighting Specialists</div>
            </div>

            <div class="welcome-text">
                <h2>Welcome Back</h2>
                <p>Sign in to access your dashboard</p>
            </div>

            <% If strMsg <> "" Then %>
            <div class="error-message">
                <i class="fas fa-exclamation-circle"></i>
                <span><%= strMsg %></span>
            </div>
            <% End If %>

            <form action="/Clients/SalesEngineTL/Portal/Validate.asp" method="post" class="login-form" id="loginForm">
                <div class="form-group">
                    <label class="form-label">Username</label>
                    <div class="input-wrapper">
                        <input type="text" id="Username" name="Username" class="form-input" placeholder="Enter your username" autocomplete="username" required>
                        <i class="fas fa-user input-icon"></i>
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">Password</label>
                    <div class="input-wrapper">
                        <input type="password" id="Password" name="Password" class="form-input" placeholder="Enter your password" autocomplete="current-password" required>
                        <i class="fas fa-lock input-icon"></i>
                    </div>
                </div>

                <div class="form-group" style="margin-bottom: 24px;">
                    <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; font-size: 0.875rem; color: var(--tl-text);">
                        <input type="checkbox" id="RememberMe" name="RememberMe" style="width: 16px; height: 16px; cursor: pointer;">
                        <span>Remember me on this device</span>
                    </label>
                </div>

                <button type="submit" class="login-btn">
                    Sign In
                </button>
            </form>

            <div class="login-footer">
                <div class="footer-links" style="display: flex; justify-content: center; gap: 20px; margin-bottom: 16px;">
                    <a href="javascript:void(0)" onclick="document.getElementById('forgotModal').style.display='flex'" style="color: var(--tl-text-light); text-decoration: none; font-size: 0.875rem; font-weight: 500;">Forgot Password?</a>
                    <a href="javascript:void(0)" onclick="document.getElementById('supportModal').style.display='flex'" style="color: var(--tl-text-light); text-decoration: none; font-size: 0.875rem; font-weight: 500;">Support</a>
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

    <!-- Forgot Password Modal -->
    <div id="forgotModal" class="tl-modal-overlay">
        <div class="tl-modal">
            <button class="tl-close-btn" onclick="document.getElementById('forgotModal').style.display='none'"><i class="fas fa-times"></i></button>
            <h3>Reset Password</h3>
            <p>Enter your email address and we'll send you a secure link to log you in automatically.</p>
            <form action="/ForgotPassword_Proc.asp" method="post" id="forgotForm">
                <div class="form-group">
                    <label class="form-label">Email Address</label>
                    <div class="input-wrapper">
                        <input type="email" name="ResetEmail" class="form-input" placeholder="Enter your email" required>
                        <i class="fas fa-envelope input-icon"></i>
                    </div>
                </div>
                <button type="submit" class="login-btn" style="margin-top:20px;">Send Secure Link</button>
            </form>
        </div>
    </div>

    <!-- Support Modal -->
    <div id="supportModal" class="tl-modal-overlay">
        <div class="tl-modal">
            <button class="tl-close-btn" onclick="document.getElementById('supportModal').style.display='none'"><i class="fas fa-times"></i></button>
            <h3>Technical Support</h3>
            <p>If you're having trouble accessing Techlight MyDesk, please contact Digital Response for assistance.</p>
            <div class="support-info">
                <div class="support-row">
                    <i class="fas fa-mobile-alt"></i>
                    <a href="tel:0452491013">0452 491 013</a>
                </div>
                <div class="support-row">
                    <i class="fas fa-envelope"></i>
                    <a href="mailto:info@digitalresponse.com.au">info@digitalresponse.com.au</a>
                </div>
            </div>
        </div>
    </div>

    <script>
        // Save credentials to cookies if Remember Me is checked
        document.getElementById('loginForm').addEventListener('submit', function(e) {
            var username = document.getElementById('Username').value.trim();
            var password = document.getElementById('Password').value.trim();
            var rememberMe = document.getElementById('RememberMe').checked;
            
            if (!username || !password) {
                e.preventDefault();
                alert('Please enter both username and password');
                return false;
            }
            
            if (rememberMe) {
                // Save credentials to cookies for 30 days
                var expiryDate = new Date();
                expiryDate.setDate(expiryDate.getDate() + 30);
                document.cookie = 'RememberMeUsername=' + encodeURIComponent(username) + '; expires=' + expiryDate.toUTCString() + '; path=/; SameSite=Strict';
                document.cookie = 'RememberMePassword=' + encodeURIComponent(password) + '; expires=' + expiryDate.toUTCString() + '; path=/; SameSite=Strict';
            } else {
                // Clear credentials if checkbox not checked
                document.cookie = 'RememberMeUsername=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; SameSite=Strict';
                document.cookie = 'RememberMePassword=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; SameSite=Strict';
            }
            
            return true;
        });
        
        // Load credentials from cookies on page load
        document.addEventListener('DOMContentLoaded', function() {
            function getCookie(name) {
                var value = '; ' + document.cookie;
                var parts = value.split('; ' + name + '=');
                if (parts.length == 2) return decodeURIComponent(parts.pop().split(';').shift());
                return null;
            }
            
            var savedUsername = getCookie('RememberMeUsername');
            var savedPassword = getCookie('RememberMePassword');
            
            if (savedUsername && savedPassword) {
                document.getElementById('Username').value = savedUsername;
                document.getElementById('Password').value = savedPassword;
                document.getElementById('RememberMe').checked = true;
            }
            
            // Close modals when clicking outside
            window.onclick = function(event) {
                var forgotModal = document.getElementById('forgotModal');
                var supportModal = document.getElementById('supportModal');
                if (event.target == forgotModal) forgotModal.style.display = "none";
                if (event.target == supportModal) supportModal.style.display = "none";
            }
        });
    </script>
</body>
</html>