/**
 * Redux Store Configuration
 * Centralized state management for MyDesk Mobile
 * Part of Phase 7: Mobile Applications
 */

import { configureStore } from '@reduxjs/toolkit';
import { persistStore, persistReducer } from 'redux-persist';
import AsyncStorage from '@react-native-async-storage/async-storage';

import authReducer from './slices/authSlice';
import expensesReducer from './slices/expensesSlice';
import approvalsReducer from './slices/approvalsSlice';
import notificationsReducer from './slices/notificationsSlice';
import syncReducer from './slices/syncSlice';
import uiReducer from './slices/uiSlice';

// Persist configuration
const persistConfig = {
  key: 'root',
  storage: AsyncStorage,
  whitelist: ['auth', 'ui'], // Only persist auth and UI state
  version: 1,
};

// Create persisted reducer for root
const persistedAuthReducer = persistReducer(
  {
    ...persistConfig,
    key: 'auth',
  },
  authReducer
);

const persistedUiReducer = persistReducer(
  {
    ...persistConfig,
    key: 'ui',
  },
  uiReducer
);

export const store = configureStore({
  reducer: {
    auth: persistedAuthReducer,
    expenses: expensesReducer,
    approvals: approvalsReducer,
    notifications: notificationsReducer,
    sync: syncReducer,
    ui: persistedUiReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        // redux-persist actions are not serializable
        ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE', 'persist/PURGE'],
      },
    }),
});

export const persistor = persistStore(store);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

export default store;
