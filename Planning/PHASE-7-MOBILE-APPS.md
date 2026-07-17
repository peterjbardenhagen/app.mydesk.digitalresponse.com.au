# Phase 7: Mobile Applications Implementation Plan

**Approach:** React Native cross-platform (iOS + Android) with offline-first design  
**Duration:** ~18 weeks (Design: 3w, Build: 8w, Test: 3w, Beta: 2w, Launch: 2w)  
**Status:** 🔄 In Progress  
**Started:** July 17, 2026  
**Target Completion:** November 30, 2026

---

## Phase 7 Overview

Build native-quality mobile applications for iOS and Android allowing employees and managers to submit expenses, approve workflows, and receive notifications on-the-go. Leverage React Native for code sharing while maintaining platform-specific performance.

### Core MVP Features
- Expense submission with receipt camera capture
- Approval workflows with in-app actions
- Real-time push notifications (FCM)
- Personal dashboard & spending analytics
- Offline capability with local SQLite caching
- OAuth 2.0 authentication

### Success Metrics
- **User Adoption:** 60%+ of platform users (target 300+ daily active users)
- **Performance:** App startup < 3 seconds, screen load < 2 seconds
- **Reliability:** 99%+ crash-free rate
- **Engagement:** 80%+ user retention (30-day)
- **Approval Speed:** 50% of approvals via mobile within 2 hours

---

## Workstream 1: Architecture & Setup (Tasks 1-20)

### Project Structure & Configuration (Tasks 1-8)

1. **Create React Native project structure**
   - Initialize Expo + TypeScript project
   - Setup monorepo structure (shared: types, API client, utils)
   - Configure ESLint, Prettier, Jest
   - Setup git workflow and branch strategy

2. **Configure development environment**
   - iOS: Xcode, Cocoapods, Swift Package Manager
   - Android: Android Studio, Gradle
   - React Native CLI and EAS (Expo Application Services)
   - Local test device configuration

3. **Setup CI/CD pipeline**
   - GitHub Actions for test runs
   - EAS Build for iOS/Android builds
   - Testflight for iOS beta distribution
   - Google Play beta testing setup

4. **Design navigation architecture**
   - React Navigation v6 (nested stack + bottom tabs)
   - Deep linking strategy
   - Stack:
     - Auth Stack (Login, Signup, Password Reset)
     - App Stack (Dashboard, Expenses, Approvals, Settings)

5. **Setup API client layer**
   - Axios client with interceptors (auth, error handling)
   - Offline request queueing
   - Token refresh logic
   - Environment configuration (dev, staging, prod)

6. **Configure local storage**
   - SQLite database setup
   - Redux Persist integration
   - AsyncStorage for user preferences
   - Migration strategy for schema changes

7. **Setup authentication flow**
   - OAuth 2.0 integration with backend
   - Token storage (secure keychain)
   - Session management
   - Logout cleanup

8. **Create project documentation**
   - Setup guide for developers
   - Architecture decision records (ADRs)
   - Component library documentation
   - Testing strategy document

### State Management & Core Services (Tasks 9-20)

9. **Setup Redux store**
   - Actions: auth, expenses, approvals, notifications
   - Reducers with clear patterns
   - Selectors for derived state
   - Middleware for logging, error handling

10. **Implement authentication service**
    - Login/logout/signup flows
    - JWT token management
    - Secure credential storage
    - Session persistence

11. **Create API service layer**
    - Expenses endpoints (GET, POST, PATCH, DELETE)
    - Approvals endpoints (GET, POST with actions)
    - Notifications endpoints (GET, mark as read)
    - User profile endpoint

12. **Implement offline queue service**
    - Queue failed requests
    - Retry logic with exponential backoff
    - Track sync status
    - Conflict resolution strategy

13. **Setup error handling**
    - Global error boundary
    - Network error detection
    - User-friendly error messages
    - Error logging to backend

