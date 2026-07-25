# Technical Decisions & Architectural Insights

## Mobile Architecture Overview

### State Management
- **Redux Toolkit** with **RTK Query** chosen for centralized state management
- Provides devtools, immutability, and automatic cache invalidation
- Selectors optimize re-renders by preventing unnecessary component updates
- Async thunks handle API calls with loading/error states

### Offline-First Strategy
- **SQLite + Redux Persist** for local data storage
- Custom offline queue with exponential backoff retry mechanism
- Conflict resolution via timestamp-based "last write wins" with user confirmation
- Background sync using WorkManager (Android) and BackgroundTask (iOS)

### OCR Implementation
- **OpenAI Vision API** selected for highest accuracy on receipts
- Multi-step validation: confidence threshold, manual edit fallback
- Australian-specific enhancements: GST calculation, ABN parsing, ATO category mapping
- Fallback to gallery selection when camera unavailable

### Navigation Architecture
- **React Navigation v6** with nested stacks
- Auth stack (login/signup) vs App stack (main app) separation
- Deep linking from push notifications to specific expense/approval screens
- Header hidden for cleaner mobile UI, with platform-appropriate gestures

### UI/animations
- **React Native Paper** for Material Design components
- Custom theming to match brand colors
- Touch targets ≥44dp for accessibility
- Smooth transitions between screens

### State Sync & Conflict Resolution
- Client-side timestamp for all local changes
- Server-side timestamps for conflict detection
- User prompted to choose version when conflicts detected
- Audit trail maintained for all changes

### Performance Optimizations
- **FlatList** for long lists
- Image caching, resize mode='contain' for receipts
- Lazy loading of screens via React Navigation
- Memoization of expensive computations

## Backend Architecture

### API Design
- RESTful endpoints with JSON payloads
- Versioned API (/api/v1/)
- Standardized response format: { success, data, error, timestamp }
- Pagination for list endpoints
- Filtering, sorting, search parameters

### Database Design
- TenantId column on all tables for multi-tenancy
- Indexed foreign keys for performance
- Soft deletes for audit compliance
- Enum tables for statuses (ExpenseStatus, ApprovalStatus, etc.)
- JSON columns for flexible data (receiptOcrData, notification data)

### Security Measures
- JWT tokens with refresh token rotation
- HTTPS enforced for all API calls
- Rate limiting per IP/user
- Input validation and sanitization
- CORS restrictions
- SQL injection prevention via parameterized queries

### Real-time Communication
- **SignalR** hub for real-time notifications
- Automatic reconnection handling
- Groups for user-specific notifications
- Fallback to polling for clients without WebSocket support

### Offline Sync Backend
- Dedicated sync endpoints for batch operations
- Conflict detection based on client timestamps
- Transactional updates to prevent partial writes
- Detailed sync logs for troubleshooting

## Data Flow Examples

### Expense Submission Flow
1. User fills form and takes picture
2. Image sent to backend OCR endpoint
3. OCR returns extracted fields
4. User validates/edits fields
5. Expense saved to local SQLite with status='Draft'
6. On submit: status='Submitted' + queued for sync
7. Background sync sends to API
8. API validates, creates expense, returns ID
9. Local record updated with server ID and status

### Approval Flow
1. Expense submitted triggers notification via SignalR
2. Manager receives push notification (FCM)
3. Tap notification opens ApprovalDetailScreen
4. Manager approves/rejects with optional comment
5. Optimistic UI update + queued for sync
6. Backend validates permissions, updates status
7. Sync confirms success/failure
8. Subscriber notified of decision

### Offline Sync Process
1. Detect network change via NetInfo
2. If online: process queue in FIFO order
3. For each item: attempt API call
4. On success: remove from queue, update local record
5. On failure: increment retry, apply exponential backoff
6. Max retries reached: move to error state, notify user
7. Sync status updated in Redux store

## Quality Assurance

### Testing Strategy
- Unit tests: Jest for Redux slices, services, utils
- Integration tests: React Native Testing Library for screens
- E2E tests: Detox for critical user flows
- Performance testing: Firebase Performance Monitoring
- Crash reporting: Sentry

### CI/CD Pipeline
- GitHub Actions for automated testing
- EAS Build for iOS/Android distribution
- Staging vs production environment separation
- Automated version bumping and changelog generation
- Store deployment automation

## Scalability Considerations

### Horizontal Scaling
- Stateless API services behind load balancer
- Redis for session storage and rate limiting
- Database read replicas for reporting
- Message queues (RabbitMQ) for background jobs

### Database Optimization
- Partitioning by tenantId for multi-tenant isolation
- Archiving old data to cold storage
- Connection pooling
- Query optimization with execution plan analysis

### Mobile App Size
- Code splitting with dynamic imports
- Asset optimization (images, fonts)
- Tree shaking to remove unused code
- ProGuard/R8 for Android, bitcode for iOS

## Compliance & Security

### Australian Data Sovereignty
- Data stored in Australian Azure regions
- Regular backups to secondary Australian region
- Encryption at rest and in transit
- GDPR/APP compliance for data handling

### Audit Trail
- Immutable audit log table for all financial transactions
- User ID, timestamp, IP address, action details
- Regular export to WORM storage for compliance
- Regular integrity checks