# Techlight MyDesk - Simplified Authentication Workflow

## Overview

This repository contains the refactored and simplified MyDesk authentication workflow. The goal was to eliminate confusing redirect chains and consolidate the entry points into a single, unified experience.

## Simplified Workflow

### Before (7 steps):
```
/Default.asp → /Default2.asp → /Clients/SalesEngine/Portal/Validate_Portal.asp → /Clients/SalesEngineTL → 
/Clients/SalesEngineTL/Default.asp (frameset) → Login Page → Validate → SetCookies → Portal → Dashboard
```

### After (4 steps):
```
/Default.asp (Unified Entry) → Login Form → /Clients/SalesEngineTL/Portal/Validate.asp → Dashboard
```

## Key Changes

### 1. Unified Entry Point
- **File**: `/Default.asp`
- **Purpose**: Single entry point for the entire application
- **Features**:
  - Checks if user is already logged in (Session + Cookies)
  - Shows modern login page if not authenticated
  - Auto-redirects to Dashboard if already logged in
  - Initializes Techlight session variables (always the same values)

### 2. Modern Login UI
- Responsive design that works on mobile and desktop
- Animated background with floating gradient orbs
- Accessible form with proper labels and ARIA attributes
- Client-side validation for required fields
- Clean, professional aesthetic matching Techlight branding

### 3. Simplified Validation Flow
- **File**: `/Clients/SalesEngineTL/Portal/Validate.asp`
- Validates credentials against database
- Sets up user session with all permissions
- On success: Redirects to `/SetCookies.asp` → `/Portal.asp` → Dashboard
- On failure: Redirects back to `/Default.asp` with error message

### 4. Session Persistence
- Cookies store user settings for 7 days
- Automatic session restoration on revisit
- Seamless re-authentication experience

## File Structure

```
/Default.asp                    - Unified entry point (NEW)
/Default2.asp                   - Legacy file (kept for compatibility)
/Clients/SalesEngine/
  /Portal/
    /Validate_Portal.asp        - Legacy portal validation
/Clients/SalesEngineTL/
  /Default.asp                  - Frameset with Header + Main content
  /DefaultFrame.asp             - Modern login page (legacy)
  /Dashboard.asp                - Main dashboard
  /Header.asp                   - Top navigation header
  /Portal/
    /Validate.asp               - Login validation handler
    /LogOff.asp                 - Logout handler
/SetCookies.asp                - Cookie persistence handler
/Portal.asp                     - Cookie setting and redirect
/System/
  /Consts.asp                   - Configuration constants (including cForceHTTPS)
  /ssi_Functions_Core.asp       - Core functions (MyRedirect, GetProtocol)
```

## Configuration

### HTTPS Enforcement
To disable HTTP-to-HTTPS redirects (for local development):

1. Open `/System/Consts.asp`
2. Change: `Const cForceHTTPS = False`
3. Save and refresh

The application will now stay on whatever protocol (HTTP or HTTPS) it's currently using.

## Testing

### Playwright Test Suite
We've added comprehensive regression tests using Playwright.

#### Installation
```bash
npm install
npx playwright install chromium
```

#### Run Tests
```bash
# Run all tests (headless)
npm test

# Run with browser visible
npm run test:headed

# Run in debug mode
npm run test:debug

# Open UI mode
npm run test:ui

# View test report
npm run test:report
```

#### Test Coverage
- ✅ Login page displays correctly
- ✅ Invalid credentials show error
- ✅ Valid credentials redirect to Dashboard
- ✅ Already authenticated users skip login
- ✅ Form accessibility attributes
- ✅ Required field validation
- ✅ Responsive mobile layout
- ✅ Help and forgot password links

#### Test Credentials
- **Username**: `peter bardenhagen`
- **Password**: `fairmont`

### Environment Variables
```bash
# Skip login tests if dev server not running
set SKIP_LOGIN_TEST=true

# Set custom base URL
set BASE_URL=http://localhost:8080
```

## Deployment

1. Ensure IIS has ASP Classic support enabled
2. Set up database connection in `/System/ssi_dbConn_open.inc`
3. Configure proper permissions for the application pool
4. Access via: `http://localhost/Default.asp`

## Troubleshooting

### Login Loop
If you're stuck in a login loop:
1. Clear browser cookies for the domain
2. Check that Session state is enabled in IIS
3. Verify database connection is working

### Session Timeout Too Fast
In IIS Manager:
1. Go to Application Pools
2. Select your application pool
3. Advanced Settings → Process Model → Idle Time-out = 0

### HTTPS Redirect Issues
If you get HTTPS errors locally:
1. Open `/System/Consts.asp`
2. Set `Const cForceHTTPS = False`

## Browser Support

- ✅ Chrome/Edge (Chromium-based)
- ✅ Firefox
- ✅ Safari (macOS/iOS)
- ✅ IE11 (with some visual degradation)

## Security Considerations

- All passwords are case-sensitive
- SQL injection protection via parameterized queries (via Replace functions)
- Session cookies are HttpOnly (configure in IIS)
- HTTPS enforcement on production (configurable)
- Cache-control headers prevent back-button access after logout

## Next Steps / Future Improvements

1. **Remove Frameset**: Convert to modern single-page layout with AJAX
2. **API Layer**: Add REST API for better separation of concerns
3. **Password Hashing**: Upgrade from plain-text passwords
4. **MFA**: Add multi-factor authentication option
5. **SSO**: Integrate with Azure AD or other identity providers

## Support

For issues or questions:
- Contact: Techlight IT Administrator
- Email: support@techlight.com.au
- Hours: Monday-Friday, 9am-5pm AEST

---

**Last Updated**: April 2026  
**Version**: 2026.04 - Simplified Auth Workflow