14. **Create notification service**
    - Firebase Cloud Messaging setup
    - Local notification handler
    - Badge count management
    - Deep link from notification

15. **Implement logging & analytics**
    - Crash reporting (Sentry)
    - Event tracking
    - Performance monitoring
    - User session tracking

16. **Create common components library**
    - Button, TextInput, Card, Modal
    - Loading spinner, error state, empty state
    - Form helpers
    - Theme configuration (light/dark)

17. **Setup testing infrastructure**
    - Jest configuration
    - React Testing Library setup
    - Mock factory for API responses
    - Test data generators

18. **Create shared TypeScript types**
    - User, Expense, Approval models
    - API request/response schemas
    - Navigation params
    - Redux state shape

19. **Implement theming system**
    - Light/dark mode support
    - Brand colors from design system
    - Typography scale
    - Spacing system

20. **Setup performance monitoring**
    - React Native Performance API
    - Bundle size tracking
    - Render profiling
    - Memory usage monitoring

---

## Workstream 2: Expense Features (Tasks 21-45)

### Expense Submission Flow (Tasks 21-30)

21. **Build Expenses list screen**
    - Display user's expenses (submitted, approved, paid)
    - Filter by status, date range, category
    - Pull-to-refresh
    - Pagination/infinite scroll

22. **Create expense detail screen**
    - Full expense information
    - Receipt image view (zoomable)
    - Edit capability (draft only)
    - Share/print functionality

23. **Build expense form screens**
    - Amount input with validation
    - Category selection (dropdown)
    - Description (multiline input)
    - Date picker
    - Form state management

24. **Implement receipt camera**
    - Native camera integration
    - Photo capture and preview
    - Crop/rotate/orientation
    - Fallback to photo library

25. **Add receipt OCR processing**
    - Send to backend OpenAI Vision API
    - Display extracted fields (supplier, date, amount, GST)
    - Allow user to edit extracted fields
    - Show confidence score

26. **Create attachment management**
    - Multi-file upload capability
    - Progress tracking
    - Retry failed uploads
    - Offline queuing of uploads

27. **Implement form validation**
    - Real-time validation feedback
    - Error messages per field
    - Amount/currency validation
    - Required field enforcement

28. **Build draft saving**
    - Auto-save to local storage every 30 seconds
    - Recover draft on app return
    - Clear draft after submission
    - Draft expiration (30 days)

29. **Create submission confirmation**
    - Show expense details before submit
    - Confirmation modal
    - Success notification
    - Option to add another

30. **Add offline capability for expenses**
    - Queue submissions while offline
    - Sync when connection restored
    - Show sync status
    - Conflict detection (edited by approver)

### Expense Analytics (Tasks 31-35)

31. **Build expense summary cards**
    - Month-to-date total
    - Approved amount
    - Pending approvals count
    - Next reimbursement date

32. **Create spending trend chart**
    - Last 6 months spending
    - Line chart (Recharts Native)
    - Touch interactions
    - Date range filter

33. **Implement category breakdown**
    - Pie/donut chart by category
    - Percentage labels
    - Touch to drill down
    - Tap to filter list

34. **Build budget progress indicator**
    - Department budget remaining
    - Percentage bar
    - Color coding (green/yellow/red)
    - Warning message at 85%+

35. **Create expense insights**
    - Average expense amount
    - Most common category
    - Spending trend (up/down)
    - Policy compliance status

### Expense Search & Filtering (Tasks 36-40)

36. **Implement search functionality**
    - Search by description, supplier, amount
    - Real-time search with debouncing
    - Search history
    - Clear button

37. **Build filter UI**
    - Filter by status (draft, submitted, approved, rejected, paid)
    - Filter by category
    - Filter by date range
    - Multiple filter selection

38. **Create sort options**
    - Sort by date (newest/oldest)
    - Sort by amount (high/low)
    - Sort by status
    - Remember user preference

39. **Implement expense sharing**
    - Share expense as PDF
    - Share receipt image
    - Print receipt
    - Copy expense details

