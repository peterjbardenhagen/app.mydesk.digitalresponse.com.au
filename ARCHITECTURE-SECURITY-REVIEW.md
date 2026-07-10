# MyDesk Architecture & Security Review
## Compliance-First SaaS Platform for Australian Enterprises

**Audit Date**: July 2026  
**Compliance Target**: ISO 27001, SOC 2 Type II, Sarbanes-Oxley, Australian Privacy Act  
**Target Market**: Australian SME/SMB (Data Sovereignty Critical)  
**SaaS Model**: Multi-tenant with domain-based tenant routing

---

## Executive Summary

MyDesk is designed as a **Tier-1 Enterprise SaaS Platform** requiring audit-grade security controls. Current implementation shows strong foundational patterns but requires hardening in several critical areas:

### Current Strengths ✅
- Multi-tenant database architecture with tenant isolation via claims
- Parameterized SQL queries (SQL injection protection)
- Bearer token authentication with 1-year PAT expiration
- Soft-delete patterns for audit trails
- Health check endpoints for monitoring

### Critical Gaps 🔴
1. **Multi-Tenancy Routing** - Missing domain-based tenant resolution (email domain → tenant mapping)
2. **Approval Permissions** - No fine-grained approval authority assignment system
3. **Audit Logging** - Incomplete compliance audit trail (no who/what/when/where/why on API calls)
4. **Data Encryption** - No encryption at rest specified for sensitive fields
5. **Product Admin Module** - Non-existent (required for client management & billing)
6. **Rate Limiting & DDoS** - No API rate limiting or abuse detection
7. **Compliance Logging** - Missing centralized compliance event logging
8. **Mobile Security** - Token management and cache encryption gaps

### High-Priority Fixes
1. Implement domain-based multi-tenancy routing
2. Build approval permissions system with role-based access
3. Add comprehensive audit logging to all data modifications
4. Implement field-level encryption for sensitive data (PII, financial)
5. Create Product Admin module with client lifecycle management
6. Add API rate limiting and abuse prevention
7. Establish compliance event logging infrastructure

---

## PART 1: CURRENT ARCHITECTURE ANALYSIS

### 1.1 Multi-Tenancy Implementation

**Current State:**
```csharp
// Current approach: Tenant ID from claims
var tenantId = ctx.User.FindFirst("tenant_id")?.Value;
```

**Issues:**
- Relies entirely on JWT claim (no validation of domain ownership)
- No mechanism to route users to correct tenant based on email domain
- User could theoretically claim any tenant_id in JWT
- No prevention of cross-tenant user creation

**Vulnerability Scenario:**
```
Attacker flow:
1. Login as attacker@domain.com
2. JWT includes fake tenant_id claim
3. System trusts the claim without verification
4. Attacker gains access to wrong tenant's data
```

**Risk Level**: 🔴 CRITICAL - Violates core multi-tenancy principle

---

### 1.2 Authentication & Tenant Resolution

**Current Flow:**
```
User Email → PAT Token → JWT Claims (tenant_id) → Data Access
```

**Missing Link:**
- No email domain → tenant_id mapping
- No validation that user@example.com belongs to example.com tenant
- No way to prevent a user from changing tenants

**Correct Flow Should Be:**
```
User Email Domain → Lookup Tenant in Directory
    ↓
Validate User in Tenant
    ↓
Issue JWT with verified tenant_id
    ↓
All API calls validate tenant_id against stored user's tenant
```

---

### 1.3 Approval Workflow Security

**Current Implementation Issues:**
1. No approval authority assignment system
   - No way to designate who can approve what
   - No role-based approval routing
   - System assumes anyone with correct level can approve

2. Missing delegation validation
   - DelegationManager allows delegating to any user ID
   - No verification that delegate is in same tenant
   - No validation that delegate can actually approve

3. No approval workflow audit trail completeness
   - Missing: original request initiator details
   - Missing: approval authority basis (role? direct assignment?)
   - Missing: why user was selected as approver

**Vulnerability Scenario:**
```
Attack: Unauthorized Approval
1. Attacker knows another user's ID
2. Calls delegate endpoint with that user ID
3. Temporarily receives approval authority
4. Approves expense for themselves
5. Delegates back - no trail of anomaly
```

---

### 1.4 API Security Gaps

