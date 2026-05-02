# MyDesk Security Guide

**Version:** 3.1  
**Last Updated:** May 2026

---

## Overview

This document outlines the security measures implemented in MyDesk and best practices for maintaining a secure deployment.

---

## Authentication

### Login Security
- **Password Hashing**: BCrypt with work factor 12
- **Legacy Support**: Plain-text passwords still accepted for backward compatibility (migrate to hashed)
- **Session Management**: Cookie-based authentication with 8-hour sliding expiration
- **One-Time Tokens**: Blazor Server login uses 30-second one-time tokens to prevent replay attacks

### Password Requirements
- Minimum 8 characters
- Must include uppercase, lowercase, and number
- Password strength indicator in Settings page
- Change password feature available to all users

---

## Authorization

### Granular Permission System
All access control is managed through the `PermissionService`:
- 80+ granular permissions across all modules
- Permissions stored in `RolePermissions` database table
- Cached for performance, invalidated on changes
- Default permissions for standard roles (see PERMISSIONS.md)

### Role-Based Restrictions
- **Director**: Full access except cannot manage Administrators
- **Administrator**: Full access to everything
- **Accounts**: Financial modules only
- **Sales**: CRM and quotes only

### Enforcement Points
1. **Page Level**: `[Authorize]` attributes on all Razor pages
2. **Component Level**: Permission checks in `@code` blocks
3. **Service Level**: Business logic validates permissions before operations
4. **API Level**: `.RequireAuthorization()` on all endpoints

---

## Search Engine Protection

### robots.txt
Located at `/robots.txt`, blocks ALL crawlers:
```
User-agent: *
Disallow: /
```

### HTTP Headers
All responses include:
```
X-Robots-Tag: noindex, nofollow, noarchive, nosnippet, noimageindex
Cache-Control: no-store, no-cache, must-revalidate
```

### Meta Tags
All pages include meta tags blocking:
- Google, Bing, Yahoo, DuckDuckGo
- AI crawlers: GPTBot, ChatGPT-User, ClaudeBot, CCBot, PerplexityBot

---

## Data Protection

### SQL Injection Prevention
- **Parameterized Queries**: All database queries use parameters
- **No String Concatenation**: Dynamic SQL avoided
- **DatabaseService**: Central service enforces parameterized queries

### XSS Prevention
- **Blazor Auto-Encoding**: Razor automatically encodes output
- **Input Validation**: Form validation on all user inputs
- **Content Security Policy**: Can be added via middleware if needed

### CSRF Protection
- **Antiforgery Tokens**: Enabled via `app.UseAntiforgery()`
- **Cookie-Based**: ASP.NET Core handles token validation

---

## Logging & Audit

### Activity Logging
- All user actions logged to `Activity` table
- Includes: user code, entity type, entity ID, action, timestamp
- Viewable in Admin > Log Viewer

### AI Audit
- All AI interactions logged to `AiAudit` table
- Compliance audit trail for Ask AI feature
- Viewable in Admin > AI Audit Log

### Error Logging
- **Serilog**: Structured logging to `/Logs/` directory
- **Daily Rotation**: New log file each day
- **Retention**: 30 days for app logs, 60 days for error logs
- **Enriched**: Includes remote IP, user, user agent

---

## Hardening Checklist

### Development
- [ ] Use parameterized queries (enforced by DatabaseService)
- [ ] Check permissions before allowing actions
- [ ] Validate all user inputs
- [ ] Never log sensitive data (passwords, tokens)
- [ ] Use BCrypt for password hashing

### Deployment
- [ ] Use HTTPS in production (configure SSL in IIS)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure strong connection strings (use Windows Auth or encrypted passwords)
- [ ] Restrict IIS application pool identity
- [ ] Enable IIS request filtering
- [ ] Configure CORS if API accessed from other domains
- [ ] Regular database backups
- [ ] Monitor logs for suspicious activity

### Production
- [ ] Enable Windows Firewall rules
- [ ] Use network segmentation
- [ ] Regular security updates
- [ ] Penetration testing annually
- [ ] Review user permissions quarterly
- [ ] Disable unused modules in navmenu.json
- [ ] Monitor AI usage for abuse

---

## Known Security Considerations

### Legacy Plain-Text Passwords
- **Issue**: Old users may still have plain-text passwords
- **Mitigation**: BCrypt verification falls back to plain-text comparison
- **Action**: Migrate all users to hashed passwords by forcing password reset

### Director Restriction
- **Issue**: Directors cannot manage Administrators
- **Enforcement**: Multiple layers (UI, service, database)
- **Bypass**: Only Administrators can manage other Administrators

### AI Assistant
- **Issue**: AI can access business data via natural language
- **Mitigation**: All AI interactions logged and auditable
- **Action**: Review AI audit log regularly, limit AI access to sensitive roles

---

## Incident Response

### If a Security Breach is Detected
1. **Isolate**: Take affected systems offline
2. **Assess**: Review logs to determine scope
3. **Contain**: Change passwords, revoke sessions
4. **Recover**: Restore from backup if needed
5. **Report**: Document incident and notify affected users

### Log Locations
- Application logs: `/Logs/app-YYYYMMDD.log`
- Error logs: `/Logs/errors-YYYYMMDD.log`
- Activity log: `Activity` database table
- AI audit log: `AiAudit` database table

---

## Compliance

### Australian Privacy Act
- Data stored in Australia (typically)
- Privacy policy available at `/privacy-policy`
- Terms & conditions at `/terms-and-conditions`

### GDPR Considerations
- Right to access: User data can be exported
- Right to deletion: Soft delete used (can be hardened to hard delete)
- Data retention: Configurable via Parameters table

---

## Future Security Enhancements

Planned for future releases:
- [ ] Two-factor authentication (2FA)
- [ ] Single Sign-On (SSO) via Azure AD
- [ ] Role-based API rate limiting
- [ ] IP whitelisting for admin access
- [ ] Session timeout warnings
- [ ] Password complexity policies
- [ ] Account lockout after failed attempts
- [ ] Security headers (CSP, HSTS, X-Frame-Options)

---

**Powered by Digital Response**  
**© 2026 Digital Response. All rights reserved.**