40. **Add expense duplication detection**
    - Alert if similar expense (date ± 2 days, amount ± 5%) exists
    - Allow user to override
    - Log duplicate submission attempt

### Expense List & Management (Tasks 41-45)

41. **Create expenses master list**
    - SwipeOut actions (edit, delete, duplicate)
    - Bulk select capability
    - Empty state messaging
    - Skeleton loading state

42. **Implement swipe actions**
    - Swipe left: Delete (for drafts)
    - Swipe left: Copy/Duplicate (for submitted)
    - Swipe right: Quick actions menu
    - Haptic feedback

43. **Build status indicators**
    - Visual status badges (Draft, Pending, Approved, Rejected, Paid)
    - Status animations
    - Tooltip on hover
    - Status history

44. **Create quick actions menu**
    - Edit expense (draft)
    - View details
    - Share/Print
    - Delete option
    - Duplicate expense

45. **Implement expense grouping**
    - Group by date (Today, This Week, This Month, Older)
    - Group by status
    - Sticky headers
    - Collapse/expand groups

---

## Workstream 3: Approval Features (Tasks 46-60)

### Approval Inbox (Tasks 46-55)

46. **Build approvals list screen**
    - All expenses pending user's approval
    - Filter by status, priority, date
    - Sort by amount, date, submitter
    - Pull-to-refresh

47. **Create approval detail view**
    - Expense full details
    - Submitter information
    - Receipt preview
    - Audit trail (previous approvals)

48. **Implement inline approval actions**
    - Approve button
    - Reject button with comment field
    - Request more info
    - Hold for later
    - Bulk approval checkbox

49. **Add approval comments**
    - Text input for approval/rejection reason
    - Comment history view
    - Threaded comments (future)
    - @mention support (future)

50. **Create bulk approval UI**
    - Multi-select with select-all
    - Bulk action toolbar
    - Approve all selected
    - Confirmation modal
    - Progress indicator

51. **Build approval queue**
    - Oldest-first sorting
    - SLA tracking (due in 3 days warning)
    - Priority indicator (amount-based)
    - Overdue highlighting

52. **Implement approval priorities**
    - Quick approvals (< $500)
    - Standard (< $5000)
    - Escalation (> $5000)
    - Visual priority badges

53. **Create delegation workflow**
    - Delegate to colleague
    - Delegation period selector
    - List of available delegates
    - Notification to delegate

54. **Add out-of-office auto-delegation**
    - Set OOO dates
    - Select delegate
    - Auto-redirect approvals
    - Show OOO status

55. **Build approval history**
    - Approved expenses
    - Rejected expenses
    - Approval metrics (count, time)
    - Statistics view

### Approval Analytics (Tasks 56-60)

56. **Create approval metrics summary**
    - Pending count
    - Average approval time
    - Approval rate
    - Weekly/monthly stats

57. **Build approval trend chart**
    - Approvals per day/week
    - Average time to approve
    - Rejection rate
    - Throughput tracking

58. **Implement team approval view** (Manager)
    - Team's expenses needing approval
    - Team member breakdown
    - SLA status
    - Escalation alerts

59. **Create approval recommendations**
    - Similar expenses auto-approved
    - Policy-compliant expenses highlighted
    - Suggested actions
    - Confidence scores

60. **Build approval SLA tracking**
    - Time remaining indicator
    - Visual countdown
    - Alert notifications
    - SLA miss report

---

## Workstream 4: Notifications (Tasks 61-75)

### Push Notifications (Tasks 61-70)

61. **Setup Firebase Cloud Messaging**
    - FCM project configuration
    - Device token management
    - Token refresh handling
    - Provider registration

62. **Create notification handler**
    - Foreground notification handling
    - Background notification receipt
    - Notification badge count
    - Sound/vibration preferences

63. **Implement notification parsing**
    - Parse notification payload
    - Extract deep link target
    - Extract data fields
    - Handle multiple notification types

