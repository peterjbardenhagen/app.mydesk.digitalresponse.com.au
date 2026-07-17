/**
 * Redux Notifications Slice
 * Manages notification data and preferences
 */

import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { Notification, NotificationPreferences } from '@types';
import { apiClient } from '@services/ApiClient';

interface NotificationsState {
  items: Notification[];
  unreadCount: number;
  preferences?: NotificationPreferences;
  isLoading: boolean;
  error?: string;
}

const initialState: NotificationsState = {
  items: [],
  unreadCount: 0,
  isLoading: false,
};

export const fetchNotifications = createAsyncThunk(
  'notifications/fetchNotifications',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get<Notification[]>('/api/notifications');
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data || [];
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const fetchNotificationPreferences = createAsyncThunk(
  'notifications/fetchPreferences',
  async (_, { rejectWithValue }) => {
    try {
      const response = await apiClient.get<NotificationPreferences>(
        '/api/notifications/preferences'
      );
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const updateNotificationPreferences = createAsyncThunk(
  'notifications/updatePreferences',
  async (preferences: NotificationPreferences, { rejectWithValue }) => {
    try {
      const response = await apiClient.patch<NotificationPreferences>(
        '/api/notifications/preferences',
        preferences
      );
      if (!response.success) {
        return rejectWithValue(response.error?.message);
      }
      return response.data;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

export const markNotificationAsRead = createAsyncThunk(
  'notifications/markAsRead',
  async (notificationId: number, { rejectWithValue }) => {
    try {
      await apiClient.post(`/api/notifications/${notificationId}/read`);
      return notificationId;
    } catch (error: any) {
      return rejectWithValue(error.message);
    }
  }
);

const notificationsSlice = createSlice({
  name: 'notifications',
  initialState,
  reducers: {
    addNotification: (state, action: PayloadAction<Notification>) => {
      state.items.unshift(action.payload);
      if (!action.payload.isRead) {
        state.unreadCount += 1;
      }
    },
    clearError: (state) => {
      state.error = undefined;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchNotifications.pending, (state) => {
        state.isLoading = true;
        state.error = undefined;
      })
      .addCase(fetchNotifications.fulfilled, (state, action) => {
        state.isLoading = false;
        state.items = action.payload;
        state.unreadCount = action.payload.filter((n) => !n.isRead).length;
      })
      .addCase(fetchNotifications.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });

    builder
      .addCase(fetchNotificationPreferences.fulfilled, (state, action) => {
        state.preferences = action.payload;
      });

    builder
      .addCase(updateNotificationPreferences.fulfilled, (state, action) => {
        state.preferences = action.payload;
      });

    builder
      .addCase(markNotificationAsRead.fulfilled, (state, action) => {
        const notification = state.items.find((n) => n.notificationId === action.payload);
        if (notification && !notification.isRead) {
          notification.isRead = true;
          state.unreadCount = Math.max(0, state.unreadCount - 1);
        }
      });
  },
});

export const { addNotification, clearError } = notificationsSlice.actions;
export default notificationsSlice.reducer;
