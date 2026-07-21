/**
 * Redux Sync Slice
 * Manages offline sync state and queue
 */

import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { SyncStatus, OfflineQueue } from '@types';

interface SyncState extends SyncStatus {
  queue: OfflineQueue[];
}

const initialState: SyncState = {
  isSyncing: false,
  lastSyncAt: undefined,
  syncError: undefined,
  pendingChanges: 0,
  offlineMode: false,
  queue: [],
};

const syncSlice = createSlice({
  name: 'sync',
  initialState,
  reducers: {
    setSyncing: (state, action: PayloadAction<boolean>) => {
      state.isSyncing = action.payload;
    },
    setSyncError: (state, action: PayloadAction<string | undefined>) => {
      state.syncError = action.payload;
    },
    setOfflineMode: (state, action: PayloadAction<boolean>) => {
      state.offlineMode = action.payload;
    },
    setLastSyncAt: (state, action: PayloadAction<string>) => {
      state.lastSyncAt = action.payload;
      state.syncError = undefined;
    },
    addToQueue: (state, action: PayloadAction<OfflineQueue>) => {
      state.queue.push(action.payload);
      state.pendingChanges = state.queue.filter((q) => q.status === 'Pending').length;
    },
    updateQueueItem: (state, action: PayloadAction<{ id: string; status: string; error?: string }>) => {
      const item = state.queue.find((q) => q.id === action.payload.id);
      if (item) {
        item.status = action.payload.status as any;
        if (action.payload.error) {
          item.lastError = action.payload.error;
          item.retryCount += 1;
        }
      }
      state.pendingChanges = state.queue.filter((q) => q.status === 'Pending').length;
    },
    removeFromQueue: (state, action: PayloadAction<string>) => {
      state.queue = state.queue.filter((q) => q.id !== action.payload);
      state.pendingChanges = state.queue.filter((q) => q.status === 'Pending').length;
    },
    clearQueue: (state) => {
      state.queue = [];
      state.pendingChanges = 0;
    },
  },
});

export const {
  setSyncing,
  setSyncError,
  setOfflineMode,
  setLastSyncAt,
  addToQueue,
  updateQueueItem,
  removeFromQueue,
  clearQueue,
} = syncSlice.actions;

export default syncSlice.reducer;