64. **Build notification inbox screen**
    - List of all notifications (7-day retention)
    - Unread badge counter
    - Filter by type (approval, expense, system)
    - Mark as read/unread

65. **Add notification actions**
    - Tap notification to navigate
    - Action buttons in notification (Approve/Reject)
    - Quick actions (snooze, dismiss)
    - Notification grouping

66. **Create notification preferences**
    - Toggle notification types on/off
    - Sound settings
    - Vibration settings
    - Quiet hours (DND)

67. **Implement notification badges**
    - App icon badge count
    - Notification center count
    - Persistent badge value
    - Clear badge on app open

68. **Add notification persistence**
    - Save to SQLite
    - 7-day retention
    - Archive old notifications
    - Full-text search

69. **Build notification testing**
    - Send test notification
    - Various notification types
    - Payload validation
    - Deep link verification

70. **Create notification analytics**
    - Track notification delivery
    - Track notification opens
    - Track action button clicks
    - Report to backend

### In-App Notifications (Tasks 71-75)

71. **Implement toast notifications**
    - Success messages (approval sent)
    - Error messages (network failed)
    - Info messages (offline mode)
    - Auto-dismiss or persistent

72. **Create in-app notification banner**
    - Top banner for critical alerts
    - Dismiss button
    - Action button
    - Persistent storage option

73. **Build notification center**
    - Slide-in drawer with recent notifications
    - Unread count badge
    - Mark all as read
    - Clear all button

74. **Add notification navigation**
    - Deep link from notification to related expense/approval
    - Smooth navigation animation
    - Maintain back stack
    - Return to previous screen

75. **Implement notification testing UI** (Debug menu)
    - Trigger test notifications
    - Various severity levels
    - Payload preview
    - FCM token display

---

## Workstream 5: Analytics & Dashboard (Tasks 76-90)

### Personal Dashboard (Tasks 76-85)

76. **Create dashboard layout**
    - Header with user profile
    - Quick stats cards
    - Charts section
    - Recent activity
    - Bottom tab navigation

77. **Build quick stats section**
    - Month-to-date total
    - Pending approvals count
    - Approved this month
    - Paid this month
    - Refreshable cards

78. **Implement spending trend chart**
    - Last 6 months line chart
    - Interactive touch (show value on tap)
    - Date range filter (1m, 3m, 6m, 1y)
    - Export to image

79. **Create category breakdown chart**
    - Pie chart by category
    - Tap to see category name + amount
    - Tap to filter expense list
    - Toggle between pie/donut/horizontal bar

80. **Build budget status widget**
    - Department budget remaining
    - Percentage bar
    - Remaining amount
    - Forecast
    - Tap for budget details

81. **Add recent expenses section**
    - Last 10 expenses
    - Status badges
    - Swipe to approve/delete
    - Tap for details
    - Infinite scroll

82. **Create monthly summary**
    - Month picker
    - Monthly total
    - By category breakdown
    - By department breakdown
    - Comparison to previous month

83. **Implement dashboard refresh**
    - Pull-to-refresh
    - Auto-refresh on interval
    - Loading states
    - Error states
    - Offline mode handling

84. **Add dashboard customization** (Future)
    - Rearrange widgets
    - Hide/show sections
    - Preferred charts
    - Save preferences

85. **Create dashboard offline view**
    - Show cached data
    - Indicate refresh is pending
    - Show sync status
    - Estimated refresh time

### Manager Dashboard (Tasks 86-90)

86. **Build team expense view**
    - Team's total expenses
    - Top 5 team members by spend
    - Team spending by category
    - Budget utilization

87. **Create team pending approvals widget**
    - Count of pending expenses
    - Oldest pending expense
    - Total pending amount
    - Tap to go to approvals

88. **Implement team performance metrics**
    - Average approval time
    - On-time approval rate
    - Team members with pending
    - Policy violations count