#### Missing Rate Limiting
- No protection against approval spam attacks
- No brute force protection on token validation
- Could DOS approval queue by submitting 1000s of expenses

#### Missing Input Validation
- Comment fields accept unlimited length strings
- No validation of date ranges (start=2050, end=2025?)
- No check that delegate_user_id is valid user

#### Missing CORS & CSRF Protection
- Not visible in current implementation
- Critical for browser-based SPA attacks

#### Insufficient Error Messages
- May leak internal information about user/approval existence
- Could allow user enumeration attacks

---

### 1.5 Data Security Gaps

#### No Encryption at Rest Specified
- Sensitive data (expense amounts, employee names, approver identity) stored in plaintext
- No field-level encryption for PII
- Database backup also unencrypted

#### No Encryption in Transit
- Assumes TLS/HTTPS (good) but no verification
- No certificate pinning for mobile app
- No API request/response encryption layer

#### Cache Security Issues (Mobile)
- localStorage caching with 1-hour TTL
- No encryption of cached data
- PAT token stored in localStorage (vulnerable to XSS)
- Cache cleared on logout but not on device rotation

---

### 1.6 Audit & Compliance Logging

**Current State**: Partial
- ✅ ApprovalActions table tracks approvals
- ✅ ActivityService logs some activities
- ❌ Missing API-level audit trail
- ❌ No "who accessed what when" logging
- ❌ No compliance event classification
- ❌ No immutable audit log (standard DB allows modification)

**Required for Compliance:**
```
Every data modification must log:
- WHO (UserId, Domain)
- WHAT (Expense ID, Field Changed, Old Value, New Value)
- WHEN (Timestamp with millisecond precision)
- WHERE (IP Address, Geographic Location)
- WHY (Request Type, Approval Authority, Business Reason)
- HOW (API Endpoint, Method, User Agent)
```

**Missing**: WHERE & WHY - Critical for investigations

---

### 1.7 Mobile App Security Issues

#### Token Management
```javascript
// Current: Store PAT in localStorage
localStorage.setItem('pat', token);

// Risk: Vulnerable to XSS attacks
// Solution: Use secure storage (iOS Keychain, Android Keystore)
```

#### Cache Vulnerability
```javascript
// Current: Store API responses in localStorage
state.expenses = responseData;

// Risk: Unencrypted sensitive data (amounts, approver names, etc.)
// Risk: Data persists after logout in some scenarios
```

#### No Certificate Pinning
- Server certificate could be MITM'd on untrusted network
- Critical for field consultants on 4G/5G

