# MyDesk Mobile - React Native Application

Professional mobile application for MyDesk expense management platform. Built with React Native, TypeScript, and Redux for iOS and Android.

## Overview

MyDesk Mobile enables employees and managers to:
- Submit expenses with receipt capture and OCR
- Approve/reject expense requests in real-time
- Receive push notifications
- View personal and team analytics
- Work offline with automatic sync when connection restored

## Tech Stack

- **Framework:** React Native with Expo
- **Language:** TypeScript
- **State Management:** Redux + Redux Toolkit
- **Navigation:** React Navigation v6
- **UI Components:** React Native Paper
- **Charts:** Recharts Native
- **Offline Storage:** SQLite + Redux Persist
- **Push Notifications:** Firebase Cloud Messaging (FCM)
- **Authentication:** JWT + OAuth 2.0
- **HTTP Client:** Axios with interceptors

## Project Structure

```
mobile/
├── app/                    # App navigation and entry points
│   ├── (auth)/            # Auth stack screens
│   ├── (app)/             # App stack screens
│   └── index.ts           # Root navigation
├── src/
│   ├── components/        # Reusable UI components
│   ├── screens/           # Screen components
│   ├── services/          # Business logic services
│   │   ├── ApiClient.ts   # API communication
│   │   ├── SyncService.ts # Offline sync logic
│   │   └── ...
│   ├── store/             # Redux store
│   │   ├── slices/        # Redux slices (auth, expenses, etc.)
│   │   └── store.ts       # Store configuration
│   ├── hooks/             # Custom React hooks
│   ├── types/             # TypeScript type definitions
│   ├── utils/             # Utility functions
│   └── theme/             # Theming system
├── __tests__/             # Test files
├── app.json               # Expo configuration
├── package.json           # Dependencies
└── tsconfig.json          # TypeScript configuration
```

## Getting Started

### Prerequisites

- Node.js 18+
- npm 9+
- Expo CLI: `npm install -g eas-cli`
- iOS: Xcode 14+
- Android: Android Studio with SDK

### Installation

1. **Clone and install dependencies**
   ```bash
   cd mobile
   npm install
   ```

2. **Setup environment**
   ```bash
   cp .env.example .env
   # Edit .env with your configuration
   ```

3. **Start development server**
   ```bash
   npm start
   ```

4. **Run on device/simulator**
   ```bash
   # iOS Simulator
   npm run ios

   # Android Emulator
   npm run android

   # Web (preview only)
   npm run web
   ```

## Development Workflow

### Feature Development

1. **Create feature branch**
   ```bash
   git checkout -b feature/expense-submission
   ```

2. **Implement feature** following the directory structure

3. **Write tests**
   ```bash
   npm run test -- MyFeature.test.ts
   ```

4. **Check code quality**
   ```bash
   npm run type-check
   npm run lint:fix
   npm run format
   ```

5. **Create Pull Request**

### Testing

```bash
# Run all tests
npm test

# Watch mode
npm run test:watch

# Coverage report
npm run test:coverage
```

### Linting & Formatting

```bash
# Check code style
npm run lint

# Auto-fix issues
npm run lint:fix

# Format code
npm run format
```

## API Integration

### API Client Usage

```typescript
import { apiClient } from '@services/ApiClient';

// GET request
const response = await apiClient.get<Expense>('/api/expenses/123');
if (response.success) {
  console.log(response.data);
} else {
  console.error(response.error?.message);
}

// POST request
const createResponse = await apiClient.post<Expense>('/api/expenses', {
  amount: 150,
  category: 'Meals',
  description: 'Client lunch',
});

// Upload file
const uploadResponse = await apiClient.uploadFile<Expense>(
  '/api/expenses/123/receipt',
  { uri: '...', name: 'receipt.jpg', type: 'image/jpeg' },
);
```

### Authentication

- Tokens stored securely in Keychain (iOS) / Keystore (Android)
- Automatic token refresh on 401 response
- Logout on token expiration

## Offline Support

### How It Works

1. All data synced to SQLite on first load
2. User actions queued if offline
3. Queue replayed when connection restored
4. Conflicts handled with user approval
5. Full audit trail maintained

### Testing Offline

```typescript
// Debug menu - toggle offline mode
// In development, use app.json debug settings
```

## Push Notifications

### Setup

1. Create Firebase project
2. Generate FCM key
3. Configure in app.json
4. Run `npm run build:ios` / `npm run build:android`

### Handling Notifications

```typescript
// Automatically handled by NotificationService
// Deep links handled by React Navigation
// Badge count updated automatically
```

## Building for Production

### iOS

```bash
# Build for TestFlight
npm run build:ios

# Submit to App Store
npm run submit:ios
```

### Android

```bash
# Build for Play Store
npm run build:android

# Submit to Google Play
npm run submit:android
```

## Performance Optimization

- Lazy load screens
- Virtualize long lists
- Memoize expensive computations
- Optimize bundle size
- Monitor memory usage

## Troubleshooting

### Common Issues

**Build fails with module not found**
```bash
rm -rf node_modules package-lock.json
npm install
```

**Expo Go not connecting**
```bash
npm start -- --clear
# Scan QR code again
```

**Android build fails**
```bash
cd android
./gradlew clean
cd ..
npm run android
```

## Documentation

- [API Integration Guide](./docs/API.md)
- [State Management](./docs/REDUX.md)
- [Component Library](./docs/COMPONENTS.md)
- [Testing Strategy](./docs/TESTING.md)
- [Performance Guide](./docs/PERFORMANCE.md)

## Contributing

1. Follow TypeScript strict mode
2. Write tests for new features
3. Run linting before commit
4. Keep components under 300 lines
5. Document complex logic
6. Request review before merge

## Security

- No sensitive data in logs
- All API calls over HTTPS
- JWT tokens in secure storage
- Row-level security enforced server-side
- Input validation before submission
- XSS protection via React

## Monitoring

- Crash reporting via Sentry
- Adoption metrics via Firebase Analytics
- Performance monitoring
- Error rate tracking
- User session analytics

## Support

For issues or questions:
1. Check documentation
2. Review troubleshooting guide
3. Create GitHub issue with reproducible example
4. Contact team via Slack

## License

Proprietary - Digital Response