89. **Add team analytics charts**
    - Spending trend (team)
    - Category breakdown (team)
    - Department comparison (if multi-dept)
    - Cost per employee

90. **Build team member drilldown**
    - Tap team member to see their expenses
    - Their pending approvals
    - Their spending trends
    - Quick actions (approve, contact)

---

## Workstream 6: Offline & Sync (Tasks 91-100)

### Offline Architecture (Tasks 91-98)

91. **Implement offline detection**
    - Network state monitoring
    - Connection quality assessment
    - Offline banner notification
    - Graceful degradation

92. **Create SQLite schema**
    - Users table (local copy)
    - Expenses table (complete copy)
    - Approvals table (list cache)
    - Notifications table
    - Sync metadata table

93. **Build data sync service**
    - Initial data sync on login
    - Incremental sync on app resume
    - Conflict detection
    - Sync queue for offline changes

94. **Implement offline forms**
    - Submit while offline
    - Queue for sync
    - Show pending badge
    - Auto-retry with backoff

95. **Add offline UI indicators**
    - Offline banner at top
    - Sync status indicator
    - Pending items badge
    - Sync progress

96. **Create conflict resolution**
    - Detect server vs local conflicts
    - User chooses version
    - Audit trail of conflicts
    - Revert capability

97. **Implement cache management**
    - Cache invalidation strategy
    - Cache size limits
    - Cleanup old data
    - Storage monitoring

98. **Build offline testing**
    - Toggle offline mode (debug)
    - Simulate slow networks
    - Test sync after offline
    - Verify data integrity

### Sync & Background Operations (Tasks 99-105)

99. **Implement background sync**
    - Workmanager for scheduled sync (iOS/Android)
    - Sync on WiFi availability
    - Sync on app resume
    - Exponential backoff on failure

100. **Create sync queue persistence**
    - Queue offline changes to SQLite
    - Replay queue on connection
    - Dedup identical requests
    - Track retry count

101. **Build data refresh strategy**
    - Pull-to-refresh
    - Auto-refresh on interval (5 min)
    - Background refresh (15 min)
    - Priority refresh (user-initiated)

102. **Implement optimistic updates**
    - Update local UI immediately
    - Queue API call
    - Sync with server
    - Revert if server fails

103. **Add sync error handling**
    - Network errors
    - 4xx client errors (validation)
    - 5xx server errors
    - Token expiration during sync

104. **Create sync monitoring**
    - Sync success/failure rate
    - Average sync time
    - Queue depth monitoring
    - Error distribution

105. **Build data deduplication**
    - Detect duplicate submissions
    - Prevent double expenses
    - Alert user
    - Server-side validation

---

## Workstream 7: Testing & QA (Tasks 106-120)

### Unit & Integration Testing (Tasks 106-112)

106. **Write API client tests**
    - Request interceptors
    - Error handling
    - Token refresh
    - Offline queueing
    - Test coverage: 85%+

107. **Test Redux store**
    - Actions and reducers
    - Selectors
    - Side effects
    - State persistence
    - Test coverage: 90%+

108. **Test expense service**
    - Submission logic
    - Draft saving
    - Offline queueing
    - Sync and conflict resolution
    - Test coverage: 85%+

109. **Test approval service**
    - Approval actions
    - Delegation logic
    - Status transitions
    - Permission checks
    - Test coverage: 80%+

110. **Test notification service**
    - FCM token handling
    - Notification parsing
    - Badge count
    - Deep linking
    - Test coverage: 80%+

111. **Test UI components**
    - Expense form validation
    - List rendering
    - Filter/sort
    - Swipe actions
    - Snapshot tests
    - Test coverage: 75%+

112. **Test offline sync**
    - Queue persistence
    - Conflict resolution
    - Retry logic
    - Data integrity
    - Test coverage: 85%+

### End-to-End Testing (Tasks 113-116)

