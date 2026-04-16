<%
' ===============================================================================
' Techlight MyDesk - Login Entry Point
' ===============================================================================
' STANDARD INCLUDES: Constants → ResponseHeaders → Functions (no DB needed for login)
' ===============================================================================

Option Explicit

' Layer 1: Constants (no dependencies)
%>
<!--#include virtual="/System/Constants.asp"-->
<%

' Layer 2: Response Headers (no dependencies)
Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

' Layer 3: Functions (no DB connection needed for login page)
%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<%

' Legacy session vars for backward compatibility (only set if missing)
If Session("Stylesheet") = "" Then Session("Stylesheet") = TL_STYLESHEET
If Session("HomeColor1") = "" Then Session("HomeColor1") = TL_COLOR_HOME

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

' If already logged in, redirect to Dashboard
If isLoggedIn Then
    Response.Redirect TL_WORKING_DIR & "/Dashboard.asp"
    Response.End
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
    <link rel="stylesheet" href="/System/Style_Login.css">
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
                        <input type="text" id="Username" name="Username" class="form-input" placeholder="Enter your username" autocomplete="username" required autofocus>
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
                    &copy; Techlight. All rights reserved.
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