#### Offline Data Handling
- Sensitive approval data cached offline
- No encryption
- No watermark (user should know they're working offline)

---

## PART 2: NEW REQUIREMENTS ARCHITECTURE

### 2.1 Domain-Based Multi-Tenancy

**Required New System:**
```
User Registration Flow:
1. User enters email: john@cartercapner.com.au
2. System extracts domain: cartercapner.com.au
3. Looks up in TenantDomains table
4. Resolves to Tenant: Carter Capner Law (ID: 5)
5. Creates user in that tenant only
6. JWT issued with verified tenant_id=5
```

**New Database Tables:**
```sql
-- Domain → Tenant mapping (with verification)
TenantDomains (
    DomainId INT PRIMARY KEY,
    TenantId GUID,
    Domain NVARCHAR(255) UNIQUE,
    IsVerified BIT,
    VerifiedAt DATETIME2,
    VerificationType ENUM ('DNS', 'Email', 'Manual'),
    VerificationCode NVARCHAR(100)
)

-- Audit trail for domain verification
DomainVerificationAudit (
    AuditId INT PRIMARY KEY,
    DomainId INT,
    Action NVARCHAR(50),
    VerifiedBy INT,
    VerifiedAt DATETIME2,
    IPAddress NVARCHAR(50)
)
```

**API Endpoints:**
```
POST   /auth/register                   - Register with domain validation
POST   /auth/verify-domain              - Complete domain verification
POST   /api/tenant-admin/domains        - Manage domains (tenant admin)
GET    /api/tenant-admin/domains        - List verified domains
DELETE /api/tenant-admin/domains/{id}   - Remove domain (deprovisioning)
```

---

### 2.2 Approval Permissions System

**Current Problem:**
- Approval routing hardcoded: manager approves expense
- No way to assign approval authority to specific users/roles
- No support for hierarchical approval chains

**Required Solution:**

```sql
-- Who can approve what, in what circumstances
ApprovalPermissions (
    PermissionId INT PRIMARY KEY,
    TenantId GUID,
    RoleId INT,                    -- OR specific user below
    UserId INT,                    -- Specific user override
    ApprovalType ENUM ('Expense', 'Timesheet', 'All'),
    ApprovalLevel INT,             -- 1, 2, 3... (workflow level)
    ThresholdMin DECIMAL(18,2),    -- Can approve amounts >= this
    ThresholdMax DECIMAL(18,2),    -- Can approve amounts <= this
    IsActive BIT,
    CreatedAt DATETIME2,
    CreatedBy INT,
    RevokedAt DATETIME2,           -- Soft delete
    RevokedBy INT
)

-- Approval authority audit trail
ApprovalPermissionAudit (
    AuditId INT PRIMARY KEY,
    PermissionId INT,
    Action NVARCHAR(50),           -- 'Created', 'Modified', 'Revoked'
    ChangedBy INT,
    ChangedAt DATETIME2,
    IPAddress NVARCHAR(50),
    Reason NVARCHAR(500),          -- Why permission changed
    OldValue NVARCHAR(MAX),        -- JSON of what changed
    NewValue NVARCHAR(MAX)
)
```

**Approval Logic Updated:**
```csharp
// OLD: Check if user is manager
var canApprove = currentUser.IsManager;

// NEW: Check assigned permission + authority scope
var permission = await db.QueryAsync(
    @"SELECT * FROM ApprovalPermissions
      WHERE TenantId = @TenantId 
      AND ((UserId = @UserId) OR (RoleId = @UserRoleId))
      AND ApprovalType = @Type
      AND ApprovalLevel = @Level
      AND ThresholdMin <= @Amount AND ThresholdMax >= @Amount
      AND IsActive = 1
      AND RevokedAt IS NULL");

var canApprove = permission.Count > 0;
```

**API Endpoints:**
```
GET    /api/approval-permissions          - List permissions for current user
GET    /api/approval-permissions/audit    - Audit trail of changes
POST   /api/admin/approval-permissions    - Assign permission
PUT    /api/admin/approval-permissions/{id} - Modify permission
DELETE /api/admin/approval-permissions/{id} - Revoke permission
GET    /api/admin/approval-permissions    - List all (admin only)
```

---

### 2.3 Audit Logging Infrastructure

**Required Comprehensive Audit Trail:**

```sql
-- Immutable audit log (append-only, no updates/deletes)
ComplianceAuditLog (
    AuditId BIGINT PRIMARY KEY IDENTITY(1,1),
    TenantId GUID NOT NULL,
    EventType NVARCHAR(100),               -- 'DataModified', 'Approved', 'UserCreated', etc
    EventCategory NVARCHAR(50),            -- 'Authentication', 'Authorization', 'DataChange', 'Approval', 'Admin'
    Severity NVARCHAR(20),                 -- 'Info', 'Warning', 'Critical'
    
    -- WHO
    UserId INT,
    UserEmail NVARCHAR(255),
    UserDomain NVARCHAR(255),              -- Extracted from email
    
    -- WHAT
    ResourceType NVARCHAR(100),            -- 'Expense', 'ApprovalRequest', 'User', etc
    ResourceId INT,
    FieldChanged NVARCHAR(255),            -- For updates
    OldValue NVARCHAR(MAX),                -- JSON
    NewValue NVARCHAR(MAX),                -- JSON
    
    -- WHEN
    EventTimestampUTC DATETIME2(3),        -- Millisecond precision
    
    -- WHERE
    IPAddress NVARCHAR(50),
    UserAgent NVARCHAR(MAX),               -- Browser/app version
    GeographicLocation NVARCHAR(255),      -- From IP lookup
    
    -- WHY
    RequestType NVARCHAR(100),             -- 'API', 'UI', 'System', 'Webhook'
    RequestEndpoint NVARCHAR(255),         -- /api/expenses/123/approve
    BusinessReason NVARCHAR(500),          -- Free text: why this action
    ApprovalAuthority NVARCHAR(255),       -- 'Role:Manager', 'Permission:123', 'System:Auto'
    
    -- System
    TransactionId UNIQUEIDENTIFIER,        -- Correlate related events
    IsCompliance BIT,                      -- Whether needed for compliance report
    CreatedAt DATETIME2,
    
    -- Indexes for performance
    INDEX IX_TenantId_EventTimestamp (TenantId, EventTimestampUTC DESC),
    INDEX IX_UserId_EventTimestamp (UserId, EventTimestampUTC DESC),
    INDEX IX_ResourceType_ResourceId (ResourceType, ResourceId),
    INDEX IX_EventCategory (EventCategory)
)

-- CRITICAL: Prevent deletion/modification of audit log
-- Only allow SELECT, no UPDATE/DELETE/TRUNCATE permissions
DENY UPDATE, DELETE, TRUNCATE ON ComplianceAuditLog TO [application_role];
GRANT SELECT ON ComplianceAuditLog TO [application_role];
```

**Logging Patterns:**
```csharp
// Every API endpoint should log
public async Task LogComplianceEventAsync(
    string eventType,
    string eventCategory,
    string resourceType,
    int? resourceId,
    object? oldValue,
    object? newValue,
    string? businessReason = null)
{
    var userEmail = ctx.User.FindFirst(ClaimTypes.Email)?.Value;
    var userDomain = userEmail?.Split('@')[1];
    var ipAddress = ctx.Connection.RemoteIpAddress?.ToString();
    
    await db.ExecuteNonQueryAsync(
        @"INSERT INTO ComplianceAuditLog 
          (TenantId, EventType, EventCategory, Severity, UserId, UserEmail, UserDomain,
           ResourceType, ResourceId, OldValue, NewValue, EventTimestampUTC, IPAddress,
           UserAgent, RequestEndpoint, BusinessReason, TransactionId, IsCompliance)
          VALUES (@TenantId, @EventType, @Category, 'Info', @UserId, @Email, @Domain,
                  @ResType, @ResId, @Old, @New, GETUTCDATE(), @IP,
                  @Agent, @Endpoint, @Reason, @TxId, 1)",
        new() { /* parameters */ });
}

// Usage:
await LogComplianceEventAsync(
    "DataModified",
    "Approval",
    "Expense",
    expenseId,
    oldExpenseAmount,
    newExpenseAmount,
    "Approved by manager due to authority assignment");
```

---

### 2.4 Product Admin Module (Digital Response Only)

**Purpose**: Manage MyDesk clients, track usage, handle billing

```sql
-- Tenant hierarchy (DR owns MyDesk, DR manages clients)
TenantHierarchy (
    HierarchyId INT PRIMARY KEY,
    ParentTenantId GUID,           -- Digital Response
    ChildTenantId GUID,            -- Client (e.g., Carter Capner Law)
    Relationship ENUM ('Owner', 'Partner', 'Reseller'),
    ContractStartDate DATE,
    ContractEndDate DATE,
    ContractStatus ENUM ('Draft', 'Active', 'Suspended', 'Ended'),
    ContractNotes NVARCHAR(MAX)
)

-- Billing configuration per client
ClientBillingConfig (
    BillingConfigId INT PRIMARY KEY,
    TenantId GUID,                 -- Client tenant
    BillingModel ENUM ('MonthlyAdvance', 'YearlyAdvance', 'PayAsYouGo'),
    
    -- Monthly Advance: Fixed fee per month
    MonthlyFeeAmount DECIMAL(18,2),
    MonthlyFeeUsers INT,           -- Included users
    PerAdditionalUserFee DECIMAL(18,2),
    
    -- Yearly Advance: Discount for annual payment
    YearlyFeeAmount DECIMAL(18,2),
    YearlyFeeUsers INT,
    YearlyDiscountPercent DECIMAL(5,2),
    
    -- Pay as you go: Per-transaction or per-feature
    TransactionFeePercent DECIMAL(5,2),
    TransactionFeeFixed DECIMAL(18,2),
    
    BillingCycle ENUM ('Monthly', 'Quarterly', 'Yearly'),
    DayOfMonthForBilling INT,      -- 1-31
    IsActive BIT,
    EffectiveFrom DATETIME2,
    EffectiveTo DATETIME2
)

-- Usage tracking for invoicing
ClientUsageLog (
    UsageId INT PRIMARY KEY,
    TenantId GUID,
    MetricType ENUM ('UserCount', 'ExpenseSubmitted', 'ApprovalProcessed', 'StorageGB'),
    MetricValue DECIMAL(18,2),
    UsageDateUTC DATETIME2,        -- Daily snapshot
    CollectedAt DATETIME2          -- When measured
)

-- Invoices generated from usage
ClientInvoice (
    InvoiceId INT PRIMARY KEY,
    TenantId GUID,
    InvoiceNumber NVARCHAR(50),    -- INV-2026-001-CCL
    InvoiceDateUTC DATETIME2,
    BillingPeriodStart DATE,
    BillingPeriodEnd DATE,
    InvoiceStatus ENUM ('Draft', 'Issued', 'Paid', 'Overdue', 'Cancelled'),
    
    -- Line items
    BaseSubscriptionFee DECIMAL(18,2),
    AdditionalUserFee DECIMAL(18,2),
    UsageFee DECIMAL(18,2),
    DiscountAmount DECIMAL(18,2),
    TaxAmount DECIMAL(18,2),
    TotalAmount DECIMAL(18,2),
    
    -- Payment tracking
    DueDate DATE,
    PaidDate DATETIME2,
    PaidAmount DECIMAL(18,2),
    PaymentMethod NVARCHAR(100),   -- 'BankTransfer', 'CreditCard', etc
    PaymentReference NVARCHAR(255),
    
    CreatedAt DATETIME2,
    CreatedBy INT,                 -- DR admin who generated
    IssuedAt DATETIME2,
    IssuedBy INT
)

-- Client self-service: manage own users and features
ClientAdminUsers (
    AdminAssignmentId INT PRIMARY KEY,
    TenantId GUID,
    UserId INT,
    AdminRole ENUM ('TenantAdmin', 'BillingAdmin', 'UserManager'),
    AssignedAt DATETIME2,
    AssignedBy INT,
    RevokedAt DATETIME2,
    RevokedBy INT
)
```

**Product Admin API Endpoints:**
```
-- Client Management
GET    /api/product-admin/clients                    - List all clients
POST   /api/product-admin/clients                    - Create new client
GET    /api/product-admin/clients/{id}               - Client details
PUT    /api/product-admin/clients/{id}               - Update client
GET    /api/product-admin/clients/{id}/health        - Client usage stats
GET    /api/product-admin/clients/{id}/users         - List client users
DELETE /api/product-admin/clients/{id}               - Deactivate client

-- Billing Configuration
GET    /api/product-admin/clients/{id}/billing       - Get billing config
PUT    /api/product-admin/clients/{id}/billing       - Update billing config
GET    /api/product-admin/clients/{id}/usage         - Usage metrics
GET    /api/product-admin/clients/{id}/invoices      - Invoice history
POST   /api/product-admin/invoices/generate          - Generate invoices (monthly)
GET    /api/product-admin/invoices/{id}              - Download invoice PDF

-- Admin Access Delegation
POST   /api/product-admin/clients/{id}/admins        - Grant admin to client user
DELETE /api/product-admin/clients/{id}/admins/{uid}  - Revoke admin

-- Reports
GET    /api/product-admin/reports/revenue            - Revenue by period
GET    /api/product-admin/reports/usage              - Usage metrics
GET    /api/product-admin/reports/churn              - Client retention
```

**Access Control:**
```csharp
// Product Admin module ONLY accessible to DR tenant
// Protected by:
// 1. tenant_id claim must be Digital Response's ID
// 2. User must have 'ProductAdmin' role
// 3. Audit log all product admin actions

[Authorize(Roles = "ProductAdmin")]
[RequiresTenant("digitalresponse.com.au")]  // Custom attribute
app.MapGet("/api/product-admin/clients", async (HttpContext ctx, DatabaseService db) =>
{
    // Only DR admins can see this
    ...
});
```

---

### 2.5 Client Onboarding Wizard

**New Workflow: Multi-Step Client Setup**

```
Step 1: Basic Information
├─ Client Name
├─ Primary Contact Email
├─ Domain(s) for user authentication
├─ Industry/Company Type

Step 2: Domain Verification
├─ Verify domain ownership (DNS record or email)
├─ Enable domain for user login
├─ Preview user login flow

Step 3: Billing Configuration
├─ Select billing model (Monthly Advance / Yearly / Pay-as-you-go)
├─ Set pricing
├─ Configure billing cycle
├─ Add billing contact email

Step 4: Features & Modules
├─ Select enabled modules (Expenses, Timesheets, etc)
├─ Configure approval workflows
├─ Set default approval assignments

Step 5: Initial Admin User
├─ Create first user in client tenant
├─ Grant admin permissions
├─ Send activation email

Step 6: Review & Activate
├─ Review all settings
├─ DR admin confirms
├─ Tenant activated
├─ Welcome email sent to client
```

**API Endpoints for Wizard:**
```
POST   /api/product-admin/onboard/start              - Initiate wizard
POST   /api/product-admin/onboard/basic-info         - Save step 1
POST   /api/product-admin/onboard/verify-domain      - Verify domain
POST   /api/product-admin/onboard/billing            - Set billing
POST   /api/product-admin/onboard/features           - Select features
POST   /api/product-admin/onboard/admin-user         - Create first admin
POST   /api/product-admin/onboard/activate           - Finalize & activate
GET    /api/product-admin/onboard/{wizardId}/status  - Check progress
```

**Database Table:**
```sql
ClientOnboardingWizard (
    WizardId UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId GUID,                 -- New client being created
    CreatedBy INT,                 -- DR admin initiating
    CurrentStep INT,               -- 1-6
    StepData NVARCHAR(MAX),        -- JSON of all steps
    IsCompleted BIT,
    CompletedAt DATETIME2,
    ExpiresAt DATETIME2,           -- 30 days to complete
    CreatedAt DATETIME2
)
```

---

## PART 3: SECURITY HARDENING ROADMAP

### Phase 1: CRITICAL (30 days)

- [ ] Implement domain-based tenant routing & validation
- [ ] Add approval permissions system
- [ ] Implement comprehensive audit logging
- [ ] Add rate limiting to all APIs
- [ ] Implement field-level encryption for PII
- [ ] Add mobile token storage to secure keychain/keystore
- [ ] Implement CORS & CSRF protection

### Phase 2: HIGH (60 days)

- [ ] Build Product Admin module
- [ ] Implement client onboarding wizard
- [ ] Add compliance event classification
- [ ] Implement geographic IP lookup
- [ ] Add request signing for mobile API calls
- [ ] Implement certificate pinning (mobile)
- [ ] Create audit log export for compliance teams

### Phase 3: MEDIUM (90 days)

- [ ] Add anomaly detection (unusual approval patterns)
- [ ] Implement time-based access restrictions (e.g., no approvals after hours)
- [ ] Add approval escalation workflows (auto-escalate if pending >5 days)
- [ ] Implement data residency controls (all data stored in AU)
- [ ] Add encryption key rotation procedures
- [ ] Create compliance dashboard (ISO27001, SOC2 readiness)

---

## PART 4: MOBILE APP SECURITY HARDENING

### 4.1 Token Storage

**CRITICAL FIX - Current:**
```javascript
// INSECURE: Stored in localStorage
localStorage.setItem('pat', token);
```

**Required - Secure Storage:**
```javascript
// iOS: Use Keychain
import * as SecureStore from 'expo-secure-store';
await SecureStore.setItemAsync('pat', token);

// Android: Use Keystore
// Handled by expo-secure-store automatically

// Web: Use sessionStorage (cleared on tab close)
// NOT localStorage (persists across sessions)
```

### 4.2 Encrypted Cache

```javascript
// Current: Plaintext cache
state.expenses = responseData;

// Required: Encrypted cache with watermarking
import * as FileSystem from 'expo-file-system';
import * as SecureStore from 'expo-secure-store';
import * as crypto from 'expo-crypto';

async function cacheExpensesSecurely(expenses) {
    // Generate encryption key unique to device
    let encryptionKey = await SecureStore.getItemAsync('cacheKey');
    if (!encryptionKey) {
        encryptionKey = crypto.getRandomBytes(32);
        await SecureStore.setItemAsync('cacheKey', encryptionKey);
    }
    
    // Encrypt data
    const encrypted = await encryptData(
        JSON.stringify(expenses),
        encryptionKey
    );
    
    // Add offline watermark
    const cacheObject = {
        data: encrypted,
        timestamp: new Date(),
        isOffline: true,
        warning: 'Data is cached from an earlier sync'
    };
    
    // Store to secure file
    await FileSystem.writeAsStringAsync(
        `${FileSystem.cacheDirectory}expenses.enc`,
        JSON.stringify(cacheObject)
    );
}
```

### 4.3 Certificate Pinning

```javascript
import axios from 'axios';
import { CertificatePinning } from 'react-native-certificate-pinning';

// Pin the server's certificate
const publicKeyHash = 'sha256/AbCdEf1234567890...'; // From server cert
const serverUrl = 'https://api.mydesk.com.au';

const client = axios.create({
    baseURL: serverUrl,
    timeout: 10000
});

// Verify certificate on every request
client.interceptors.request.use(async (config) => {
    await CertificatePinning.check({
        serverUrl: serverUrl,
        certificates: ['your_certificate.cer'],
        pins: [publicKeyHash]
    });
    return config;
}, error => Promise.reject(error));
```

### 4.4 API Request Signing

```javascript
// Sign all API requests with device-specific signature
async function signRequest(method, endpoint, body = null) {
    // Get device ID from secure storage
    let deviceId = await SecureStore.getItemAsync('deviceId');
    if (!deviceId) {
        deviceId = generateUUID();
        await SecureStore.setItemAsync('deviceId', deviceId);
    }
    
    // Create signature: HMAC-SHA256(method + endpoint + timestamp + deviceId)
    const timestamp = Date.now();
    const signatureBase = `${method}${endpoint}${timestamp}${deviceId}`;
    const signature = await crypto.digestStringAsync(
        crypto.CryptoDigestAlgorithm.SHA256,
        signatureBase
    );
    
    return {
        'X-Device-ID': deviceId,
        'X-Request-Signature': signature,
        'X-Request-Timestamp': timestamp
    };
}

// Usage:
const headers = await signRequest('GET', '/api/approval/pending');
const response = await fetch(
    'https://api.mydesk.com.au/api/approval/pending',
    { headers }
);
```

---

## PART 5: COMPLIANCE MAPPINGS

### ISO 27001 Mapping

| ISO 27001 Requirement | MyDesk Implementation |
|---|---|
| A.9.2.1 User Registration | Domain-based user creation via TenantDomains |
| A.9.2.5 Access Rights Review | ApprovalPermissions audit trail |
| A.12.4.1 Event Logging | ComplianceAuditLog (append-only) |
| A.12.4.3 Admin Activity | ProductAdminAudit (all admin actions) |
| A.13.1.1 Information Transfer | TLS 1.3, certificate pinning on mobile |
| A.14.2.1 Security Controls | Code review checklist, static analysis |

### SOC 2 Type II Mapping

| SOC 2 Control | Implementation |
|---|---|
| CC6.2 Reduce malware risk | Input validation, rate limiting, WAF |
| CC7.2 System monitoring | ComplianceAuditLog with alerting |
| CC7.5 Change management | Audit trail for configuration changes |
| CC9.2 Access to systems | Domain-based tenant isolation + MFA required |

### Sarbanes-Oxley Mapping

| SOX Requirement | Implementation |
|---|---|
| IT-4: Access Controls | Approval permissions + audit trail |
| IT-5: System Monitoring | Real-time audit logging |
| IT-6: Change Controls | Complete audit of approval permission changes |

---

## PART 6: IMPLEMENTATION PRIORITIES

### IMMEDIATE (Week 1)

1. **Domain-Based Tenant Resolution**
   - Add TenantDomains table
   - Modify authentication to validate email domain
   - Add domain verification endpoint

2. **Approval Permissions System**
   - Add ApprovalPermissions table
   - Update approval logic to check permissions
   - Add permissions API

3. **Basic Audit Logging**
   - Add ComplianceAuditLog table
   - Log all API modifications
   - Secure log with append-only access

### SHORT-TERM (Weeks 2-4)

4. **Product Admin Module MVP**
   - Client list management
   - Basic usage tracking
   - Invoice generation

5. **Mobile Security Hardening**
   - Token storage to secure keystore
   - Encrypted cache
   - API request signing

6. **Rate Limiting & DDoS Protection**
   - Add rate limiting middleware
   - Implement abuse detection

### MEDIUM-TERM (Weeks 5-8)

7. **Full Onboarding Wizard**
   - Multi-step client setup
   - Domain verification in wizard
   - Automated first admin creation

8. **Field-Level Encryption**
   - Identify sensitive fields
   - Implement encryption/decryption
   - Key rotation procedures

---

## PART 7: SECURITY TESTING CHECKLIST

Before production release, verify:

- [ ] Domain validation prevents tenant cross-contamination
- [ ] Approval permissions correctly restrict actions
- [ ] Audit log captures all sensitive operations
- [ ] Rate limiting blocks spam attacks
- [ ] Mobile token storage verified (no plaintext)
- [ ] CORS prevents cross-origin attacks
- [ ] CSRF tokens validated on state-changing operations
- [ ] SQL injection tests pass (parameterized queries)
- [ ] XSS tests pass (input sanitization)
- [ ] Authentication bypass attempts fail
- [ ] Unauthorized users cannot access other tenants
- [ ] Approval authority cannot be escalated through delegation
- [ ] Admin actions logged and reversible
- [ ] API error messages don't leak information
- [ ] Certificate pinning works on mobile
- [ ] Audit logs cannot be modified after creation

---

## PART 8: RECOMMENDED ARCHITECTURE PATTERNS

### Pattern 1: Tenant Isolation Middleware

```csharp
// Apply to all endpoints to verify tenant consistency
public class TenantIsolationMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value;
        var userId = int.TryParse(
            context.User.FindFirst("user_id")?.Value ?? "", out var uid) ? uid : 0;
        
        // Verify user actually belongs to claimed tenant
        var userTenant = await db.QueryAsync(
            "SELECT TenantId FROM Users WHERE UserId = @UserId",
            new() { ["UserId"] = userId });
        
        if (userTenant.Rows.Count == 0 || 
            userTenant.Rows[0]["TenantId"].ToString() != tenantId)
        {
            context.Response.StatusCode = 403;
            return;
        }
        
        // Store tenant context for use in request
        context.Items["TenantId"] = tenantId;
        context.Items["UserId"] = userId;
        
        await _next(context);
    }
}

// Register in Program.cs
app.UseMiddleware<TenantIsolationMiddleware>();
```

### Pattern 2: Compliance Event Logging

```csharp
public class ComplianceLogger
{
    public async Task LogEventAsync(
        HttpContext context,
        string eventType,
        string category,
        string resourceType,
        int? resourceId,
        object? oldValue = null,
        object? newValue = null)
    {
        var tenantId = context.Items["TenantId"];
        var userId = context.Items["UserId"];
        var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        
        await db.ExecuteNonQueryAsync(
            @"INSERT INTO ComplianceAuditLog
              (TenantId, EventType, EventCategory, UserId, UserEmail,
               ResourceType, ResourceId, OldValue, NewValue, EventTimestampUTC,
               IPAddress, UserAgent, RequestEndpoint, TransactionId, IsCompliance)
              VALUES (@T, @Et, @Ec, @U, @E, @Rt, @Ri, @O, @N, GETUTCDATE(),
                      @Ip, @Ua, @Re, @Tx, 1)",
            new() {
                ["T"] = tenantId,
                ["Et"] = eventType,
                ["Ec"] = category,
                ["U"] = userId,
                ["E"] = userEmail,
                ["Rt"] = resourceType,
                ["Ri"] = resourceId,
                ["O"] = JsonConvert.SerializeObject(oldValue),
                ["N"] = JsonConvert.SerializeObject(newValue),
                ["Ip"] = ipAddress,
                ["Ua"] = context.Request.Headers["User-Agent"],
                ["Re"] = context.Request.Path,
                ["Tx"] = context.TraceIdentifier
            });
    }
}
```

---

## CONCLUSION

MyDesk has strong foundational architecture but requires critical hardening for enterprise compliance. Priority must be:

1. **Tenant isolation validation** (prevent data leakage)
2. **Approval permissions** (prevent unauthorized approvals)
3. **Audit logging** (compliance & investigation)
4. **Mobile security** (protect field data)
5. **Product admin module** (client management & billing)

Timeline: **12 weeks to full compliance-ready state**

**Success Criteria:**
- ✅ Passes ISO 27001 gap assessment
- ✅ Ready for SOC 2 Type II audit
- ✅ Complies with Australian Privacy Act
- ✅ Data sovereignty: All data stored in Australia
- ✅ Audit trail complete for Sarbanes-Oxley
- ✅ Multi-tenant security verified by penetration test

---

**Next Steps:**
1. Review this architecture with security team
2. Prioritize Phase 1 items
3. Assign implementation team
4. Schedule security review after Phase 1 completion
5. Plan penetration testing for Weeks 8-9