113. **Create E2E test scenarios**
    - User login flow
    - Create and submit expense
    - Receive notification
    - Approve expense
    - View analytics
    - 20+ test cases

114. **Test iOS specific features**
    - Camera integration
    - Photo library access
    - Keychain storage
    - Push notifications
    - Background refresh

115. **Test Android specific features**
    - Camera permissions
    - Shared preferences
    - FCM setup
    - Material design components
    - System back button

116. **Conduct cross-device testing**
    - iPhone 12, 14, 15
    - Android S12, S13, S14
    - Tablet orientation
    - Landscape mode
    - Various screen sizes

### Performance Testing (Tasks 117-120)

117. **Benchmark app startup**
    - Cold start time (target: < 3s)
    - Warm start time (target: < 1.5s)
    - Memory usage at startup
    - CPU usage during startup

118. **Test screen load performance**
    - Dashboard load (target: < 2s)
    - Expense list load (target: < 2s)
    - Approval detail (target: < 1.5s)
    - Chart rendering (target: < 1s)

119. **Profile memory usage**
    - Baseline memory
    - Memory leaks detection
    - Memory under load (1000 expenses)
    - Memory with many images

120. **Optimize bundle size**
    - Target: < 30MB (both platforms)
    - Monitor third-party deps
    - Tree shake unused code
    - Lazy load features

---

## Workstream 8: Beta & Launch (Tasks 121-140)

### Testflight & Play Store Beta (Tasks 121-128)

121. **Setup Testflight for iOS**
    - Create App ID
    - Configure certificates & profiles
    - Build signed IPA
    - Upload to Testflight
    - Invite beta testers

122. **Setup Google Play beta**
    - Create app listing
    - Configure signing key
    - Build signed APK/AAB
    - Create closed beta track
    - Invite beta testers

123. **Create release notes**
    - Feature list
    - Bug fixes
    - Known issues
    - Screenshots
    - Video walkthrough

124. **Conduct beta testing**
    - Internal team (1 week)
    - Close beta (20 users, 1 week)
    - Open beta (100+ users, 2 weeks)
    - Feedback collection

125. **Monitor beta metrics**
    - Crash reports
    - Usage patterns
    - Feature adoption
    - Performance data
    - User feedback

126. **Fix critical beta issues**
    - Crash fixes
    - Major bugs
    - Performance issues
    - UI/UX feedback
    - Retriage and re-test

127. **Prepare release candidates**
    - Final build from main
    - Full regression testing
    - Security scan
    - Performance validation
    - Ready for submission

128. **Create app store listings**
    - App screenshots (5-8 per platform)
    - App description
    - Keywords for ASO
    - Privacy policy link
    - Support contact info

### App Store & Play Store Review (Tasks 129-135)

129. **Submit to App Store**
    - Complete app review information
    - Privacy policy
    - Terms of service
    - App review notes
    - Release notes
    - Submit for review

130. **Submit to Google Play**
    - Same documentation
    - Additional: data safety form
    - Targeting information
    - Marketing materials
    - Submit for review

131. **Respond to review feedback**
    - Address any reviewer questions
    - Provide additional info if needed
    - Screenshots/videos for features
    - Re-submit if rejected

132. **Monitor store submissions**
    - App Store review time (typical: 24-48h)
    - Google Play review time (typical: 1-3h)
    - Status tracking
    - Communicate with team

133. **Prepare for approval**
    - Have support ready
    - Monitor for crashes
    - Have rollback plan
    - Communicate with stakeholders

134. **Create update strategy**
    - Semantic versioning
    - Release notes template
    - Update cadence (bi-weekly)
    - Breaking change policy

135. **Setup analytics dashboard**
    - Track installs
    - Track DAU/MAU
    - Track crash rates
    - Track ratings
    - Feature adoption

### Launch & Post-Launch (Tasks 136-140)

136. **Soft launch in one market**
    - Limited availability (Australia only)
    - Monitor metrics closely
    - Gather feedback
    - Fix any issues

137. **Monitor launch metrics**
    - Download/install rate
    - Crash-free rate (target: 99%+)
    - Session length
    - Feature usage
    - User retention

138. **Create support resources**
    - FAQ document
    - Video tutorials
    - Email support setup
    - In-app help/chat (future)
    - Community support (future)

139. **Launch post-release updates**
    - Bug fixes (1-2 weeks)
    - Performance improvements
    - New features (Phase 7.1)
    - Continuous monitoring

140. **Establish update cadence**
    - Weekly monitoring
    - Bi-weekly minor updates
    - Monthly major features
    - Quarterly platform updates

---

## Development Timeline

| Workstream | Tasks | Duration | Start | End | Owner |
|------------|-------|----------|-------|-----|-------|
| 1. Architecture | 1-20 | 2 weeks | Jul 17 | Jul 31 | Full team |
| 2. Expenses | 21-45 | 3 weeks | Aug 1 | Aug 21 | Frontend lead |
| 3. Approvals | 46-60 | 2 weeks | Aug 22 | Sep 4 | Frontend lead |
| 4. Notifications | 61-75 | 2 weeks | Sep 5 | Sep 18 | Push specialist |
| 5. Analytics | 76-90 | 2 weeks | Sep 19 | Oct 2 | Data viz dev |
| 6. Offline/Sync | 91-105 | 2 weeks | Oct 3 | Oct 16 | Backend liaison |
| 7. Testing | 106-120 | 2 weeks | Oct 17 | Oct 30 | QA lead |
| 8. Beta/Launch | 121-140 | 3 weeks | Oct 31 | Nov 20 | Product manager |
| **Total** | **140** | **18 weeks** | Jul 17 | Nov 30 | |

---

## Resource Requirements

### Team Composition
- **React Native Lead:** 1 (architecture, complex features, reviews)
- **Frontend Developers:** 2 (feature implementation, components)
- **Backend Liaison:** 0.5 (API integration, deployment)
- **QA Engineer:** 1 (testing, beta coordination)
- **Product Manager:** 0.5 (requirements, stakeholder management)
- **Designer:** 0.5 (UI/UX refinement, edge cases)
- **Total:** 5.5 FTE

### Infrastructure
- **Development:** MacBook Pro (M1+) for iOS development
- **Testing:** iPhone 12/14/15 + Android devices (S12/S13/S14)
- **CI/CD:** GitHub Actions + EAS Build
- **Monitoring:** Sentry for crash reporting
- **Analytics:** Firebase Analytics

---

## Technology Stack

### Frontend
- **Framework:** React Native with TypeScript
- **State Management:** Redux + Redux Toolkit
- **Navigation:** React Navigation v6
- **UI Components:** React Native Paper + custom components
- **Charts:** Recharts Native
- **Forms:** React Hook Form
- **Validation:** Zod

### Backend Integration
- **API Client:** Axios
- **Auth:** JWT + OAuth 2.0 (backend handled)
- **Offline:** SQLite + redux-persist
- **Notifications:** Firebase Cloud Messaging (FCM)

### DevOps
- **Build:** Expo EAS Build
- **CI/CD:** GitHub Actions
- **Code Quality:** ESLint + Prettier
- **Testing:** Jest + React Native Testing Library
- **Monitoring:** Sentry + Firebase Crashlytics

### Tools
- **IDE:** Visual Studio Code + Xcode + Android Studio
- **Version Control:** Git + GitHub
- **Project Management:** GitHub Projects + Linear

---

## Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|-----------|
| iOS App Review rejection | 1-2 week delay | Medium | Early App Review submission, follow guidelines strictly |
| Android release delay | 1-2 day delay | Low | Standard review is fast, but plan ahead |
| Performance issues on low-end devices | Bad user experience | Medium | Profile early, optimize before beta, test on varied devices |
| Data sync conflicts | Data integrity | Medium | Robust conflict resolution, clear error messages, testing |
| Push notification delivery | Poor engagement | Low | Test thoroughly, monitor delivery rates, set up fallbacks |
| Team availability | Timeline slip | Low | Cross-train on each feature, clear documentation |
| Third-party API changes (FCM, backend) | Unexpected rework | Low | Monitor API changelogs, version pinning, abstraction layer |

---

## Success Criteria

### Functional Criteria
- ✅ All 140 MVP tasks completed
- ✅ Feature parity with web (expense, approval, notifications, analytics)
- ✅ Offline-first functionality working end-to-end
- ✅ Push notifications tested and working
- ✅ Both iOS and Android apps available on stores

### Quality Criteria
- ✅ Crash-free rate: 99%+
- ✅ Unit test coverage: 80%+
- ✅ Performance: Startup < 3s, screen load < 2s
- ✅ All critical bugs fixed before launch
- ✅ Zero security vulnerabilities

### User Metrics
- ✅ 60%+ of platform users install app (300+ DAU)
- ✅ 4.5+ star rating on both app stores
- ✅ 80%+ 30-day retention rate
- ✅ 1000+ monthly active users
- ✅ NPS score > 70

### Business Criteria
- ✅ Launch on schedule (Nov 30, 2026)
- ✅ Budget not exceeded by >10%
- ✅ Support team prepared for launch
- ✅ Marketing materials ready
- ✅ User documentation complete

---

## Deliverables

1. ✅ React Native monorepo with TypeScript setup
2. ✅ Shared API client with offline queueing
3. ✅ Redux store with complete state management
4. ✅ Expense submission flow (form, camera, OCR)
5. ✅ Approval workflow (list, detail, actions, bulk)
6. ✅ Notification system (FCM, in-app, inbox)
7. ✅ Personal dashboard with analytics
8. ✅ Manager dashboard
9. ✅ Offline-first sync architecture
10. ✅ Comprehensive test suite (unit, integration, E2E)
11. ✅ Beta release on Testflight & Play Store
12. ✅ App Store & Google Play release
13. ✅ User documentation & video tutorials
14. ✅ Team training materials
15. ✅ Post-launch support playbook

---

## Status Tracking

| Task Range | Description | Status | Progress | Est. Completion | Notes |
|------------|-------------|--------|----------|-----------------|-------|
| 1-20 | Architecture & Setup | ✅ Completed | 100% | Jul 17, 2026 | React Native + TypeScript setup, Redux store, API client, navigation |
| 21-45 | Expense Features | 🔄 In Progress | 30% | Aug 28, 2026 | Screens and Redux slices created; need: camera, OCR, form validation |
| 46-60 | Approval Features | ⏳ Queued | 0% | Sep 11, 2026 | Placeholder screens ready |
| 61-75 | Notifications | ⏳ Queued | 0% | Sep 25, 2026 | Push notification setup needed |
| 76-90 | Analytics & Dashboard | 🔄 In Progress | 15% | Oct 9, 2026 | Dashboard screen created with basic stats |
| 91-105 | Offline & Sync | ⏳ Queued | 0% | Oct 23, 2026 | Sync slice ready; need SQLite integration |
| 106-120 | Testing & QA | ⏳ Queued | 0% | Nov 6, 2026 | Jest config done |
| 121-140 | Beta & Launch | ⏳ Queued | 0% | Nov 30, 2026 | Ready to start when features complete |
| **Total** | **Phase 7: Mobile Apps** | **🔄 In Progress** | **17%** | **Nov 30, 2026** | 23 of 140 tasks done |

---

## References

- [DEVELOPMENT-PHASES.md](./DEVELOPMENT-PHASES.md) — Phase 7 specification
- [PRODUCT-REQUIREMENTS.md](../PRODUCT-REQUIREMENTS.md) — Feature requirements
- [CLAUDE.md](../CLAUDE.md) — Development guide
- [agents.md](../docs/agents.md) — Team assignments
